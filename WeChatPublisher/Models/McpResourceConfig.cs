using SqlSugar;

namespace WeChatPublisher.Models;

[SugarTable("McpResourceConfigs")]
public class McpResourceConfig
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string ResourceType { get; set; } = "LocalFolder";
    public string Path { get; set; } = "";
    public string? FileFilter { get; set; } = "*.*";
    public string? Description { get; set; }
    public string? LastCachedAt { get; set; }
    public int CachedFileCount { get; set; }
    public int IsHealthy { get; set; }
    public string? LastHealthCheck { get; set; }
    public string? HealthMessage { get; set; }
    public string CreatedAt { get; set; } = "";
    public string UpdatedAt { get; set; } = "";

    [SugarColumn(IsIgnore = true)]
    public string TypeDisplay => ResourceType switch
    {
        "LocalFolder" => $"📁 本地文件夹",
        "NetworkShare" => $"🖥 网络共享",
        "WebUrl" => $"🌐 网页",
        "RssFeed" => $"📡 RSS订阅",
        _ => ResourceType
    };

    [SugarColumn(IsIgnore = true)]
    public string HealthDisplay => IsHealthy == 1 ? "●" : "○";
}

[SugarTable("McpCacheEntries")]
public class McpCacheEntry
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }
    public int ResourceId { get; set; }
    public string FilePath { get; set; } = "";
    public string FileName { get; set; } = "";
    public long FileSize { get; set; }
    public string LastModified { get; set; } = "";
    public string? ContentSample { get; set; }
    public string? AiSummary { get; set; }
    public int IsProcessed { get; set; }
    public string CachedAt { get; set; } = "";
}
