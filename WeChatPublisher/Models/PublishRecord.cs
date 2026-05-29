namespace WeChatPublisher.Models;

public class PublishRecord
{
    public int Id { get; set; }
    public string? PublishType { get; set; }
    public int? ArticleDraftId { get; set; }
    public int? VideoDraftId { get; set; }
    public string? Title { get; set; }
    public string? PublishId { get; set; }
    public string Status { get; set; } = "";
    public string? ErrorMessage { get; set; }
    public string CreatedAt { get; set; } = "";
    public string? CompletedAt { get; set; }
}
