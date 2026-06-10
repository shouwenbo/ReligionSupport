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
    private readonly McpService _mcp = new();
    private string _generatedTitle1 = "", _generatedTitle2 = "";

    public VideoGeneratorWindow()
    {
        InitializeComponent();
        AppIcon.Set(this);
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        ShowActiveAccount();
        var biblePath = AppSettings.Instance.BibleDbPath;
        Dispatcher.BeginInvoke(new Action(async () =>
        {
            await Task.Delay(300);
            var mats = await MaterialLoader.LoadMaterialsAsync(_mcp);
            LbMaterials.ItemsSource = mats;
            TbMaterialCount.Text = $"{mats.Count}篇";
            if (File.Exists(biblePath))
            {
                var verses = await MaterialLoader.LoadVersesAsync(biblePath);
                LbVerses.ItemsSource = verses;
                TbVerseCount.Text = $"{verses.Count}条";
            }
        }), System.Windows.Threading.DispatcherPriority.Background);
    }

    private void ShowActiveAccount()
    {
        var cfg = AppSettings.Instance.GetActiveWeChatConfig();
        TbActiveAccount.Text = cfg != null
            ? $"当前公众号: {cfg.AccountName}"
            : "未配置公众号";
    }

    private void CheckBox_Click(object s, RoutedEventArgs e) { }
    private void BtnRefreshMaterials_Click(object s, RoutedEventArgs e) => _ = RefreshMaterialsAsync();
    private void BtnRefreshVerses_Click(object s, RoutedEventArgs e) => _ = RefreshVersesAsync();

    private async Task RefreshMaterialsAsync()
    {
        BtnRefreshMaterials.IsEnabled = false;
        try { var items = await MaterialLoader.LoadMaterialsAsync(_mcp); LbMaterials.ItemsSource = items; TbMaterialCount.Text = $"{items.Count}篇"; }
        catch { } finally { BtnRefreshMaterials.IsEnabled = true; }
    }
    private async Task RefreshVersesAsync()
    {
        BtnRefreshVerses.IsEnabled = false;
        try
        {
            var biblePath = AppSettings.Instance.BibleDbPath;
            if (File.Exists(biblePath)) { var items = await MaterialLoader.LoadVersesAsync(biblePath); LbVerses.ItemsSource = items; TbVerseCount.Text = $"{items.Count}条"; }
        }
        catch { } finally { BtnRefreshVerses.IsEnabled = true; }
    }

    private void LbMaterials_MouseDoubleClick(object s, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (LbMaterials.SelectedItem is SelectableItem item)
            new DetailWindow(item.SourceLabel, item.SourceDetail, item.Data).Show();
    }
    private void LbVerses_MouseDoubleClick(object s, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (LbVerses.SelectedItem is SelectableItem item)
            new DetailWindow("随机经文", "来自: 圣经数据库", item.Data).Show();
    }

    private async void BtnStart_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // 隐藏素材区，显示输出区
            SetTopPanels(false);
            PanelOutput.Visibility = Visibility.Visible;
            BtnStart.IsEnabled = false; BtnStop.Visibility = Visibility.Visible;
            TbOutput.Text = ""; TbProgress.Text = "准备中...\n";

            _cts = new CancellationTokenSource();
            var context = new AgentContext { TaskType = "Video", MaxRounds = 2, Temperature = 0.7 };
            var selectedSources = new List<string>();
            if (LbMaterials.ItemsSource is List<SelectableItem> mats)
                selectedSources.AddRange(mats.Where(m => m.IsSelected).Select(m => m.Data));
            if (LbVerses.ItemsSource is List<SelectableItem> verses)
                selectedSources.AddRange(verses.Where(v => v.IsSelected).Select(v => v.Data));
            if (selectedSources.Count > 0)
                context.SourceText = string.Join("\n\n---\n\n", selectedSources);

            var ai = new AIService(new SensitiveWordService());
            var prompt = new PromptBuilderService();
            var loop = new AgentLoop(ai, new SensitiveWordService(), prompt,
                AppSettings.Instance, _mcp, new AIImageService());
            loop.OnLog += (_, msg) => Dispatcher.BeginInvoke(() =>
            { TbProgress.AppendText(msg + "\n"); TbProgress.ScrollToEnd(); });
            loop.OnTextChunk += (chunk) => Dispatcher.BeginInvoke(() => TbOutput.AppendText(chunk));

            var result = await loop.ExecuteAsync(context, _cts.Token);
            if (result.FinalText != null)
            {
                _lastGeneratedText = result.FinalText;
                TbOutput.Text = result.FinalText;
                // 从生成内容中提取标题词
                ExtractTitles(result.FinalText);
                BtnStart.Visibility = Visibility.Collapsed;
            }
        }
        catch (OperationCanceledException) { SetTopPanels(true); BtnStart.Visibility = Visibility.Visible; }
        catch (Exception ex) { MessageBox.Show($"生成失败: {ex.Message}"); SetTopPanels(true); BtnStart.Visibility = Visibility.Visible; }
        finally { BtnStop.Visibility = Visibility.Collapsed; }
    }

    private void ExtractTitles(string text)
    {
        // 尝试从生成内容中提取标题（格式：标题: xxx 或 标题词）
        var lines = text.Split('\n');
        var titles = new List<string>();
        foreach (var line in lines.Take(10))
        {
            var t = line.Trim().TrimStart('#', ' ', '【', '】');
            if (t.StartsWith("标题") && t.Contains("："))
            {
                var parts = t.Split('：', 2);
                if (parts.Length > 1 && parts[1].Length >= 2 && parts[1].Length <= 15)
                    titles.Add(parts[1].Trim());
            }
        }
        if (titles.Count >= 2)
        {
            _generatedTitle1 = titles[0]; _generatedTitle2 = titles[1];
            TxtVideoTitle1.Text = _generatedTitle1;
            TxtVideoTitle2.Text = _generatedTitle2;
        }
    }

    private void SetTopPanels(bool visible)
    {
        var v = visible ? Visibility.Visible : Visibility.Collapsed;
        TbSourceSummary.Visibility = v;
        TbActiveAccount.Visibility = v;
        LbMaterials.Visibility = v;
        LbVerses.Visibility = v;
        PanelParams.Visibility = v;
        BtnRefreshMaterials.Visibility = v;
        BtnRefreshVerses.Visibility = v;
    }

    private void BtnStop_Click(object s, RoutedEventArgs e) => _cts?.Cancel();

    private async void BtnLearn_Click(object sender, RoutedEventArgs e)
    {
        var edited = TbOutput.Text;
        if (string.IsNullOrWhiteSpace(_lastGeneratedText) || _lastGeneratedText == edited) return;
        try { await new StyleLearningService().AnalyzeEditsAsync(_lastGeneratedText, edited, "Video", CancellationToken.None); _lastGeneratedText = edited; MessageBox.Show("风格已学习"); }
        catch (Exception ex) { MessageBox.Show($"学习失败: {ex.Message}"); }
    }

    private void BtnSaveDraft_Click(object s, RoutedEventArgs e)
    {
        var text = TbOutput.Text;
        if (string.IsNullOrWhiteSpace(text)) { MessageBox.Show("没有可保存的内容"); return; }
        AppSettings.Instance.Db.ExecuteInScope(db => db.Insertable(new VideoDraft
        {
            TitleWord1 = _generatedTitle1.Length > 0 ? _generatedTitle1 : TxtVideoTitle1.Text,
            TitleWord2 = _generatedTitle2.Length > 0 ? _generatedTitle2 : TxtVideoTitle2.Text,
            Content = text, Status = "draft",
            CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        }).ExecuteCommand());
        MessageBox.Show("草稿已保存");
    }

    private async void BtnPublish_Click(object sender, RoutedEventArgs e)
    {
        var text = TbOutput.Text;
        if (string.IsNullOrWhiteSpace(text)) return;
        try
        {
            var t1 = _generatedTitle1.Length > 0 ? _generatedTitle1 : TxtVideoTitle1.Text;
            var t2 = _generatedTitle2.Length > 0 ? _generatedTitle2 : TxtVideoTitle2.Text;
            var wechatService = new WeChatService();
            var draft = new ArticleDraft
            {
                Title = $"{t1} {t2}",
                Content = $"<h2>{t1} {t2}</h2><p>{text.Replace("\n", "<br/>")}</p>",
                Status = "published",
                CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            var mediaId = await wechatService.CreateDraftAsync(draft);
            draft.MediaId = mediaId;
            AppSettings.Instance.Db.ExecuteInScope(db => db.Insertable(draft).ExecuteCommand());
            MessageBox.Show($"视频文案已发布到公众号!\nMediaId: {mediaId}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { MessageBox.Show($"发布失败: {ex.Message}"); }
    }
}
