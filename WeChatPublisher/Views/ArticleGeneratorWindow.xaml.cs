using System.Windows;
using System.Windows.Controls;
using WeChatPublisher.Agent;
using WeChatPublisher.Models;
using WeChatPublisher.Services;

namespace WeChatPublisher.Views;

public partial class ArticleGeneratorWindow : Window
{
    private int? _taskId;
    private CancellationTokenSource? _cts;
    private string? _lastGeneratedText;
    private string? _formattedContent;
    private string? _coverImagePath;
    private readonly SensitiveWordService _sensitiveService = new();
    private readonly McpService _mcpService = new();
    private readonly AIImageService _aiImageService = new();
    private List<SelectableItem> _selectedMaterials = [];
    private List<SelectableItem> _selectedVerses = [];

    public ArticleGeneratorWindow(int? taskId = null) { _taskId = taskId; InitializeComponent(); AppIcon.Set(this); Loaded += OnLoaded; SldTemperature.ValueChanged += (_, _) => TbTempValue.Text = SldTemperature.Value.ToString("F1"); }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        try { RefreshPersonaCount(); if (_taskId.HasValue) LoadExistingTask(_taskId.Value); }
        catch (Exception ex) { Logger.Error("ArticleGeneratorWindow OnLoaded 失败", ex); }
    }

    private void RefreshPersonaCount()
    {
        try { var p = new StyleLearningService().GetOrCreatePersona("article"); if (p.EditCount > 0) TbLearnCount.Text = $"已学习 {p.EditCount} 次"; } catch { }
    }

    private void BtnSelectMaterials_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new MaterialSelectorWindow();
        if (dialog.ShowDialog() == true)
        {
            _selectedMaterials = dialog.SelectedMaterials;
            _selectedVerses = dialog.SelectedVerses;
            var total = _selectedMaterials.Count + _selectedVerses.Count;
            TbSelectedCount.Text = total > 0 ? $"已选 {total} 条" : "";
            TbSourceSummary.Text = total > 0 ? "素材已就绪" : "未选择素材（将使用 MCP 自动扫描）";
        }
    }

    private void BtnToggleSettings_Click(object sender, RoutedEventArgs e)
    {
        var v = PanelSettings.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        PanelSettings.Visibility = v;
        BtnToggleSettings.Content = v == Visibility.Visible ? "⚙ ▲" : "⚙ 高级设置";
    }

    private void LoadExistingTask(int taskId)
    {
        try
        {
            var task = AppSettings.Instance.Db.ExecuteInScope(db => db.Queryable<AgentTask>().InSingle(taskId));
            if (task?.FinalText != null)
            {
                PanelOutput.Visibility = Visibility.Visible;
                TbOutput.Text = task.FinalText;
                _lastGeneratedText = task.FinalText;
                BtnStart.Visibility = Visibility.Collapsed;
                BtnToggleSettings.Visibility = Visibility.Collapsed;
                BtnSelectMaterials.Visibility = Visibility.Collapsed;
                BtnPublish.IsEnabled = true;
            }
        }
        catch { }
    }

    private async void BtnStart_Click(object sender, RoutedEventArgs e) => await GenerateAsync();

    private async Task GenerateAsync()
    {
        try
        {
            BtnSelectMaterials.Visibility = Visibility.Collapsed;
            BtnToggleSettings.Visibility = Visibility.Collapsed;
            PanelSettings.Visibility = Visibility.Collapsed;
            PanelOutput.Visibility = Visibility.Visible;
            BtnStart.IsEnabled = false; BtnStop.Visibility = Visibility.Visible;
            TbOutput.Text = ""; TbProgress.Text = "准备中...\n"; PbProgress.Value = 0;

            _cts = new CancellationTokenSource();
            var context = new AgentContext
            {
                TaskType = "article",
                MaxRounds = int.TryParse(TxtMaxRounds.Text, out var r) ? r : 3,
                Temperature = SldTemperature.Value
            };
            var sources = new List<string>();
            sources.AddRange(_selectedMaterials.Select(m => m.Data));
            sources.AddRange(_selectedVerses.Select(v => v.Data));
            if (sources.Count > 0) context.SourceText = string.Join("\n\n---\n\n", sources);

            var ai = new AIService(_sensitiveService);
            var prompt = new PromptBuilderService();
            var loop = new AgentLoop(ai, _sensitiveService, prompt, AppSettings.Instance, _mcpService, _aiImageService);
            loop.OnLog += (_, msg) => Dispatcher.BeginInvoke(() => { TbProgress.AppendText(msg + "\n"); TbProgress.ScrollToEnd(); });
            loop.OnTextChunk += (chunk) => Dispatcher.BeginInvoke(() => TbOutput.AppendText(chunk));
            loop.OnStepExecuted += (_) => Dispatcher.BeginInvoke(() => PbProgress.Value = Math.Min(100, PbProgress.Value + 15));

            var result = await loop.ExecuteAsync(context, _cts.Token);
            if (result.FinalText != null)
            {
                _lastGeneratedText = result.FinalText;
                TbOutput.Text = result.FinalText;
                PbProgress.Value = 100;
                BtnStart.Visibility = Visibility.Collapsed;
                await RunLayoutAndGenerateImages(result.FinalText);
            }
        }
        catch (OperationCanceledException) { BtnStart.Visibility = Visibility.Visible; BtnSelectMaterials.Visibility = Visibility.Visible; BtnToggleSettings.Visibility = Visibility.Visible; }
        catch (Exception ex) { Logger.Error("生成失败", ex); MessageBox.Show($"生成失败: {ex.Message}"); BtnStart.Visibility = Visibility.Visible; BtnSelectMaterials.Visibility = Visibility.Visible; BtnToggleSettings.Visibility = Visibility.Visible; }
        finally { BtnStop.Visibility = Visibility.Collapsed; }
    }

    private async Task RunLayoutAndGenerateImages(string text)
    {
        try
        {
            var wechatCfg = AppSettings.Instance.GetActiveWeChatConfig();
            var contactImage = wechatCfg?.ContactImage;
            if (!File.Exists(contactImage)) contactImage = null;
            var imgService = new AIImageService();
            var imgDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output", "Images"); Directory.CreateDirectory(imgDir);
            string? coverPath = null, img1 = null, img2 = null;
            try
            {
                var cb = await imgService.GenerateImageAsync($"公众号封面: 信仰灵修主题, 温暖明亮, 无文字\n{text[..Math.Min(text.Length, 300)]}", size: "1792x1024");
                coverPath = Path.Combine(imgDir, $"cover_{DateTime.Now:HHmmss}.png"); await File.WriteAllBytesAsync(coverPath, cb);
                var ip = $"为信仰灵修文章生成温馨配图\n{text[..Math.Min(text.Length, 500)]}";
                var b1 = await imgService.GenerateImageAsync(ip); img1 = Path.Combine(imgDir, $"pub_{DateTime.Now:HHmmss}_1.png"); await File.WriteAllBytesAsync(img1, b1);
                var b2 = await imgService.GenerateImageAsync(ip + " 另一张"); img2 = Path.Combine(imgDir, $"pub_{DateTime.Now:HHmmss}_2.png"); await File.WriteAllBytesAsync(img2, b2);
            }
            catch (Exception ex) { Logger.Warn($"配图失败: {ex.Message}"); }
            var formatted = await new LayoutService().FormatArticleAsync(text, "article", null, img1, null);
            var ph = "<div class='insert-image'>此处配图</div>";
            foreach (var img in new[] { img1, img2 })
            { if (img == null) continue; var idx = formatted.IndexOf(ph); if (idx >= 0) formatted = formatted[..idx] + $"<img src='{img}' style='width:100%;'/>" + formatted[(idx + ph.Length)..]; }
            if (contactImage != null) formatted += $"\n<div style='text-align:center;margin-top:24px;'><img src='{contactImage}' style='max-width:100%;'/></div>";
            _formattedContent = formatted; _coverImagePath = coverPath; BtnRelayout.Visibility = Visibility.Visible; BtnPublish.IsEnabled = true;
        }
        catch (Exception ex) { Logger.Error("排版失败", ex); }
    }

    private void BtnRelayout_Click(object sender, RoutedEventArgs e) => _ = RunLayoutAndGenerateImages(TbOutput.Text);

    private async void BtnPublish_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_formattedContent)) { await RunLayoutAndGenerateImages(TbOutput.Text); }
        BtnPublish.IsEnabled = false; BtnPublish.Content = "发布中...";
        try
        {
            var title = "AI生成文章"; var m = System.Text.RegularExpressions.Regex.Match(TbOutput.Text, @"^#+\s*(.+)", System.Text.RegularExpressions.RegexOptions.Multiline);
            if (m.Success && m.Groups[1].Value.Length > 0) title = m.Groups[1].Value.Trim();
            var svc = new WeChatService();
            var draft = new ArticleDraft { Title = title, Content = _formattedContent, ImagePaths = _coverImagePath, Status = "published", CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") };
            var mediaId = await svc.CreateDraftAsync(draft); draft.MediaId = mediaId;
            AppSettings.Instance.Db.ExecuteInScope(db => db.Insertable(draft).ExecuteCommand());
            MessageBox.Show($"已发布到公众号草稿箱!\n标题: {title}", "发布成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { Logger.Error("发布失败", ex); MessageBox.Show($"发布失败: {ex.Message}"); }
        finally { BtnPublish.IsEnabled = true; BtnPublish.Content = "发布到公众号"; }
    }

    private void BtnStop_Click(object s, RoutedEventArgs e) => _cts?.Cancel();
    private void BtnSaveDraft_Click(object s, RoutedEventArgs e)
    {
        var text = TbOutput.Text; if (string.IsNullOrWhiteSpace(text)) { MessageBox.Show("没有可保存的内容"); return; }
        AppSettings.Instance.Db.ExecuteInScope(db => db.Insertable(new ArticleDraft { Title = "草稿", Content = text, Status = "draft", CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }).ExecuteCommand());
        MessageBox.Show("草稿已保存");
    }
    private void BtnCopy_Click(object s, RoutedEventArgs e) => Clipboard.SetText(TbOutput.Text);
    private async void BtnLearn_Click(object sender, RoutedEventArgs e)
    {
        var edited = TbOutput.Text; if (string.IsNullOrWhiteSpace(_lastGeneratedText) || _lastGeneratedText == edited) return;
        BtnLearn.IsEnabled = false; BtnLearn.Content = "...";
        try { var r = await new StyleLearningService().AnalyzeEditsAsync(_lastGeneratedText, edited, "article", CancellationToken.None); _lastGeneratedText = edited; RefreshPersonaCount(); MessageBox.Show(r.Summary); } catch (Exception ex) { MessageBox.Show(ex.Message); }
        finally { BtnLearn.IsEnabled = true; BtnLearn.Content = "学习风格"; }
    }
}

public class SelectableItem
{
    public string Display { get; set; } = "";
    public string Data { get; set; } = "";
    public string SourceType { get; set; } = "file";
    public string SourceLabel { get; set; } = "";
    public string SourceDetail { get; set; } = "";
    public string SourceIcon { get; set; } = "📁";
    public bool IsSelected { get; set; }
}
