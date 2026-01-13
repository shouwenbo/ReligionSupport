using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using PhotoBGCheck.WPF.Commands;
using PhotoBGCheck.WPF.Models;
using PhotoBGCheck.WPF.Services;
using MessageBox = System.Windows.MessageBox;
using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;
using SaveFileDialog = System.Windows.Forms.SaveFileDialog;
using DialogResult = System.Windows.Forms.DialogResult;

namespace PhotoBGCheck.WPF.ViewModels
{
    /// <summary>
    /// 主窗口 ViewModel
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private readonly PhotoBackgroundCheckService _checkService;
        private string _selectedFolderPath = string.Empty;
        private int _whiteThreshold = 210;
        private double _whitePixelPercentage = 0.75;
        private int _progressPercentage;
        private bool _isChecking;
        private int _totalPhotoCount;
        private int _qualifiedPhotoCount;
        private int _unqualifiedPhotoCount;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isFastMode = true;
        private bool _isAIMode = false;
        private string _aiStatusMessage = string.Empty;
        private bool _showAIStatus = false;

        public MainViewModel()
        {
            _checkService = new PhotoBackgroundCheckService();
            UnqualifiedPhotos = new ObservableCollection<PhotoCheckResult>();

            // 初始化命令
            SelectFolderCommand = new RelayCommand(_ => SelectFolder());
            StartCheckCommand = new RelayCommand(_ => StartCheck(), _ => CanStartCheck);
            ExportUnqualifiedListCommand = new RelayCommand(_ => ExportUnqualifiedList(), _ => HasUnqualifiedPhotos);
            OpenInExplorerCommand = new RelayCommand(_ => OpenInExplorer(), _ => HasSelectedFolder);

            // 从设置中加载配置
            LoadSettings();
            
            // 检查AI模式可用性
            CheckAIAvailability();
        }

        #region 属性

        public string SelectedFolderPath
        {
            get => _selectedFolderPath;
            set
            {
                if (SetProperty(ref _selectedFolderPath, value))
                {
                    OnPropertyChanged(nameof(CanStartCheck));
                    OnPropertyChanged(nameof(HasSelectedFolder));
                }
            }
        }

        public int WhiteThreshold
        {
            get => _whiteThreshold;
            set => SetProperty(ref _whiteThreshold, value);
        }

        public double WhitePixelPercentage
        {
            get => _whitePixelPercentage;
            set => SetProperty(ref _whitePixelPercentage, value);
        }

        public int ProgressPercentage
        {
            get => _progressPercentage;
            set => SetProperty(ref _progressPercentage, value);
        }

        public bool IsChecking
        {
            get => _isChecking;
            set
            {
                if (SetProperty(ref _isChecking, value))
                {
                    OnPropertyChanged(nameof(CanStartCheck));
                }
            }
        }

        public int TotalPhotoCount
        {
            get => _totalPhotoCount;
            set => SetProperty(ref _totalPhotoCount, value);
        }

        public int QualifiedPhotoCount
        {
            get => _qualifiedPhotoCount;
            set
            {
                if (SetProperty(ref _qualifiedPhotoCount, value))
                {
                    OnPropertyChanged(nameof(QualificationRate));
                }
            }
        }

        public int UnqualifiedPhotoCount
        {
            get => _unqualifiedPhotoCount;
            set
            {
                if (SetProperty(ref _unqualifiedPhotoCount, value))
                {
                    OnPropertyChanged(nameof(HasUnqualifiedPhotos));
                    OnPropertyChanged(nameof(HasNoUnqualifiedPhotos));
                    OnPropertyChanged(nameof(QualificationRate));
                }
            }
        }

        public double QualificationRate
        {
            get
            {
                if (TotalPhotoCount == 0) return 0;
                return (double)QualifiedPhotoCount / TotalPhotoCount;
            }
        }

        public ObservableCollection<PhotoCheckResult> UnqualifiedPhotos { get; }

