using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenCvSharp;
using SlideExtractor.WPF.Helpers;
using SlideExtractor.WPF.Models;
using SlideExtractor.WPF.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace SlideExtractor.WPF.ViewModels;

public partial class MainViewModel : ObservableRecipient
{
	private readonly FileDialogHelper _fileDialogHelper;
	private readonly SettingsHelper _settingsHelper;
	private readonly IFrameExtractionService _frameService;
	private readonly ISlideDeduplicationService _dedupService;
	private readonly IOcrService _ocrService;
	private readonly IPresentationService _presentationService;
	private readonly ILoggingService _logger;
	private readonly VideoMetadataStore _metadata;
	private CancellationTokenSource? _cts;

	[ObservableProperty] private string _selectedVideoPath = string.Empty;
	[ObservableProperty] private string _statusText = "就绪";
	[ObservableProperty] private double _progressValue;
	[ObservableProperty] private double _progressMaximum = 100;
	[ObservableProperty] private bool _isBusy;
	[ObservableProperty] private string _stage = "等待开始";
	[ObservableProperty] private TimeSpan _estimatedTime = TimeSpan.Zero;

	public ObservableCollection<SlideModel> Slides { get; } = new();
	public ObservableCollection<string> LogEntries { get; } = new();
	public IAsyncRelayCommand BrowseCommand { get; }
	public IRelayCommand ClearVideoCommand { get; }
	public IAsyncRelayCommand StartExtractionCommand { get; }
	public IRelayCommand CancelCommand { get; }
	public IAsyncRelayCommand ExportImagesCommand { get; }

