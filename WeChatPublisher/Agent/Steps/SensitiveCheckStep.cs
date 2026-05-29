using System.Text.Json;
using WeChatPublisher.Services;

namespace WeChatPublisher.Agent.Steps;

public class SensitiveCheckStep : IAgentStep
{
    private readonly SensitiveWordService _service;

    public string Name => "敏感词检查";

    public SensitiveCheckStep(SensitiveWordService service)
    {
        _service = service;
    }

    public Task<StepResult> ExecuteAsync(AgentContext context, CancellationToken ct)
    {
        var text = context.State.GetValueOrDefault("generated_text", "");
        if (string.IsNullOrWhiteSpace(text))
            return Task.FromResult(new StepResult { Success = true, Output = "无文本需要检查" });

        var matches = _service.Scan(text);
        var forced = matches.Where(m => m.Rule.StrictLevel == 1).ToList();

        var issues = forced.Select(m => new
        {
            word = m.MatchedText,
            position = m.Position,
            replacements = m.Rule.ReplacementWords
        }).ToList();

        context.State["sensitive_issues"] = JsonSerializer.Serialize(issues);

        var needsRevision = forced.Count > 0 && context.CurrentRound < context.MaxRounds;

        var summary = forced.Count > 0
            ? $"发现 {forced.Count} 处需强制替换的敏感词: {string.Join(", ", forced.Select(m => m.MatchedText))}"
            : "未发现强制敏感词";

        return Task.FromResult(new StepResult
        {
            Success = true,
            OutputSummary = summary,
            SensitiveWordCount = forced.Count,
            NeedsRevision = needsRevision
        });
    }
}
