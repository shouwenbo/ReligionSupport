using WeChatPublisher.Models;

namespace WeChatPublisher.Services;

public class McpService
{
    private readonly AppSettings _settings;

    public McpService()
    {
        _settings = AppSettings.Instance;
    }

    public List<McpResourceConfig> GetAllResources()
        => _settings.GetMcpResources();

    public void SaveResource(McpResourceConfig config)
        => _settings.SaveMcpResource(config);

    public void DeleteResource(int id)
        => _settings.DeleteMcpResource(id);

    public List<string> ListFiles(int resourceId)
    {
        var resources = _settings.GetMcpResources();
        var resource = resources.FirstOrDefault(r => r.Id == resourceId)
            ?? throw new InvalidOperationException("资源不存在");

        if (!Directory.Exists(resource.Path))
            return [];

        var filter = string.IsNullOrWhiteSpace(resource.FileFilter) ? "*.*" : resource.FileFilter;
        var patterns = filter.Split(';', StringSplitOptions.RemoveEmptyEntries);
        var files = new List<string>();

        foreach (var pattern in patterns)
        {
            files.AddRange(Directory.GetFiles(resource.Path, pattern.Trim(),
                SearchOption.AllDirectories));
        }

        return files.Distinct().OrderBy(f => f).ToList();
    }

    public List<ParsedDocx> ReadDocxFiles(int resourceId, int? maxCount = null)
    {
        var files = ListFiles(resourceId).Where(f => f.EndsWith(".docx", StringComparison.OrdinalIgnoreCase)).ToList();
        if (maxCount.HasValue && files.Count > maxCount.Value)
            files = files.Take(maxCount.Value).ToList();

        var results = new List<ParsedDocx>();
        foreach (var file in files)
        {
            try
            {
                var parsed = DocxTemplateService.ParseArticleDocx(file);
                if (parsed != null) results.Add(parsed);
            }
            catch { }
        }
        return results;
    }

    public string ReadTextFile(string filePath)
        => File.ReadAllText(filePath);

    public List<string> PickRandomImages(int resourceId, int count)
    {
        var files = ListFiles(resourceId)
            .Where(f =>
            {
                var ext = Path.GetExtension(f).ToLowerInvariant();
                return ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".gif";
            })
            .ToList();

        var rng = new Random();
        return files.OrderBy(_ => rng.Next()).Take(Math.Min(count, files.Count)).ToList();
    }
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

    public static ParsedDocx? ParseArticleDocx(string filePath)
    {
        try
        {
            using var doc = Xceed.Words.NET.DocX.Load(filePath);
            var paragraphs = doc.Paragraphs.Select(p => p.Text.Trim())
                .Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
            var text = string.Join("\n", paragraphs);

            var match = System.Text.RegularExpressions.Regex.Match(text,
                @"^标题：(.*)\n简介：(.*)\n(.*)\n([\s\S]*?)\n#(.+)$");

            if (!match.Success) return null;

            return new ParsedDocx
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                Title = match.Groups[1].Value,
                Description = match.Groups[2].Value,
                Verse = match.Groups[3].Value,
                Content = match.Groups[4].Value,
                Tags = match.Groups[5].Value
            };
        }
        catch
        {
            return null;
        }
    }
}
