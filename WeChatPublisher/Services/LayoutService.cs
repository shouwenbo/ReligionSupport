using System.Text;
using System.Text.Json;

namespace WeChatPublisher.Services;

/// <summary>
/// AI智能排版引擎。
/// 为纯文本文章生成格式化HTML，包含字号、颜色、加粗、缩进、行距、配图位置。
/// 排版偏好随时间积累，存入AuthorPersona。
/// </summary>
public class LayoutService
{
    private readonly AIService _ai;

    public LayoutService() { _ai = new AIService(new SensitiveWordService()); }

    // ========== 核心：为文章生成智能排版 ==========
    public async Task<string> FormatArticleAsync(string plainText, string category,
        string? personaInjection = null, string? coverImagePath = null,
        string? contactImagePath = null, CancellationToken ct = default)
    {
        var prompt = BuildLayoutPrompt(plainText, personaInjection, coverImagePath, contactImagePath);

        var result = await _ai.GenerateTextAsync(
            "你是资深公众号排版设计师。直接输出HTML代码，不要解释，不要markdown包裹。",
            prompt, 0.3, ct);

        return CleanHtml(result);
    }

    private static string BuildLayoutPrompt(string text, string? persona,
        string? coverImage, string? contactImage)
    {
        var sb = new StringBuilder();
        sb.AppendLine("为以下公众号文章设计精美HTML排版。要求：");
        sb.AppendLine("1. 使用内联CSS控制字号(15-18px)、行高(1.8-2.0)、字间距、段落间距");
        sb.AppendLine("2. 关键语句用不同颜色或加粗强调(颜色用#333家族为主, 强调用#c0392b或#e67e22)");
        sb.AppendLine("3. 每段首行缩进2em, 段落之间留白适度");
        sb.AppendLine("4. 在文章中段(约1/3和2/3位置)自然插入2处图片占位符: <div class='insert-image'>此处配图</div>");
        sb.AppendLine("5. 段落控制在500字以内, 过长要拆分");
        sb.AppendLine("6. 文章要有呼吸感, 排版优雅但不花哨");
        sb.AppendLine("7. 整体最大宽度680px, 左右边距auto, 背景#fefefe");

        if (coverImage != null)
            sb.AppendLine($"8. 封面图片: <img src='{coverImage}' style='width:100%;border-radius:8px;margin-bottom:20px;'/>");

        if (contactImage != null)
            sb.AppendLine($"9. 文章末尾必须插入联系方式图片: <img src='{contactImage}' style='width:100%;margin-top:30px;'/>");

        if (persona != null)
            sb.AppendLine($"\n排版风格参考:\n{persona}");

        sb.AppendLine($"\n文章内容:\n{text}");
        sb.AppendLine("\n直接输出完整HTML。");

        return sb.ToString();
    }

    private static string CleanHtml(string raw)
    {
        // Remove markdown wrapping if any
        raw = raw.Replace("```html", "").Replace("```", "").Trim();
        // Ensure it has a container
        if (!raw.Contains("<div") && !raw.Contains("<p"))
            raw = $"<div style='max-width:680px;margin:0 auto;font-size:16px;line-height:1.9;color:#333;'>{raw}</div>";
        return raw;
    }

    // ========== 分析用户对排版的修改 ==========
    public async Task<LayoutLearningResult> AnalyzeLayoutEditsAsync(
        string originalHtml, string editedHtml, string category, CancellationToken ct)
    {
        var prompt = "分析用户对HTML排版的修改，提取排版风格偏好。只输出JSON：\n" +
            "{\n  \"font_preferences\": \"字号偏好\",\n  \"color_scheme\": \"配色方案\",\n" +
            "  \"spacing_style\": \"间距风格\",\n  \"emphasis_style\": \"强调方式\",\n" +
            "  \"image_placement\": \"配图偏好\",\n  \"summary\": \"一句话总结\"\n}\n\n" +
            $"原排版:\n{originalHtml[..Math.Min(originalHtml.Length, 800)]}\n\n" +
            $"修改后:\n{editedHtml[..Math.Min(editedHtml.Length, 800)]}";

        try
        {
            var result = await _ai.GenerateTextAsync(
                "你是排版分析师。只输出JSON。", prompt, 0.2, ct);
            var json = ExtractJson(result);
            if (json != null)
            {
                return JsonSerializer.Deserialize<LayoutLearningResult>(json)
                    ?? new LayoutLearningResult();
            }
        }
        catch { }
        return new LayoutLearningResult();
    }

    private static string? ExtractJson(string text)
    {
        int s = text.IndexOf('{'), e = text.LastIndexOf('}');
        return s >= 0 && e > s ? text[s..(e + 1)] : null;
    }
}

public class LayoutLearningResult
{
    public string FontPreferences { get; set; } = "";
    public string ColorScheme { get; set; } = "";
    public string SpacingStyle { get; set; } = "";
    public string EmphasisStyle { get; set; } = "";
    public string ImagePlacement { get; set; } = "";
    public string Summary { get; set; } = "";
}
