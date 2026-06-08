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

        // MCP智能采样 + AI相关性过滤
        if (_mcp != null && context.ReferenceFiles.Count == 0)
        {
            var resources = AppSettings.Instance.GetMcpResources()
                .Where(r => r.ResourceType == "LocalFolder").ToList();

            if (resources.Count > 0)
            {
                // 1. 扫描最多3个资源，每个采样10个文件预览
                var allFiles = new List<FileSample>();
                foreach (var res in resources.Take(3))
                    allFiles.AddRange(_mcp.SampleFiles(res.Id, 10, 500, bypassCache: false));

                if (allFiles.Count > 0)
                {
                    // 2. 让AI判断哪些文件对创作有帮助
                    var filtered = await FilterByAi(allFiles, context.TaskType, ct);

                    if (filtered.Count > 0)
                    {
                        // 3. 加载被选中文件的完整内容作为素材
                        var merged = string.Join("\n\n---\n\n",
                            filtered.Select(f => f.FullContent));

                        if (!string.IsNullOrWhiteSpace(merged))
                        {
                            sourceText = merged;
                            context.SourceText = merged;
                            context.State["sampled_files"] = string.Join(", ",
                                filtered.Select(f => f.FileName));
                        }
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

    private static List<FileSample> QuickFilter(List<FileSample> samples)
    {
        var junkPatterns = new[] { "账号", "密码", "password", "username", "config",
            "登录", "注册", "验证码", "充值", "余额", "交易", "订单", "QQ", "微信",
            "localhost", "http://", "https://", ".exe", ".dll", ".com", ".cn",
            "function", "class ", "import ", "require(", "npm ", "pip " };
        var goodPatterns = new[] { "神", "爱", "恩典", "平安", "祷告", "信心", "盼望",
            "生命", "光", "喜乐", "感恩", "祝福", "安慰", "诗歌", "经文", "耶稣",
            "圣灵", "救赎", "赞美", "默想", "温暖", "感动", "故事", "孩子", "父亲",
            "母亲", "家庭", "成长", "日记", "感悟", "反思" };

        // 排除已发布公众号文章目录，避免重复内容
        var publishedFolder = @"F:\传道 & 公众号文案";
        var result = new List<FileSample>();
        foreach (var s in samples)
        {
            if (s.FilePath.StartsWith(publishedFolder, StringComparison.OrdinalIgnoreCase))
                continue;

            var shortContent = s.Content.Length > 200 ? s.Content[..200] : s.Content;

            // 明显的垃圾文件跳过
            if (junkPatterns.Any(p => shortContent.Contains(p, StringComparison.OrdinalIgnoreCase))
                && !goodPatterns.Any(p => shortContent.Contains(p)))
                continue;

            // 内容太少或文件名明显是系统文件
            if (shortContent.Length < 30) continue;
            if (s.FileName.StartsWith('.')) continue;

            result.Add(s);
        }
        return result;
    }

    private async Task<List<FileSample>> FilterByAi(List<FileSample> samples,
        string taskType, CancellationToken ct)
    {
        // 1. 本地快速过滤
        samples = QuickFilter(samples);
        if (samples.Count <= 3) return samples; // 少于3个不再调用AI
        if (samples.Count > 10) samples = samples[..10];

        // 2. AI精确判断（只发少数字符的预览）
        var previews = string.Join("\n",
            samples.Select((s, i) => $"[{i}] {s.Content[..Math.Min(s.Content.Length, 150)]}"));

        var filterPrompt = $"判断以下文件是否适合作为{taskType}创作素材。只输出有用文件的编号(如0,3,5)，或NONE:\n{previews}";

        try
        {
            var aiResult = await _ai.GenerateTextAsync(
                "只输出编号或NONE，不要解释。", filterPrompt, 0.1, ct);

            var nums = System.Text.RegularExpressions.Regex.Matches(
                aiResult.Trim(), @"\d+")
                .Select(m => int.TryParse(m.Value, out var n) ? n : -1)
                .Where(i => i >= 0 && i < samples.Count)
                .Distinct().ToList();

            return nums.Count > 0 ? nums.Select(i => samples[i]).ToList() : [];
        }
        catch { return samples; }
    }
}
