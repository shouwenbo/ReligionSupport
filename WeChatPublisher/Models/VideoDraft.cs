namespace WeChatPublisher.Models;

public class VideoDraft
{
    public int Id { get; set; }
    [SqlSugar.SugarColumn(IsNullable = true)] public string? TitleWord1 { get; set; }
    [SqlSugar.SugarColumn(IsNullable = true)] public string? TitleWord2 { get; set; }
    [SqlSugar.SugarColumn(IsNullable = true)] public string? Content { get; set; }
    [SqlSugar.SugarColumn(IsNullable = true)] public string? Verse { get; set; }
    [SqlSugar.SugarColumn(IsNullable = true)] public string? VerseContent { get; set; }
    [SqlSugar.SugarColumn(IsNullable = true)] public string? Summary { get; set; }
    [SqlSugar.SugarColumn(IsNullable = true)] public string? AudioPath { get; set; }
    [SqlSugar.SugarColumn(IsNullable = true)] public string? SrtPath { get; set; }
    [SqlSugar.SugarColumn(IsNullable = true)] public string? VideoPath { get; set; }
    [SqlSugar.SugarColumn(IsNullable = true)] public string? OutputFolder { get; set; }
    public string Status { get; set; } = "draft";
    public string CreatedAt { get; set; } = "";
    public string UpdatedAt { get; set; } = "";
}
