using System.Windows;
using PhotoBGCheck.WPF.ViewModels;

namespace PhotoBGCheck.WPF
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            
            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            // 加载窗口设置
            LoadWindowSettings();

            // 保存窗口设置
            Closing += MainWindow_Closing;
        }

        private void LoadWindowSettings()
        {
            var settings = Properties.Settings.Default;
            
            if (settings.WindowWidth > 0 && settings.WindowHeight > 0)
            {
                Width = settings.WindowWidth;
                Height = settings.WindowHeight;
            }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // 保存窗口大小
            var settings = Properties.Settings.Default;
            settings.WindowWidth = Width;
            settings.WindowHeight = Height;
            settings.Save();

            // 保存 ViewModel 设置
            _viewModel.SaveSettings();
        }
    }
}
