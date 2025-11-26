/*
 * PlaylistGenerationService.cs 是播放列表生成服务
 * 
 * 这是一个典型的服务类，负责具体的业务逻辑
 * 它与 UI 完全隔离，可以独立测试
 * 
 * 服务层的职责：
 * 1. 实现具体的业务逻辑
 * 2. 与外部资源交互（文件系统、网络等）
 * 3. 数据转换和处理
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using PlaylistMaker.WPF.Models;

namespace PlaylistMaker.WPF.Services;

/// <summary>
/// 播放列表生成服务的具体实现
/// sealed 关键字表示这个类不能被继承
/// </summary>
public sealed class PlaylistGenerationService : IPlaylistGenerationService
{
    // XML 命名空间常量
    // XSPF 是一种标准的播放列表格式
    private static readonly XNamespace Xspf = "http://xspf.org/ns/0/";
    // VLC 播放器的扩展命名空间
    private static readonly XNamespace Vlc = "http://www.videolan.org/vlc/playlist/ns/0/";
    
    private readonly ILogger<PlaylistGenerationService> _logger;

    /// <summary>
    /// 构造函数，注入日志服务
    /// </summary>
    public PlaylistGenerationService(ILogger<PlaylistGenerationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 异步生成播放列表
    /// </summary>
    /// <param name="request">生成请求参数</param>
    /// <param name="cancellationToken">取消令牌，用于支持取消操作</param>
    /// <returns>生成结果</returns>
    public Task<PlaylistGenerationResult> GenerateAsync(
        PlaylistGenerationRequest request, 
        CancellationToken cancellationToken)
        // Task.Run 将同步方法包装为异步任务，在线程池中执行
        // 这样不会阻塞 UI 线程
        => Task.Run(() => GenerateCore(request, cancellationToken), cancellationToken);

    /// <summary>
    /// 核心生成逻辑（同步方法）
    /// </summary>
    private PlaylistGenerationResult GenerateCore(
        PlaylistGenerationRequest request, 
        CancellationToken cancellationToken)
    {
        // 验证文件夹路径
        if (string.IsNullOrWhiteSpace(request.FolderPath) || !Directory.Exists(request.FolderPath))
        {
            throw new DirectoryNotFoundException("指定的媒体文件夹不存在。");
        }

        // 规范化扩展名并创建 HashSet 用于快速查找
        // HashSet 的查找时间复杂度是 O(1)，比 List 的 O(n) 快得多
        var extensions = new HashSet<string>(
            request.Extensions.Select(NormalizeExtension).Where(e => !string.IsNullOrWhiteSpace(e)),
            StringComparer.OrdinalIgnoreCase);  // 忽略大小写

        if (extensions.Count == 0)
        {
            throw new InvalidOperationException("请至少提供一个媒体文件扩展名。");
        }

        // 确定搜索选项
        var searchOption = request.IncludeSubdirectories 
            ? SearchOption.AllDirectories    // 包含子目录
            : SearchOption.TopDirectoryOnly; // 仅当前目录

        // 枚举所有匹配的文件
        // EnumerateFiles 是惰性枚举，比 GetFiles 更节省内存
        var files = Directory.EnumerateFiles(request.FolderPath, "*.*", searchOption)
            .Where(f => extensions.Contains(Path.GetExtension(f)))  // 过滤扩展名
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)       // 按路径排序
            .ToList();  // 转换为列表以便多次遍历

        // 检查取消请求
        // CancellationToken 是 .NET 中取消异步操作的标准机制
        cancellationToken.ThrowIfCancellationRequested();

        if (files.Count == 0)
        {
            throw new InvalidOperationException("没有找到符合条件的媒体文件。");
        }

        // 构建 XSPF 播放列表的轨道列表
        var trackList = new XElement(Xspf + "trackList");
        int trackId = 0;

        foreach (var file in files)
        {
            // 检查取消请求
            cancellationToken.ThrowIfCancellationRequested();
            
            // 将文件路径转换为 URI 格式
            // URL 编码确保特殊字符（如空格、中文）被正确处理
            var encodedPath = "file:///" + WebUtility.UrlEncode(file.Replace("\\", "/"))
                .Replace("%3A", ":");  // 保留驱动器盘符的冒号

            // 创建轨道元素
            var track = new XElement(Xspf + "track",
                new XElement(Xspf + "location", encodedPath),
                new XElement(Xspf + "extension",
                    new XAttribute("application", "http://www.videolan.org/vlc/playlist/0"),
                    new XElement(Vlc + "id", trackId),
                    new XElement(Vlc + "option", "file-caching=1000")));  // VLC 缓存设置

            trackList.Add(track);
            trackId++;
        }

        // 创建 VLC 扩展元素，包含所有轨道的引用
        var vlcExtension = new XElement(Xspf + "extension",
            new XAttribute("application", "http://www.videolan.org/vlc/playlist/0"),
            files.Select((_, index) => new XElement(Vlc + "item", new XAttribute("tid", index))));

        // 创建根元素 playlist
        var playlist = new XElement(Xspf + "playlist",
            new XAttribute("version", "1"),
            new XAttribute(XNamespace.Xmlns + "vlc", Vlc),  // 声明 VLC 命名空间
            new XElement(Xspf + "title", 
                string.IsNullOrWhiteSpace(request.PlaylistTitle) ? "播放列表" : request.PlaylistTitle),
            trackList,
            vlcExtension);

        // 生成安全的文件名
        var safeName = SanitizeFileName(
            string.IsNullOrWhiteSpace(request.PlaylistTitle) ? "playlist_vlc" : request.PlaylistTitle);
        var outputPath = Path.Combine(request.FolderPath, $"{safeName}.xspf");

        // 配置 XML 写入设置
        var xmlSettings = new XmlWriterSettings
        {
            Indent = true,  // 格式化输出，便于阅读
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)  // UTF-8 无 BOM
        };

        // 写入文件
        using (var writer = XmlWriter.Create(outputPath, xmlSettings))
        {
            playlist.Save(writer);
        }

        // 记录日志
        _logger.LogInformation("生成播放列表成功，共 {Count} 个条目，输出 {Output}", files.Count, outputPath);
        
        return new PlaylistGenerationResult(files.Count, outputPath);
    }

    /// <summary>
    /// 规范化扩展名，确保以点号开头并小写
    /// </summary>
    private static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return string.Empty;
        }

        extension = extension.Trim();
        if (!extension.StartsWith(".", StringComparison.Ordinal))
        {
            extension = "." + extension;
        }

        return extension.ToLowerInvariant();
    }

    /// <summary>
    /// 清理文件名中的非法字符
    /// </summary>
    private static string SanitizeFileName(string name)
    {
        // Path.GetInvalidFileNameChars() 返回文件名中不允许的字符
        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(invalid, '_');
        }

        return string.IsNullOrWhiteSpace(name) ? "playlist_vlc" : name.Trim();
    }
}
