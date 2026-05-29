using WeChatPublisher.Services;

namespace WeChatPublisher.Agent.Steps;

public class AnalyzeSourceStep : IAgentStep
{
    private readonly AIService _ai;
    private readonly PromptBuilderService _prompt;

    public string Name => "分析原文";

    public AnalyzeSourceStep(AIService ai, PromptBuilderService prompt)
    {
        _ai = ai;
        _prompt = prompt;
    }

    public async Task<StepResult> ExecuteAsync(AgentContext context, CancellationToken ct)
    {
        var prompt = _prompt.BuildAnalysisPrompt(
            context.SourceTitle ?? "",
            context.SourceDescription ?? "",
            context.SourceVerse ?? "",
            context.SourceText ?? "");

        var result = await _ai.GenerateTextAsync(
            "你是一位公众号文案创作者，擅长用温暖、柔和、真诚的文字打动人心。",
            prompt, context.Temperature, ct);

        context.State["analysis"] = result;
        return new StepResult { Output = result, OutputSummary = result[..Math.Min(200, result.Length)] };
    }
}
