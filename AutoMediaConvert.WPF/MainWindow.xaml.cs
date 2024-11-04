using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace AutoMediaConvert.WPF
{
    public partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; }

        public MainWindow()
        {
            InitializeComponent();
            ViewModel = new MainViewModel();
            DataContext = ViewModel;
        }

        // Drag & drop support (simple)
        private void ListView_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }

        private async void ListView_PreviewDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                await ViewModel.AddFilesAsync(files.Where(f => f.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)).ToArray());
            }
        }
    }

    // RelayCommand: small ICommand helper
    public class RelayCommand : ICommand
    {
        private readonly Func<object?, bool>? _canExecute;
        private readonly Action<object?> _execute;
        public event EventHandler? CanExecuteChanged;

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        public void Execute(object? parameter) => _execute(parameter);

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    public class MediaFileInfo : INotifyPropertyChanged
    {
        public string FilePath { get; set; }
        public string FileName => Path.GetFileName(FilePath);

        private double _progress;
        public double Progress { get => _progress; set { _progress = value; OnPropertyChanged(nameof(Progress)); } }

        private string _status = "待处理";
        public string Status { get => _status; set { _status = value; OnPropertyChanged(nameof(Status)); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        #region Properties
        public ObservableCollection<MediaFileInfo> Files { get; } = new ObservableCollection<MediaFileInfo>();

        private string _ffmpegPath = Properties.Settings.Default.FfmpegPath ?? string.Empty;
        public string FfmpegPath
        {
            get => _ffmpegPath;
            set
            {
                _ffmpegPath = value;
                OnPropertyChanged(nameof(FfmpegPath));
                Properties.Settings.Default.FfmpegPath = value;
                Properties.Settings.Default.Save();
            }
        }

        private int _maxConcurrency = 2;
        public int MaxConcurrency { get => _maxConcurrency; set { _maxConcurrency = value; OnPropertyChanged(nameof(MaxConcurrency)); } }

        private double _overallProgress;
        public double OverallProgress { get => _overallProgress; set { _overallProgress = value; OnPropertyChanged(nameof(OverallProgress)); } }

        private string _summary = "就绪";
        public string Summary { get => _summary; set { _summary = value; OnPropertyChanged(nameof(Summary)); } }

        private string _logText = string.Empty;
        public string LogText { get => _logText; set { _logText = value; OnPropertyChanged(nameof(LogText)); } }
        #endregion

        #region Commands
        public ICommand BrowseFfmpegCommand { get; }
        public ICommand AddFilesCommand { get; }
        public ICommand AddFolderCommand { get; }
        public ICommand ClearFilesCommand { get; }
        public ICommand RemoveCompletedCommand { get; }
        public ICommand StartCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand ExportLogCommand { get; }
        public ICommand RemoveFileCommand { get; }
        public ICommand OpenContainingFolderCommand { get; }
        #endregion

        private CancellationTokenSource? _cts;

        public MainViewModel()
        {
            BrowseFfmpegCommand = new RelayCommand(_ => BrowseFfmpeg());
            AddFilesCommand = new RelayCommand(async _ => await AddFilesAsync());
            AddFolderCommand = new RelayCommand(async _ => await AddFolderAsync());
            ClearFilesCommand = new RelayCommand(_ => Files.Clear());
            RemoveCompletedCommand = new RelayCommand(_ => RemoveCompleted());
            StartCommand = new RelayCommand(async _ => await StartAsync(), _ => Files.Any() && !IsRunning);
            StopCommand = new RelayCommand(_ => Stop(), _ => IsRunning);
            ExportLogCommand = new RelayCommand(_ => ExportLog());
            RemoveFileCommand = new RelayCommand(p => { if (p is MediaFileInfo m) Files.Remove(m); });
            OpenContainingFolderCommand = new RelayCommand(p => { if (p is MediaFileInfo m) OpenContainingFolder(m); });
        }

        #region UI Actions
        private void BrowseFfmpeg()
        {
            var dlg = new OpenFileDialog { Filter = "ffmpeg.exe|ffmpeg.exe|All files|*.*" };
            if (dlg.ShowDialog() == true)
            {
                FfmpegPath = dlg.FileName;
                Log($"已选择 ffmpeg：{FfmpegPath}");
            }
        }

        public async Task AddFilesAsync(string[]? paths = null)
        {
            string[] files = paths ?? Array.Empty<string>();
            if (files.Length == 0)
            {
                var dlg = new OpenFileDialog { Multiselect = true, Filter = "MP4 Files|*.mp4|All files|*.*" };
                if (dlg.ShowDialog() == true)
                    files = dlg.FileNames;
            }

            foreach (var f in files.Where(f => !Files.Any(x => x.FilePath == f)))
            {
                Files.Add(new MediaFileInfo { FilePath = f });
            }

            UpdateCommands();
            await Task.CompletedTask;
        }

        public async Task AddFolderAsync()
        {
            var dialog = new OpenFileDialog
            {
                Title = "选择任意一个 MP4 文件所在的文件夹",
                Filter = "视频文件 (*.mp4)|*.mp4",
                CheckFileExists = true,
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                string dir = Path.GetDirectoryName(dialog.FileName)!;
                var files = Directory.GetFiles(dir, "*.mp4", SearchOption.TopDirectoryOnly);
                await AddFilesAsync(files);
                MessageBox.Show($"已添加 {files.Length} 个 MP4 文件。", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void RemoveCompleted()
        {
            var toRemove = Files.Where(f => f.Status == "完成" || f.Status.StartsWith("失败")).ToList();
            foreach (var f in toRemove) Files.Remove(f);
        }

        private void OpenContainingFolder(MediaFileInfo m)
        {
            try
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{m.FilePath}\"") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Log($"打开目录失败：{ex.Message}");
            }
        }
        #endregion

        #region Conversion
        private bool IsRunning => _cts != null && !_cts.IsCancellationRequested;

        private void Stop()
        {
            _cts?.Cancel();
            Log("已请求停止。");
        }

        private async Task StartAsync()
        {
            if (string.IsNullOrWhiteSpace(FfmpegPath) || !File.Exists(FfmpegPath))
            {
                MessageBox.Show("请先选择有效的 ffmpeg.exe 路径。", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            Summary = "开始转换...";

            int total = Files.Count;
            int finished = 0;
            var tasks = new List<Task>();
            using var sem = new SemaphoreSlim(MaxConcurrency);

            foreach (var file in Files)
            {
                await sem.WaitAsync(token);
                if (token.IsCancellationRequested) break;

                file.Progress = 0;
                file.Status = "排队中";

                var t = Task.Run(async () =>
                {
                    try
                    {
                        file.Status = "转换中";
                        var outFile = Path.Combine(Path.GetDirectoryName(file.FilePath)!, Path.GetFileNameWithoutExtension(file.FilePath) + ".mp3");
                        var svc = new FfmpegService(FfmpegPath);
                        await svc.ConvertWithProgressAsync(file.FilePath, outFile, p =>
                        {
                            file.Progress = p * 100.0;
                            UpdateOverallProgress();
                        }, token);

                        file.Status = "完成";
                        finished++;
                        Log($"完成：{file.FileName}");
                    }
                    catch (OperationCanceledException)
                    {
                        file.Status = "已取消";
                        Log($"已取消：{file.FileName}");
                    }
                    catch (Exception ex)
                    {
                        file.Status = "失败: " + ex.Message;
                        Log($"转换失败 {file.FileName}：{ex.Message}");
                    }
                    finally
                    {
                        sem.Release();
                    }
                }, token);

                tasks.Add(t);
            }

            try
            {
                await Task.WhenAll(tasks);
                Summary = $"已完成 {finished}/{total}。";
            }
            catch (OperationCanceledException)
            {
                Summary = $"已取消 {finished}/{total}。";
            }
            finally
            {
                _cts = null;
                UpdateCommands();
            }
        }

        private void UpdateOverallProgress()
        {
            OverallProgress = Files.Count == 0 ? 0 : Files.Average(f => f.Progress);
        }
        #endregion

        #region Logging
        private void Log(string text)
        {
            LogText += $"[{DateTime.Now:HH:mm:ss}] {text}\n";
        }

        private void ExportLog()
        {
            var dlg = new SaveFileDialog { FileName = "AutoMediaConvert_Log.txt", Filter = "Text files|*.txt|All files|*.*" };
            if (dlg.ShowDialog() == true)
            {
                File.WriteAllText(dlg.FileName, LogText);
                MessageBox.Show("日志已保存。", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        #endregion

        private void UpdateCommands()
        {
            (StartCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (StopCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}