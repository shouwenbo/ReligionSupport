namespace WeChatPublisher.Models;

public class AgentTask
{
    [SqlSugar.SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }
    public string TaskType { get; set; } = "article";
    public string Status { get; set; } = "pending";
    [SqlSugar.SugarColumn(IsNullable = true)] public string? SourceFileName { get; set; }
    [SqlSugar.SugarColumn(IsNullable = true)] public string? SourceTitle { get; set; }
    [SqlSugar.SugarColumn(IsNullable = true)] public string? FinalText { get; set; }
    [SqlSugar.SugarColumn(IsNullable = true)] public string? FinalImagePaths { get; set; }
    public int TotalRounds { get; set; }
    public int FinalSensitiveCount { get; set; }
    public string CreatedAt { get; set; } = "";
    [SqlSugar.SugarColumn(IsNullable = true)] public string? CompletedAt { get; set; }
}

public class AgentStepLog
{
    [SqlSugar.SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }
    public int TaskId { get; set; }
    public int Round { get; set; }
    public string StepName { get; set; } = "";
    public string Status { get; set; } = "running";
    [SqlSugar.SugarColumn(IsNullable = true)] public string? InputSummary { get; set; }
    [SqlSugar.SugarColumn(IsNullable = true)] public string? OutputSummary { get; set; }
    [SqlSugar.SugarColumn(IsNullable = true)] public string? FullOutput { get; set; }
    public int SensitiveWordCount { get; set; }
    [SqlSugar.SugarColumn(IsNullable = true)] public string? ErrorMessage { get; set; }
    public string StartedAt { get; set; } = "";
    [SqlSugar.SugarColumn(IsNullable = true)] public string? CompletedAt { get; set; }
}
