using SqlSugar;

namespace WeChatPublisher.Models;

[SugarTable("TtsApiConfigs")]
public class TtsApiConfig
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }
    public string ConfigType { get; set; } = "TTS";
    public string ApiType { get; set; } = "WebScrape";
    [SugarColumn(IsNullable = true)] public string? BaseUrl { get; set; }
    [SugarColumn(IsNullable = true)] public string? TokenFetchUrl { get; set; }
    [SugarColumn(IsNullable = true)] public string? TokenRegexPattern { get; set; }
    [SugarColumn(IsNullable = true)] public string? GenerateEndpoint { get; set; }
    public string SuccessCodeField { get; set; } = "code";
    public string SuccessCodeValue { get; set; } = "200";
    [SugarColumn(IsNullable = true)] public string? DefaultParamsJSON { get; set; }
    public int IsActive { get; set; }
    public int IsHealthy { get; set; }
    [SugarColumn(IsNullable = true)] public string? LastHealthCheck { get; set; }
    [SugarColumn(IsNullable = true)] public string? HealthMessage { get; set; }
    public string CreatedAt { get; set; } = "";
    public string UpdatedAt { get; set; } = "";
}
