namespace WeChatPublisher.Models;

public class WeChatConfig
{
    public int Id { get; set; }
    public string AppId { get; set; } = "";
    public string? AppSecretEncrypted { get; set; }
    public string ApiBaseUrl { get; set; } = "https://api.weixin.qq.com";
    public int PublishAsDraft { get; set; } = 1;
    public int AutoSanitize { get; set; } = 1;
    public string? DefaultTags { get; set; }
    public int IsActive { get; set; }
    public string CreatedAt { get; set; } = "";
    public string UpdatedAt { get; set; } = "";
}
