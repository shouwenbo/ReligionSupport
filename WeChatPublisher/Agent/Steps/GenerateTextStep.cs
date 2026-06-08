using WeChatPublisher.Services;

namespace WeChatPublisher.Agent.Steps;

public class GenerateTextStep : IAgentStep
{
    private readonly AIService _ai;
    private readonly PromptBuilderService _prompt;
    private readonly AgentLoop? _loop;

    public string Name => "AI生成文本";

    public GenerateTextStep(AIService ai, PromptBuilderService prompt, AgentLoop? loop = null)
    {
        _ai = ai; _prompt = prompt; _loop = loop;
    }

    public async Task<StepResult> ExecuteAsync(AgentContext context, CancellationToken ct)
    {
        var sourceText = context.SourceText ?? "";
        var sourceTitle = context.SourceTitle ?? "";

        // 灵修主题指引
        var themes = context.State.GetValueOrDefault("spiritual_themes", "");
        var sourceIsRaw = context.State.GetValueOrDefault("source_is_raw_journal", "") == "true";
        var extraGuidance = "";

        if (sourceIsRaw && themes.Length > 0)
            extraGuidance = $"\n【灵修主题指引】\n原文是生活记录，请根据以下提炼的灵修主题，舍弃流水账细节，从信仰视角全新创作：\n{themes}\n";

        var prompt = _prompt.BuildPrompt(context.TaskType,
            sourceTitle, context.SourceDescription ?? "",
            context.SourceVerse ?? "", sourceText);

        if (extraGuidance.Length > 0)
            prompt = extraGuidance + "\n\n" + prompt;

        var systemPrompt = _prompt.GetSystemPrompt(context.TaskType);

        // 注入作者风格
        var learner = new StyleLearningService();
        var personaInjection = learner.BuildPersonaInjection(context.TaskType);
        if (personaInjection.Length > 0)
            systemPrompt += "\n\n" + personaInjection;

        // 流式生成
        var fullText = "";
        await foreach (var chunk in _ai.GenerateTextStreamAsync(systemPrompt, prompt,
            context.Temperature, ct))
        {
            fullText += chunk;
            _loop?.FireTextChunk(chunk);
        }

        context.State["generated_text"] = fullText;
        return new StepResult
        {
            Output = fullText,
            OutputSummary = fullText.Length > 200 ? fullText[..200] + "..." : fullText
        };
    }
}
