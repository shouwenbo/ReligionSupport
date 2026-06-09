using WeChatPublisher.Services;

namespace WeChatPublisher.Agent.Steps;

public class FinalizeStep : IAgentStep
{
    private readonly SensitiveWordService _sensitiveService;

    public string Name => "终稿";

    public FinalizeStep(SensitiveWordService sensitiveService)
    {
        _sensitiveService = sensitiveService;
    }

    public Task<StepResult> ExecuteAsync(AgentContext context, CancellationToken ct)
    {
        var text = context.State.GetValueOrDefault("generated_text", "");

        // 始终输出净化后的文本
        if (_sensitiveService.HasForcedMatches(text, out _))
            text = _sensitiveService.Sanitize(text);

        context.FinalText = text;
        return Task.FromResult(new StepResult
        {
            Success = true,
            IsFinal = true,
            Output = text
        });
    }
}
