namespace WeChatPublisher.Agent;

public interface IAgentStep
{
    string Name { get; }
    Task<StepResult> ExecuteAsync(AgentContext context, CancellationToken ct);
}

public class StepResult
{
    public bool Success { get; set; } = true;
    public string? Output { get; set; }
    public string? OutputSummary { get; set; }
    public int SensitiveWordCount { get; set; }
    public bool NeedsRevision { get; set; }
    public bool IsFinal { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}
