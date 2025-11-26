/*
 * MainWindowViewModel.cs 是主窗口的视图模型
 * 
 * ViewModel 是 MVVM 模式的核心，它负责：
 * 1. 保存 UI 状态（属性）
 * 2. 提供数据（属性）
 * 3. 处理用户交互（命令）
 * 4. 与服务层交互（调用 Service）
 * 
 * ViewModel 不应该直接引用 View（窗口、控件等）
 * 这样可以实现 UI 和逻辑的完全分离
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PlaylistMaker.WPF.Models;
using PlaylistMaker.WPF.Services;

namespace PlaylistMaker.WPF.ViewModels;

/// <summary>
/// 主窗口视图模型
/// 
/// ObservableObject 是 CommunityToolkit.Mvvm 提供的基类
/// 它实现了 INotifyPropertyChanged 接口，用于通知 UI 属性已更改
/// 
/// partial 关键字是因为使用了源代码生成器
/// [ObservableProperty] 特性会自动生成属性的完整代码
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    // 私有字段，存储依赖的服务
    // readonly 表示字段只能在构造函数中赋值，之后不能修改
    private readonly IPlaylistGenerationService _playlistService;
    private readonly SettingsService _settingsService;
    private readonly IDialogService _dialogService;
    private readonly ThemeService _themeService;
    private readonly ILogger<MainWindowViewModel> _logger;
    
    // 用于取消正在进行的操作
    private CancellationTokenSource? _generationCts;

    /// <summary>
    /// 构造函数，通过依赖注入接收所有需要的服务
    /// </summary>
    public MainWindowViewModel(
        IPlaylistGenerationService playlistService,
        SettingsService settingsService,
        IDialogService dialogService,
        ThemeService themeService,
        ILogger<MainWindowViewModel> logger)
    {
        // 保存依赖到私有字段
        _playlistService = playlistService;
        _settingsService = settingsService;
        _dialogService = dialogService;
        _themeService = themeService;
        _logger = logger;

        // 初始化可观察集合
        // ObservableCollection<T> 在增删项目时会自动通知 UI 更新
        ActivityLog = new ObservableCollection<string>();
        
        // 从主题服务获取可用的主题选项
        ThemeOptions = themeService.ThemeOptions;

        // 从设置服务加载保存的设置
        var snapshot = settingsService.Current;
        
        // 初始化属性（小写字段名对应 [ObservableProperty] 生成的属性）
        folderPath = snapshot.LastScanFolder;
        extensionsText = snapshot.Extensions;
        includeSubdirectories = snapshot.IncludeSubdirectories;
        autoOpenAfterExport = snapshot.AutoOpenAfterExport;
        playlistTitle = snapshot.PlaylistTitle;

        // 初始化选中的主题，确保值在可选列表中
        var initialTheme = ThemeOptions.FirstOrDefault(
            theme => theme.Equals(snapshot.ThemeVariant, StringComparison.OrdinalIgnoreCase));
        selectedTheme = initialTheme ?? ThemeOptions.FirstOrDefault() ?? "MicaLight";
    }

    /// <summary>
    /// 活动日志集合，用于在 UI 中显示操作记录
    /// </summary>
    public ObservableCollection<string> ActivityLog { get; }

    /// <summary>
    /// 可用的主题选项列表
    /// </summary>
    public IReadOnlyList<string> ThemeOptions { get; }

    /*
     * [ObservableProperty] 是 CommunityToolkit.Mvvm 提供的特性
     * 它会自动生成完整的属性代码，包括：
     * 1. 公共属性（首字母大写）
     * 2. PropertyChanged 通知
     * 3. 可选的属性变化回调方法 OnXxxChanged
     * 
     * 例如，下面的 folderPath 字段会生成一个 FolderPath 属性
     */
    
    /// <summary>
    /// 文件夹路径（字段，由源生成器生成对应的属性 FolderPath）
    /// </summary>
    [ObservableProperty]
    private string folderPath = string.Empty;

    /// <summary>
    /// 文件扩展名文本
    /// </summary>
    [ObservableProperty]
    private string extensionsText = ".mp3;.mp4";

    /// <summary>
    /// 播放列表标题
    /// </summary>
    [ObservableProperty]
    private string playlistTitle = "播放列表";

    /// <summary>
    /// 是否包含子目录
    /// </summary>
    [ObservableProperty]
    private bool includeSubdirectories = true;

    /// <summary>
    /// 导出后是否自动打开
    /// </summary>
    [ObservableProperty]
    private bool autoOpenAfterExport = true;

    /// <summary>
    /// 选中的主题
    /// </summary>
    [ObservableProperty]
    private string selectedTheme = "MicaLight";

    /// <summary>
    /// 是否正忙（用于显示加载指示器和禁用按钮）
    /// </summary>
    [ObservableProperty]
    private bool isBusy;

    /*
     * [RelayCommand] 是 CommunityToolkit.Mvvm 提供的特性
     * 它会自动生成 ICommand 实现，用于绑定到 XAML 中的按钮等控件
     * 
     * 方法名会去掉 Async 后缀并加上 Command 后缀
     * 例如：ClearLog() → ClearLogCommand
     */

    /// <summary>
    /// 清空日志命令
    /// </summary>
    [RelayCommand]
    private void ClearLog()
    {
        ActivityLog.Clear();
        AppendLog("🧹 日志已清空。");
    }

    /// <summary>
    /// 浏览文件夹命令
    /// </summary>
    [RelayCommand]
    private Task BrowseAsync()
    {
        // 调用对话框服务打开文件夹选择对话框
        var chosen = _dialogService.PickFolder(FolderPath);
        if (!string.IsNullOrWhiteSpace(chosen))
        {
            FolderPath = chosen;
            AppendLog($"📁 已选择：{chosen}");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 生成播放列表命令
    /// CanExecute 参数指定判断命令是否可执行的方法
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanGeneratePlaylist))]
    private async Task GenerateAsync()
    {
        // 验证输入
        if (string.IsNullOrWhiteSpace(FolderPath))
        {
            AppendLog("⚠️ 请先选择有效的文件夹。");
            return;
        }

        var extensions = GetExtensions().ToList();
        if (extensions.Count == 0)
        {
            AppendLog("⚠️ 请配置至少一个有效扩展名。");
            return;
        }

        // 设置忙碌状态
        IsBusy = true;
        
        // 取消之前的操作（如果有）
        _generationCts?.Cancel();
        // 创建新的取消令牌源
        _generationCts = new CancellationTokenSource();

        try
        {
            // 创建请求对象
            var request = new PlaylistGenerationRequest
            {
                FolderPath = FolderPath,
                Extensions = extensions,
                IncludeSubdirectories = IncludeSubdirectories,
                PlaylistTitle = PlaylistTitle
            };

            AppendLog("🎧 正在生成 VLC XSPF 播放列表...");
            
            // 异步调用服务生成播放列表
            var result = await _playlistService.GenerateAsync(request, _generationCts.Token);
            
            AppendLog($"✅ 已生成 {result.FileCount} 个条目：{result.OutputPath}");

            // 如果启用了自动打开，则打开生成的文件
            if (AutoOpenAfterExport && File.Exists(result.OutputPath))
            {
                // 使用系统默认程序打开文件
                Process.Start(new ProcessStartInfo(result.OutputPath) { UseShellExecute = true });
                AppendLog("🚀 已自动打开生成的播放列表。");
            }
        }
        catch (OperationCanceledException)
        {
            // 操作被取消
            AppendLog("⏹️ 操作已取消。");
        }
        catch (Exception ex)
        {
            // 记录错误日志并显示给用户
            _logger.LogError(ex, "生成播放列表失败。");
            AppendLog($"❌ 生成失败：{ex.Message}");
        }
        finally
        {
            // 无论成功或失败，都重置忙碌状态
            IsBusy = false;
        }
    }

    /// <summary>
    /// 判断生成命令是否可执行
    /// 当返回 false 时，绑定此命令的按钮会自动禁用
    /// </summary>
    private bool CanGeneratePlaylist()
        => !IsBusy && !string.IsNullOrWhiteSpace(FolderPath) && Directory.Exists(FolderPath);

    /// <summary>
    /// 解析扩展名文本，返回规范化的扩展名列表
    /// </summary>
    private IEnumerable<string> GetExtensions()
    {
        if (string.IsNullOrWhiteSpace(ExtensionsText))
        {
            return Enumerable.Empty<string>();
        }

        // 支持多种分隔符
        return ExtensionsText
            .Split(new[] { ',', ';', '\r', '\n', '\t', '|' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(ext =>
            {
                ext = ext.Trim();
                // 确保扩展名以点号开头
                if (!ext.StartsWith(".", StringComparison.Ordinal))
                {
                    ext = "." + ext;
                }
                return ext.ToLowerInvariant();
            })
            .Distinct(StringComparer.OrdinalIgnoreCase);  // 去重
    }

    /// <summary>
    /// 向活动日志添加一条记录
    /// 使用 Dispatcher 确保在 UI 线程上操作
    /// </summary>
    private void AppendLog(string message)
    {
        // 定义添加日志的操作
        void Append() => ActivityLog.Insert(0, $"{DateTime.Now:HH:mm:ss} {message}");

        // 检查是否在 UI 线程上
        if (System.Windows.Application.Current?.Dispatcher?.CheckAccess() == true)
        {
            // 如果在 UI 线程上，直接执行
            Append();
        }
        else
        {
            // 如果不在 UI 线程上，调度到 UI 线程执行
            System.Windows.Application.Current?.Dispatcher?.Invoke(Append);
        }
    }

    /*
     * partial void OnXxxChanged(T value) 方法
     * 
     * 这些方法由源生成器自动调用，在对应属性值改变后执行
     * 可以用于：
     * 1. 持久化设置
     * 2. 触发其他逻辑
     * 3. 更新相关命令的可执行状态
     */

    /// <summary>
    /// 当 FolderPath 属性改变时调用
    /// </summary>
    partial void OnFolderPathChanged(string value)
    {
        // 保存设置
        Persist(s => s.LastScanFolder = value ?? string.Empty);
        // 通知命令重新检查可执行状态
        GenerateCommand?.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// 当 ExtensionsText 属性改变时调用
    /// </summary>
    partial void OnExtensionsTextChanged(string value) 
        => Persist(s => s.Extensions = value ?? string.Empty);

    /// <summary>
    /// 当 IncludeSubdirectories 属性改变时调用
    /// </summary>
    partial void OnIncludeSubdirectoriesChanged(bool value) 
        => Persist(s => s.IncludeSubdirectories = value);

    /// <summary>
    /// 当 AutoOpenAfterExport 属性改变时调用
    /// </summary>
    partial void OnAutoOpenAfterExportChanged(bool value) 
        => Persist(s => s.AutoOpenAfterExport = value);

    /// <summary>
    /// 当 PlaylistTitle 属性改变时调用
    /// </summary>
    partial void OnPlaylistTitleChanged(string value) 
        => Persist(s => s.PlaylistTitle = string.IsNullOrWhiteSpace(value) ? "播放列表" : value);

    /// <summary>
    /// 当 SelectedTheme 属性改变时调用
    /// </summary>
    partial void OnSelectedThemeChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        // 应用新主题
        _themeService.ApplyTheme(value);
        // 保存设置
        Persist(s => s.ThemeVariant = value);
    }

    /// <summary>
    /// 当 IsBusy 属性改变时调用
    /// </summary>
    partial void OnIsBusyChanged(bool value) 
        => GenerateCommand?.NotifyCanExecuteChanged();

    /// <summary>
    /// 持久化设置的辅助方法
    /// </summary>
    /// <param name="apply">要应用的设置更改</param>
    private void Persist(Action<Properties.Settings> apply) 
        => _settingsService.Update(apply);
}
