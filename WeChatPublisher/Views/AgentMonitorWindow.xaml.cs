using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WeChatPublisher.Models;

namespace WeChatPublisher.Views;

public partial class AgentMonitorWindow : Window
{
    public AgentMonitorWindow()
    {
        InitializeComponent();
        AppIcon.Set(this);
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        RefreshTaskList();
    }

    private void RefreshTaskList()
    {
        TvSteps.Items.Clear();
        try
        {
            var tasks = AppSettings.Instance.Db.ExecuteInScope(db =>
                db.Queryable<AgentTask>().OrderBy(t => t.Id, SqlSugar.OrderByType.Desc).Take(10).ToList());

            foreach (var task in tasks)
            {
                var taskItem = CreateTaskNode(task);
                TvSteps.Items.Add(taskItem);
            }

            TbStatus.Text = $"共 {tasks.Count} 个任务";
        }
        catch (Exception ex)
        {
            TbStatus.Text = $"加载失败: {ex.Message}";
        }
    }

    private static TreeViewItem CreateTaskNode(AgentTask task)
    {
        var icon = task.Status switch
        {
            "completed" => "✓",
            "running" => "▶",
            "failed" => "✗",
            _ => "○"
        };

        var item = new TreeViewItem
        {
            Header = $"{icon} [{task.CreatedAt}] {task.TaskType}: {task.SourceTitle ?? "无标题"} ({task.Status})",
            Foreground = task.Status switch
            {
                "completed" => new SolidColorBrush(Colors.LightGreen),
                "running" => new SolidColorBrush(Colors.Cyan),
                "failed" => new SolidColorBrush(Colors.OrangeRed),
                _ => new SolidColorBrush(Colors.Gray)
            }
        };

        try
        {
            var logs = AppSettings.Instance.Db.ExecuteInScope(db =>
                db.Queryable<AgentStepLog>()
                  .Where(l => l.TaskId == task.Id)
                  .OrderBy(l => l.Round, SqlSugar.OrderByType.Asc)
                  .OrderBy(l => l.Id, SqlSugar.OrderByType.Asc)
                  .ToList());

            for (int round = 1; round <= task.TotalRounds; round++)
            {
                var roundLogs = logs.Where(l => l.Round == round).ToList();
                if (roundLogs.Count == 0) continue;

                var roundItem = new TreeViewItem
                {
                    Header = $"第 {round} 轮",
                    Foreground = new SolidColorBrush(Colors.Yellow)
                };

                foreach (var log in roundLogs)
                {
                    var statusIcon = log.Status switch
                    {
                        "completed" => "✓",
                        "running" => "▶",
                        "failed" => "✗",
                        _ => "○"
                    };

                    var header = $"{statusIcon} {log.StepName}";
                    if (log.OutputSummary != null)
                        header += $" - {log.OutputSummary}";
                    if (log.SensitiveWordCount > 0)
                        header += $" [敏感词: {log.SensitiveWordCount}]";

                    roundItem.Items.Add(new TreeViewItem
                    {
                        Header = header,
                        Foreground = log.Status switch
                        {
                            "completed" => new SolidColorBrush(Colors.LightGray),
                            "running" => new SolidColorBrush(Colors.Cyan),
                            "failed" => new SolidColorBrush(Colors.OrangeRed),
                            _ => new SolidColorBrush(Colors.Gray)
                        },
                        ToolTip = log.FullOutput
                    });
                }

                item.Items.Add(roundItem);
            }
        }
        catch { }

        return item;
    }

    private void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        RefreshTaskList();
    }

    private void BtnClear_Click(object sender, RoutedEventArgs e)
    {
        TvSteps.Items.Clear();
        TbStatus.Text = "已清空";
    }
}
