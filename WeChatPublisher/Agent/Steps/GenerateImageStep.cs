using WeChatPublisher.Services;

namespace WeChatPublisher.Agent.Steps;

public class GenerateImageStep : IAgentStep
{
    private readonly AIImageService _imageService;

    public string Name => "AI生成配图";

    public GenerateImageStep(AIImageService imageService)
    {
        _imageService = imageService;
    }

    public async Task<StepResult> ExecuteAsync(AgentContext context, CancellationToken ct)
    {
        var articleText = context.State.GetValueOrDefault("generated_text", "");
        if (string.IsNullOrWhiteSpace(articleText))
            return new StepResult { Success = true, OutputSummary = "无生成文本，跳过配图" };

        var imageConfig = AppSettings.Instance.GetActiveImageAiConfig();
        if (imageConfig == null)
            return new StepResult { Success = true, OutputSummary = "未配置图像AI，跳过配图" };

        var prompt = BuildImagePrompt(articleText);
        var outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output", "Images");
        Directory.CreateDirectory(outputDir);

        var imageCount = Math.Max(1, imageConfig.ImageCount);
        var imageSize = imageConfig.ImageSize ?? "1024x1024";

        try
        {
            for (int i = 0; i < imageCount; i++)
            {
                ct.ThrowIfCancellationRequested();

                var bytes = await _imageService.GenerateImageAsync(
                    prompt, size: imageSize, count: 1, ct: ct);

                var fileName = $"generated_{DateTime.Now:yyyyMMddHHmmss}_{i}.png";
                var filePath = Path.Combine(outputDir, fileName);
                await File.WriteAllBytesAsync(filePath, bytes, ct);

                context.GeneratedImages.Add(filePath);
            }

            return new StepResult
            {
                Success = true,
                OutputSummary = $"已生成{context.GeneratedImages.Count}张配图"
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new StepResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                OutputSummary = $"配图生成失败: {ex.Message}"
            };
        }
    }

    private static string BuildImagePrompt(string article)
    {
        var snippet = article.Length <= 800 ? article : article[..800];
        return $"""
            为以下公众号文章内容生成一张温馨、柔和、适合配图的插画。不要包含文字。
            风格：温暖色调，柔和画面，无文字。

            文章内容：
            {snippet}
            """;
    }
}