        public bool CanStartCheck => !string.IsNullOrWhiteSpace(SelectedFolderPath) && !IsChecking;
        public bool HasUnqualifiedPhotos => UnqualifiedPhotoCount > 0;
        public bool HasNoUnqualifiedPhotos => !IsChecking && UnqualifiedPhotoCount == 0 && TotalPhotoCount > 0;
        public bool HasSelectedFolder => !string.IsNullOrWhiteSpace(SelectedFolderPath);

        public bool IsFastMode
        {
            get => _isFastMode;
            set
            {
                if (SetProperty(ref _isFastMode, value))
                {
                    if (value) _isAIMode = false;
                    OnPropertyChanged(nameof(IsAIMode));
                }
            }
        }

        public bool IsAIMode
        {
            get => _isAIMode;
            set
            {
                if (SetProperty(ref _isAIMode, value))
                {
                    if (value) _isFastMode = false;
                    OnPropertyChanged(nameof(IsFastMode));
                }
            }
        }

        public string AIStatusMessage
        {
            get => _aiStatusMessage;
            set => SetProperty(ref _aiStatusMessage, value);
        }

        public bool ShowAIStatus
        {
            get => _showAIStatus;
            set => SetProperty(ref _showAIStatus, value);
        }

        #endregion

        #region 命令

        public RelayCommand SelectFolderCommand { get; }
        public RelayCommand StartCheckCommand { get; }
        public RelayCommand ExportUnqualifiedListCommand { get; }
        public RelayCommand OpenInExplorerCommand { get; }

        #endregion

        #region 方法

        private void CheckAIAvailability()
        {
            if (!_checkService.IsAIModeAvailable())
            {
                AIStatusMessage = "（AI模式不可用：缺少模型文件）";
                ShowAIStatus = true;
                
                // 如果AI模式不可用，强制使用快速模式
                if (IsAIMode)
                {
                    IsFastMode = true;
                }
            }
            else
            {
                ShowAIStatus = false;
            }
        }

        private void SelectFolder()
        {
            var dialog = new FolderBrowserDialog
            {
                Description = "请选择包含证件照的文件夹",
                ShowNewFolderButton = false,
                SelectedPath = SelectedFolderPath
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                SelectedFolderPath = dialog.SelectedPath;
                SaveSettings();
            }
        }

