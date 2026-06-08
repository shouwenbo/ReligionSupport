using System.Text.Json;
using WeChatPublisher.Services;

namespace WeChatPublisher.Agent.Steps;

public class ReviseStep : IAgentStep
{
    private readonly AIService _ai;

    public string Name => "AI修正";

    public ReviseStep(AIService ai) { _ai = ai; }

    public async Task<StepResult> ExecuteAsync(AgentContext context, CancellationToken ct)
    {
        var text = context.State.GetValueOrDefault("generated_text", "");

        // Phase 1: 直接替换（不需要AI）
        var replaceJson = context.State.GetValueOrDefault("sensitive_replace", "[]");
        text = ApplyReplacements(text, replaceJson);

        // Phase 2: AI意境改写（需要AI理解上下文后巧妙避开）
        var rewriteJson = context.State.GetValueOrDefault("sensitive_rewrite", "[]");
        var rewriteItems = JsonSerializer.Deserialize<List<JsonElement>>(rewriteJson) ?? [];

        if (rewriteItems.Count > 0)
            text = await RewriteWithAi(text, rewriteItems, context.Temperature, ct);

        var summary = (rewriteItems.Count > 0)
            ? $"完成 {rewriteItems.Count} 处AI意境改写"
            : "完成直接替换";

        context.State["generated_text"] = text;
        return new StepResult { Output = text, OutputSummary = summary };
    }

    private static string ApplyReplacements(string text, string issuesJson)
    {
        var items = JsonSerializer.Deserialize<List<JsonElement>>(issuesJson) ?? [];
        foreach (var item in items)
        {
            var word = item.GetProperty("word").GetString()!;
            var reps = item.GetProperty("replacement").GetString();
            if (!string.IsNullOrWhiteSpace(reps))
            {
                var alt = reps.Split(';')[0].Trim();
                text = text.Replace(word, alt);
            }
        }
        return text;
    }

    private async Task<string> RewriteWithAi(string text,
        List<JsonElement> rewriteItems, double temperature, CancellationToken ct)
    {
        var instructions = new List<string>();
        foreach (var item in rewriteItems)
        {
            var word = item.GetProperty("word").GetString();
            var ctx = item.GetProperty("context").GetString();
            var cat = item.TryGetProperty("category", out var c) ? c.GetString() : "";
            var examples = item.TryGetProperty("examples", out var ex) ? ex.GetString() : null;

            var hint = $"""
                敏感词: 「{word}」({cat})
                当前位置上下文: 「{ctx}」
                """;
            if (!string.IsNullOrWhiteSpace(examples))
                hint += $"\n参考改写示例:\n{examples}";
            instructions.Add(hint);
        }

        var prompt = $"""
            以下文章包含需要回避的敏感词汇。请不要简单地用一个词替换另一个词，
            而是用更巧妙的表达方式——改写整个句子或段落，让读者完全能理解原意，
            但看不到敏感词及其直接替身。

            【需要回避的词汇及上下文】
            {string.Join("\n\n", instructions)}

            【改写要求】
            1. 不要出现原始敏感词，也不要直接使用其替身词
            2. 用生活化的表达、比喻、或换个角度来传达同样的含义
            3. 改后的文字要更温暖、更自然、更有感染力
            4. 保持文章整体结构、段落数量不变
            5. 直接输出完整修改后的文章

            【原文】
            {text}
            """;

        var result = await _ai.GenerateTextAsync(
            "你是一位文学编辑，擅长用优雅的表达替代敏感词汇。",
            prompt, temperature, ct);

        return result;
    }
}
