using WeChatPublisher.Models;

namespace WeChatPublisher.Services;

public class SensitiveWordService
{
    private readonly AppSettings _settings;
    private List<SensitiveWord> _cache = [];
    private DateTime _cacheLastRefresh = DateTime.MinValue;

    public SensitiveWordService()
    {
        _settings = AppSettings.Instance;
    }

    public void RefreshCache()
    {
        _cache = GetAllFromDb();
        _cacheLastRefresh = DateTime.Now;
    }

    private List<SensitiveWord> GetCache()
    {
        if (_cache.Count == 0) RefreshCache();
        return _cache;
    }

    public List<SensitiveWord> GetAll()
    {
        return GetAllFromDb();
    }

    private List<SensitiveWord> GetAllFromDb()
    {
        return _settings.Db.ExecuteInScope(db =>
            db.Queryable<SensitiveWord>().OrderBy(s => s.Id, SqlSugar.OrderByType.Asc).ToList());
    }

    public void Save(SensitiveWord word)
    {
        word.UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        if (word.Id == 0)
        {
            word.CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            _settings.Db.ExecuteInScope(db => db.Insertable(word).ExecuteCommand());
        }
        else
        {
            _settings.Db.ExecuteInScope(db => db.Updateable(word).ExecuteCommand());
        }
        RefreshCache();
    }

    public void Delete(int id)
    {
        _settings.Db.ExecuteInScope(db => db.Deleteable<SensitiveWord>().In(id).ExecuteCommand());
        RefreshCache();
    }

    public void SeedDefaults()
    {
        var existing = GetAllFromDb();
        if (existing.Count > 0) return;

        var defaults = new List<SensitiveWord>
        {
            new() { SourceWord = "神",     ReplacementWords = "父亲;阿爸;源头",       Category = "称谓", StrictLevel = 1 },
            new() { SourceWord = "上帝",   ReplacementWords = "父亲;源头",            Category = "称谓", StrictLevel = 1 },
            new() { SourceWord = "耶稣",   ReplacementWords = "老师;TA;榜样",          Category = "人名", StrictLevel = 1 },
            new() { SourceWord = "基督",   ReplacementWords = "老师;TA",              Category = "称谓", StrictLevel = 1 },
            new() { SourceWord = "圣灵",   ReplacementWords = "内心引导;内在力量",     Category = "概念", StrictLevel = 1 },
            new() { SourceWord = "主",     ReplacementWords = "TA;父亲",              Category = "称谓", StrictLevel = 1 },
            new() { SourceWord = "基督教", ReplacementWords = "信仰团体",              Category = "组织", StrictLevel = 1 },
            new() { SourceWord = "教堂",   ReplacementWords = "聚会场所;家园",         Category = "场所", StrictLevel = 0 },
            new() { SourceWord = "祷告",   ReplacementWords = "默想;交流;倾诉",        Category = "行为", StrictLevel = 0 },
            new() { SourceWord = "圣经",   ReplacementWords = "智慧书;经典",           Category = "物品", StrictLevel = 1 },
            new() { SourceWord = "福音",   ReplacementWords = "好消息;佳音",           Category = "概念", StrictLevel = 0 },
            new() { SourceWord = "救赎",   ReplacementWords = "更新;改变",             Category = "概念", StrictLevel = 0 },
            new() { SourceWord = "恩典",   ReplacementWords = "礼物;馈赠",             Category = "概念", StrictLevel = 0 },
            new() { SourceWord = "阿门",   ReplacementWords = "诚心所愿;真心祝愿",     Category = "用语", StrictLevel = 0 },
            new() { SourceWord = "哈利路亚", ReplacementWords = "赞美;感恩",           Category = "用语", StrictLevel = 1 },
            new() { SourceWord = "弥赛亚", ReplacementWords = "老师;TA",              Category = "概念", StrictLevel = 1 },
            new() { SourceWord = "救主",   ReplacementWords = "老师;TA",              Category = "称谓", StrictLevel = 1 },
            new() { SourceWord = "敬拜",   ReplacementWords = "赞美;表达敬意",         Category = "行为", StrictLevel = 0 },
            new() { SourceWord = "团契",   ReplacementWords = "聚会;小组",             Category = "活动", StrictLevel = 1 },
            new() { SourceWord = "传道",   ReplacementWords = "分享;讲述",             Category = "行为", StrictLevel = 0 },
        };

        foreach (var word in defaults)
        {
            word.CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            word.UpdatedAt = word.CreatedAt;
            word.IsEnabled = 1;
        }

        _settings.Db.ExecuteInScope(db => db.Insertable(defaults).ExecuteCommand());
        RefreshCache();
    }

    public List<SensitiveWordMatch> Scan(string text)
    {
        var matches = new List<SensitiveWordMatch>();
        var rules = GetCache().Where(r => r.IsEnabled == 1).ToList();

        foreach (var rule in rules)
        {
            int idx = 0;
            while ((idx = text.IndexOf(rule.SourceWord, idx, StringComparison.Ordinal)) != -1)
            {
                matches.Add(new SensitiveWordMatch
                {
                    Rule = rule,
                    Position = idx,
                    Length = rule.SourceWord.Length,
                    MatchedText = text.Substring(idx, rule.SourceWord.Length)
                });
                idx += rule.SourceWord.Length;
            }
        }

        return matches.OrderBy(m => m.Position).ToList();
    }

    public string Sanitize(string text, List<SensitiveWordMatch>? matches = null)
    {
        matches ??= Scan(text);
        var forced = matches.Where(m => m.Rule.StrictLevel == 1)
                            .OrderByDescending(m => m.Position)
                            .ToList();

        var result = text;
        foreach (var match in forced)
        {
            var replacements = match.Rule.ReplacementWords.Split(';');
            var replacement = replacements[0].Trim();
            result = result.Remove(match.Position, match.Length)
                           .Insert(match.Position, replacement);
        }
        return result;
    }

    public bool HasForcedMatches(string text, out int count)
    {
        var matches = Scan(text);
        count = matches.Count(m => m.Rule.StrictLevel == 1);
        return count > 0;
    }
}
