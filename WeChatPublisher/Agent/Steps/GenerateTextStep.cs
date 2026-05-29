using WeChatPublisher.Services;

namespace WeChatPublisher.Agent.Steps;

public class GenerateTextStep : IAgentStep
{
    private readonly AIService _ai;
    private readonly PromptBuilderService _prompt;

    public string Name => "AI生成文本";

    public GenerateTextStep(AIService ai, PromptBuilderService prompt)
    {
        _ai = ai;
        _prompt = prompt;
    }

    public async Task<StepResult> ExecuteAsync(AgentContext context, CancellationToken ct)
    {
        var structure = context.State.GetValueOrDefault("structure", "");
        var prompt = _prompt.BuildCreationPrompt(structure);

        var result = await _ai.GenerateTextAsync(
            "你是一位公众号文案创作者，本公众号主题为'爱与祝福同行'，文章整体风格应温暖、柔和、真诚，传递希望、安慰、连接、鼓励的情感。",
            prompt, context.Temperature, ct);

        context.State["generated_text"] = result;
        return new StepResult { Output = result, OutputSummary = result[..Math.Min(200, result.Length)] };
    }
}
