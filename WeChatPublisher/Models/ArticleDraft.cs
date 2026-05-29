namespace WeChatPublisher.Models;

public class ArticleDraft
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Verse { get; set; }
    public string? Content { get; set; }
    public string? Tags { get; set; }
    public string? ImagePaths { get; set; }
    public string? MediaId { get; set; }
    public string? PublishId { get; set; }
    public string Status { get; set; } = "draft";
    public string CreatedAt { get; set; } = "";
    public string UpdatedAt { get; set; } = "";
}
