namespace WeChatPublisher.Models;

public class TtsApiConfig
{
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
    public string CreatedAt { get; set; } = "";
    public string UpdatedAt { get; set; } = "";
}
