using WeChatPublisher.Services;

namespace WeChatPublisher.Views;

public static class MaterialLoader
{
    public static async Task<List<SelectableItem>> LoadMaterialsAsync(McpService mcp, int count = 30)
    {
        var items = new List<SelectableItem>();
        await Task.Run(() =>
        {
            var resources = mcp.GetAllResources()
                .Where(r => r.ResourceType is "LocalFolder" or "HttpMcp" or "RssFeed").ToList();
            foreach (var res in resources)
            {
                var samples = mcp.SampleFiles(res.Id, count, 300, bypassCache: true);
                var icon = res.ResourceType switch { "HttpMcp" => "🌐", "RssFeed" => "📡", _ => "📁" };
                var stype = res.ResourceType switch { "HttpMcp" => "mcp", "RssFeed" => "rss", _ => "file" };
                foreach (var s in samples)
                {
                    var preview = s.Content.Replace('\n', ' ').Replace('\r', ' ').Replace("  ", " ").Trim();
                    if (preview.Length > 55) preview = preview[..55] + "...";
                    items.Add(new SelectableItem
                    {
                        Display = preview, Data = s.FullContent, SourceType = stype,
                        SourceLabel = res.Name,
                        SourceDetail = $"{res.Name}\n路径: {s.FilePath}\n类型: {res.ResourceType}",
                        SourceIcon = icon, IsSelected = false
                    });
                }
            }
        });
        return items;
    }

    public static async Task<List<SelectableItem>> LoadVersesAsync(string bibleDbPath, int count = 30)
    {
        var items = new List<SelectableItem>();
        if (!File.Exists(bibleDbPath)) return items;
        await Task.Run(() =>
        {
            var bible = new BibleService(bibleDbPath);
            for (int i = 0; i < count; i++)
            {
                try
                {
                    var passage = bible.GetRandomPassage();
                    if (passage.Length > 10)
                    {
                        var clean = passage.Replace('\n', ' ').Replace('\r', ' ').Replace("  ", " ").Trim();
                        items.Add(new SelectableItem
                        {
                            Display = clean.Length > 60 ? clean[..60] + "..." : clean,
                            Data = passage, SourceType = "bible",
                            SourceLabel = "圣经", SourceDetail = "来自: 圣经数据库", SourceIcon = "📖",
                            IsSelected = false
                        });
                    }
                }
                catch { }
            }
        });
        return items;
    }
}
