using System.Windows;
using TextOCR.WPF.ViewModels;
using TextOCR.WPF.Services;

namespace TextOCR.WPF.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // 简单直接地创建 ViewModel
        var videoService = new VideoProcessingService();
        var ocrService = new PythonOcrService();
        var settingsService = new SettingsService();
        
        DataContext = new MainViewModel(videoService, ocrService, settingsService);
    }
}
