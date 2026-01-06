using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using TextOCR.WPF.Models;
using TextOCR.WPF.Services;

namespace TextOCR.WPF.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IVideoProcessingService _videoService;
    private readonly IOcrService _ocrService;
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private string _videoPath = string.Empty;

    [ObservableProperty]
    private VideoInfo? _videoInfo;

    [ObservableProperty]
    private int _subtitleHeight = 80;

    [ObservableProperty]
    private bool _fromTop = false;

    [ObservableProperty]
    private double _frameInterval = 0.5;

    [ObservableProperty]
    private int _previewSecond = 30;

    [ObservableProperty]
    private string? _timeRange;

    [ObservableProperty]
    private BitmapImage? _framePreview;

    [ObservableProperty]
    private BitmapImage? _subtitleRegionPreview;

    [ObservableProperty]
    private string _recognizedText = string.Empty;

    [ObservableProperty]
    private string _windowsOcrResult = string.Empty;

    [ObservableProperty]
    private string _paddleOcrResult = string.Empty;

    [ObservableProperty]
    private string _tesseractOcrResult = string.Empty;

    [ObservableProperty]
    private bool _isProcessing = false;

    [ObservableProperty]
    private int _progress = 0;

    [ObservableProperty]
    private string _statusMessage = "就绪";

    [ObservableProperty]
    private string _logMessages = string.Empty;

    [ObservableProperty]
    private string _selectedOcrEngine = "Windows";  // "Windows", "PaddleOCR", "Tesseract"

    private readonly IOcrService _paddleOcrService;
    private readonly IOcrService _tesseractOcrService;
    private readonly IOcrService _windowsOcrService;

    public MainViewModel(
        IVideoProcessingService videoService,
        IOcrService ocrService,
        ISettingsService settingsService)
    {
        _videoService = videoService;
        _paddleOcrService = ocrService;
        _tesseractOcrService = new TesseractOcrService();
        _windowsOcrService = new WindowsOcrService();
        _ocrService = _windowsOcrService;  // 默认使用 Windows OCR
        _settingsService = settingsService;

        // 订阅 PaddleOCR 日志事件
        PythonOcrService.LogMessage += AddLog;

        LoadSettings();
        
        // 清理旧的临时文件
        CleanupTempFiles();
        
        // 启动 PaddleOCR 测试
        Task.Run(async () => await TestPaddleOCR());
        
        // 监听属性变化以自动保存设置
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SubtitleHeight) ||
                e.PropertyName == nameof(FromTop) ||
                e.PropertyName == nameof(FrameInterval) ||
                e.PropertyName == nameof(PreviewSecond) ||
                e.PropertyName == nameof(VideoPath))
            {
                SaveSettings();
            }
        };
    }

    private async Task TestPaddleOCR()
    {
        try
        {
            await Task.Delay(1000); // 等待UI加载完成
            
            AddLog("正在测试 PaddleOCR...");
            
            var testImagePath = @"C:\PaddleOCR_Models\temp\debug_subtitle_084125.png";
            
            if (!File.Exists(testImagePath))
            {
                AddLog($"PaddleOCR 测试跳过（测试图片不存在）");
                return;
            }

            var imageBytes = await File.ReadAllBytesAsync(testImagePath);
            var result = await _paddleOcrService.RecognizeText(imageBytes);
            
            if (!string.IsNullOrWhiteSpace(result))
            {
                AddLog($"✓ PaddleOCR 测试成功！识别结果: {result}");
            }
            else
            {
                AddLog("⚠ PaddleOCR 测试完成，但无识别结果");
            }
        }
        catch (Exception ex)
        {
            AddLog($"✗ PaddleOCR 测试失败: {ex.Message}");
        }
    }

    private void AddLog(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var logEntry = $"[{timestamp}] {message}\n";
        LogMessages += logEntry;
        
        // 自动滚动到底部（通过触发属性更改）
        OnPropertyChanged(nameof(LogMessages));
    }

    [RelayCommand]
    private void SelectVideo()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "视频文件|*.mp4;*.avi;*.mkv;*.mov|所有文件|*.*",
            Title = "选择视频文件"
        };

        if (dialog.ShowDialog() == true)
        {
            VideoPath = dialog.FileName;
            AddLog($"选择视频: {Path.GetFileName(VideoPath)}");
            LoadVideoInfo();
            StatusMessage = $"已加载视频: {Path.GetFileName(VideoPath)}";
        }
    }

    [RelayCommand]
    private async Task Preview()
    {
        if (string.IsNullOrWhiteSpace(VideoPath))
        {
            MessageBox.Show("请先选择视频文件", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            IsProcessing = true;
            
            // 根据选择切换 OCR 引擎
            IOcrService currentOcrService;
            string engineName;
            
            switch (SelectedOcrEngine)
            {
                case "Windows":
                    currentOcrService = _windowsOcrService;
                    engineName = "Windows OCR";
                    break;
                case "Tesseract":
                    currentOcrService = _tesseractOcrService;
                    engineName = "Tesseract";
                    break;
                case "PaddleOCR":
                default:
                    currentOcrService = _paddleOcrService;
                    engineName = "PaddleOCR";
                    break;
            }
            
            AddLog($"开始预览 - 时间点: {PreviewSecond}秒, 字幕高度: {SubtitleHeight}, 位置: {(FromTop ? "顶部" : "底部")}, OCR引擎: {engineName}");

            var result = await Task.Run(() =>
                _videoService.ExtractFrame(VideoPath, PreviewSecond, SubtitleHeight, FromTop));

            AddLog("视频帧提取成功");

            if (result.FrameImage != null)
            {
                FramePreview = BytesToBitmapImage(result.FrameImage);
                AddLog("帧预览图像已加载");
            }

            if (result.SubtitleRegionImage != null)
            {
                SubtitleRegionPreview = BytesToBitmapImage(result.SubtitleRegionImage);
                AddLog("字幕区域图像已加载");
                
                // 保存调试图片
                var debugImagePath = Path.Combine(@"C:\PaddleOCR_Models\temp", $"debug_subtitle_{DateTime.Now:HHmmss}.png");
                File.WriteAllBytes(debugImagePath, result.SubtitleRegionImage);
                AddLog($"调试图片已保存: {debugImagePath}");
            }

            // 识别文字 - 只使用 PaddleOCR
            StatusMessage = "正在使用 PaddleOCR 识别文字...";
            if (result.SubtitleRegionImage != null)
            {
                AddLog("开始调用 PaddleOCR...");
                
                try
                {
                    var text = await _paddleOcrService.RecognizeText(result.SubtitleRegionImage);
                    PaddleOcrResult = string.IsNullOrEmpty(text) ? "(无识别结果)" : text;
                    RecognizedText = PaddleOcrResult;
                    AddLog($"[PaddleOCR] {PaddleOcrResult}");
                }
                catch (Exception ex)
                {
                    PaddleOcrResult = $"(错误: {ex.Message})";
                    RecognizedText = PaddleOcrResult;
                    AddLog($"[PaddleOCR] 失败: {ex.Message}");
                }
                
                AddLog("PaddleOCR 识别完成");
                StatusMessage = "预览完成";
            }
        }
        catch (Exception ex)
        {
            var errorMsg = $"预览失败: {ex.Message}";
            AddLog($"错误: {errorMsg}");
            if (ex.InnerException != null)
            {
                AddLog($"内部错误: {ex.InnerException.Message}");
            }
            AddLog($"堆栈跟踪: {ex.StackTrace}");
            MessageBox.Show(errorMsg, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "预览失败";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private async Task StartExtraction()
    {
        if (string.IsNullOrWhiteSpace(VideoPath))
        {
            MessageBox.Show("请先选择视频文件", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var saveDialog = new SaveFileDialog
        {
            Filter = "文本文件|*.txt|所有文件|*.*",
            FileName = Path.GetFileNameWithoutExtension(VideoPath) + ".txt",
            Title = "保存字幕文件"
        };

        if (saveDialog.ShowDialog() != true)
            return;

        try
        {
            IsProcessing = true;
            AddLog("========== 开始提取字幕 ==========");
            AddLog($"视频路径: {VideoPath}");
            AddLog($"配置 - 字幕高度: {SubtitleHeight}, 位置: {(FromTop ? "顶部" : "底部")}, 帧间隔: {FrameInterval}秒");
            AddLog($"时间范围: {(string.IsNullOrEmpty(TimeRange) ? "全部" : TimeRange)}");

            SaveSettings();

            var config = new OcrConfig
            {
                VideoPath = VideoPath,
                SubtitleHeight = SubtitleHeight,
                FromTop = FromTop,
                FrameInterval = FrameInterval,
                TimeRange = TimeRange,
                OutputPath = saveDialog.FileName
            };

            var progressReporter = new Progress<int>(value =>
            {
                Progress = value;
                StatusMessage = $"正在提取字幕... {value}%";
            });

            AddLog("开始执行字幕提取...");
            // 使用 PaddleOCR 进行字幕提取
            var result = await _paddleOcrService.ExtractSubtitles(config, progressReporter);

            if (result.Success)
            {
                AddLog($"✓ 提取成功！用时: {result.ProcessingTime:mm\\:ss}");
                AddLog($"输出文件: {result.OutputFilePath}");
                StatusMessage = $"提取完成！用时: {result.ProcessingTime:mm\\:ss}";
                
                var messageResult = MessageBox.Show(
                    $"字幕提取成功！\n\n用时: {result.ProcessingTime:mm\\:ss}\n输出文件: {result.OutputFilePath}\n\n是否立即打开文件？",
                    "成功",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (messageResult == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = result.OutputFilePath,
                        UseShellExecute = true
                    });
                }
            }
            else
            {
                AddLog($"✗ 提取失败: {result.ErrorMessage}");
                MessageBox.Show($"提取失败:\n{result.ErrorMessage}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "提取失败";
            }
        }
        catch (Exception ex)
        {
            var errorMsg = $"提取失败: {ex.Message}";
            AddLog($"✗ 异常: {errorMsg}");
            if (ex.InnerException != null)
            {
                AddLog($"内部错误: {ex.InnerException.Message}");
            }
            MessageBox.Show(errorMsg, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "提取失败";
        }
        finally
        {
            IsProcessing = false;
            Progress = 0;
            
            // 提取完成后清理临时文件（不需要等待）
            _ = Task.Run(() => CleanupTempFiles());
        }
    }

    private void LoadVideoInfo()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(VideoPath) || !File.Exists(VideoPath))
                return;

            AddLog($"加载视频信息: {Path.GetFileName(VideoPath)}");
            VideoInfo = _videoService.GetVideoInfo(VideoPath);
            AddLog($"视频信息 - {VideoInfo.Width}×{VideoInfo.Height}, {VideoInfo.Fps:F2}fps, {VideoInfo.Duration:hh\\:mm\\:ss}");
        }
        catch (Exception ex)
        {
            VideoInfo = null;
            AddLog($"加载视频信息失败: {ex.Message}");
        }
    }

    private void LoadSettings()
    {
        try
        {
            SubtitleHeight = _settingsService.GetValue("SubtitleHeight", 80);
            FromTop = _settingsService.GetValue("FromTop", false);
            FrameInterval = _settingsService.GetValue("FrameInterval", 0.5);
            PreviewSecond = _settingsService.GetValue("PreviewSecond", 30);
            
            var lastVideo = _settingsService.GetValue("LastVideoPath", string.Empty);
            if (!string.IsNullOrEmpty(lastVideo) && File.Exists(lastVideo))
            {
                VideoPath = lastVideo;
                // 延迟加载视频信息，避免在构造函数中出错
                System.Windows.Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        LoadVideoInfo();
                    }
                    catch
                    {
                        // 忽略加载错误
                    }
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }
        catch
        {
            // 忽略设置加载错误
        }
    }

    private void SaveSettings()
    {
        _settingsService.SetValue("SubtitleHeight", SubtitleHeight);
        _settingsService.SetValue("FromTop", FromTop);
        _settingsService.SetValue("FrameInterval", FrameInterval);
        _settingsService.SetValue("PreviewSecond", PreviewSecond);
        _settingsService.SetValue("LastVideoPath", VideoPath);
        _settingsService.Save();
    }

    private BitmapImage BytesToBitmapImage(byte[] bytes)
    {
        var image = new BitmapImage();
        using var stream = new MemoryStream(bytes);
        
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        
        return image;
    }

    private void CleanupTempFiles()
    {
        try
        {
            var tempDir = @"C:\PaddleOCR_Models\temp";
            if (Directory.Exists(tempDir))
            {
                var files = Directory.GetFiles(tempDir, "*.png");
                var cleanedCount = 0;
                
                foreach (var file in files)
                {
                    try
                    {
                        // 只删除 ocr_temp_*.png 和 debug_subtitle_*.png 文件
                        var fileName = Path.GetFileName(file);
                        if (fileName.StartsWith("ocr_temp_") || fileName.StartsWith("debug_subtitle_"))
                        {
                            File.Delete(file);
                            cleanedCount++;
                        }
                    }
                    catch
                    {
                        // 忽略删除失败的文件
                    }
                }
                
                if (cleanedCount > 0)
                {
                    AddLog($"清理了 {cleanedCount} 个临时文件");
                }
            }
        }
        catch (Exception ex)
        {
            AddLog($"清理临时文件失败: {ex.Message}");
        }
    }
}
