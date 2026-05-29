using SqlSugar;

namespace WeChatPublisher.Models;

[SugarTable("Bible")]
public class Bible
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }
    public int VolumeSN { get; set; }
    public int ChapterSN { get; set; }
    public int VerseSN { get; set; }
    public string Lection { get; set; } = "";
    public int SoundBegin { get; set; }
    public int SoundEnd { get; set; }
}

[SugarTable("BibleID")]
public class BibleID
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int SN { get; set; }
    public int KindSN { get; set; }
    public int ChapterNumber { get; set; }
    public int NewOrOld { get; set; }
    public string? PinYin { get; set; }
    public string ShortName { get; set; } = "";
    public string FullName { get; set; } = "";
}
