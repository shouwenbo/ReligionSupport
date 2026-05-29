namespace WeChatPublisher.Agent;

public class AgentContext
{
    public string TaskType { get; set; } = "article";
    public string? SourceText { get; set; }
    public string? SourceTitle { get; set; }
    public string? SourceDescription { get; set; }
    public string? SourceVerse { get; set; }
    public List<string> ReferenceFiles { get; set; } = [];

    public Dictionary<string, string> State { get; set; } = new();
    public int MaxRounds { get; set; } = 3;
    public int CurrentRound { get; set; } = 0;
    public double Temperature { get; set; } = 0.7;
    public string ArticleStyle { get; set; } = "温情生活型";
    public bool PauseForManualReview { get; set; }

    public string? FinalText { get; set; }
    public List<string> GeneratedImages { get; set; } = [];
    public List<Models.AgentStepLog> StepLogs { get; set; } = [];
}
