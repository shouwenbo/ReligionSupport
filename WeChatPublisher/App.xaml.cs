using System.Windows;

namespace WeChatPublisher;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        AppSettings.Instance.Initialize();
    }
}
