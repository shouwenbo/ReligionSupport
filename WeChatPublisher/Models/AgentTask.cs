namespace WeChatPublisher.Models;

public class AgentTask
{
    public int Id { get; set; }
    public string TaskType { get; set; } = "article";
    public string Status { get; set; } = "pending";
    public string? SourceFileName { get; set; }
    public string? SourceTitle { get; set; }
    public string? FinalText { get; set; }
    public string? FinalImagePaths { get; set; }
    public int TotalRounds { get; set; }
    public int FinalSensitiveCount { get; set; }
    public string CreatedAt { get; set; } = "";
    public string? CompletedAt { get; set; }
}

public class AgentStepLog
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public int Round { get; set; }
    public string StepName { get; set; } = "";
    public string Status { get; set; } = "running";
    public string? InputSummary { get; set; }
    public string? OutputSummary { get; set; }
    public string? FullOutput { get; set; }
    public int SensitiveWordCount { get; set; }
    public string? ErrorMessage { get; set; }
    public string StartedAt { get; set; } = "";
    public string? CompletedAt { get; set; }
}
