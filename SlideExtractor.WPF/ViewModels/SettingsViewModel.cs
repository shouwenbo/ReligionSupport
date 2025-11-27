using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SlideExtractor.WPF.Models;
using SlideExtractor.WPF.Properties;
using System;
using System.IO;

namespace SlideExtractor.WPF.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
	private const double DefaultFps = 30d;
	private readonly VideoMetadataStore _metadata;

	public SettingsViewModel(VideoMetadataStore metadata)
	{
		_metadata = metadata;
		_metadata.PropertyChanged += (_, __) =>
		{
			OnPropertyChanged(nameof(TotalFramesLabel));
			OnPropertyChanged(nameof(DurationLabel));
			OnPropertyChanged(nameof(EstimatedSamplesLabel));
		};
	}

	[ObservableProperty]
	private int _frameInterval = Settings.Default.FrameInterval;
	[ObservableProperty]
	private int _hashThreshold = Settings.Default.HashThreshold;
	[ObservableProperty]
	private string _tesseractPath = Settings.Default.TesseractPath;
	[ObservableProperty]
	private string _ocrLanguages = Settings.Default.OcrLanguages ?? "eng";
	[ObservableProperty]
	private string _outputDirectory = Settings.Default.OutputDirectory;
	[ObservableProperty]
	private string _theme = Settings.Default.Theme;
	[ObservableProperty]
	private int _thumbnailSize = Settings.Default.ThumbnailSize;
	[ObservableProperty]
	private int _thumbnailColumns = Settings.Default.ThumbnailColumns;
	[ObservableProperty]
	private string _imageFormat = Settings.Default.ImageFormat;
	[ObservableProperty]
	private int _imageQuality = Settings.Default.ImageQuality;

	[RelayCommand]
	private void Save()
	{
		Settings.Default.FrameInterval = FrameInterval;
		Settings.Default.HashThreshold = HashThreshold;
		Settings.Default.TesseractPath = TesseractPath;
		Settings.Default.OcrLanguages = OcrLanguages;
		Settings.Default.OutputDirectory = string.IsNullOrWhiteSpace(OutputDirectory)
			? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SlideExtractor")
			: OutputDirectory;
		Settings.Default.Theme = Theme;
		Settings.Default.ThumbnailSize = ThumbnailSize;
		Settings.Default.ThumbnailColumns = ThumbnailColumns;
		Settings.Default.ImageFormat = ImageFormat;
		Settings.Default.ImageQuality = ImageQuality;
		Settings.Default.Save();
		MessageBox.Show("设置已保存。", "设置", MessageBoxButton.OK, MessageBoxImage.Information);
	}

	[RelayCommand]
	private void Reset()
	{
		FrameInterval = 30;
		HashThreshold = 5;
		ImageFormat = "JPG";
		ImageQuality = 90;
	}

	[RelayCommand]
	private void BrowseTesseract()
	{
		var dialog = new Microsoft.Win32.OpenFileDialog
		{
			Filter = "Tesseract 可执行文件|tesseract.exe",
			Title = "选择 Tesseract 可执行文件"
		};

		if (dialog.ShowDialog() == true)
		{
			TesseractPath = dialog.FileName;
		}
	}

	partial void OnOutputDirectoryChanged(string value)
	{
		Settings.Default.OutputDirectory = value;
		Settings.Default.Save();
	}

	partial void OnFrameIntervalChanged(int value)
	{
		OnPropertyChanged(nameof(FrameIntervalSeconds));
		OnPropertyChanged(nameof(EstimatedSamplesLabel));
		Settings.Default.FrameInterval = value;
		Settings.Default.Save();
	}

	public VideoMetadataStore Metadata => _metadata;
	public double FrameIntervalSeconds
	{
		get => Math.Round(FrameInterval / DefaultFps, 2);
		set
		{
			var frames = (int)Math.Max(1, Math.Round(value * DefaultFps));
			if (FrameInterval != frames)
			{
				FrameInterval = frames;
			}
		}
	}

	public string TotalFramesLabel => _metadata.TotalFrames > 0 ? $"{_metadata.TotalFrames:N0} 帧" : "未加载";
	public string DurationLabel => _metadata.TotalFrames > 0 ? $"{_metadata.Duration:hh\\:mm\\:ss}" : "未知";
	public string EstimatedSamplesLabel
		=> _metadata.TotalFrames > 0
			? $"{Math.Max(1, _metadata.TotalFrames / Math.Max(1, FrameInterval)):N0} 张样本"
			: "未加载";

	public SettingsViewModel()
	{
		PropertyChanged += (_, args) =>
		{
			if (args.PropertyName == nameof(FrameInterval))
			{
				OnPropertyChanged(nameof(FrameIntervalSeconds));
			}
		};
	}
}
