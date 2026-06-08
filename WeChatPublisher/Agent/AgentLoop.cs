using WeChatPublisher.Models;
using WeChatPublisher.Agent.Steps;
using WeChatPublisher.Services;

namespace WeChatPublisher.Agent;

public class AgentLoop
{
    private readonly List<IAgentStep> _steps;
    private readonly AppSettings _settings;

    public event Action<AgentStepLog>? OnStepExecuted;
    public event Action<string, string>? OnLog;

    public AgentLoop(AIService aiService, SensitiveWordService sensitiveService,
        PromptBuilderService promptBuilder, AppSettings settings, McpService? mcpService = null,
        AIImageService? aiImageService = null)
    {
        _settings = settings;

        _steps =
        [
            new AnalyzeSourceStep(aiService, promptBuilder, mcpService),
            new PlanStructureStep(aiService),
            new GenerateTextStep(aiService, promptBuilder),
            new GenerateImageStep(aiImageService!, mcpService),
            new SensitiveCheckStep(sensitiveService),
            new ReviewStep(),
            new ReviseStep(aiService),
            new FinalizeStep(sensitiveService)
        ];
    }

    public async Task<AgentContext> ExecuteAsync(AgentContext context, CancellationToken ct = default)
    {
        var taskId = SaveTaskStart(context);

        for (int round = 1; round <= context.MaxRounds; round++)
        {
            context.CurrentRound = round;
            OnLog?.Invoke("info", $"--- 第 {round}/{context.MaxRounds} 轮 ---");

            bool needsRevision = false;

            foreach (var step in _steps)
            {
                if (ct.IsCancellationRequested) break;

                // Skip ReviseStep unless needed
                if (step is ReviseStep && !needsRevision) continue;
                // Skip FinalizeStep unless we're done
                if (step is FinalizeStep && needsRevision && round < context.MaxRounds) continue;

                OnLog?.Invoke("info", $"执行步骤: {step.Name}");

                var log = new AgentStepLog
                {
                    TaskId = taskId,
                    Round = round,
                    StepName = step.Name,
                    Status = "running",
                    StartedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
                context.StepLogs.Add(log);

                try
                {
                    var result = await step.ExecuteAsync(context, ct);

                    log.Status = result.Success ? "completed" : "failed";
                    log.OutputSummary = result.OutputSummary ?? result.Output?[..Math.Min(200, result.Output.Length)];
                    log.FullOutput = result.Output;
                    log.SensitiveWordCount = result.SensitiveWordCount;
                    log.ErrorMessage = result.ErrorMessage;
                    log.CompletedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                    if (result.NeedsRevision) needsRevision = true;
                    if (result.IsFinal) break;

                    OnStepExecuted?.Invoke(log);

                    if (result.SensitiveWordCount > 0)
                        OnLog?.Invoke("warn", $"  ⚠ {result.OutputSummary}");
                    else if (result.OutputSummary != null)
                        OnLog?.Invoke("info", $"  ✓ {result.OutputSummary}");
                }
                catch (Exception ex)
                {
                    log.Status = "failed";
                    log.ErrorMessage = ex.Message;
                    log.CompletedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    OnLog?.Invoke("error", $"  ✗ {step.Name} 失败: {ex.Message}");
                    OnStepExecuted?.Invoke(log);
                }
            }

            if (!needsRevision) break;

            if (round == context.MaxRounds)
                context.State["auto_sanitized"] = "true";
        }

        SaveTaskComplete(context, taskId);
        return context;
    }

    private int SaveTaskStart(AgentContext context)
    {
        var task = new AgentTask
        {
            TaskType = context.TaskType,
            Status = "running",
            SourceFileName = context.ReferenceFiles.FirstOrDefault(),
            SourceTitle = context.SourceTitle,
            CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };
        return _settings.Db.ExecuteInScope(db => db.Insertable(task).ExecuteReturnIdentity());
    }

    private void SaveTaskComplete(AgentContext context, int taskId)
    {
        _settings.Db.ExecuteInScope(db =>
        {
            var task = db.Queryable<AgentTask>().InSingle(taskId);
            if (task != null)
            {
                task.FinalText = context.FinalText;
                task.FinalImagePaths = context.GeneratedImages.Count > 0
                    ? string.Join(";", context.GeneratedImages) : null;
                task.TotalRounds = context.CurrentRound;
                task.FinalSensitiveCount = context.State.GetValueOrDefault("sensitive_issues", "[]") != "[]" ? 1 : 0;
                task.Status = "completed";
                task.CompletedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                db.Updateable(task).ExecuteCommand();
            }

            // Save step logs
            foreach (var log in context.StepLogs)
            {
                log.TaskId = taskId;
                db.Insertable(log).ExecuteCommand();
            }
        });
    }
}
