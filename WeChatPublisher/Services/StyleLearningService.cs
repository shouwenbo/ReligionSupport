using System.Text.Json;
using WeChatPublisher.Models;

namespace WeChatPublisher.Services;

/// <summary>
/// 作者风格学习 + RAG检索。
/// 敏感词：本地扫描(不占prompt)，只把命中的发给AI。
/// 风格：关键词索引 → 按文章主题检索 → 最多注入5条。
/// </summary>
public class StyleLearningService
{
    private readonly AIService _ai;
    private const int CompressInterval = 10;
    private const int MaxRecentPatterns = 50;   // 保留50条供RAG检索
    private const int MaxInjectionChars = 8000;  // 风格注入上限, 占64K窗口的~8%
    private const int MaxRetrievedPatterns = 8;  // RAG最多检索8条

    public StyleLearningService() { _ai = new AIService(new SensitiveWordService()); }

    // ========== 分析编辑，提取风格 ==========
    public async Task<LearningResult> AnalyzeEditsAsync(
        string original, string edited, string category, CancellationToken ct)
    {
        var result = new LearningResult();
        if (string.IsNullOrWhiteSpace(original) || string.IsNullOrWhiteSpace(edited))
            return result;

        var diffs = ComputeDiffs(original, edited);
        if (diffs.Count == 0) return result;

        try
        {
            var prompt = BuildAnalysisPrompt(original, edited, diffs, category);
            var aiResult = await _ai.GenerateTextAsync(
                "你是写作风格分析师。只输出JSON，不要其他内容。", prompt, 0.2, ct);

            var json = ExtractJson(aiResult);
            if (json != null)
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                ParseStylePatterns(root, result);
                ParseDiscoveredWords(root, result);
                if (root.TryGetProperty("summary", out var s))
                    result.Summary = s.GetString() ?? "";
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"风格分析AI失败: {ex.Message}, 使用本地分析");
            result.Summary = $"本地分析: {diffs.Count} 处修改";
        }

