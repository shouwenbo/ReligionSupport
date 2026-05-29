using SqlSugar;
using WeChatPublisher.Models;

namespace WeChatPublisher.Services;

public class BibleService
{
    private readonly string _dbPath;

    public BibleService(string dbPath)
    {
        _dbPath = dbPath;
    }

    private SqlSugarClient GetDb()
    {
        return new SqlSugarClient(new ConnectionConfig
        {
            DbType = DbType.Sqlite,
            ConnectionString = $"Data Source={_dbPath}",
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute
        });
    }

    public string QueryVerses(string reference, int format = 1)
    {
        using var db = GetDb();
        reference = reference.Replace("：", ":").Replace("章", ":").Replace("，", ",")
                             .Replace("节", "").Replace("到", "-").Replace("至", "-");

        if (reference.StartsWith("诗:")) reference = "诗篇" + reference[2..];

        int colonIdx = reference.IndexOf(':');
        if (colonIdx <= 0) return "格式错误，请使用如：约3:16 或 创世记1:1-3";

        string bookPart = reference[..colonIdx];
        string versePart = reference[(colonIdx + 1)..];

        string bookName = GetFullVolumeName(bookPart);
        var bibleId = db.Queryable<BibleID>().First(b => b.ShortName == bookPart || b.FullName == bookPart);
        if (bibleId == null) return $"未找到书卷：{bookPart}";

        int chapterNum;
        string verseRange;
        int dashIdx = versePart.IndexOf('-');
        if (dashIdx > 0)
        {
            chapterNum = int.Parse(versePart[..dashIdx]);
            verseRange = versePart;
        }
        else
        {
            var parts = versePart.Split(',');
            chapterNum = int.Parse(parts[0]);
            verseRange = versePart;
        }

        var verses = ParseVerseRange(verseRange);
        var result = new List<string>();

        foreach (var (start, end) in verses)
        {
            var rows = db.Queryable<Bible>()
                .Where(b => b.VolumeSN == bibleId.SN
                         && b.ChapterSN == chapterNum
                         && b.VerseSN >= start
                         && b.VerseSN <= end)
                .OrderBy(b => b.VerseSN, OrderByType.Asc)
                .ToList();

            foreach (var row in rows)
            {
                if (format == 1)
                    result.Add($"{row.VerseSN} {row.Lection}");
                else
                    result.Add(row.Lection.Trim('"', ' ', '“', '”'));
            }
        }

        string text = format == 1
            ? $"{bookName} {chapterNum}:{verseRange}\r\n" + string.Join("\r\n", result)
            : string.Join("", result);

        return text;
    }

    public string GetFullVolumeName(string shortName)
    {
        return shortName switch
        {
            "创" or "创世记" => "创世记", "出" or "出埃及记" => "出埃及记",
            "利" or "利未记" => "利未记", "民" or "民数记" => "民数记",
            "申" or "申命记" => "申命记", "书" or "约书亚记" => "约书亚记",
            "士" or "士师记" => "士师记", "得" or "路得记" => "路得记",
            "撒上" or "撒母耳记上" => "撒母耳记上", "撒下" or "撒母耳记下" => "撒母耳记下",
            "王上" or "列王纪上" => "列王纪上", "王下" or "列王纪下" => "列王纪下",
            "代上" or "历代志上" => "历代志上", "代下" or "历代志下" => "历代志下",
            "拉" or "以斯拉记" => "以斯拉记", "尼" or "尼希米记" => "尼希米记",
            "斯" or "以斯帖记" => "以斯帖记", "伯" or "约伯记" => "约伯记",
            "诗" or "诗篇" => "诗篇", "箴" or "箴言" => "箴言",
            "传" or "传道书" => "传道书", "歌" or "雅歌" => "雅歌",
            "赛" or "以赛亚书" => "以赛亚书", "耶" or "耶利米书" => "耶利米书",
            "哀" or "耶利米哀歌" => "耶利米哀歌", "结" or "以西结书" => "以西结书",
            "但" or "但以理书" => "但以理书", "何" or "何西阿书" => "何西阿书",
            "珥" or "约珥书" => "约珥书", "摩" or "阿摩司书" => "阿摩司书",
            "俄" or "俄巴底亚书" => "俄巴底亚书", "拿" or "约拿书" => "约拿书",
            "弥" or "弥迦书" => "弥迦书", "鸿" or "那鸿书" => "那鸿书",
            "哈" or "哈巴谷书" => "哈巴谷书", "番" or "西番雅书" => "西番雅书",
            "该" or "哈该书" => "哈该书", "亚" or "撒迦利亚书" => "撒迦利亚书",
            "玛" or "玛拉基书" => "玛拉基书",
            "太" or "马太福音" => "马太福音", "可" or "马可福音" => "马可福音",
            "路" or "路加福音" => "路加福音", "约" or "约翰福音" => "约翰福音",
            "徒" or "使徒行传" => "使徒行传", "罗" or "罗马书" => "罗马书",
            "林前" or "哥林多前书" => "哥林多前书", "林后" or "哥林多后书" => "哥林多后书",
            "加" or "加拉太书" => "加拉太书", "弗" or "以弗所书" => "以弗所书",
            "腓" or "腓立比书" => "腓立比书", "西" or "歌罗西书" => "歌罗西书",
            "帖前" or "帖撒罗尼迦前书" => "帖撒罗尼迦前书", "帖后" or "帖撒罗尼迦后书" => "帖撒罗尼迦后书",
            "提前" or "提摩太前书" => "提摩太前书", "提后" or "提摩太后书" => "提摩太后书",
            "多" or "提多书" => "提多书", "门" or "腓利门书" => "腓利门书",
            "来" or "希伯来书" => "希伯来书", "雅" or "雅各书" => "雅各书",
            "彼前" or "彼得前书" => "彼得前书", "彼后" or "彼得后书" => "彼得后书",
            "约壹" or "约一" or "约翰一书" => "约翰一书", "约贰" or "约二" or "约翰二书" => "约翰二书",
            "约叁" or "约三" or "约翰三书" => "约翰三书", "犹" or "犹大书" => "犹大书",
            "启" or "启示录" => "启示录",
            _ => shortName
        };
    }

    private static List<(int start, int end)> ParseVerseRange(string versePart)
    {
        var result = new List<(int, int)>();
        var parts = versePart.Split(',');

        foreach (var part in parts)
        {
            int dashIdx = part.IndexOf('-');
            if (dashIdx > 0)
            {
                int start = int.Parse(part[..dashIdx]);
                int end = int.Parse(part[(dashIdx + 1)..]);
                for (int v = start; v <= end; v++)
                    result.Add((v, v));
            }
            else
            {
                int v = int.Parse(part);
                result.Add((v, v));
            }
        }
        return result;
    }
}
