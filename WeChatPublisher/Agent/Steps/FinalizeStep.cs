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

        // Last resort: auto-sanitize if final round had issues
        if (context.CurrentRound >= context.MaxRounds)
        {
            if (_sensitiveService.HasForcedMatches(text, out var count) && count > 0)
            {
                text = _sensitiveService.Sanitize(text);
                context.State["auto_sanitized"] = "true";
            }
        }

        context.FinalText = text;
        return Task.FromResult(new StepResult
        {
            Success = true,
            IsFinal = true,
            Output = text
        });
    }
}
