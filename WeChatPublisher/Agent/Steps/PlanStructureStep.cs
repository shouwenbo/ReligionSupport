using WeChatPublisher.Services;

namespace WeChatPublisher.Agent.Steps;

public class PlanStructureStep : IAgentStep
{
    private readonly AIService _ai;

    public string Name => "结构设计";

    public PlanStructureStep(AIService ai)
    {
        _ai = ai;
    }

    public async Task<StepResult> ExecuteAsync(AgentContext context, CancellationToken ct)
    {
        var analysis = context.State.GetValueOrDefault("analysis", "");
        var prompt = "基于以下分析结果，规划文章结构。直接输出JSON格式的段落规划。\n\n" +
            $"分析结果:\n{analysis}\n\n" +
            "输出格式:\n{\n  \"title\": \"文章标题\",\n  \"paragraphs\": [\"段落1概述\", \"段落2概述\", ...]\n}";

        var result = await _ai.GenerateTextAsync(
            "你是一位内容策划专家，请简洁回答，不要额外解释。",
            prompt, 0.5, ct);

        context.State["structure"] = result;
        return new StepResult { Output = result, OutputSummary = "结构规划完成" };
    }
}
