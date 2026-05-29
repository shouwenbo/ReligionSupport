using System.Windows;
using System.Windows.Threading;
using WeChatPublisher.Services;

namespace WeChatPublisher;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Global exception handlers
        DispatcherUnhandledException += (s, args) =>
        {
            Logger.Error("UI线程未处理异常", args.Exception);
            MessageBox.Show($"发生错误:\n{args.Exception.Message}\n\n详情已写入 app.log",
                "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            Logger.Error("未处理异常", ex);
            if (ex != null)
                MessageBox.Show($"严重错误:\n{ex.Message}\n\n应用即将退出。详情见 app.log",
                    "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        };

        TaskScheduler.UnobservedTaskException += (s, args) =>
        {
            Logger.Error("后台任务异常", args.Exception);
            args.SetObserved();
        };

        base.OnStartup(e);

        try
        {
            Logger.Info("正在初始化配置...");
            AppSettings.Instance.Initialize();
            Logger.Info("配置初始化完成");
        }
        catch (Exception ex)
        {
            Logger.Error("配置初始化失败", ex);
            throw;
        }
    }
}
