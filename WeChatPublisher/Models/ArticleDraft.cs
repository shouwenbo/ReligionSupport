namespace WeChatPublisher.Models;

public class ArticleDraft
{
    public int Id { get; set; }
    [SqlSugar.SugarColumn(IsNullable = true)] public string? Title { get; set; }
    [SqlSugar.SugarColumn(IsNullable = true)] public string? Description { get; set; }
    [SqlSugar.SugarColumn(IsNullable = true)] public string? Verse { get; set; }
    [SqlSugar.SugarColumn(IsNullable = true)] public string? Content { get; set; }
    [SqlSugar.SugarColumn(IsNullable = true)] public string? Tags { get; set; }
    [SqlSugar.SugarColumn(IsNullable = true)] public string? ImagePaths { get; set; }
    [SqlSugar.SugarColumn(IsNullable = true)] public string? MediaId { get; set; }
    [SqlSugar.SugarColumn(IsNullable = true)] public string? PublishId { get; set; }
    public string Status { get; set; } = "draft";
    public string CreatedAt { get; set; } = "";
    public string UpdatedAt { get; set; } = "";
}
