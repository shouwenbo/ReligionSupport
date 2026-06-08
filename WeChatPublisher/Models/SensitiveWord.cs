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
    /// <summary>0=直接替换, 1=AI意境改写, 2=AI自动抉择</summary>
    public int Strategy { get; set; } = 0;
    [SugarColumn(IsNullable = true)] public string? ContextHint { get; set; }
    [SugarColumn(IsNullable = true)] public string? RewriteExamples { get; set; }
    public int IsEnabled { get; set; } = 1;
    [SugarColumn(IsNullable = true)] public string? Notes { get; set; }
    public string CreatedAt { get; set; } = "";
    public string UpdatedAt { get; set; } = "";

    [SugarColumn(IsIgnore = true)]
    public string StrategyDisplay => Strategy switch { 0 => "直接替换", 1 => "AI意境改写", _ => "AI自动抉择" };
}

public class SensitiveWordMatch
{
    public SensitiveWord Rule { get; set; } = null!;
    public int Position { get; set; }
    public int Length { get; set; }
    public string MatchedText { get; set; } = "";
    /// <summary>AI建议的处理策略: replace / rewrite</summary>
    public string? SuggestedStrategy { get; set; }
}
