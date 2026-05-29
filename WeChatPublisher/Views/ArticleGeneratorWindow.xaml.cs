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
    private readonly SensitiveWordService _sensitiveService = new();
    private readonly McpService _mcpService = new();

    public ArticleGeneratorWindow(int? taskId = null)
    {
        _taskId = taskId;
        Logger.Info($"ArticleGeneratorWindow 构造 taskId={taskId}");
        try
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }
        catch (Exception ex)
        {
            Logger.Error("ArticleGeneratorWindow InitializeComponent 失败", ex);
            throw;
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            Logger.Info("ArticleGeneratorWindow OnLoaded");
            RefreshMcpResources();
            RefreshAiStatus();
            if (_taskId.HasValue) LoadExistingTask(_taskId.Value);
        }
        catch (Exception ex)
        {
            Logger.Error("ArticleGeneratorWindow OnLoaded 失败", ex);
            MessageBox.Show($"加载失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RefreshMcpResources()
    {
        CmbMcpResource.Items.Clear();
        var resources = _mcpService.GetAllResources();
        foreach (var res in resources)
            CmbMcpResource.Items.Add(new ComboBoxItem { Content = res.Name, Tag = res.Id });
        if (CmbMcpResource.Items.Count > 0) CmbMcpResource.SelectedIndex = 0;
    }

    private void RefreshAiStatus()
    {
        var config = AppSettings.Instance.GetActiveTextAiConfig();
        TbAiStatus.Text = config != null
            ? $"当前模型: {config.ProviderName} - {config.ModelName}"
            : "未配置AI - 请先配置";
        TbAiStatus.Foreground = config != null
            ? System.Windows.Media.Brushes.Green
            : System.Windows.Media.Brushes.OrangeRed;
    }

    private void RbMcp_Checked(object sender, RoutedEventArgs e)
    {
        CmbMcpResource.IsEnabled = true;
        GbManualInput.Visibility = Visibility.Collapsed;
    }

    private void RbManual_Checked(object sender, RoutedEventArgs e)
    {
        CmbMcpResource.IsEnabled = false;
        GbManualInput.Visibility = Visibility.Visible;
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
                TxtTitle.Text = task.SourceTitle ?? "";
            }
        }
        catch { }
    }

    private async void BtnStart_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            BtnStart.IsEnabled = false;
            BtnStop.IsEnabled = true;
            PbProgress.Value = 0;
            TbOutput.Text = "";
            TbSanitized.Text = "";

            _cts = new CancellationTokenSource();

            var context = new AgentContext
            {
                TaskType = "article",
                MaxRounds = int.TryParse(TxtMaxRounds.Text, out var r) ? r : 3,
                Temperature = SldTemperature.Value,
                ArticleStyle = (CmbStyle.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "温情生活型",
                PauseForManualReview = CbManualReview.IsChecked == true
            };

            // Get source
            if (RbMcp.IsChecked == true)
            {
                if (CmbMcpResource.SelectedItem is ComboBoxItem item && item.Tag is int resId)
                {
                    var docxFiles = _mcpService.ReadDocxFiles(resId, 1);
                    if (docxFiles.Count == 0)
                    {
                        MessageBox.Show("MCP资源中没有.docx文件", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    var doc = docxFiles[0];
                    context.SourceTitle = doc.Title;
                    context.SourceDescription = doc.Description;
                    context.SourceVerse = doc.Verse;
                    context.SourceText = doc.Content;
                    context.ReferenceFiles.Add(doc.FilePath);
                }
            }
            else
            {
                context.SourceTitle = TxtTitle.Text;
                context.SourceDescription = TxtDesc.Text;
                context.SourceVerse = TxtVerse.Text;
                context.SourceText = TxtContent.Text;
            }

            var aiService = new AIService(_sensitiveService);
            var promptBuilder = new PromptBuilderService();
            var agentLoop = new AgentLoop(aiService, _sensitiveService, promptBuilder, AppSettings.Instance, _mcpService);

            agentLoop.OnLog += (level, msg) =>
            {
                Dispatcher.Invoke(() => TbProgress.Text = msg);
            };

            agentLoop.OnStepExecuted += (log) =>
            {
                Dispatcher.Invoke(() =>
                {
                    PbProgress.Value = Math.Min(100, PbProgress.Value + 10);
                });
            };

            var result = await agentLoop.ExecuteAsync(context, _cts.Token);

            if (result.FinalText != null)
            {
                TbOutput.Text = result.FinalText;
                TbSanitized.Text = _sensitiveService.Sanitize(result.FinalText);
                PbProgress.Value = 100;
                TbProgress.Text = "生成完成!";
            }
        }
        catch (OperationCanceledException)
        {
            TbProgress.Text = "已停止";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"生成失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            TbProgress.Text = $"错误: {ex.Message}";
        }
        finally
        {
            BtnStart.IsEnabled = true;
            BtnStop.IsEnabled = false;
        }
    }

    private void BtnStop_Click(object sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
    }

    private void BtnSaveDraft_Click(object sender, RoutedEventArgs e)
    {
        var text = TbSanitized.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            MessageBox.Show("没有可保存的内容", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var draft = new ArticleDraft
        {
            Title = TxtTitle.Text,
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
        var text = TbSanitized.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            MessageBox.Show("没有可发布的内容", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var wechatService = new WeChatService();
            var draft = new ArticleDraft
            {
                Title = TxtTitle.Text,
                Content = text,
                Status = "published"
            };

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
        var text = TbSanitized.Text;
        if (!string.IsNullOrWhiteSpace(text))
        {
            System.Windows.Clipboard.SetText(text);
            TbProgress.Text = "已复制到剪贴板";
        }
    }
}
