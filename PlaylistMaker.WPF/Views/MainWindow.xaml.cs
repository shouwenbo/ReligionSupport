/*
 * MainWindow.xaml.cs 是主窗口的代码后台文件（Code-Behind）
 * 
 * 在 MVVM 模式中，代码后台应该尽量精简
 * 大部分逻辑应该放在 ViewModel 中
 * 代码后台只负责：
 * 1. 初始化组件
 * 2. 设置 DataContext
 * 3. 处理一些纯 UI 相关的逻辑（如动画、焦点等）
 */

using System.Windows;
using PlaylistMaker.WPF.ViewModels;

namespace PlaylistMaker.WPF.Views;

/// <summary>
/// 主窗口类，继承自 WPF 的 Window 类
/// partial 关键字表示这个类的定义分散在多个文件中（.xaml 和 .xaml.cs）
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// 构造函数，通过依赖注入接收 ViewModel
    /// 
    /// 依赖注入的好处：
    /// 1. 窗口不需要知道如何创建 ViewModel
    /// 2. ViewModel 可以轻松替换为模拟对象进行测试
    /// 3. ViewModel 的依赖也会被自动注入
    /// </summary>
    /// <param name="viewModel">由依赖注入容器自动提供的视图模型实例</param>
    public MainWindow(MainWindowViewModel viewModel)
    {
        // InitializeComponent 是自动生成的方法
        // 它负责解析 XAML 文件并创建所有 UI 元素
        InitializeComponent();
        
        // DataContext 是 WPF 数据绑定的核心
        // 设置 DataContext 后，XAML 中的 {Binding} 表达式会从这个对象查找属性
        DataContext = viewModel;
    }
}
