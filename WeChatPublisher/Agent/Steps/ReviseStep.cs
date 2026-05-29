using System.Text.Json;
using WeChatPublisher.Services;

namespace WeChatPublisher.Agent.Steps;

public class ReviseStep : IAgentStep
{
    private readonly AIService _ai;

    public string Name => "AI修正";

    public ReviseStep(AIService ai)
    {
        _ai = ai;
    }

    public async Task<StepResult> ExecuteAsync(AgentContext context, CancellationToken ct)
    {
        var text = context.State.GetValueOrDefault("generated_text", "");
        var issuesJson = context.State.GetValueOrDefault("sensitive_issues", "[]");

        var prompt = BuildRevisionPrompt(text, issuesJson);

        var revised = await _ai.GenerateTextAsync(
            "你是一位文案改写专家，请根据指示修改文章中的敏感词汇，保持文章原意和风格。直接输出修改后的完整文章。",
            prompt, context.Temperature, ct);

        context.State["generated_text"] = revised;
        return new StepResult { Output = revised, OutputSummary = revised[..Math.Min(200, revised.Length)] };
    }

    private static string BuildRevisionPrompt(string text, string issuesJson)
    {
        var issues = JsonSerializer.Deserialize<List<JsonElement>>(issuesJson) ?? [];

        var replacements = new List<string>();
        foreach (var issue in issues)
        {
            var word = issue.GetProperty("word").GetString();
            var reps = issue.GetProperty("replacements").GetString();
            replacements.Add($"'{word}' → 请替换为: {reps}");
        }

        return $"""
            以下文章需要修改其中的敏感词汇，请按以下替换规则修改：
            {string.Join("\n", replacements)}

            要求：
            1. 只替换指定的词汇，不要改变其他内容
            2. 保持文章的结构、风格和语调不变
            3. 替换后确保语句通顺自然
            4. 直接输出修改后的完整文章，不要说明修改了什么

            原文：
            {text}
            """;
    }
}
