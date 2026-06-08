namespace WeChatPublisher.Agent.Steps;

public class ReviewStep : IAgentStep
{
    public string Name => "质量审核";

    public Task<StepResult> ExecuteAsync(AgentContext context, CancellationToken ct)
    {
        var text = context.State.GetValueOrDefault("generated_text", "");
        var notes = new List<string>();

        if (text.Length < 200) notes.Add("文章较短");
        else if (text.Length > 5000) notes.Add("文章较长");
        if (!text.Contains("#")) notes.Add("缺少标签");

        // ReviewStep 仅作质量报告，不阻塞流程
        return Task.FromResult(new StepResult
        {
            Success = true,
            IsFinal = true,
            OutputSummary = notes.Count > 0 ? $"质量备注: {string.Join("; ", notes)}" : "质量合格"
        });
    }
}
