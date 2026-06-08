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

        // 如果有提炼的灵修主题，注入到 prompt 前面引导 AI
        var themes = context.State.GetValueOrDefault("spiritual_themes", "");
        var sourceIsRaw = context.State.GetValueOrDefault("source_is_raw_journal", "") == "true";
        var extraGuidance = "";

        if (sourceIsRaw && themes.Length > 0)
        {
            extraGuidance = $"""

                【灵修主题指引】
                原文是生活记录/日记片段。请根据以下提炼的灵修主题，将琐碎的日常升华为信仰文章：
                {themes}

                记住：舍弃原文中的流水账细节（人名、地名、吃了什么等），保留情感和感悟，
                从信仰视角重新创作，让文章有灵修深度。
                """;
        }

        var prompt = _prompt.BuildPrompt(context.TaskType,
            sourceTitle, context.SourceDescription ?? "",
            context.SourceVerse ?? "", sourceText);

        if (extraGuidance.Length > 0)
            prompt = extraGuidance + "\n\n" + prompt;

        var systemPrompt = _prompt.GetSystemPrompt(context.TaskType);

        // 注入学到的作者风格
        var learner = new StyleLearningService();
        var personaInjection = learner.BuildPersonaInjection(context.TaskType);
        if (personaInjection.Length > 0)
            systemPrompt += "\n\n" + personaInjection;

        var result = await _ai.GenerateTextAsync(systemPrompt, prompt, context.Temperature, ct);

        context.State["generated_text"] = result;
        return new StepResult
        {
            Output = result,
            OutputSummary = result.Length > 200 ? result[..200] + "..." : result
        };
    }
}
