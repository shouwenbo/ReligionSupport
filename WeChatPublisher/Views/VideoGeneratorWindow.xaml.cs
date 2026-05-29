using System.Windows;
using WeChatPublisher.Models;
using WeChatPublisher.Services;

namespace WeChatPublisher.Views;

public partial class VideoGeneratorWindow : Window
{
    private readonly BibleService? _bibleService;
    private string? _lastAudioPath;
    private string? _lastSrtPath;

    public VideoGeneratorWindow()
    {
        InitializeComponent();
        TxtOutputFolder.Text = AppSettings.Instance.DefaultOutputVideoRoot;

        var biblePath = AppSettings.Instance.BibleDbPath;
        if (File.Exists(biblePath))
            _bibleService = new BibleService(biblePath);
    }

    private void BtnVerseLookup_Click(object sender, RoutedEventArgs e)
    {
        if (_bibleService == null)
        {
            MessageBox.Show("圣经数据库未找到", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            var result = _bibleService.QueryVerses(TxtVerseRef.Text, 1);
            TxtVerseContent.Text = result;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"查询失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnBrowseVideoFolder_Click(object sender, RoutedEventArgs e)
        => BrowseFolder(TxtVideoFolder);

    private void BtnBrowseMusicFolder_Click(object sender, RoutedEventArgs e)
        => BrowseFolder(TxtMusicFolder);

    private void BtnBrowseCover_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "图片文件|*.jpg;*.jpeg;*.png|所有文件|*.*"
        };
        if (dialog.ShowDialog() == true)
            TxtCoverPath.Text = dialog.FileName;
    }

    private void BtnBrowseOutputFolder_Click(object sender, RoutedEventArgs e)
        => BrowseFolder(TxtOutputFolder);

    private static void BrowseFolder(System.Windows.Controls.TextBox target)
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog();
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            target.Text = dialog.SelectedPath;
    }

    private async void BtnGenerateTts_Click(object sender, RoutedEventArgs e)
    {
        var text = TxtVideoContent.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            MessageBox.Show("请输入正文内容", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var config = AppSettings.Instance.GetActiveTtsConfig();
        if (config == null)
        {
            MessageBox.Show("请先配置TTS API", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            TbTtsStatus.Text = "正在生成TTS...";
            var ttsService = new TtsService(config);
            var tempPath = Path.Combine(Path.GetTempPath(), "WeChatPublisher",
                $"tts_{DateTime.Now:yyyyMMddHHmmss}.mp3");

            _lastAudioPath = await ttsService.GenerateAudioAsync(text, tempPath);

            if (_lastAudioPath != null)
            {
                TbTtsStatus.Text = $"TTS已生成: {_lastAudioPath}";
                TbTtsStatus.Foreground = System.Windows.Media.Brushes.Green;
                Log("TTS音频生成成功");
            }
            else
            {
                TbTtsStatus.Text = "TTS生成失败";
                TbTtsStatus.Foreground = System.Windows.Media.Brushes.Red;
            }
        }
        catch (Exception ex)
        {
            TbTtsStatus.Text = $"错误: {ex.Message}";
            TbTtsStatus.Foreground = System.Windows.Media.Brushes.Red;
            Log($"TTS错误: {ex.Message}");
        }
    }

    private async void BtnGenerateSubtitle_Click(object sender, RoutedEventArgs e)
    {
        if (_lastAudioPath == null)
        {
            MessageBox.Show("请先生成TTS音频", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var config = AppSettings.Instance.GetActiveSubtitleConfig();
        if (config == null)
        {
            MessageBox.Show("请先配置字幕API", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            Log("正在生成字幕...");
            var subService = new SubtitleService(config);
            _lastSrtPath = await subService.GenerateSubtitleAsync(_lastAudioPath);
            Log($"字幕已生成: {_lastSrtPath}");
        }
        catch (Exception ex)
        {
            Log($"字幕错误: {ex.Message}");
            MessageBox.Show($"字幕生成失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnGenerateVideo_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtVideoTitle1.Text) || string.IsNullOrWhiteSpace(TxtVideoTitle2.Text))
        {
            MessageBox.Show("请输入两个标题词", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_lastAudioPath == null)
        {
            MessageBox.Show("请先生成TTS音频", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var videoFolder = TxtVideoFolder.Text;
        var musicFolder = TxtMusicFolder.Text;

        if (string.IsNullOrWhiteSpace(videoFolder) || !Directory.Exists(videoFolder))
        {
            MessageBox.Show("请选择有效的背景视频文件夹", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            BtnGenerateVideo.IsEnabled = false;
            TbVideoProgress.Text = "正在合成视频...";

            var ffmpeg = new FFmpegService();
            var compositor = new VideoCompositorService(ffmpeg, Log);

            var draft = new VideoDraft
            {
                TitleWord1 = TxtVideoTitle1.Text,
                TitleWord2 = TxtVideoTitle2.Text,
                Content = TxtVideoContent.Text,
                Verse = TxtVerseRef.Text,
                VerseContent = TxtVerseContent.Text,
                Summary = TxtSummary.Text,
                AudioPath = _lastAudioPath,
                SrtPath = _lastSrtPath,
                Status = "completed"
            };

            var extraSeconds = int.TryParse(TxtExtraSeconds.Text, out var es) ? es : 3;
            var outputFolder = string.IsNullOrWhiteSpace(TxtOutputFolder.Text)
                ? AppSettings.Instance.DefaultOutputVideoRoot
                : TxtOutputFolder.Text;

            var videoPath = await compositor.GenerateVideoAsync(
                draft, videoFolder, musicFolder,
                TxtCoverPath.Text.Length > 0 ? TxtCoverPath.Text : null,
                extraSeconds, outputFolder);

            TbVideoProgress.Text = "视频合成完成!";
            Log($"视频已生成: {videoPath}");

            MessageBox.Show($"视频已生成!\n{videoPath}", "成功",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Log($"合成错误: {ex.Message}");
            TbVideoProgress.Text = $"错误: {ex.Message}";
            MessageBox.Show($"视频合成失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            BtnGenerateVideo.IsEnabled = true;
        }
    }

    private void Log(string msg)
    {
        Dispatcher.Invoke(() =>
        {
            TbLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\n");
            TbLog.ScrollToEnd();
        });
    }
}