	public MainViewModel(
		FileDialogHelper fileDialogHelper,
		SettingsHelper settingsHelper,
		IFrameExtractionService frameService,
		ISlideDeduplicationService dedupService,
		IOcrService ocrService,
		IPresentationService presentationService,
		ILoggingService logger,
		VideoMetadataStore metadata)
	{
		_fileDialogHelper = fileDialogHelper;
		_settingsHelper = settingsHelper;
		_frameService = frameService;
		_dedupService = dedupService;
		_ocrService = ocrService;
		_presentationService = presentationService;
		_logger = logger;
		_metadata = metadata;

		BrowseCommand = new AsyncRelayCommand(BrowseAsync);
		ClearVideoCommand = new RelayCommand(ClearVideo);
		StartExtractionCommand = new AsyncRelayCommand(StartExtractionAsync, CanStartExtraction);
		CancelCommand = new RelayCommand(Cancel, () => IsBusy);
		ExportImagesCommand = new AsyncRelayCommand(ExportImagesAsync, CanExport);

		Slides.CollectionChanged += (_, _) => ExportImagesCommand.NotifyCanExecuteChanged();

		if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.LastVideoPath))
		{
			SelectedVideoPath = Properties.Settings.Default.LastVideoPath;
			UpdateVideoMetadata(SelectedVideoPath);
		}
	}

	partial void OnSelectedVideoPathChanged(string value)
	{
		_logger.Debug($"选中文件: {value}");
		AddLog($"已选择文件: {value}");
		UpdateVideoMetadata(value);
		StartExtractionCommand.NotifyCanExecuteChanged();
	}

	partial void OnIsBusyChanged(bool value)
	{
		StartExtractionCommand.NotifyCanExecuteChanged();
		CancelCommand.NotifyCanExecuteChanged();
		ExportImagesCommand.NotifyCanExecuteChanged();
	}

	private async Task BrowseAsync()
	{
		var path = await _fileDialogHelper.PickVideoAsync();
		if (!string.IsNullOrWhiteSpace(path))
		{
			SelectedVideoPath = path;
			_settingsHelper.RegisterRecentFile(path);
		}
	}

	private void ClearVideo() => SelectedVideoPath = string.Empty;

	private async Task StartExtractionAsync()
	{
		if (!CanStartExtraction()) return;

		_cts = new CancellationTokenSource();
		IsBusy = true;
		Slides.Clear();
		StatusText = "正在初始化...";
		AddLog("开始提取流程");

		var sw = System.Diagnostics.Stopwatch.StartNew();
		try
		{
			var fileInfo = new FileInfo(SelectedVideoPath);
			var source = new VideoSourceModel
			{
				FilePath = SelectedVideoPath,
				FileSizeBytes = fileInfo.Length,
				TotalFrames = _metadata.TotalFrames,
				Fps = _metadata.Fps
			};

			var extractionSettings = new ExtractionSettings
			{
				FrameInterval = Properties.Settings.Default.FrameInterval,
				HashThreshold = Properties.Settings.Default.HashThreshold,
				TesseractPath = Properties.Settings.Default.TesseractPath,
				OcrLanguages = Properties.Settings.Default.OcrLanguages ?? "eng",
				OutputDirectory = Properties.Settings.Default.OutputDirectory,
				ImageFormat = Properties.Settings.Default.ImageFormat,
				ImageQuality = Properties.Settings.Default.ImageQuality,
				AutoSave = Properties.Settings.Default.AutoSaveEnabled
			};

			var frameProgress = new Progress<FrameExtractionProgress>(update =>
			{
				Stage = update.Stage;
				ProgressMaximum = update.Total;
				ProgressValue = update.Extracted;
				StatusText = $"{update.Stage} {update.Extracted}/{update.Total}";
			});
			var frames = await _frameService.ExtractAsync(source, extractionSettings, frameProgress, _cts.Token);

			AddLog($"帧提取完成，共 {frames.Count} 个样本");

			var dedupProgress = new Progress<DeduplicationProgress>(update =>
			{
				Stage = "去重";
				ProgressMaximum = update.Total;
				ProgressValue = update.Processed;
				StatusText = $"去重 {update.Processed}/{update.Total}";
			});
			var uniqueSlides = await _dedupService.DeduplicateAsync(frames, extractionSettings.HashThreshold, dedupProgress, _cts.Token);

			var ocrProgress = new Progress<OcrProgress>(update =>
			{
				Stage = "OCR";
				ProgressMaximum = uniqueSlides.Count;
				ProgressValue = update.Processed;
				StatusText = $"OCR {update.Processed}/{uniqueSlides.Count}";
			});
			var ocrResult = await _ocrService.PerformOcrAsync(uniqueSlides, extractionSettings, ocrProgress, _cts.Token);
			if (!ocrResult.Success)
			{
				AddLog($"OCR 已跳过：{ocrResult.ErrorMessage}");
				StatusText = $"OCR 跳过：{ocrResult.ErrorMessage}";
			}

			foreach (var slide in uniqueSlides)
			{
				Slides.Add(slide);
			}

			StatusText = $"完成，找到 {Slides.Count} 张幻灯片";
			AddLog($"完成，找到 {Slides.Count} 张幻灯片");
			EstimatedTime = sw.Elapsed;
		}
		catch (OperationCanceledException)
		{
			StatusText = "操作已取消";
			AddLog("用户取消操作");
		}
		catch (Exception ex)
		{
			StatusText = $"错误：{ex.Message}";
			_logger.Error("处理失败", ex);
			AddLog($"错误：{ex.Message}");
			MessageBox.Show(ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
		}
		finally
		{
			sw.Stop();
			IsBusy = false;
			_cts = null;
		}
	}

	private bool CanStartExtraction() => !IsBusy && !string.IsNullOrWhiteSpace(SelectedVideoPath);

	private void Cancel()
	{
		if (_cts is { IsCancellationRequested: false })
		{
			_cts.Cancel();
			AddLog("已发送取消请求");
		}
	}

	private async Task ExportImagesAsync()
	{
		if (!CanExport()) return;
		var slidesToExport = GetSlidesToExport();
		var baseDir = ResolveOutputDirectory();
		var folder = Path.Combine(baseDir, $"Images_{DateTime.Now:yyyyMMddHHmmss}");
		Directory.CreateDirectory(folder);
		await _presentationService.ExportImagesAsync(slidesToExport, folder, Properties.Settings.Default.ImageFormat, CancellationToken.None);
		StatusText = $"已导出图片至：{folder}";
		AddLog($"已导出图片至：{folder}");
	}

	private string ResolveOutputDirectory()
	{
		var target = Properties.Settings.Default.OutputDirectory;
		if (string.IsNullOrWhiteSpace(target))
		{
			target = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SlideExtractor");
		}

		Directory.CreateDirectory(target);
		return target;
	}

	private bool CanExport() => Slides.Count > 0 && !IsBusy;

	private IReadOnlyList<SlideModel> GetSlidesToExport()
	{
		var selected = Slides.Where(s => s.IsSelected).ToList();
		return selected.Count > 0 ? selected : Slides.ToList();
	}

	private void UpdateVideoMetadata(string path)
	{
		if (!File.Exists(path))
		{
			_metadata.TotalFrames = 0;
			_metadata.DurationSeconds = 0;
			_metadata.Fps = 0;
			return;
		}

		try
		{
			using var capture = new VideoCapture(path);
			if (capture.IsOpened())
			{
				var total = (int)capture.FrameCount;
				var fps = capture.Fps;
				_metadata.TotalFrames = total;
				_metadata.Fps = fps;
				_metadata.DurationSeconds = fps > 0 ? total / fps : 0;
				AddLog($"视频总帧数 {total:N0}，时长约 {TimeSpan.FromSeconds(_metadata.DurationSeconds):hh\\:mm\\:ss}");
			}
			else
			{
				_metadata.TotalFrames = 0;
				_metadata.DurationSeconds = 0;
				_metadata.Fps = 0;
				AddLog("无法读取视频元数据。");
			}
		}
		catch (Exception ex)
		{
			_logger.Warn($"读取视频元数据失败：{ex.Message}");
			AddLog($"读取视频元数据失败：{ex.Message}");
		}
	}

	private void AddLog(string message)
	{
		var entry = $"{DateTime.Now:HH:mm:ss} {message}";
		if (Application.Current?.Dispatcher?.CheckAccess() == true)
		{
			AppendLog(entry);
		}
		else
		{
			Application.Current?.Dispatcher?.Invoke(() => AppendLog(entry));
		}
	}

	private void AppendLog(string entry)
	{
		LogEntries.Add(entry);
		const int maxEntries = 200;
		while (LogEntries.Count > maxEntries)
		{
			LogEntries.RemoveAt(0);
		}
	}
}
