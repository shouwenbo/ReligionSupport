namespace WeChatPublisher.Services;

public class PromptBuilderService
{
    public string BuildAnalysisPrompt(string title, string desc, string verse, string content)
    {
        return $"""
            第一阶段——思路解析（不要写成品）
            请分析以下原文，按以下格式输出：
            1.核心思想总结（1-2句）
            2.关键细节提取（列举3-5个要点）
            3.标出需要替换或软化的宗教/敏感用语，并给出明确替换词表（例如：神→父亲/源头；耶稣→老师/榜样；圣灵→内心引导 等）
            4.给出3种改写思路并选出最佳方案

            【标题】：{title}
            【简介】：{desc}
            【经文】：{verse}
            【内容】：{content}
            """;
    }

    public string BuildStructurePrompt(string analysisResult)
    {
        return $"""
            第二阶段——结构设计
            根据以下分析结果，设计输出结构：
            1.提供3个备选标题及简介（1-2句）
            2.文章分段规划（最多6段），每段写一句话概括
            3.确认经文使用方式（放文章开头）
            4.确认简介、标签放置位置

            分析结果：
            {analysisResult}
            """;
    }

    public string BuildCreationPrompt(string structureResult)
    {
        return $"""
            第三阶段——成品创作
            根据以下结构设计，写出最终文章。

            最重要的要求（必须遵守）：
            1.最终导出的内容必须在最开头放置经文原文，不可以放在正文中间或末尾
            2.最终导出必须包含一段简介（1-2句）并显著呈现
            3.最终导出必须包含五个两字标签，格式为：#xx #xx #xx #xx #xx，放在正文结尾
            4.文章正文须为深度改写，用全新句式和表达方式呈现，语言温暖、细腻、真诚
            5.不要向用户提问或请求确认，自动做出最优选择并按序输出

            结构设计：
            {structureResult}
            """;
    }

    public string BuildRevisionPrompt(string text, List<string> issues)
    {
        return $"""
            以下文章需要修改其中的敏感词汇，请按以下规则修改：
            {string.Join("\n", issues)}

            要求：
            1. 只替换指定的词汇
            2. 保持文章结构和风格不变
            3. 替换后确保语句通顺自然
            4. 直接输出修改后的完整文章

            原文：
            {text}
            """;
    }
}
