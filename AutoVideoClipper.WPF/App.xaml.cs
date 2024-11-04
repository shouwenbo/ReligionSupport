using System.Configuration;
using System.Data;
using System.Windows;

namespace AutoVideoClipper.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
            {
                MessageBox.Show(ex.ExceptionObject.ToString(), "未处理异常");
            };

            DispatcherUnhandledException += (s, ex) =>
            {
                MessageBox.Show(ex.Exception.ToString(), "UI线程异常");
                ex.Handled = true;
            };

            base.OnStartup(e);
        }


    }

}
