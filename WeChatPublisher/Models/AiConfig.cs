using SqlSugar;

namespace WeChatPublisher.Models;

[SugarTable("AiConfigs")]
public class AiConfig
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }
    public string ProviderType { get; set; } = "TextGeneration";
    public string ProviderName { get; set; } = "DeepSeek";
    public string BaseUrl { get; set; } = "";
    [SugarColumn(IsNullable = true)] public string? ApiKeyEncrypted { get; set; }
    public string ModelName { get; set; } = "";
    public int DefaultMaxTokens { get; set; } = 4096;
    public double DefaultTemperature { get; set; } = 0.7;
    [SugarColumn(IsNullable = true)] public string? SystemPrompt { get; set; }
    [SugarColumn(IsNullable = true)] public string? ExtraHeaders { get; set; }
    public int IsActive { get; set; }
    public int IsHealthy { get; set; }
    [SugarColumn(IsNullable = true)] public string? LastHealthCheck { get; set; }
    [SugarColumn(IsNullable = true)] public string? HealthMessage { get; set; }
    public string CreatedAt { get; set; } = "";
    public string UpdatedAt { get; set; } = "";
}
