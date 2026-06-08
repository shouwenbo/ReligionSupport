using SqlSugar;

namespace WeChatPublisher.Models;

[SugarTable("WeChatConfigs")]
public class WeChatConfig
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }
    public string AccountName { get; set; } = "";
    public string Platform { get; set; } = "OfficialAccount";
    public string AppId { get; set; } = "";
    [SugarColumn(IsNullable = true)] public string? AppSecretEncrypted { get; set; }
    public string ApiBaseUrl { get; set; } = "https://api.weixin.qq.com";
    public int PublishAsDraft { get; set; } = 1;
    public int AutoSanitize { get; set; } = 1;
    [SugarColumn(IsNullable = true)] public string? DefaultTags { get; set; }
    /// <summary>联系方式图片路径(插入文章末尾)</summary>
    [SugarColumn(IsNullable = true)] public string? ContactImage { get; set; }
    [SugarColumn(IsNullable = true)] public string? CoverPrompt { get; set; }
    public int IsActive { get; set; }
    public int SortOrder { get; set; }
    public int IsHealthy { get; set; }
    [SugarColumn(IsNullable = true)] public string? LastHealthCheck { get; set; }
    [SugarColumn(IsNullable = true)] public string? HealthMessage { get; set; }
    public string CreatedAt { get; set; } = "";
    public string UpdatedAt { get; set; } = "";

    [SugarColumn(IsIgnore = true)]
    public string HealthDisplay => IsHealthy == 1 ? "● 正常" : "○ 未测";
    [SugarColumn(IsIgnore = true)]
    public string ActiveDisplay => IsActive == 1 ? "★ 当前" : "";
    [SugarColumn(IsIgnore = true)]
    public string PlatformDisplay => Platform == "OfficialAccount" ? "公众号" : "视频号";
}
