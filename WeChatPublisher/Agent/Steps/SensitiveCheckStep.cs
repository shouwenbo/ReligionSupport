using System.Text.Json;
using WeChatPublisher.Services;

namespace WeChatPublisher.Agent.Steps;

public class SensitiveCheckStep : IAgentStep
{
    private readonly SensitiveWordService _service;

    public string Name => "敏感词检查";

    public SensitiveCheckStep(SensitiveWordService service) { _service = service; }

    public Task<StepResult> ExecuteAsync(AgentContext context, CancellationToken ct)
    {
        var text = context.State.GetValueOrDefault("generated_text", "");
        if (string.IsNullOrWhiteSpace(text))
            return Task.FromResult(new StepResult { OutputSummary = "无文本需要检查" });

        var matches = _service.Scan(text);
        var forced = matches.Where(m => m.Rule.StrictLevel == 1).ToList();
        if (forced.Count == 0)
            return Task.FromResult(new StepResult
            {
                Success = true,
                OutputSummary = "未发现强制敏感词",
                SensitiveWordCount = 0
            });

        // 区分两种处理策略
        var replaceList = new List<object>();
        var rewriteList = new List<object>();

        foreach (var m in forced)
        {
            var rule = m.Rule;
            var before = text[Math.Max(0, m.Position - 20)..Math.Min(text.Length, m.Position + m.Length + 20)];

            if (rule.Strategy == 1 || rule.Strategy == 2)
            {
                rewriteList.Add(new
                {
                    word = m.MatchedText,
                    position = m.Position,
                    context = before,
                    replacement = rule.ReplacementWords,
                    category = rule.Category,
                    examples = rule.RewriteExamples
                });
            }
            else
            {
                replaceList.Add(new
                {
                    word = m.MatchedText,
                    position = m.Position,
                    replacement = rule.ReplacementWords
                });
            }
        }

        context.State["sensitive_replace"] = JsonSerializer.Serialize(replaceList);
        context.State["sensitive_rewrite"] = JsonSerializer.Serialize(rewriteList);

        var summary = $"发现 {forced.Count} 处敏感词";
        if (replaceList.Count > 0) summary += $" (直接替换:{replaceList.Count})";
        if (rewriteList.Count > 0) summary += $" (AI改写:{rewriteList.Count})";

        var needsRevise = forced.Count > 0 && context.CurrentRound < context.MaxRounds;

        return Task.FromResult(new StepResult
        {
            Success = true,
            OutputSummary = summary,
            SensitiveWordCount = forced.Count,
            NeedsRevision = needsRevise
        });
    }
}
