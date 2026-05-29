using System.Windows;
using System.Windows.Media;
using SqlSugar;
using WeChatPublisher.Models;

using WeChatPublisher.Services;

namespace WeChatPublisher.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        Logger.Info("MainWindow 启动");
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        RefreshRecentTasks();
        RefreshStatus();
    }

    private void RefreshRecentTasks()
    {
        try
        {
            var tasks = AppSettings.Instance.Db.ExecuteInScope(db =>
                db.Queryable<AgentTask>().OrderBy(t => t.Id, OrderByType.Desc).Take(20).ToList());
            DgTasks.ItemsSource = tasks;
        }
        catch (Exception ex)
        {
            TbStatus.Text = $"加载任务失败: {ex.Message}";
        }
    }

    public void RefreshStatus()
    {
        var ai = AppSettings.Instance.GetActiveTextAiConfig();
        var img = AppSettings.Instance.GetActiveImageAiConfig();
        var tts = AppSettings.Instance.GetActiveTtsConfig();
        var sub = AppSettings.Instance.GetActiveSubtitleConfig();
        var wechat = AppSettings.Instance.GetActiveWeChatConfig();
        var mcp = AppSettings.Instance.GetMcpResources();

        SetHealth(ElAiHealth, TbAiHealth, "AI",
            ai?.IsHealthy == 1 && img?.IsHealthy == 1,
            ai != null ? $"{ai.ProviderName}" : null);

        SetHealth(ElTtsHealth, TbTtsHealth, "TTS",
            tts?.IsHealthy == 1 && sub?.IsHealthy == 1,
            tts != null ? $"{tts.ApiType}" : null);

        SetHealth(ElWechatHealth, TbWechatHealth, "公众号",
            wechat?.IsHealthy == 1,
            wechat != null ? wechat.AccountName : null);

        var mcpHealthy = mcp.Count > 0 && mcp.All(r => r.IsHealthy == 1 || r.IsHealthy == 0);
        var mcpChecked = mcp.Any(r => r.IsHealthy == 1);
        SetHealth(ElMcpHealth, TbMcpHealth, "MCP",
            mcpChecked && mcpHealthy,
            $"{mcp.Count}个资源");

        TbConfigStatus.Text = $"{CountHealthy()}/4 健康";
    }

    private static void SetHealth(System.Windows.Shapes.Ellipse el, System.Windows.Controls.TextBlock tb,
        string name, bool isHealthy, string? detail)
    {
        el.Fill = isHealthy
            ? new SolidColorBrush(Colors.LimeGreen)
            : new SolidColorBrush(Colors.Gray);
        tb.Text = detail != null ? $"{name}: {detail}" : name;
    }

    private int CountHealthy()
    {
        int count = 0;
        var ai = AppSettings.Instance.GetActiveTextAiConfig();
        var img = AppSettings.Instance.GetActiveImageAiConfig();
        if (ai?.IsHealthy == 1 && img?.IsHealthy == 1) count++;
        var tts = AppSettings.Instance.GetActiveTtsConfig();
        var sub = AppSettings.Instance.GetActiveSubtitleConfig();
        if (tts?.IsHealthy == 1 && sub?.IsHealthy == 1) count++;
        if (AppSettings.Instance.GetActiveWeChatConfig()?.IsHealthy == 1) count++;
        if (AppSettings.Instance.GetMcpResources().Any(r => r.IsHealthy == 1)) count++;
        return count;
    }

    private void BtnArticle_Click(object sender, RoutedEventArgs e)
        => new ArticleGeneratorWindow().Show();

    private void BtnVideo_Click(object sender, RoutedEventArgs e)
        => new VideoGeneratorWindow().Show();

    private void BtnPublish_Click(object sender, RoutedEventArgs e)
        => new PublishWindow().Show();

    private void BtnAgent_Click(object sender, RoutedEventArgs e)
        => new AgentMonitorWindow().Show();

    private void BtnAiConfig_Click(object sender, RoutedEventArgs e)
    {
        new AiConfigWindow().ShowDialog();
        RefreshStatus();
    }

    private void BtnTtsConfig_Click(object sender, RoutedEventArgs e)
        => new TtsConfigWindow().ShowDialog();

    private void BtnWeChatConfig_Click(object sender, RoutedEventArgs e)
    {
        new WeChatConfigWindow().ShowDialog();
        RefreshStatus();
    }

    private void BtnMcpConfig_Click(object sender, RoutedEventArgs e)
        => new McpConfigWindow().ShowDialog();

    private void BtnSensitiveWord_Click(object sender, RoutedEventArgs e)
        => new SensitiveWordWindow().ShowDialog();

    private void DgTasks_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DgTasks.SelectedItem is AgentTask task)
        {
            if (task.TaskType == "article")
                new ArticleGeneratorWindow(task.Id).Show();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        Application.Current.Shutdown();
    }
}
