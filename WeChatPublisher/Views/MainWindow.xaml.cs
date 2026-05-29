using System.Windows;
using System.Windows.Media;
using SqlSugar;
using WeChatPublisher.Models;

namespace WeChatPublisher.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
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
        var aiConfig = AppSettings.Instance.GetActiveTextAiConfig();
        var wechatConfig = AppSettings.Instance.GetActiveWeChatConfig();
        if (aiConfig == null || wechatConfig == null)
        {
            TbConfigStatus.Text = "请先配置 AI 和公众号";
            TbConfigStatus.Foreground = new SolidColorBrush(Colors.OrangeRed);
        }
        else
        {
            TbConfigStatus.Text = $"AI: {aiConfig.ProviderName} | 已就绪";
            TbConfigStatus.Foreground = new SolidColorBrush(Colors.Green);
        }
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
