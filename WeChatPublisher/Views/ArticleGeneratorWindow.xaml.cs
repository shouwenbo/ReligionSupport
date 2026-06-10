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
    private bool _loaded;
    private readonly SensitiveWordService _sensitiveService = new();
    private readonly McpService _mcpService = new();
    private readonly AIImageService _aiImageService = new();

    public ArticleGeneratorWindow(int? taskId = null)
    {
        _taskId = taskId;
        Logger.Info($"ArticleGeneratorWindow 构造 taskId={taskId}");
        InitializeComponent();
        AppIcon.Set(this);
        _loaded = true;
        Loaded += OnLoaded;
        SldTemperature.ValueChanged += (_, _) =>
            TbTempValue.Text = SldTemperature.Value.ToString("F1");
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            AutoScanMcp();
            RefreshAiSummary();
            RefreshPersonaCount();
            _ = RefreshMaterialsAsync();
            _ = RefreshVersesAsync();
            if (_taskId.HasValue) LoadExistingTask(_taskId.Value);
        }
        catch (Exception ex)
        {
            Logger.Error("ArticleGeneratorWindow OnLoaded 失败", ex);
        }
    }

    private void AutoScanMcp()
    {
        CmbMcpResource.Items.Clear();
        var resources = _mcpService.GetAllResources();

        foreach (var res in resources)
            CmbMcpResource.Items.Add(new ComboBoxItem { Content = res.Name, Tag = res.Id });

        if (CmbMcpResource.Items.Count > 0)
            CmbMcpResource.SelectedIndex = 0;

        // 异步采样首选的MCP资源
        _ = Task.Run(async () =>
        {
            try
            {
                var samples = _mcpService.SampleFiles(
                    resources.FirstOrDefault()?.Id ?? 0, 10, 300);
                var count = samples.Count;
                var cached = samples.Count(s => s.IsCached);
                var types = samples.Select(s => Path.GetExtension(s.FileName).ToLowerInvariant())
                    .Where(e => e.Length > 0).Distinct().ToList();

                Dispatcher.Invoke(() =>
                {
                    if (count > 0)
                        TbSourceSummary.Text = $"从 MCP 找到 {count} 篇素材"
                            + (cached > 0 ? $" ({cached} 篇已缓存)" : "")
                            + $" | 类型: {string.Join(", ", types)}";
                    else
                        TbSourceSummary.Text = "MCP 中暂无可用素材";
                });
            }
            catch
            {
                Dispatcher.Invoke(() =>
                    TbSourceSummary.Text = "MCP 扫描失败，将使用纯AI生成");
            }
        });
    }

    // ========== 素材选择 ==========
    private void BtnRefreshMaterials_Click(object sender, RoutedEventArgs e)
        => _ = RefreshMaterialsAsync();

    private async Task RefreshMaterialsAsync()
    {
        BtnRefreshMaterials.IsEnabled = false;
        try
        {
            var items = new List<SelectableItem>();
            var resources = _mcpService.GetAllResources()
                .Where(r => r.ResourceType == "LocalFolder").ToList();
            if (resources.Count > 0)
            {
                var samples = _mcpService.SampleFiles(resources[0].Id, 20, 300, bypassCache: true);
                foreach (var s in samples)
                    items.Add(new SelectableItem
                    {
                        Display = $"[{s.FileName}] {s.Content[..Math.Min(s.Content.Length, 60)]}...",
                        Data = s.FullContent,
                        IsSelected = false
                    });
            }
            LbMaterials.ItemsSource = items;
        }
        catch (Exception ex) { Logger.Warn($"刷新素材失败: {ex.Message}"); }
        finally { BtnRefreshMaterials.IsEnabled = true; }
    }

    private void BtnRefreshVerses_Click(object sender, RoutedEventArgs e)
        => _ = RefreshVersesAsync();

    private async Task RefreshVersesAsync()
    {
        BtnRefreshVerses.IsEnabled = false;
        try
        {
            var items = new List<SelectableItem>();
            var biblePath = AppSettings.Instance.BibleDbPath;
            if (File.Exists(biblePath))
            {
                var bible = new BibleService(biblePath);
                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        var passage = bible.GetRandomPassage();
                        if (passage.Length > 10)
                            items.Add(new SelectableItem
                            {
                                Display = passage[..Math.Min(passage.Length, 80)],
                                Data = passage,
                                IsSelected = false
                            });
                    }
                    catch { }
                }
            }
            LbVerses.ItemsSource = items;
        }
        catch (Exception ex) { Logger.Warn($"刷新经文失败: {ex.Message}"); }
        finally { BtnRefreshVerses.IsEnabled = true; }
    }

    private void BtnPreviewMaterial_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string data)
            MessageBox.Show(data, "素材段落全貌", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void RefreshPersonaCount()
    {
        try
        {
            var learner = new StyleLearningService();
            var persona = learner.GetOrCreatePersona("article");
            if (persona.EditCount > 0)
                TbLearnCount.Text = $"已学习 {persona.EditCount} 次";
        }
        catch { }
    }

    private void RefreshAiSummary()
    {
        var textCfg = AppSettings.Instance.GetActiveTextAiConfig();
        var imgCfg = AppSettings.Instance.GetActiveImageAiConfig();
        var parts = new List<string>();

        if (textCfg != null)
            parts.Add($"文本: {textCfg.ProviderName} ({textCfg.ModelName})");
        if (imgCfg != null)
            parts.Add($"配图: {imgCfg.ProviderName}({imgCfg.ModelName})");

        TbAiSummary.Text = string.Join("  |  ", parts);
        if (parts.Count == 0)
            TbAiSummary.Text = "请先配置 AI";
    }

    private void BtnToggleSettings_Click(object sender, RoutedEventArgs e)
    {
        var visible = PanelSettings.Visibility == Visibility.Visible;
        PanelSettings.Visibility = visible ? Visibility.Collapsed : Visibility.Visible;
        BtnToggleSettings.Content = visible ? "高级设置 ▾" : "高级设置 ▴";
    }

    private void LoadExistingTask(int taskId)
    {
        try
        {
            var task = AppSettings.Instance.Db.ExecuteInScope(db =>
                db.Queryable<AgentTask>().InSingle(taskId));
            if (task?.FinalText != null)
            {
                TbOutput.Text = task.FinalText;
                TbOutput.Text = _sensitiveService.Sanitize(task.FinalText);
            }
        }
        catch { }
    }

    private void SetControlsEnabled(bool enabled)
    {
        BtnStart.IsEnabled = enabled;
        BtnToggleSettings.IsEnabled = enabled;
        BtnLearn.IsEnabled = enabled;
        BtnSaveDraft.IsEnabled = enabled;
        BtnCopy.IsEnabled = enabled;
        BtnPublish.IsEnabled = enabled;
        SldTemperature.IsEnabled = enabled;
        TxtMaxRounds.IsEnabled = enabled;
        CmbStyle.IsEnabled = enabled;
        CmbMcpResource.IsEnabled = enabled;
        CbManualReview.IsEnabled = enabled;
    }

    private async void BtnStart_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SetControlsEnabled(false);
            BtnStop.Visibility = Visibility.Visible;
            PbProgress.Value = 0;
            TbProgress.Text = "准备中...";

            _cts = new CancellationTokenSource();
            var context = BuildContext();

            var aiService = new AIService(_sensitiveService);
            var promptBuilder = new PromptBuilderService();
            var agentLoop = new AgentLoop(aiService, _sensitiveService,
                promptBuilder, AppSettings.Instance, _mcpService, _aiImageService);

            TbProgress.Text = ""; // 清空上次日志
            TbOutput.Text = "";    // 清空输出区
            agentLoop.OnLog += (level, msg) =>
                Dispatcher.BeginInvoke(() =>
                {
                    var icon = level switch { "error" => "✗", "warn" => "⚠", _ => "✓" };
                    TbProgress.AppendText($"{icon} {msg}\n");
                    TbProgress.ScrollToEnd();
                });
            agentLoop.OnTextChunk += (chunk) =>
                Dispatcher.BeginInvoke(() => TbOutput.AppendText(chunk));
            agentLoop.OnStepExecuted += (log) =>
                Dispatcher.BeginInvoke(() => PbProgress.Value = Math.Min(100, PbProgress.Value + 15));

            var result = await agentLoop.ExecuteAsync(context, _cts.Token);

            if (result.FinalText != null)
            {
                _lastGeneratedText = result.FinalText;
                TbOutput.Text = result.FinalText;
                PbProgress.Value = 100;
                TbProgress.Text = "生成完成!";
                BtnStart.Content = "重新生成";
                PanelMaterials.Visibility = Visibility.Collapsed;
                PanelSettings.Visibility = Visibility.Collapsed;
            }
        }
        catch (OperationCanceledException)
        {
            TbProgress.Text = "已停止";
        }
        catch (Exception ex)
        {
            Logger.Error("文章生成失败", ex);
            MessageBox.Show($"生成失败: {ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            BtnStart.Visibility = Visibility.Visible;
            BtnStop.Visibility = Visibility.Collapsed;
            SetControlsEnabled(true);
        }
    }

    private AgentContext BuildContext()
    {
        var context = new AgentContext
        {
            TaskType = "article",
            MaxRounds = int.TryParse(TxtMaxRounds.Text, out var r) ? r : 3,
            Temperature = SldTemperature.Value,
            PauseForManualReview = CbManualReview.IsChecked == true
        };

        var style = (CmbStyle.SelectedItem as ComboBoxItem)?.Content?.ToString();
        context.ArticleStyle = style ?? "AI自动选择";
        context.State["article_style"] = context.ArticleStyle;

        // 合并用户勾选的素材 + 经文作为源文本
        var selectedSources = new List<string>();
        if (LbMaterials.ItemsSource is List<SelectableItem> materials)
            selectedSources.AddRange(materials.Where(m => m.IsSelected).Select(m => m.Data));
        if (LbVerses.ItemsSource is List<SelectableItem> verses)
            selectedSources.AddRange(verses.Where(v => v.IsSelected).Select(v => v.Data));

        if (selectedSources.Count > 0)
            context.SourceText = string.Join("\n\n---\n\n", selectedSources);

        // MCP资源ID存入context供Agent使用
        if (CmbMcpResource.SelectedItem is ComboBoxItem item && item.Tag is int resId)
            context.State["mcp_resource_id"] = resId.ToString();

        return context;
    }

    private void BtnStop_Click(object sender, RoutedEventArgs e) => _cts?.Cancel();

    private string? _lastGeneratedText; // 保存原始AI生成文本用于对比

    private async void BtnLearn_Click(object sender, RoutedEventArgs e)
    {
        // 用户编辑的是 TbOutput 文本框内容，对比 _lastGeneratedText
        var edited = TbOutput.Text;
        if (string.IsNullOrWhiteSpace(_lastGeneratedText) || string.IsNullOrWhiteSpace(edited))
        {
            MessageBox.Show("请先生成文章，然后修改内容后再点击审核", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (_lastGeneratedText == edited)
        {
            MessageBox.Show("内容未修改，无需审核", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        BtnLearn.IsEnabled = false;
        BtnLearn.Content = "分析中...";
        try
        {
            var learner = new StyleLearningService();
            var result = await learner.AnalyzeEditsAsync(
                _lastGeneratedText, edited, "article", CancellationToken.None);

            _lastGeneratedText = edited; // 更新基准

            // 更新学习次数显示
            var persona = learner.GetOrCreatePersona("article");
            TbLearnCount.Text = $"已学习 {persona.EditCount} 次";

            // 反馈结果
            var msg = result.Summary.Length > 0 ? result.Summary : "已学习你的编辑风格";

            if (result.DiscoveredWords.Count > 0)
            {
                var words = result.DiscoveredWords.Take(3)
                    .Select(w => $"{w.SourceWord}→{w.Replacement}");
                msg += $"\n\n发现潜在敏感词: {string.Join(", ", words)}\n可在敏感词知识库中查看和管理。";
            }

            if (result.StylePatterns.Count > 0)
                msg += $"\n\n学到 {result.StylePatterns.Count} 个写作风格模式。下次生成将更贴近你的风格。";

            MessageBox.Show(msg, "风格学习完成",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"学习失败: {ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            BtnLearn.IsEnabled = true;
            BtnLearn.Content = "审核完成 ✓";
        }
    }

    private void BtnSaveDraft_Click(object sender, RoutedEventArgs e)
    {
        var text = TbOutput.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            MessageBox.Show("没有可保存的内容", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var draft = new ArticleDraft
        {
            Title = "AI生成文章",
            Content = text,
            Status = "draft",
            CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };
        AppSettings.Instance.Db.ExecuteInScope(db => db.Insertable(draft).ExecuteCommand());
        MessageBox.Show("草稿已保存", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void BtnPublish_Click(object sender, RoutedEventArgs e)
    {
        var text = TbOutput.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            MessageBox.Show("没有可发布的内容", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var wechatCfg = AppSettings.Instance.GetActiveWeChatConfig();
        var contactImage = wechatCfg?.ContactImage;
        if (string.IsNullOrWhiteSpace(contactImage) || !File.Exists(contactImage))
            contactImage = null;

        BtnPublish.IsEnabled = false;
        BtnPublish.Content = "生成配图中...";
        try
        {
            // 1. 用TokenHub生成配图
            string? imagePath1 = null, imagePath2 = null;
            try
            {
                var imgService = new AIImageService();
                var imgPrompt = $"为信仰灵修文章生成温馨配图, 无文字, 温暖色调, 柔和画面\n{text[..Math.Min(text.Length, 500)]}";
                var imgDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output", "Images");
                Directory.CreateDirectory(imgDir);

                var bytes = await imgService.GenerateImageAsync(imgPrompt);
                imagePath1 = Path.Combine(imgDir, $"pub_{DateTime.Now:HHmmss}_1.png");
                await File.WriteAllBytesAsync(imagePath1, bytes);

                bytes = await imgService.GenerateImageAsync(imgPrompt + " 另一张");
                imagePath2 = Path.Combine(imgDir, $"pub_{DateTime.Now:HHmmss}_2.png");
                await File.WriteAllBytesAsync(imagePath2, bytes);

                BtnPublish.Content = "智能排版中...";
            }
            catch (Exception ex) { Logger.Warn($"配图生成失败, 继续无图发布: {ex.Message}"); }

            // 2. 智能排版(传入真实图片)
            var layout = new LayoutService();
            var learner = new StyleLearningService();
            var persona = learner.BuildPersonaInjection("article");
            var formatted = await layout.FormatArticleAsync(text, "article",
                persona, imagePath1, contactImage);

            // 替换占位符为真实图片
            var placeholder = "<div class='insert-image'>此处配图</div>";
            if (imagePath1 != null)
            {
                var idx = formatted.IndexOf(placeholder);
                if (idx >= 0) formatted = formatted[..idx]
                    + $"<img src='{imagePath1}' style='width:100%;border-radius:8px;margin:16px 0;'/>"
                    + formatted[(idx + placeholder.Length)..];
            }
            if (imagePath2 != null)
            {
                var idx = formatted.IndexOf(placeholder);
                if (idx >= 0) formatted = formatted[..idx]
                    + $"<img src='{imagePath2}' style='width:100%;border-radius:8px;margin:16px 0;'/>"
                    + formatted[(idx + placeholder.Length)..];
            }

            // 3. 提取标题
            var title = "AI生成文章";
            var titleMatch = System.Text.RegularExpressions.Regex.Match(
                text, @"^#+\s*(.+)", System.Text.RegularExpressions.RegexOptions.Multiline);
            if (titleMatch.Success && titleMatch.Groups[1].Value.Length > 0)
                title = titleMatch.Groups[1].Value.Trim();

            // 3. 发布
            var wechatService = new WeChatService();
            var draft = new ArticleDraft
            {
                Title = title,
                Content = formatted,
                Status = "published",
                CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            var mediaId = await wechatService.CreateDraftAsync(draft);
            draft.MediaId = mediaId;
            AppSettings.Instance.Db.ExecuteInScope(db => db.Insertable(draft).ExecuteCommand());

            var contactMsg = contactImage != null ? "(含联系方式)" : "";
            MessageBox.Show($"已发布到公众号草稿箱!{contactMsg}\n标题: {title}\nMediaId: {mediaId}",
                "发布成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Logger.Error("发布失败", ex);
            MessageBox.Show($"发布失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            BtnPublish.IsEnabled = true;
            BtnPublish.Content = "发布到公众号";
        }
    }

    private void BtnCopy_Click(object sender, RoutedEventArgs e)
    {
        var text = TbOutput.Text;
        if (!string.IsNullOrWhiteSpace(text))
        {
            Clipboard.SetText(text);
            TbProgress.Text = "已复制到剪贴板";
        }
    }
}

public class SelectableItem
{
    public string Display { get; set; } = "";
    public string Data { get; set; } = "";
    public bool IsSelected { get; set; }
}
