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

    public ArticleGeneratorWindow(int? taskId = null)
    {
        _taskId = taskId;
        InitializeComponent();
        AppIcon.Set(this);
        Loaded += OnLoaded;
        SldTemperature.ValueChanged += (_, _) =>
            TbTempValue.Text = SldTemperature.Value.ToString("F1");
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            FillMcpDropdown();
            RefreshPersonaCount();
            if (_taskId.HasValue) LoadExistingTask(_taskId.Value);
            // 延迟加载异步素材
            Dispatcher.BeginInvoke(new Action(async () =>
            {
                await Task.Delay(300);
                await RefreshMaterialsAsync();
                await RefreshVersesAsync();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }
        catch (Exception ex) { Logger.Error("ArticleGeneratorWindow OnLoaded 失败", ex); }
    }

    private void FillMcpDropdown()
    {
        var resources = _mcpService.GetAllResources();
        TbSourceSummary.Text = resources.Count > 0
            ? $"已配置 {resources.Count} 个 MCP 资源 - 正在加载素材..."
            : "未配置 MCP 资源";
    }

    private void RefreshPersonaCount()
    {
        try
        {
            var learner = new StyleLearningService();
            var p = learner.GetOrCreatePersona("article");
            if (p.EditCount > 0) TbLearnCount.Text = $"已学习 {p.EditCount} 次";
        }
        catch { }
    }

    // ========== 素材加载 ==========
    private void BtnRefreshMaterials_Click(object sender, RoutedEventArgs e)
        => _ = RefreshMaterialsAsync();

    private async Task RefreshMaterialsAsync()
    {
        BtnRefreshMaterials.IsEnabled = false;
        BtnRefreshMaterials.Content = "加载中...";
        try
        {
            var items = new List<SelectableItem>();
            var resources = _mcpService.GetAllResources()
                .Where(r => r.ResourceType is "LocalFolder" or "HttpMcp" or "RssFeed").ToList();
            await Task.Run(() =>
            {
                foreach (var res in resources)
                {
                    var samples = _mcpService.SampleFiles(res.Id, 12, 300, bypassCache: true);
                    foreach (var s in samples)
                    {
                        var title = Path.GetFileNameWithoutExtension(s.FileName);
                        if (title.Length > 25) title = title[..25];
                        var preview = s.Content.Replace('\n', ' ').Replace('\r', ' ').Replace("  ", " ").Trim();
                        if (preview.Length > 60) preview = preview[..60] + "...";
                        items.Add(new SelectableItem
                        {
                            Display = $"[{title}] {preview}",
                            Data = s.FullContent,
                            IsSelected = false
                        });
                    }
                }
            });
            LbMaterials.ItemsSource = items;
            TbSourceSummary.Text = $"已加载 {items.Count} 篇素材";
        }
        catch (Exception ex) { Logger.Warn($"素材刷新失败: {ex.Message}"); }
        finally { BtnRefreshMaterials.IsEnabled = true; BtnRefreshMaterials.Content = "🔄 刷新素材"; }
    }

    private void BtnRefreshVerses_Click(object sender, RoutedEventArgs e)
        => _ = RefreshVersesAsync();

    private async Task RefreshVersesAsync()
    {
        BtnRefreshVerses.IsEnabled = false;
        BtnRefreshVerses.Content = "加载中...";
        try
        {
            var items = new List<SelectableItem>();
            var biblePath = AppSettings.Instance.BibleDbPath;
            if (!File.Exists(biblePath)) return;

            await Task.Run(() =>
            {
                var bible = new BibleService(biblePath);
                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        var passage = bible.GetRandomPassage();
                        if (passage.Length > 10)
                        {
                            var clean = passage.Replace('\n', ' ').Replace('\r', ' ').Replace("  ", " ").Trim();
                            items.Add(new SelectableItem
                            {
                                Display = clean.Length > 60 ? clean[..60] + "..." : clean,
                                Data = passage,
                                IsSelected = false
                            });
                        }
                    }
                    catch { }
                }
            });
            LbVerses.ItemsSource = items;
        }
        catch (Exception ex) { Logger.Warn($"经文刷新失败: {ex.Message}"); }
        finally { BtnRefreshVerses.IsEnabled = true; BtnRefreshVerses.Content = "🔄 刷新经文"; }
    }

    private void LbMaterials_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (LbMaterials.SelectedItem is SelectableItem item)
            MessageBox.Show(item.Data, "素材全文", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void LbVerses_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (LbVerses.SelectedItem is SelectableItem item)
            MessageBox.Show(item.Data, "经文全文", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void LoadExistingTask(int taskId)
    {
        try
        {
            var task = AppSettings.Instance.Db.ExecuteInScope(db =>
                db.Queryable<AgentTask>().InSingle(taskId));
            if (task?.FinalText != null)
            {
                PanelOutput.Visibility = Visibility.Visible;
                TbOutput.Text = task.FinalText;
                _lastGeneratedText = task.FinalText;
                BtnStart.Visibility = Visibility.Collapsed;
                BtnToggleSettings.Visibility = Visibility.Collapsed;
            }
        }
        catch { }
    }

    // ========== 生成 ==========
    private void BtnToggleSettings_Click(object sender, RoutedEventArgs e)
    {
        var visible = PanelSettings.Visibility == Visibility.Visible;
        PanelSettings.Visibility = visible ? Visibility.Collapsed : Visibility.Visible;
        BtnToggleSettings.Content = visible ? "高级设置 ⚙" : "高级设置 ▲";
    }

    private void BtnStart_Click(object sender, RoutedEventArgs e) => _ = GenerateAsync();

    private async Task GenerateAsync()
    {
        try
        {
            PanelOutput.Visibility = Visibility.Visible;
            BtnStart.IsEnabled = false;
            BtnStop.Visibility = Visibility.Visible;
            TbProgress.Text = "准备中...\n";
            TbOutput.Text = "";

            _cts = new CancellationTokenSource();
            var context = BuildContext();

            var aiService = new AIService(_sensitiveService);
            var promptBuilder = new PromptBuilderService();
            var agentLoop = new AgentLoop(aiService, _sensitiveService,
                promptBuilder, AppSettings.Instance, _mcpService, _aiImageService);

            agentLoop.OnLog += (level, msg) =>
                Dispatcher.BeginInvoke(() => { TbProgress.AppendText(msg + "\n"); TbProgress.ScrollToEnd(); });
            agentLoop.OnTextChunk += (chunk) =>
                Dispatcher.BeginInvoke(() => TbOutput.AppendText(chunk));
            agentLoop.OnStepExecuted += (_) =>
                Dispatcher.BeginInvoke(() => PbProgress.Value = Math.Min(100, PbProgress.Value + 15));

            var result = await agentLoop.ExecuteAsync(context, _cts.Token);

            if (result.FinalText != null)
            {
                _lastGeneratedText = result.FinalText;
                TbOutput.Text = result.FinalText;
                PbProgress.Value = 100;
                BtnStart.Visibility = Visibility.Collapsed;
                BtnToggleSettings.Visibility = Visibility.Collapsed;
                await RunLayoutAndGenerateImages(result.FinalText);
            }
        }
        catch (OperationCanceledException) { BtnStart.Visibility = Visibility.Visible; }
        catch (Exception ex)
        {
            Logger.Error("文章生成失败", ex);
            MessageBox.Show($"生成失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            BtnStart.Visibility = Visibility.Visible;
        }
        finally { BtnStop.Visibility = Visibility.Collapsed; }
    }

    private AgentContext BuildContext()
    {
        var context = new AgentContext
        {
            TaskType = "article",
            MaxRounds = int.TryParse(TxtMaxRounds.Text, out var r) ? r : 3,
            Temperature = SldTemperature.Value
        };
        var selectedSources = new List<string>();
        if (LbMaterials.ItemsSource is List<SelectableItem> materials)
            selectedSources.AddRange(materials.Where(m => m.IsSelected).Select(m => m.Data));
        if (LbVerses.ItemsSource is List<SelectableItem> verses)
            selectedSources.AddRange(verses.Where(v => v.IsSelected).Select(v => v.Data));
        if (selectedSources.Count > 0)
            context.SourceText = string.Join("\n\n---\n\n", selectedSources);
        return context;
    }

    // ========== 排版 + 发布 ==========
    private async Task RunLayoutAndGenerateImages(string text)
    {
        try
        {
            var wechatCfg = AppSettings.Instance.GetActiveWeChatConfig();
            var contactImage = wechatCfg?.ContactImage;
            if (!File.Exists(contactImage)) contactImage = null;

            var imgService = new AIImageService();
            var imgDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output", "Images");
            Directory.CreateDirectory(imgDir);

            string? coverPath = null, img1 = null, img2 = null;
            try
            {
                var coverBytes = await imgService.GenerateImageAsync(
                    $"公众号封面: 信仰灵修主题, 温暖明亮, 无文字\n{text[..Math.Min(text.Length, 300)]}",
                    size: "1792x1024");
                coverPath = Path.Combine(imgDir, $"cover_{DateTime.Now:HHmmss}.png");
                await File.WriteAllBytesAsync(coverPath, coverBytes);

                var imgPrompt = $"为信仰灵修文章生成温馨配图, 无文字, 温暖色调\n{text[..Math.Min(text.Length, 500)]}";
                var b1 = await imgService.GenerateImageAsync(imgPrompt);
                img1 = Path.Combine(imgDir, $"pub_{DateTime.Now:HHmmss}_1.png"); await File.WriteAllBytesAsync(img1, b1);
                var b2 = await imgService.GenerateImageAsync(imgPrompt + " 另一张");
                img2 = Path.Combine(imgDir, $"pub_{DateTime.Now:HHmmss}_2.png"); await File.WriteAllBytesAsync(img2, b2);
            }
            catch (Exception ex) { Logger.Warn($"配图失败: {ex.Message}"); }

            var layout = new LayoutService();
            var formatted = await layout.FormatArticleAsync(text, "article", null, img1, null);
            var ph = "<div class='insert-image'>此处配图</div>";
            foreach (var img in new[] { img1, img2 })
            {
                if (img == null) continue;
                var idx = formatted.IndexOf(ph);
                if (idx >= 0) formatted = formatted[..idx] + $"<img src='{img}' style='width:100%;'/>" + formatted[(idx + ph.Length)..];
            }
            if (contactImage != null)
                formatted += $"\n<div style='text-align:center;margin-top:24px;'><img src='{contactImage}' style='max-width:100%;'/></div>";

            _formattedContent = formatted;
            _coverImagePath = coverPath;
            BtnRelayout.Visibility = Visibility.Visible;
            BtnPublish.IsEnabled = true;
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
            var title = "AI生成文章";
            var m = System.Text.RegularExpressions.Regex.Match(
                TbOutput.Text, @"^#+\s*(.+)", System.Text.RegularExpressions.RegexOptions.Multiline);
            if (m.Success && m.Groups[1].Value.Length > 0) title = m.Groups[1].Value.Trim();

            var svc = new WeChatService();
            var draft = new ArticleDraft
            {
                Title = title, Content = _formattedContent, ImagePaths = _coverImagePath, Status = "published",
                CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            var mediaId = await svc.CreateDraftAsync(draft);
            draft.MediaId = mediaId;
            AppSettings.Instance.Db.ExecuteInScope(db => db.Insertable(draft).ExecuteCommand());
            MessageBox.Show($"已发布到公众号草稿箱!\n标题: {title}", "发布成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { Logger.Error("发布失败", ex); MessageBox.Show($"发布失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error); }
        finally { BtnPublish.IsEnabled = true; BtnPublish.Content = "发布到公众号"; }
    }

    private void BtnStop_Click(object sender, RoutedEventArgs e) => _cts?.Cancel();

    private void BtnSaveDraft_Click(object sender, RoutedEventArgs e)
    {
        var text = TbOutput.Text;
        if (string.IsNullOrWhiteSpace(text)) { MessageBox.Show("没有可保存的内容", "提示", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        var draft = new ArticleDraft
        {
            Title = "草稿", Content = text, Status = "draft",
            CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };
        AppSettings.Instance.Db.ExecuteInScope(db => db.Insertable(draft).ExecuteCommand());
        MessageBox.Show("草稿已保存", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnCopy_Click(object sender, RoutedEventArgs e)
        => Clipboard.SetText(TbOutput.Text);

    private async void BtnLearn_Click(object sender, RoutedEventArgs e)
    {
        var edited = TbOutput.Text;
        if (string.IsNullOrWhiteSpace(_lastGeneratedText) || _lastGeneratedText == edited) return;
        BtnLearn.IsEnabled = false; BtnLearn.Content = "学习中...";
        try
        {
            var learner = new StyleLearningService();
            var result = await learner.AnalyzeEditsAsync(_lastGeneratedText, edited, "article", CancellationToken.None);
            _lastGeneratedText = edited;
            RefreshPersonaCount();
            BtnPublish.IsEnabled = true;
            var msg = result.Summary.Length > 0 ? result.Summary : "已学习你的编辑风格";
            if (result.DiscoveredWords.Count > 0)
                msg += $"\n发现潜在敏感词: {string.Join(", ", result.DiscoveredWords.Take(3).Select(w => $"{w.SourceWord}→{w.Replacement}"))}";
            MessageBox.Show(msg, "风格学习完成", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { MessageBox.Show($"学习失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error); }
        finally { BtnLearn.IsEnabled = true; BtnLearn.Content = "学习风格"; }
    }
}

public class SelectableItem
{
    public string Display { get; set; } = "";
    public string Data { get; set; } = "";
    public bool IsSelected { get; set; }
}
