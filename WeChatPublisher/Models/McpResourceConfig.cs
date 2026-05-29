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
    [SugarColumn(IsNullable = true)] public string? FileFilter { get; set; } = "*.*";
    [SugarColumn(IsNullable = true)] public string? Description { get; set; }
    [SugarColumn(IsNullable = true)] public string? LastCachedAt { get; set; }
    public int CachedFileCount { get; set; }
    public int IsHealthy { get; set; }
    [SugarColumn(IsNullable = true)] public string? LastHealthCheck { get; set; }
    [SugarColumn(IsNullable = true)] public string? HealthMessage { get; set; }
    public string CreatedAt { get; set; } = "";
    public string UpdatedAt { get; set; } = "";

    [SugarColumn(IsIgnore = true)]
    public string HealthDisplay => IsHealthy == 1 ? "OK" : "-";
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
    [SugarColumn(IsNullable = true)] public string? ContentSample { get; set; }
    [SugarColumn(IsNullable = true)] public string? AiSummary { get; set; }
    public int IsProcessed { get; set; }
    public string CachedAt { get; set; } = "";
}
