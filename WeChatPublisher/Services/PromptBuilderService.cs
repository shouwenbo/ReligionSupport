using WeChatPublisher.Models;

namespace WeChatPublisher.Services;

public class PromptBuilderService
{
    public string BuildPrompt(string category, string title, string desc,
        string verse, string content)
    {
        var template = AppSettings.Instance.GetActivePromptTemplate(category);
        if (template == null)
            return FallbackPrompt(category, title, desc, verse, content);

        return template.UserPromptTemplate
            .Replace("{source_title}", title)
            .Replace("{source_verse}", verse)
            .Replace("{source_content}", content)
            .Replace("{source_desc}", desc);
    }

    public string GetSystemPrompt(string category)
    {
        var template = AppSettings.Instance.GetActivePromptTemplate(category);
        return template?.SystemPrompt
            ?? "你是一位公众号文案创作者，擅长用温暖、柔和、真诚的文字打动人心。";
    }

    public PromptTemplate? GetActiveTemplate(string category)
    {
        return AppSettings.Instance.GetActivePromptTemplate(category);
    }

    public List<PromptTemplate> GetTemplates(string? category = null)
    {
        return AppSettings.Instance.GetPromptTemplates(category);
    }

    public void SaveTemplate(PromptTemplate template)
    {
        AppSettings.Instance.SavePromptTemplate(template);
    }

    public void DeleteTemplate(int id)
    {
        AppSettings.Instance.DeletePromptTemplate(id);
    }

    public void SetActive(int id)
    {
        AppSettings.Instance.SetActivePromptTemplate(id);
    }

    private static string FallbackPrompt(string category, string title, string desc,
        string verse, string content)
    {
        if (category == "Video")
        {
            return $"""
                请用以下【原文】编写天父书信：
                1. 开头必须是"亲爱的孩子"
                2. 以天父/父亲为第一人称视角
                3. 控制在50-250字，简体中文
                4. 提供5组爆款标题(6+6或7+7或8+8格式)和简介
                5. 提供30个匹配经文和10个两字标签

                【原文】
                {content}
                """;
        }

        return $"""
            【规则】
            1. 输出格式：标题、简介、经文、正文、5个两字标签
            2. 深度学习原文，重新组织结构，保留核心思想
            3. 温暖、真诚的文风，避免批判性语气
            4. 简体中文输出

            【标题】：{title}
            【简介】：{desc}
            【经文】：{verse}
            【内容】：{content}
            """;
    }

    // Smart MCP sampling: scan files and select best content
    public string BuildSamplingPrompt(List<string> fileSummaries)
    {
        return $"""
            以下是MCP资源文件夹中的文件摘要列表。请分析这些文件，选择最适合作为创作素材的文件（选1-3个），并说明选择理由。

            {string.Join("\n\n", fileSummaries.Select((s, i) => $"文件{i + 1}:\n{s}"))}

            请按以下格式输出：
            选中的文件: [编号]
            理由: [一句话]
            核心主题: [从选中文件中提取的核心主题]
            """;
    }
}