        private async void StartCheck()
        {
            if (string.IsNullOrWhiteSpace(SelectedFolderPath) || !Directory.Exists(SelectedFolderPath))
            {
                MessageBox.Show("请先选择有效的文件夹", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                IsChecking = true;
                ProgressPercentage = 0;
                UnqualifiedPhotos.Clear();
                TotalPhotoCount = 0;
                QualifiedPhotoCount = 0;
                UnqualifiedPhotoCount = 0;

                // 获取所有支持的图片文件
                var supportedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var imageFiles = Directory.GetFiles(SelectedFolderPath)
                    .Where(f => supportedExtensions.Contains(Path.GetExtension(f).ToLower()))
                    .ToList();

                if (imageFiles.Count == 0)
                {
                    MessageBox.Show("文件夹中没有找到 JPG 或 PNG 图片文件", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                TotalPhotoCount = imageFiles.Count;

                // 获取当前检测模式
                var detectionMode = IsAIMode ? DetectionMode.AI : DetectionMode.Fast;

                // 创建检测服务实例
                var checkService = new PhotoBackgroundCheckService(WhiteThreshold, WhitePixelPercentage);

                // 创建进度报告器
                var progress = new Progress<int>(percent =>
                {
                    ProgressPercentage = percent;
                });

                // 执行检测
                _cancellationTokenSource = new CancellationTokenSource();
                var results = await checkService.CheckPhotosAsync(imageFiles, detectionMode, progress, _cancellationTokenSource.Token);

                // 统计结果
                QualifiedPhotoCount = results.Count(r => r.IsQualified);
                UnqualifiedPhotoCount = results.Count(r => !r.IsQualified);

                // 显示不合格的照片
                var unqualifiedResults = results.Where(r => !r.IsQualified).OrderBy(r => r.FileName);
                foreach (var result in unqualifiedResults)
                {
                    UnqualifiedPhotos.Add(result);
                }

                // 保存设置
                SaveSettings();

                MessageBox.Show(
                    $"检测完成！\n\n总计：{TotalPhotoCount} 张\n合格：{QualifiedPhotoCount} 张\n不合格：{UnqualifiedPhotoCount} 张\n合格率：{QualificationRate:P1}",
                    "检测完成",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"检测过程中发生错误：\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsChecking = false;
                ProgressPercentage = 0;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private void ExportUnqualifiedList()
        {
            if (UnqualifiedPhotos.Count == 0)
            {
                MessageBox.Show("没有不合格的照片可以导出", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var saveDialog = new SaveFileDialog
            {
                Filter = "文本文件 (*.txt)|*.txt|CSV文件 (*.csv)|*.csv",
                FileName = $"不合格照片列表_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var lines = new List<string>();
                    
                    if (saveDialog.FilterIndex == 1) // TXT
                    {
                        lines.Add("证件照背景检测 - 不合格照片列表");
                        lines.Add($"检测时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                        lines.Add($"检测文件夹：{SelectedFolderPath}");
                        lines.Add($"总计：{TotalPhotoCount} 张，合格：{QualifiedPhotoCount} 张，不合格：{UnqualifiedPhotoCount} 张");
                        lines.Add(new string('-', 80));
                        lines.Add("");

                        foreach (var photo in UnqualifiedPhotos)
                        {
                            lines.Add($"文件名：{photo.FileName}");
                            lines.Add($"  路径：{photo.FilePath}");
                            lines.Add($"  白色占比：{photo.WhitePixelPercentage:P2}");
                            lines.Add($"  背景亮度：{photo.AverageBackgroundBrightness}");
                            if (!string.IsNullOrEmpty(photo.ErrorMessage))
                            {
                                lines.Add($"  错误信息：{photo.ErrorMessage}");
                            }
                            lines.Add("");
                        }
                    }
                    else // CSV
                    {
                        lines.Add("文件名,完整路径,白色占比,背景亮度,错误信息");
                        foreach (var photo in UnqualifiedPhotos)
                        {
                            lines.Add($"\"{photo.FileName}\",\"{photo.FilePath}\",{photo.WhitePixelPercentage:P2},{photo.AverageBackgroundBrightness},\"{photo.ErrorMessage}\"");
                        }
                    }

                    File.WriteAllLines(saveDialog.FileName, lines, System.Text.Encoding.UTF8);
                    MessageBox.Show($"列表已成功导出到：\n{saveDialog.FileName}", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"导出失败：\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OpenInExplorer()
        {
            if (!string.IsNullOrWhiteSpace(SelectedFolderPath) && Directory.Exists(SelectedFolderPath))
            {
                Process.Start("explorer.exe", SelectedFolderPath);
            }
        }

        private void LoadSettings()
        {
            var settings = Properties.Settings.Default;
            
            if (!string.IsNullOrWhiteSpace(settings.LastSelectedFolder) && Directory.Exists(settings.LastSelectedFolder))
            {
                SelectedFolderPath = settings.LastSelectedFolder;
            }

            WhiteThreshold = settings.WhiteThreshold;
            WhitePixelPercentage = settings.WhitePixelPercentage;
            
            // 加载检测模式
            var savedMode = (DetectionMode)settings.DetectionMode;
            if (savedMode == DetectionMode.AI && _checkService.IsAIModeAvailable())
            {
                IsAIMode = true;
            }
            else
            {
                IsFastMode = true;
            }
        }

        public void SaveSettings()
        {
            var settings = Properties.Settings.Default;
            settings.LastSelectedFolder = SelectedFolderPath;
            settings.WhiteThreshold = WhiteThreshold;
            settings.WhitePixelPercentage = WhitePixelPercentage;
            settings.DetectionMode = (int)(IsAIMode ? DetectionMode.AI : DetectionMode.Fast);
            settings.Save();
        }

        #endregion
    }
}
