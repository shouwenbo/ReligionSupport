using SqlSugar;

namespace WeChatPublisher.Models;

[SugarTable("SensitiveWords")]
public class SensitiveWord
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }
    public string SourceWord { get; set; } = "";
    public string ReplacementWords { get; set; } = "";
    public string Category { get; set; } = "自定义";
    public int StrictLevel { get; set; }
    [SugarColumn(IsNullable = true)] public string? ContextHint { get; set; }
    public int IsEnabled { get; set; } = 1;
    [SugarColumn(IsNullable = true)] public string? Notes { get; set; }
    public string CreatedAt { get; set; } = "";
    public string UpdatedAt { get; set; } = "";
}

public class SensitiveWordMatch
{
    public SensitiveWord Rule { get; set; } = null!;
    public int Position { get; set; }
    public int Length { get; set; }
    public string MatchedText { get; set; } = "";
}
