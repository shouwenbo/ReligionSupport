using System.Windows;
using System.Windows.Controls;
using WeChatPublisher.Agent;
using WeChatPublisher.Models;
using WeChatPublisher.Services;

namespace WeChatPublisher.Views;

public partial class VideoGeneratorWindow : Window
{
    private CancellationTokenSource? _cts;
    private string? _lastGeneratedText;
    private string _generatedTitle1 = "", _generatedTitle2 = "";
    private List<SelectableItem> _selectedMaterials = [];
    private List<SelectableItem> _selectedVerses = [];

    public VideoGeneratorWindow() { InitializeComponent(); AppIcon.Set(this); Loaded += OnLoaded; }
    private void OnLoaded(object sender, RoutedEventArgs e) => ShowActiveAccount();
    private void ShowActiveAccount()
    {
        var cfg = AppSettings.Instance.GetActiveWeChatConfig();
        TbActiveAccount.Text = cfg != null ? $"当前公众号: {cfg.AccountName}" : "";
    }

    private void BtnSelectMaterials_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new MaterialSelectorWindow();
        if (dialog.ShowDialog() == true)
        {
            _selectedMaterials = dialog.SelectedMaterials;
            _selectedVerses = dialog.SelectedVerses;
            TbSelectedCount.Text = $"{_selectedMaterials.Count + _selectedVerses.Count} 条已选";
        }
    }

    private async void BtnStart_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            BtnSelectMaterials.Visibility = Visibility.Collapsed;
            PanelParams.Visibility = Visibility.Collapsed;
            PanelOutput.Visibility = Visibility.Visible;
            BtnStart.IsEnabled = false; BtnStop.Visibility = Visibility.Visible;
            TbOutput.Text = ""; TbProgress.Text = "准备中...\n";
            _cts = new CancellationTokenSource();
            var context = new AgentContext { TaskType = "Video", MaxRounds = 2, Temperature = 0.7 };
            var sources = new List<string>();
            sources.AddRange(_selectedMaterials.Select(m => m.Data));
            sources.AddRange(_selectedVerses.Select(v => v.Data));
            if (sources.Count > 0) context.SourceText = string.Join("\n\n---\n\n", sources);

            var loop = new AgentLoop(new AIService(new SensitiveWordService()), new SensitiveWordService(),
                new PromptBuilderService(), AppSettings.Instance, new McpService(), new AIImageService());
            loop.OnLog += (_, msg) => Dispatcher.BeginInvoke(() => { TbProgress.AppendText(msg + "\n"); TbProgress.ScrollToEnd(); });
            loop.OnTextChunk += (chunk) => Dispatcher.BeginInvoke(() => TbOutput.AppendText(chunk));
            var result = await loop.ExecuteAsync(context, _cts.Token);
            if (result.FinalText != null)
            { _lastGeneratedText = result.FinalText; TbOutput.Text = result.FinalText; ExtractTitles(result.FinalText); BtnStart.Visibility = Visibility.Collapsed; }
        }
        catch (OperationCanceledException) { RestorePanels(); }
        catch (Exception ex) { MessageBox.Show($"生成失败: {ex.Message}"); RestorePanels(); }
        finally { BtnStop.Visibility = Visibility.Collapsed; }
    }
    private void RestorePanels() { BtnSelectMaterials.Visibility = Visibility.Visible; PanelParams.Visibility = Visibility.Visible; BtnStart.Visibility = Visibility.Visible; }

    private void ExtractTitles(string text)
    {
        foreach (var line in text.Split('\n').Take(10))
        {
            var t = line.Trim().TrimStart('#', ' ', '【', '】');
            if (t.StartsWith("标题") && t.Contains("："))
            { var p = t.Split('：', 2); if (p.Length > 1 && p[1].Length >= 2 && p[1].Length <= 15) { if (_generatedTitle1.Length == 0) { _generatedTitle1 = p[1].Trim(); TxtVideoTitle1.Text = _generatedTitle1; } else { _generatedTitle2 = p[1].Trim(); TxtVideoTitle2.Text = _generatedTitle2; } } }
        }
    }
    private void BtnStop_Click(object s, RoutedEventArgs e) => _cts?.Cancel();
    private async void BtnLearn_Click(object sender, RoutedEventArgs e)
    {
        var edited = TbOutput.Text; if (string.IsNullOrWhiteSpace(_lastGeneratedText) || _lastGeneratedText == edited) return;
        try { await new StyleLearningService().AnalyzeEditsAsync(_lastGeneratedText, edited, "Video", CancellationToken.None); _lastGeneratedText = edited; MessageBox.Show("已学习"); } catch (Exception ex) { MessageBox.Show(ex.Message); }
    }
    private void BtnSaveDraft_Click(object s, RoutedEventArgs e)
    {
        var text = TbOutput.Text; if (string.IsNullOrWhiteSpace(text)) return;
        AppSettings.Instance.Db.ExecuteInScope(db => db.Insertable(new VideoDraft
        { TitleWord1 = _generatedTitle1, TitleWord2 = _generatedTitle2, Content = text, Status = "draft", CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }).ExecuteCommand());
        MessageBox.Show("已保存");
    }
    private async void BtnPublish_Click(object sender, RoutedEventArgs e)
    {
        var text = TbOutput.Text; if (string.IsNullOrWhiteSpace(text)) return;
        try
        {
            var t1 = _generatedTitle1.Length > 0 ? _generatedTitle1 : TxtVideoTitle1.Text;
            var t2 = _generatedTitle2.Length > 0 ? _generatedTitle2 : TxtVideoTitle2.Text;
            var draft = new ArticleDraft { Title = $"{t1} {t2}", Content = $"<h2>{t1} {t2}</h2><p>{text.Replace("\n", "<br/>")}</p>", Status = "published", CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") };
            var mediaId = await new WeChatService().CreateDraftAsync(draft); draft.MediaId = mediaId;
            AppSettings.Instance.Db.ExecuteInScope(db => db.Insertable(draft).ExecuteCommand());
            MessageBox.Show($"已发布!\nMediaId: {mediaId}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { MessageBox.Show($"发布失败: {ex.Message}"); }
    }
}
