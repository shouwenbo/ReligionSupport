using SqlSugar;

namespace WeChatPublisher.Models;

[SugarTable("TtsApiConfigs")]
public class TtsApiConfig
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }
    public string ConfigType { get; set; } = "TTS";
    public string ApiType { get; set; } = "WebScrape";
    public string? BaseUrl { get; set; }
    public string? TokenFetchUrl { get; set; }
    public string? TokenRegexPattern { get; set; }
    public string? GenerateEndpoint { get; set; }
    public string SuccessCodeField { get; set; } = "code";
    public string SuccessCodeValue { get; set; } = "200";
    public string? DefaultParamsJSON { get; set; }
    public int IsActive { get; set; }
    public int IsHealthy { get; set; }
    public string? LastHealthCheck { get; set; }
    public string? HealthMessage { get; set; }
    public string CreatedAt { get; set; } = "";
    public string UpdatedAt { get; set; } = "";
}
