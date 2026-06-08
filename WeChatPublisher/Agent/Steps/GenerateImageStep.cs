using WeChatPublisher.Services;

namespace WeChatPublisher.Agent.Steps;

public class GenerateImageStep : IAgentStep
{
    private readonly AIImageService _imageService;
    private readonly McpService? _mcpService;

    public string Name => "AI生成配图";

    public GenerateImageStep(AIImageService imageService, McpService? mcpService = null)
    {
        _imageService = imageService;
        _mcpService = mcpService;
    }

    public async Task<StepResult> ExecuteAsync(AgentContext context, CancellationToken ct)
    {
        var articleText = context.State.GetValueOrDefault("generated_text", "");
        if (string.IsNullOrWhiteSpace(articleText))
            return new StepResult { Success = true, OutputSummary = "无文本，跳过配图" };

        var imageConfig = AppSettings.Instance.GetActiveImageAiConfig();
        if (imageConfig == null)
            return new StepResult { Success = true, OutputSummary = "未配置图像AI，跳过配图" };

        var prompt = BuildImagePrompt(articleText);
        var outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output", "Images");
        Directory.CreateDirectory(outputDir);

        try
        {
            // 1. 优先使用AI生成
            var bytes = await _imageService.GenerateImageAsync(
                prompt, size: imageConfig.ImageSize, ct: ct);

            var fileName = $"ai_gen_{DateTime.Now:yyyyMMddHHmmss}.png";
            var filePath = Path.Combine(outputDir, fileName);
            await File.WriteAllBytesAsync(filePath, bytes, ct);
            context.GeneratedImages.Add(filePath);

            return new StepResult
            {
                Success = true,
                OutputSummary = "AI生成配图成功"
            };
        }
        catch (Exception aiEx)
        {
            // 2. AI失败 → 从MCP美图素材取后备图片
            if (_mcpService != null)
            {
                try
                {
                    var imageResources = AppSettings.Instance.GetMcpResources()
                        .Where(r => r.ResourceType == "LocalFolder")
                        .ToList();

                    foreach (var res in imageResources)
                    {
                        var images = _mcpService.PickRandomImages(res.Id, 1);
                        if (images.Count > 0)
                        {
                            var fallbackFile = Path.Combine(outputDir,
                                $"fallback_{DateTime.Now:yyyyMMddHHmmss}.png");
                            File.Copy(images[0], fallbackFile, true);
                            context.GeneratedImages.Add(fallbackFile);

                            return new StepResult
                            {
                                Success = true,
                                OutputSummary = $"AI生成失败，使用后备图片: {Path.GetFileName(images[0])}"
                            };
                        }
                    }
                }
                catch { }
            }

            return new StepResult
            {
                Success = false,
                ErrorMessage = aiEx.Message,
                OutputSummary = $"配图失败: {aiEx.Message}"
            };
        }
    }

    private static string BuildImagePrompt(string article)
    {
        var snippet = article.Length <= 800 ? article : article[..800];
        return $"根据以下文章内容生成一张温馨、柔和、适合配图的插画。不要包含文字。风格：温暖色调，柔和画面。\n\n{snippet}";
    }
}
