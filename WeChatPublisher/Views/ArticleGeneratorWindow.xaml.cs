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

    private void RefreshAiSummary()
    {
        var textCfg = AppSettings.Instance.GetActiveTextAiConfig();
        var imgCfg = AppSettings.Instance.GetActiveImageAiConfig();
        var parts = new List<string>();

        if (textCfg != null)
            parts.Add($"文本: {textCfg.ProviderName} ({textCfg.ModelName})");
        if (imgCfg != null)
            parts.Add($"配图: {imgCfg.ProviderName}");

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
                TbSanitized.Text = _sensitiveService.Sanitize(task.FinalText);
            }
        }
        catch { }
    }

    private async void BtnStart_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            BtnStart.Visibility = Visibility.Collapsed;
            BtnStop.Visibility = Visibility.Visible;
            PbProgress.Value = 0;
            TbProgress.Text = "准备中...";
            TabOutput.SelectedIndex = 0;

            _cts = new CancellationTokenSource();
            var context = BuildContext();

            var aiService = new AIService(_sensitiveService);
            var promptBuilder = new PromptBuilderService();
            var agentLoop = new AgentLoop(aiService, _sensitiveService,
                promptBuilder, AppSettings.Instance, _mcpService);

            agentLoop.OnLog += (level, msg) =>
                Dispatcher.Invoke(() => TbProgress.Text = msg);
            agentLoop.OnStepExecuted += (log) =>
                Dispatcher.Invoke(() => PbProgress.Value = Math.Min(100, PbProgress.Value + 15));

            var result = await agentLoop.ExecuteAsync(context, _cts.Token);

            if (result.FinalText != null)
            {
                TbOutput.Text = result.FinalText;
                TbSanitized.Text = _sensitiveService.Sanitize(result.FinalText);
                PbProgress.Value = 100;
                TbProgress.Text = "生成完成!";
                TabOutput.SelectedIndex = 1;
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

        // MCP资源ID存入context供Agent使用
        if (CmbMcpResource.SelectedItem is ComboBoxItem item && item.Tag is int resId)
            context.State["mcp_resource_id"] = resId.ToString();

        return context;
    }

    private void BtnStop_Click(object sender, RoutedEventArgs e) => _cts?.Cancel();

    private void BtnSaveDraft_Click(object sender, RoutedEventArgs e)
    {
        var text = TbSanitized.Text.Length > 0 ? TbSanitized.Text : TbOutput.Text;
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
        var text = TbSanitized.Text.Length > 0 ? TbSanitized.Text : TbOutput.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            MessageBox.Show("没有可发布的内容", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        try
        {
            var wechatService = new WeChatService();
            var draft = new ArticleDraft { Title = "AI生成文章", Content = text, Status = "published" };
            var mediaId = await wechatService.CreateDraftAsync(draft);
            draft.MediaId = mediaId;
            draft.UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            AppSettings.Instance.Db.ExecuteInScope(db => db.Insertable(draft).ExecuteCommand());
            MessageBox.Show($"已发布到公众号草稿箱! MediaId: {mediaId}", "成功",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"发布失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnCopy_Click(object sender, RoutedEventArgs e)
    {
        var text = TbSanitized.Text.Length > 0 ? TbSanitized.Text : TbOutput.Text;
        if (!string.IsNullOrWhiteSpace(text))
        {
            Clipboard.SetText(text);
            TbProgress.Text = "已复制到剪贴板";
        }
    }
}
