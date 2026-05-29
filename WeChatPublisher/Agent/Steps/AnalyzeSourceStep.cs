using WeChatPublisher.Services;

namespace WeChatPublisher.Agent.Steps;

public class AnalyzeSourceStep : IAgentStep
{
    private readonly AIService _ai;
    private readonly PromptBuilderService _prompt;
    private readonly McpService? _mcp;

    public string Name => "智能检索素材";

    public AnalyzeSourceStep(AIService ai, PromptBuilderService prompt, McpService? mcp = null)
    {
        _ai = ai;
        _prompt = prompt;
        _mcp = mcp;
    }

    public async Task<StepResult> ExecuteAsync(AgentContext context, CancellationToken ct)
    {
        var sourceText = context.SourceText;
        var sourceTitle = context.SourceTitle ?? "";

        // MCP智能采样: 如果配置了资源，扫描并让AI选择最佳素材
        if (_mcp != null && context.ReferenceFiles.Count == 0)
        {
            var resources = AppSettings.Instance.GetMcpResources();
            if (resources.Count > 0)
            {
                var allSamples = new List<string>();
                foreach (var res in resources.Take(3))
                {
                    var samples = _mcp.SampleFiles(res.Id, 10, 400);
                    allSamples.AddRange(samples.Select(s => $"### {s.FileName}\n{s.Content}"));
                }

                if (allSamples.Count > 0)
                {
                    var samplingPrompt = _prompt.BuildSamplingPrompt(allSamples);
                    var aiChoice = await _ai.GenerateTextAsync(
                        "你是一位内容策划专家，擅长从大量素材中选出最适合创作的内容。请简洁回答。",
                        samplingPrompt, 0.3, ct);

                    context.State["mcp_sampling"] = aiChoice;

                    // Use the first full sample as source
                    var firstSample = _mcp.SampleFiles(resources[0].Id, 5, 3000).FirstOrDefault();
                    if (firstSample != null)
                    {
                        sourceText = firstSample.FullContent;
                        context.SourceText = sourceText;
                        context.State["sampled_file"] = firstSample.FilePath;
                    }
                }
            }
        }

        var prompt = _prompt.BuildPrompt(context.TaskType,
            sourceTitle, context.SourceDescription ?? "",
            context.SourceVerse ?? "", sourceText ?? "");

        var systemPrompt = _prompt.GetSystemPrompt(context.TaskType);

        var result = await _ai.GenerateTextAsync(systemPrompt, prompt, context.Temperature, ct);

        context.State["analysis"] = result;
        return new StepResult
        {
            Output = result,
            OutputSummary = result.Length > 200 ? result[..200] + "..." : result
        };
    }
}
