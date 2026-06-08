using System.Text.Json;
using WeChatPublisher.Models;

namespace WeChatPublisher.Services;

public class McpService
{
    private readonly AppSettings _settings;

    public McpService() { _settings = AppSettings.Instance; }

    public List<McpResourceConfig> GetAllResources() => _settings.GetMcpResources();
    public void SaveResource(McpResourceConfig config) => _settings.SaveMcpResource(config);
    public void DeleteResource(int id) => _settings.DeleteMcpResource(id);

    // ========== 智能采样（带缓存） ==========
    public List<FileSample> SampleFiles(int resourceId, int maxFiles = 20, int sampleChars = 500,
        bool bypassCache = false)
    {
        var resource = GetAllResources().FirstOrDefault(r => r.Id == resourceId);
        if (resource == null) return [];

        return resource.ResourceType switch
        {
            "LocalFolder" or "NetworkShare" => SampleLocalFiles(resource, maxFiles, sampleChars, bypassCache),
            "WebUrl" => SampleWebUrl(resource, sampleChars),
            "RssFeed" => SampleRssFeed(resource, maxFiles, sampleChars),
            _ => []
        };
    }

    // ========== 本地文件采样 + 缓存 ==========
    private List<FileSample> SampleLocalFiles(McpResourceConfig resource, int maxFiles, int sampleChars,
        bool bypassCache = false)
    {
        if (!Directory.Exists(resource.Path)) return [];

        var filter = string.IsNullOrWhiteSpace(resource.FileFilter) ? "*.*" : resource.FileFilter;
        var allFiles = SafeEnumerateFiles(resource.Path, filter, maxFiles * 10);

        var supported = allFiles
            .Where(f =>
            {
                var ext = Path.GetExtension(f).ToLowerInvariant();
                return ext is ".docx" or ".txt" or ".md" or ".pdf" or ".html" or ".htm"
                    or ".jpg" or ".jpeg" or ".png" or ".webp" or ".bmp" or ".gif";
            })
            .ToList();

        // 随机打乱，避免每次返回同样的文件
        var rng = new Random();
        supported = supported.OrderBy(_ => rng.Next()).Take(maxFiles).ToList();

        // 读取缓存（bypassCache 时跳过缓存，总是重新读取）
        var cached = bypassCache ? [] : _settings.Db.ExecuteInScope(db =>
            db.Queryable<McpCacheEntry>()
              .Where(c => c.ResourceId == resource.Id)
              .ToList());

        var cacheDict = cached.ToDictionary(c => c.FilePath, c => c);
        var samples = new List<FileSample>();
        var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var newCache = new List<McpCacheEntry>();

        foreach (var file in supported)
        {
            var fileInfo = new FileInfo(file);
            var lastMod = fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");
            var size = fileInfo.Length;

            // 缓存命中：文件未变化（且未跳过缓存）
            if (!bypassCache
                && cacheDict.TryGetValue(file, out var entry)
                && entry.FileSize == size
                && entry.LastModified == lastMod
                && entry.ContentSample != null)
            {
                samples.Add(new FileSample
                {
                    FilePath = file,
                    FileName = Path.GetFileName(file),
                    Content = entry.ContentSample,
                    FullContent = entry.AiSummary ?? entry.ContentSample,
                    IsCached = true
                });
                continue;
            }

            // 缓存未命中：读取文件
            var content = ReadFileContent(file);
            if (string.IsNullOrWhiteSpace(content)) continue;

            var sample = content.Length > sampleChars ? content[..sampleChars] + "..." : content;
            samples.Add(new FileSample
            {
                FilePath = file,
                FileName = Path.GetFileName(file),
                Content = sample,
                FullContent = content,
                IsCached = false
            });

            newCache.Add(new McpCacheEntry
            {
                ResourceId = resource.Id,
                FilePath = file,
                FileName = Path.GetFileName(file),
                FileSize = size,
                LastModified = lastMod,
                ContentSample = sample,
                CachedAt = now
            });
        }

        // 更新缓存
        if (newCache.Count > 0)
        {
            _settings.Db.ExecuteInScope(db =>
            {
                foreach (var entry in newCache)
                {
                    var existing = db.Queryable<McpCacheEntry>()
                        .First(c => c.ResourceId == entry.ResourceId && c.FilePath == entry.FilePath);
                    if (existing != null)
                    {
                        existing.ContentSample = entry.ContentSample;
                        existing.FileSize = entry.FileSize;
                        existing.LastModified = entry.LastModified;
                        existing.CachedAt = now;
                        db.Updateable(existing).ExecuteCommand();
                    }
                    else
                    {
                        db.Insertable(entry).ExecuteCommand();
                    }
                }
            });

            // 更新资源缓存状态
            resource.LastCachedAt = now;
            resource.CachedFileCount = _settings.Db.ExecuteInScope(db =>
                db.Queryable<McpCacheEntry>().Count(c => c.ResourceId == resource.Id));
            _settings.SaveMcpResource(resource);
        }

        return samples;
    }

