/*
 * App.xaml.cs 是应用程序的入口点和生命周期管理类
 * 
 * 这个文件负责：
 * 1. 配置依赖注入容器（Dependency Injection）
 * 2. 启动和停止应用程序宿主（Host）
 * 3. 处理全局未处理异常
 * 4. 初始化服务和主窗口
 */

using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PlaylistMaker.WPF.Logging;
using PlaylistMaker.WPF.Services;
using PlaylistMaker.WPF.ViewModels;
using PlaylistMaker.WPF.Views;

// 使用别名解决命名冲突：System.Windows.Application 和 System.Windows.Forms.Application
// 因为项目同时引用了 WPF 和 WinForms（用于文件夹选择对话框）
using WpfApplication = System.Windows.Application;

namespace PlaylistMaker.WPF;

/// <summary>
/// 应用程序主类，继承自 WPF 的 Application 类
/// partial 关键字表示这个类的定义分散在多个文件中（App.xaml 和 App.xaml.cs）
/// </summary>
public partial class App : WpfApplication
{
    /// <summary>
    /// 应用程序宿主，管理应用程序的生命周期和依赖注入
    /// static 表示这是一个静态属性，可以在任何地方通过 App.AppHost 访问
    /// null! 是 null-forgiving 运算符，告诉编译器这个值不会为 null
    /// </summary>
    public static IHost AppHost { get; private set; } = null!;
    
    /// <summary>
    /// 日志记录器，用于记录应用程序运行时的信息
    /// ILogger<T> 是泛型接口，T 表示日志的来源类
    /// </summary>
    private ILogger<App>? _logger;

    /// <summary>
    /// 构造函数：在应用程序启动时首先执行
    /// 这里配置依赖注入容器，注册所有需要的服务
    /// </summary>
    public App()
    {
        // 创建通用主机构建器
        // Host 是 .NET 提供的应用程序托管框架，提供依赖注入、配置、日志等功能
        var builder = Host.CreateApplicationBuilder();

        // 配置日志系统
        builder.Logging.ClearProviders();  // 清除默认的日志提供程序
        builder.Logging.AddDebug();         // 添加调试输出（在 Visual Studio 输出窗口显示）
        
        // 添加自定义的文件日志提供程序
        // Environment.SpecialFolder.LocalApplicationData 是用户的本地应用数据文件夹
        // 例如：C:\Users\用户名\AppData\Local\PlaylistMaker.WPF\logs
        builder.Logging.AddProvider(new FileLoggerProvider(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PlaylistMaker.WPF", "logs")));

        /*
         * 依赖注入（Dependency Injection, DI）注册
         * 
         * AddSingleton<T>：单例模式，整个应用程序只创建一个实例
         * AddTransient<T>：瞬态模式，每次请求都创建新实例
         * AddScoped<T>：作用域模式，在同一作用域内共享实例
         * 
         * 依赖注入的好处：
         * 1. 解耦：类不需要知道依赖的具体实现
         * 2. 可测试：可以轻松替换为模拟对象
         * 3. 可维护：集中管理对象的创建和生命周期
         */
        
        // 注册服务类（业务逻辑层）
        builder.Services.AddSingleton<SettingsService>();           // 设置管理服务
        builder.Services.AddSingleton<ThemeService>();              // 主题管理服务
        builder.Services.AddSingleton<IDialogService, DialogService>();  // 对话框服务（接口+实现）
        builder.Services.AddSingleton<IPlaylistGenerationService, PlaylistGenerationService>();  // 播放列表生成服务
        
        // 注册视图模型（MVVM 中的 ViewModel）
        builder.Services.AddSingleton<MainWindowViewModel>();
        
        // 注册视图（窗口）
        builder.Services.AddSingleton<MainWindow>();

        // 构建主机，完成依赖注入容器的配置
        AppHost = builder.Build();
    }

    /// <summary>
    /// 应用程序启动时调用
    /// override 表示重写父类的方法
    /// async 表示这是一个异步方法，可以使用 await
    /// </summary>
    protected override async void OnStartup(StartupEventArgs e)
    {
        // 调用父类的 OnStartup 方法，确保基础初始化完成
        base.OnStartup(e);
        
        // 启动主机，开始运行所有注册的服务
        await AppHost.StartAsync();

        // 从依赖注入容器获取日志记录器
        // GetRequiredService<T> 会抛出异常如果服务未注册
        // GetService<T> 返回 null 如果服务未注册
        _logger = AppHost.Services.GetRequiredService<ILogger<App>>();
        
        // 订阅未处理异常事件，用于全局异常处理
        // += 是事件订阅语法，将方法绑定到事件
        DispatcherUnhandledException += OnDispatcherUnhandledException;  // UI 线程异常
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;  // 非 UI 线程异常

        // 加载用户设置
        var settings = AppHost.Services.GetRequiredService<SettingsService>();
        settings.Load();

        // 应用主题
        var themeService = AppHost.Services.GetRequiredService<ThemeService>();
        themeService.ApplyTheme(settings.Current.ThemeVariant);

        // 获取主窗口并显示
        var mainWindow = AppHost.Services.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;  // 设置为应用程序的主窗口
        mainWindow.Show();        // 显示窗口
    }

    /// <summary>
    /// 应用程序退出时调用
    /// 用于清理资源、保存状态等
    /// </summary>
    protected override async void OnExit(ExitEventArgs e)
    {
        // 取消事件订阅，防止内存泄漏
        // -= 是取消事件订阅的语法
        DispatcherUnhandledException -= OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException -= OnDomainUnhandledException;

        // 停止并释放主机
        if (AppHost is not null)
        {
            await AppHost.StopAsync();  // 优雅地停止所有服务
            AppHost.Dispose();          // 释放资源
        }

        base.OnExit(e);
    }

    /// <summary>
    /// 处理 UI 线程上的未处理异常
    /// Dispatcher 是 WPF 的 UI 线程调度器
    /// </summary>
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // 记录严重错误日志
        _logger?.LogCritical(e.Exception, "Unhandled UI exception");
        
        // 标记异常已处理，防止应用程序崩溃
        e.Handled = true;
        
        // 显示错误提示给用户
        System.Windows.MessageBox.Show(
            "应用发生未知错误，详细信息已写入日志。", 
            "播放列表工厂", 
            MessageBoxButton.OK, 
            MessageBoxImage.Error);
        
        // 关闭应用程序，-1 表示异常退出码
        Shutdown(-1);
    }

    /// <summary>
    /// 处理非 UI 线程的未处理异常
    /// 例如后台任务、线程池线程等
    /// </summary>
    private void OnDomainUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        // 尝试将异常对象转换为 Exception 类型
        if (e.ExceptionObject is Exception ex)
        {
            _logger?.LogCritical(ex, "Unhandled domain exception");
        }
    }
}