        SaveSession(original, edited, diffs, result, category);
        UpdatePersona(result, category);
        return result;
    }

    // ========== RAG核心：三层注入 ==========
    public string BuildPersonaInjection(string category, string? sourceContent = null)
    {
        var persona = AppSettings.Instance.Db.ExecuteInScope(db =>
            db.Queryable<AuthorPersona>()
              .First(p => p.Category == category && p.IsActive == 1));
        if (persona?.StyleProfileJson == null || persona.EditCount < 2) return "";

        try
        {
            var profile = LoadProfile(persona);
            var layers = new List<string>();

            // Layer 1: 风格摘要（AI压缩，始终注入）
            if (profile.TryGetValue("summary", out var s) && s is string summary && summary.Length > 0)
                layers.Add($"【风格摘要】{summary}");

            // Layer 2: RAG检索的匹配模式（按主题关键词检索）
            if (!string.IsNullOrWhiteSpace(sourceContent))
            {
                var recent = GetRecentPatterns(profile);
                var keywords = ExtractKeywords(sourceContent);
                var matches = recent
                    .Select(p => (Pattern: p, Score: KeywordScore(p, keywords)))
                    .Where(x => x.Score > 0)
                    .OrderByDescending(x => x.Score)
                    .Take(MaxRetrievedPatterns)
                    .Select(x => $"- {x.Pattern.GetValueOrDefault("pattern", "")}")
                    .ToList();
                if (matches.Count > 0)
                    layers.Add($"【主题相关风格({matches.Count}条)】\n{string.Join("\n", matches)}");
            }

            // Layer 3: 最近3次修改（新鲜记忆，始终注入）
            var fresh = GetRecentPatterns(profile).TakeLast(3)
                .Where(p => p.ContainsKey("pattern"))
                .Select(p => $"- {p["pattern"]}")
                .ToList();
            if (fresh.Count > 0)
                layers.Add($"【最近学到的风格】\n{string.Join("\n", fresh)}");

            var injection = $"【作者风格(已学习{persona.EditCount}次)】\n{string.Join("\n\n", layers)}";
            return injection.Length > MaxInjectionChars ? injection[..MaxInjectionChars] : injection;
        }
        catch { return ""; }
    }

    // ========== 关键词提取 ==========
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "的", "了", "在", "是", "我", "有", "和", "就", "不", "人", "都", "一",
        "一个", "上", "也", "很", "到", "说", "要", "去", "你", "会", "着",
        "没有", "看", "好", "自己", "这", "the", "a", "an", "is", "of", "to"
    };

    private static string[] ExtractKeywords(string text)
    {
        var snippet = text.Length > 500 ? text[..500] : text;
        // 简单分词
        var words = System.Text.RegularExpressions.Regex.Matches(
            snippet, @"[一-龥]{2,}|[a-zA-Z]{3,}")
            .Select(m => m.Value)
            .Where(w => !StopWords.Contains(w) && w.Length >= 2)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .ToArray();
        return words;
    }

    private static int KeywordScore(Dictionary<string, object> pattern, string[] keywords)
    {
        var text = (pattern.GetValueOrDefault("pattern", "").ToString() ?? "")
            + (pattern.GetValueOrDefault("before", "").ToString() ?? "")
            + (pattern.GetValueOrDefault("after", "").ToString() ?? "");
        return keywords.Count(k => text.Contains(k, StringComparison.OrdinalIgnoreCase));
    }

    // ========== 持久化 ==========
    private void UpdatePersona(LearningResult result, string category)
    {
        var persona = GetOrCreatePersona(category);
        persona.EditCount++;

        var profile = LoadProfile(persona);
        var recent = GetRecentPatterns(profile);
        recent.AddRange(result.StylePatterns.Select(p =>
            new Dictionary<string, object>
            {
                ["pattern"] = p.Pattern, ["type"] = p.Type,
                ["before"] = p.Before, ["after"] = p.After,
                ["addedAt"] = DateTime.Now.ToString("s")
            }));
        profile["recentPatterns"] = recent.TakeLast(MaxRecentPatterns).ToList<object>();
        profile["totalEdits"] = persona.EditCount;
        profile["lastUpdated"] = DateTime.Now.ToString("s");

        // 每N次编辑AI压缩成摘要
        if (persona.EditCount % CompressInterval == 0)
        {
            _ = CompressAndSave(persona, profile, category);
        }

        persona.StyleProfileJson = JsonSerializer.Serialize(profile);
        persona.UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        AppSettings.Instance.Db.ExecuteInScope(db => db.Updateable(persona).ExecuteCommand());
    }

    private async Task CompressAndSave(AuthorPersona persona,
        Dictionary<string, object> profile, string category)
    {
        try
        {
            var allPatterns = GetRecentPatterns(profile);
            var raw = string.Join("\n", allPatterns.Select(p =>
                $"- {p.GetValueOrDefault("pattern", "")}"));
            var result = await _ai.GenerateTextAsync(
                "用一段话(250字内)总结作者的写作风格。用自然语言。",
                $"从{persona.EditCount}次编辑学到的风格:\n{raw}",
                0.3, CancellationToken.None);
            profile["summary"] = result.Length > 250 ? result[..250] : result;
            profile["lastCompressedAt"] = DateTime.Now.ToString("s");
            Logger.Info($"Persona {category} compressed (edit #{persona.EditCount})");
        }
        catch (Exception ex) { Logger.Warn($"压缩失败: {ex.Message}"); }
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
            Category = category, EditCount = 0, StyleProfileJson = "{}",
            CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };
        AppSettings.Instance.Db.ExecuteInScope(db => db.Insertable(persona).ExecuteCommand());
        return persona;
    }

    // ========== 工具方法 ==========
    private static List<DiffEntry> ComputeDiffs(string original, string edited)
    {
        var diffs = new List<DiffEntry>();
        var oLines = original.Split('\n'); var eLines = edited.Split('\n');
        for (int i = 0; i < Math.Max(oLines.Length, eLines.Length); i++)
        {
            var o = i < oLines.Length ? oLines[i].Trim() : "";
            var e = i < eLines.Length ? eLines[i].Trim() : "";
            if (o != e && o.Length > 0 && e.Length > 0)
                diffs.Add(new DiffEntry
                {
                    Line = i + 1, Type = IsSensitiveStyle(o, e) ? "sensitive" : "style",
                    Before = o[..Math.Min(o.Length, 100)], After = e[..Math.Min(e.Length, 100)]
                });
        }
        return diffs.Take(20).ToList();
    }

    private static bool IsSensitiveStyle(string before, string after)
        => before.Split(' ').Except(after.Split(' ')).Count() <= 3;

    private static string BuildAnalysisPrompt(string og, string ed,
        List<DiffEntry> diffs, string cat)
    {
        var df = string.Join("\n", diffs.Take(10).Select(d =>
            $"- [{d.Type}] {d.Before} → {d.After}"));
        return "分析用户的编辑修改，提取写作风格偏好。只输出JSON：\n" +
            "{\n  \"style_patterns\": [\n    {\"pattern\": \"模式\", \"before\": \"原文\", \"after\": \"改后\", \"type\": \"style|sensitive\"}\n  ],\n" +
            "  \"discovered_words\": [\n    {\"source\": \"原词\", \"replacement\": \"替换\", \"count\": N, \"category\": \"称谓|概念|行为|用语|自定义\"}\n  ],\n" +
            "  \"summary\": \"一句话总结\"\n}\n\n" +
            $"修改类别: {cat}\n差异:\n{df}";
    }

    private static string? ExtractJson(string text)
    {
        int s = text.IndexOf('{'), e = text.LastIndexOf('}');
        return s >= 0 && e > s ? text[s..(e + 1)] : null;
    }

    private static void ParseStylePatterns(JsonElement root, LearningResult r)
    {
        if (!root.TryGetProperty("style_patterns", out var sp)) return;
        foreach (var item in sp.EnumerateArray())
            r.StylePatterns.Add(new StylePattern
            {
                Pattern = item.GetProperty("pattern").GetString() ?? "",
                Before = item.GetProperty("before").GetString() ?? "",
                After = item.GetProperty("after").GetString() ?? "",
                Type = item.TryGetProperty("type", out var t) ? t.GetString() ?? "style" : "style"
            });
    }

    private static void ParseDiscoveredWords(JsonElement root, LearningResult r)
    {
        if (!root.TryGetProperty("discovered_words", out var dw)) return;
        foreach (var item in dw.EnumerateArray())
            r.DiscoveredWords.Add(new DiscoveredWord
            {
                SourceWord = item.GetProperty("source").GetString() ?? "",
                Replacement = item.GetProperty("replacement").GetString() ?? "",
                Occurrences = item.TryGetProperty("count", out var c) ? c.GetInt32() : 1,
                Category = item.TryGetProperty("category", out var cat)
                    ? cat.GetString() ?? "自定义" : "自定义"
            });
    }

    private void SaveSession(string og, string ed, List<DiffEntry> diffs,
        LearningResult result, string category)
    {
        AppSettings.Instance.Db.ExecuteInScope(db =>
            db.Insertable(new EditSession
            {
                PersonaId = GetOrCreatePersona(category).Id,
                Category = category, OriginalText = og, EditedText = ed,
                DiffsJson = JsonSerializer.Serialize(diffs),
                LearningSummaryJson = JsonSerializer.Serialize(result),
                CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            }).ExecuteCommand());
    }

    private static Dictionary<string, object> LoadProfile(AuthorPersona p)
    {
        if (p.StyleProfileJson == null) return [];
        try { return JsonSerializer.Deserialize<Dictionary<string, object>>(
            p.StyleProfileJson) ?? []; } catch { return []; }
    }

    private static List<Dictionary<string, object>> GetRecentPatterns(
        Dictionary<string, object> p)
    {
        if (!p.TryGetValue("recentPatterns", out var v) || v is not JsonElement je) return [];
        try { return je.EnumerateArray().Select(e => new Dictionary<string, object> {
            ["pattern"] = e.GetProperty("pattern").GetString() ?? "",
            ["type"] = e.GetProperty("type").GetString() ?? "",
            ["before"] = e.TryGetProperty("before", out var b) ? b.GetString() ?? "" : "",
            ["after"] = e.TryGetProperty("after", out var a) ? a.GetString() ?? "" : ""
        }).ToList(); } catch { return []; }
    }
}

public class DiffEntry { public int Line; public string Before = "", After = "", Type = "style"; }