    // ========== 网页采样 ==========
    private static List<FileSample> SampleWebUrl(McpResourceConfig resource, int sampleChars)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            var html = http.GetStringAsync(resource.Path).Result;
            var text = StripHtml(html);
            if (string.IsNullOrWhiteSpace(text)) return [];
            return [new FileSample
            {
                FilePath = resource.Path,
                FileName = resource.Name,
                Content = text.Length > sampleChars ? text[..sampleChars] + "..." : text,
                FullContent = text,
                IsCached = false
            }];
        }
        catch { return []; }
    }

    // ========== RSS采样 ==========
    private static List<FileSample> SampleRssFeed(McpResourceConfig resource, int maxFiles, int sampleChars)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            var xml = http.GetStringAsync(resource.Path).Result;
            var items = ParseRssItems(xml).Take(maxFiles).ToList();

            return items.Select(item => new FileSample
            {
                FilePath = item.Link ?? resource.Path,
                FileName = item.Title ?? "RSS Item",
                Content = (item.Title + "\n" + (item.Description ?? "")).Length > sampleChars
                    ? (item.Title + "\n" + (item.Description ?? ""))[..sampleChars] + "..." : item.Title + "\n" + (item.Description ?? ""),
                FullContent = item.Title + "\n" + (item.Description ?? ""),
                IsCached = false
            }).ToList();
        }
        catch { return []; }
    }

    // ========== 工具方法 ==========
    private static readonly string[] DocxPasswords = ["43404", "0314", "12000", "144000"];

    private static Xceed.Words.NET.DocX? TryOpenDocx(string filePath)
    {
        try { return Xceed.Words.NET.DocX.Load(filePath); }
        catch
        {
            foreach (var pw in DocxPasswords)
            {
                // TODO: Upgrade Xceed.Words.NET to >=5.1 for password-protected docx
                try { return Xceed.Words.NET.DocX.Load(filePath); }
                catch { }
            }
            return null;
        }
    }

    private static string? ReadFileContent(string filePath)
    {
        try
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            if (ext == ".docx")
            {
                using var doc = TryOpenDocx(filePath);
                if (doc == null) return null;
                return string.Join("\n", doc.Paragraphs.Select(p => p.Text).Where(t => !string.IsNullOrWhiteSpace(t)));
            }
            if (ext is ".txt" or ".md" or ".html" or ".htm")
                return File.ReadAllText(filePath);
            return null;
        }
        catch { return null; }
    }

    private static string StripHtml(string html)
    {
        var text = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
        return text;
    }

    private static List<(string? Title, string? Description, string? Link)> ParseRssItems(string xml)
    {
        var items = new List<(string?, string?, string?)>();
        try
        {
            var doc = System.Xml.Linq.XDocument.Parse(xml);
            foreach (var item in doc.Descendants("item"))
            {
                items.Add((
                    item.Element("title")?.Value,
                    item.Element("description")?.Value,
                    item.Element("link")?.Value
                ));
            }
        }
        catch { }
        return items;
    }

    private static readonly HashSet<string> _skipFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "$RECYCLE.BIN", "System Volume Information", "Windows", "Program Files",
        "Program Files (x86)", "ProgramData", "Recovery", "Config.Msi",
        "node_modules", ".git", "obj", "bin", ".vs"
    };

    private static List<string> SafeEnumerateFiles(string root, string filter, int maxFiles)
    {
        var results = new List<string>();
        var patterns = filter.Split(';', StringSplitOptions.RemoveEmptyEntries);

        try
        {
            var dirs = new Queue<string>();
            dirs.Enqueue(root);

            while (dirs.Count > 0 && results.Count < maxFiles * 3)
            {
                var dir = dirs.Dequeue();
                var dirName = Path.GetFileName(dir);

                if (_skipFolders.Contains(dirName) || (dirName.StartsWith("$") && dirName.Length > 1))
                    continue;

                const int maxPerDir = 3;
                try
                {
                    foreach (var pattern in patterns)
                    {
                        try
                        {
                            var files = Directory.GetFiles(dir, pattern.Trim(),
                                SearchOption.TopDirectoryOnly);
                            // 每个目录最多取maxPerDir个文件，确保全盘分布
                            results.AddRange(files.Take(maxPerDir));
                        }
                        catch (UnauthorizedAccessException) { }
                        catch (DirectoryNotFoundException) { }
                    }

                    foreach (var sub in Directory.GetDirectories(dir))
                    {
                        try { dirs.Enqueue(sub); }
                        catch (UnauthorizedAccessException) { }
                        catch (DirectoryNotFoundException) { }
                    }
                }
                catch (UnauthorizedAccessException) { }
                catch (DirectoryNotFoundException) { }
            }
        }
        catch (UnauthorizedAccessException) { }
        catch (DirectoryNotFoundException) { }

        return results;
    }

    public string ReadTextFile(string filePath) => File.ReadAllText(filePath);

    public List<string> PickRandomImages(int resourceId, int count)
    {
        var resource = GetAllResources().FirstOrDefault(r => r.Id == resourceId);
        if (resource == null || !Directory.Exists(resource.Path)) return [];

        var files = Directory.GetFiles(resource.Path, "*.*", SearchOption.AllDirectories)
            .Where(f =>
            {
                var ext = Path.GetExtension(f).ToLowerInvariant();
                return ext is ".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif";
            })
            .ToList();

        var rng = new Random();
        return files.OrderBy(_ => rng.Next()).Take(Math.Min(count, files.Count)).ToList();
    }

    // ========== 健康检查 ==========
    public bool CheckHealth(McpResourceConfig resource)
    {
        try
        {
            return resource.ResourceType switch
            {
                "LocalFolder" or "NetworkShare" => Directory.Exists(resource.Path),
                "WebUrl" or "RssFeed" =>
                    Uri.TryCreate(resource.Path, UriKind.Absolute, out var uri)
                    && (uri.Scheme == "http" || uri.Scheme == "https"),
                _ => false
            };
        }
        catch { return false; }
    }

    public void UpdateHealth(int resourceId)
    {
        var resource = GetAllResources().FirstOrDefault(r => r.Id == resourceId);
        if (resource == null) return;

        resource.IsHealthy = CheckHealth(resource) ? 1 : 0;
        resource.HealthMessage = resource.IsHealthy == 1 ? "可访问" : "不可访问";
        resource.LastHealthCheck = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        _settings.SaveMcpResource(resource);
    }

    public List<ParsedDocx> ReadDocxFiles(int resourceId, int? maxCount = null)
    {
        var resource = GetAllResources().FirstOrDefault(r => r.Id == resourceId);
        if (resource == null || !Directory.Exists(resource.Path)) return [];

        var files = Directory.GetFiles(resource.Path, "*.docx", SearchOption.AllDirectories).ToList();
        if (maxCount.HasValue && files.Count > maxCount.Value)
            files = files.Take(maxCount.Value).ToList();

        return files.Select(f =>
        {
            try { return DocxTemplateService.ParseArticleDocx(f); }
            catch { return null; }
        }).Where(r => r != null).ToList()!;
    }
}

