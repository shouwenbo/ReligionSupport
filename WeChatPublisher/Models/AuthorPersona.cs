using SqlSugar;

namespace WeChatPublisher.Models;

[SugarTable("AuthorPersonas")]
public class AuthorPersona
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }
    public string Name { get; set; } = "默认风格";
    public string Category { get; set; } = "Article";
    /// <summary>JSON: 风格偏好累积数据</summary>
    [SugarColumn(IsNullable = true)] public string? StyleProfileJson { get; set; }
    /// <summary>JSON: 最近N次编辑差异记录</summary>
    [SugarColumn(IsNullable = true)] public string? EditHistoryJson { get; set; }
    public int EditCount { get; set; }
    public int IsActive { get; set; } = 1;
    public string CreatedAt { get; set; } = "";
    public string UpdatedAt { get; set; } = "";
}

[SugarTable("EditSessions")]
public class EditSession
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }
    public int PersonaId { get; set; }
    public string Category { get; set; } = "Article";
    public string? OriginalText { get; set; }
    public string? EditedText { get; set; }
    /// <summary>JSON: [{from, to, context, type: "style"|"sensitive"}]</summary>
    [SugarColumn(IsNullable = true)] public string? DiffsJson { get; set; }
    /// <summary>JSON: AI分析的学习摘要</summary>
    [SugarColumn(IsNullable = true)] public string? LearningSummaryJson { get; set; }
    /// <summary>JSON: 新发现的潜在敏感词</summary>
    [SugarColumn(IsNullable = true)] public string? DiscoveredWordsJson { get; set; }
    public string CreatedAt { get; set; } = "";
}

/// <summary>
/// 风格学习结果，包含本次学到的内容
/// </summary>
public class LearningResult
{
    public List<StylePattern> StylePatterns { get; set; } = [];
    public List<DiscoveredWord> DiscoveredWords { get; set; } = [];
    public string Summary { get; set; } = "";
}

public class StylePattern
{
    public string Pattern { get; set; } = "";
    public string Before { get; set; } = "";
    public string After { get; set; } = "";
    public string Type { get; set; } = "style";
}

public class DiscoveredWord
{
    public string SourceWord { get; set; } = "";
    public string Replacement { get; set; } = "";
    public int Occurrences { get; set; }
    public string Category { get; set; } = "自定义";
}
