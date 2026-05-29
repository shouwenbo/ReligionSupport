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
        var sourceText = context.SourceText ?? "";
        var sourceTitle = context.SourceTitle ?? "";

        var prompt = _prompt.BuildPrompt(context.TaskType,
            sourceTitle, context.SourceDescription ?? "",
            context.SourceVerse ?? "", sourceText);

        var systemPrompt = _prompt.GetSystemPrompt(context.TaskType);

        var result = await _ai.GenerateTextAsync(systemPrompt, prompt, context.Temperature, ct);

        context.State["generated_text"] = result;
        return new StepResult
        {
            Output = result,
            OutputSummary = result.Length > 200 ? result[..200] + "..." : result
        };
    }
}
