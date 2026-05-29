namespace WeChatPublisher.Agent.Steps;

public class ReviewStep : IAgentStep
{
    public string Name => "质量审核";

    public Task<StepResult> ExecuteAsync(AgentContext context, CancellationToken ct)
    {
        var text = context.State.GetValueOrDefault("generated_text", "");
        var issuesJson = context.State.GetValueOrDefault("sensitive_issues", "[]");

        // Auto-review: check word count, structure quality, sensitive word count
        bool pass = true;
        var notes = new List<string>();

        if (text.Length < 200)
        {
            pass = false;
            notes.Add("文章过短(< 200字)");
        }
        else if (text.Length > 5000)
        {
            notes.Add("文章较长(> 5000字)，请确认");
        }

        if (issuesJson != "[]" && issuesJson.Length > 5)
        {
            pass = false;
            notes.Add("仍存在敏感词问题");
        }

        if (!text.Contains("#") || !text.Contains(" "))
        {
            pass = false;
            notes.Add("缺少标签格式(#标签1 #标签2)");
        }

        return Task.FromResult(new StepResult
        {
            Success = true,
            IsFinal = pass,
            NeedsRevision = !pass,
            OutputSummary = pass ? "审核通过" : $"审核未通过: {string.Join("; ", notes)}"
        });
    }
}