public class FileSample
{
    public string FilePath { get; set; } = "";
    public string FileName { get; set; } = "";
    public string Content { get; set; } = "";
    public string FullContent { get; set; } = "";
    public bool IsCached { get; set; }
}

public class ParsedDocx
{
    public string FilePath { get; set; } = "";
    public string FileName { get; set; } = "";
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Verse { get; set; }
    public string? Content { get; set; }
    public string? Tags { get; set; }
}

public class DocxTemplateService
{
    public static void FillTemplate(string templatePath, string outputPath,
        Dictionary<string, string> replacements)
    {
        using var doc = Xceed.Words.NET.DocX.Load(templatePath);
        foreach (var (key, value) in replacements)
        {
#pragma warning disable CS0618
            doc.ReplaceText(key, value);
#pragma warning restore CS0618
        }
        doc.SaveAs(outputPath);
    }

    private static readonly string[] DocxPasswords = ["43404", "0314", "12000", "144000"];

    private static Xceed.Words.NET.DocX? TryOpenDocx(string filePath)
    {
        try { return Xceed.Words.NET.DocX.Load(filePath); }
        catch
        {
            foreach (var pw in DocxPasswords)
            {
                // TODO: Upgrade Xceed.Words.NET to >=5.1 for password-protected docx
                try { return Xceed.Words.NET.DocX.Load(filePath); }
                catch { }
            }
            return null;
        }
    }

    public static ParsedDocx? ParseArticleDocx(string filePath)
    {
        try
        {
            using var doc = TryOpenDocx(filePath);
            if (doc == null) return null;
            var paragraphs = doc.Paragraphs.Select(p => p.Text.Trim())
                .Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
            var text = string.Join("\n", paragraphs);

            var match = System.Text.RegularExpressions.Regex.Match(text,
                @"^标题：(.*)\n简介：(.*)\n(.*)\n([\s\S]*?)\n#(.+)$");
            if (!match.Success) return null;

            return new ParsedDocx
            {
                FilePath = filePath, FileName = Path.GetFileName(filePath),
                Title = match.Groups[1].Value, Description = match.Groups[2].Value,
                Verse = match.Groups[3].Value, Content = match.Groups[4].Value,
                Tags = match.Groups[5].Value
            };
        }
        catch { return null; }
    }
}
