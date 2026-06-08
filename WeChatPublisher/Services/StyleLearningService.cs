using System.Text.Json;
using WeChatPublisher.Models;

namespace WeChatPublisher.Services;

public class StyleLearningService
{
    private readonly AIService _ai;

    public StyleLearningService() { _ai = new AIService(new SensitiveWordService()); }

    // ========== 核心：分析用户编辑，学习风格 ==========
    public async Task<LearningResult> AnalyzeEditsAsync(
        string original, string edited, string category, CancellationToken ct)
    {
        var result = new LearningResult();

        if (string.IsNullOrWhiteSpace(original) || string.IsNullOrWhiteSpace(edited))
            return result;

        // 1. 本地差异分析
        var diffs = ComputeDiffs(original, edited);
        if (diffs.Count == 0) return result;

        // 2. AI深度分析：提取风格模式 + 发现敏感词
        try
        {
            var prompt = BuildAnalysisPrompt(original, edited, diffs, category);
            var aiResult = await _ai.GenerateTextAsync(
                "你是写作风格分析师。只输出JSON，不要其他内容。",
                prompt, 0.2, ct);

            // 解析AI返回的JSON
            var json = ExtractJson(aiResult);
            if (json != null)
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("style_patterns", out var sp))
                {
                    foreach (var item in sp.EnumerateArray())
                    {
                        result.StylePatterns.Add(new StylePattern
                        {
                            Pattern = item.GetProperty("pattern").GetString() ?? "",
                            Before = item.GetProperty("before").GetString() ?? "",
                            After = item.GetProperty("after").GetString() ?? "",
                            Type = item.TryGetProperty("type", out var t) ? t.GetString() ?? "style" : "style"
                        });
                    }
                }

                if (root.TryGetProperty("discovered_words", out var dw))
                {
                    foreach (var item in dw.EnumerateArray())
                    {
                        result.DiscoveredWords.Add(new DiscoveredWord
                        {
                            SourceWord = item.GetProperty("source").GetString() ?? "",
                            Replacement = item.GetProperty("replacement").GetString() ?? "",
                            Occurrences = item.TryGetProperty("count", out var c) ? c.GetInt32() : 1,
                            Category = item.TryGetProperty("category", out var cat)
                                ? cat.GetString() ?? "自定义" : "自定义"
                        });
                    }
                }

                if (root.TryGetProperty("summary", out var s))
                    result.Summary = s.GetString() ?? "";
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"风格分析AI调用失败: {ex.Message}, 使用本地分析结果");
            result.Summary = $"本地分析: 检测到 {diffs.Count} 处修改";
        }

        // 3. 持久化编辑会话
        SaveSession(original, edited, diffs, result, category);

        // 4. 更新 AuthorPersona
        UpdatePersona(result, category);

        return result;
    }

    // ========== 本地差异计算 ==========
    private static List<DiffEntry> ComputeDiffs(string original, string edited)
    {
        var diffs = new List<DiffEntry>();
        var origLines = original.Split('\n');
        var editLines = edited.Split('\n');
        var maxLen = Math.Max(origLines.Length, editLines.Length);

        for (int i = 0; i < maxLen; i++)
        {
            var orig = i < origLines.Length ? origLines[i].Trim() : "";
            var edit = i < editLines.Length ? editLines[i].Trim() : "";

            if (orig != edit && !string.IsNullOrWhiteSpace(orig) && !string.IsNullOrWhiteSpace(edit))
            {
                // 判断是风格还是敏感词替换
                var type = IsLikelySensitiveWord(orig, edit) ? "sensitive" : "style";
                diffs.Add(new DiffEntry
                {
                    Line = i + 1,
                    Before = orig.Length > 100 ? orig[..100] : orig,
                    After = edit.Length > 100 ? edit[..100] : edit,
                    Type = type
                });
            }
        }
        return diffs.Take(20).ToList();
    }

    private static bool IsLikelySensitiveWord(string before, string after)
    {
        // 简单判断：如果只有个别词变了，句子结构不变，很可能是替换
        var wordsChanged = before.Split(' ').Except(after.Split(' ')).Count();
        return wordsChanged <= 3 && before.Length > 0;
    }

    // ========== AI分析Prompt ==========
    private static string BuildAnalysisPrompt(string original, string edited,
        List<DiffEntry> diffs, string category)
    {
        var diffsStr = string.Join("\n",
            diffs.Take(10).Select(d => $"- [{d.Type}] 原文: {d.Before}\n  改后: {d.After}"));

        return $$"""
            分析用户的编辑修改，提取写作风格偏好和潜在的敏感词替换规律。

            【修改类别】{{category}}
            【差异列表】
            {{diffsStr}}

            请输出JSON（只输出JSON，不要其他文字）：
            {
              "style_patterns": [
                {"pattern": "模式描述", "before": "原文片段", "after": "改后片段", "type": "style|sensitive"}
              ],
              "discovered_words": [
                {"source": "原词", "replacement": "替换词", "count": 出现次数, "category": "称谓|概念|行为|用语|自定义"}
              ],
              "summary": "一句话总结用户最核心的2-3个写作偏好"
            }
            """;
    }

    private static string? ExtractJson(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start >= 0 && end > start)
            return text[start..(end + 1)];
        return null;
    }

    // ========== 持久化 ==========
    private void SaveSession(string original, string edited,
        List<DiffEntry> diffs, LearningResult result, string category)
    {
        var persona = GetOrCreatePersona(category);
        var session = new EditSession
        {
            PersonaId = persona.Id,
            Category = category,
            OriginalText = original,
            EditedText = edited,
            DiffsJson = JsonSerializer.Serialize(diffs),
            LearningSummaryJson = JsonSerializer.Serialize(result),
            CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        AppSettings.Instance.Db.ExecuteInScope(db =>
            db.Insertable(session).ExecuteCommand());
    }

    private void UpdatePersona(LearningResult result, string category)
    {
        var persona = GetOrCreatePersona(category);
        persona.EditCount++;

        // 融合风格数据
        var profile = new Dictionary<string, object>();
        if (persona.StyleProfileJson != null)
        {
            try { profile = JsonSerializer.Deserialize<Dictionary<string, object>>(
                persona.StyleProfileJson) ?? []; } catch { }
        }

        // 累积模式
        var patterns = new List<object>();
        if (profile.TryGetValue("patterns", out var existing) && existing is JsonElement e)
        {
            foreach (var p in e.EnumerateArray()) patterns.Add(p);
        }
        foreach (var p in result.StylePatterns)
            patterns.Add(new { p.Pattern, p.Type, p.Before, p.After, addedAt = DateTime.Now.ToString("s") });

        profile["patterns"] = patterns;
        profile["totalEdits"] = persona.EditCount;
        profile["lastUpdated"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        persona.StyleProfileJson = JsonSerializer.Serialize(profile);
        persona.UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        AppSettings.Instance.Db.ExecuteInScope(db =>
            db.Updateable(persona).ExecuteCommand());
    }

    public AuthorPersona GetOrCreatePersona(string category)
    {
        var existing = AppSettings.Instance.Db.ExecuteInScope(db =>
            db.Queryable<AuthorPersona>()
              .First(p => p.Category == category && p.IsActive == 1));

        if (existing != null) return existing;

        var persona = new AuthorPersona
        {
            Name = category == "Article" ? "公众号作者风格" : "视频号作者风格",
            Category = category,
            EditCount = 0,
            CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };
        AppSettings.Instance.Db.ExecuteInScope(db =>
            db.Insertable(persona).ExecuteCommand());
        return persona;
    }

    // ========== 获取个人风格注入到AI Prompt ==========
    public string BuildPersonaInjection(string category)
    {
        var persona = AppSettings.Instance.Db.ExecuteInScope(db =>
            db.Queryable<AuthorPersona>()
              .First(p => p.Category == category && p.IsActive == 1));

        if (persona?.StyleProfileJson == null || persona.EditCount < 2)
            return "";

        try
        {
            var profile = JsonSerializer.Deserialize<Dictionary<string, object>>(
                persona.StyleProfileJson) ?? [];
            if (!profile.TryGetValue("patterns", out var patterns) || patterns is not JsonElement pe)
                return "";

            var recentPatterns = pe.EnumerateArray().Reverse().Take(10)
                .Select(p =>
                {
                    var pattern = p.GetProperty("pattern").GetString();
                    var before = p.GetProperty("before").GetString();
                    var after = p.GetProperty("after").GetString();
                    return $"- {pattern}: 「{before}」→「{after}」";
                }).ToList();

            if (recentPatterns.Count == 0) return "";

            return $"""
                【作者风格参考】
                以下是根据你 {persona.EditCount} 次编辑习得的写作偏好，请内化到创作中：
                {string.Join("\n", recentPatterns)}
                """;
        }
        catch { return ""; }
    }
}

public class DiffEntry
{
    public int Line { get; set; }
    public string Before { get; set; } = "";
    public string After { get; set; } = "";
    public string Type { get; set; } = "style";
}
