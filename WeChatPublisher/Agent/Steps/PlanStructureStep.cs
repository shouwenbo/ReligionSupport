using WeChatPublisher.Services;

namespace WeChatPublisher.Agent.Steps;

public class PlanStructureStep : IAgentStep
{
    private readonly AIService _ai;
    private readonly PromptBuilderService _prompt;

    public string Name => "结构设计";

    public PlanStructureStep(AIService ai, PromptBuilderService prompt)
    {
        _ai = ai;
        _prompt = prompt;
    }

    public async Task<StepResult> ExecuteAsync(AgentContext context, CancellationToken ct)
    {
        var analysis = context.State.GetValueOrDefault("analysis", "");
        var prompt = _prompt.BuildStructurePrompt(analysis);

        var result = await _ai.GenerateTextAsync(
            "你是一位公众号内容策划专家，擅长规划文章结构。",
            prompt, context.Temperature, ct);

        context.State["structure"] = result;
        return new StepResult { Output = result, OutputSummary = result[..Math.Min(200, result.Length)] };
    }
}
