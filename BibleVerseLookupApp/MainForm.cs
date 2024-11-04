using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Text;
using Microsoft.Win32;
using Newtonsoft.Json;
using RestSharp;
using BibleVerseLookupApp.Models;
using System.Security.Cryptography.X509Certificates;
using static BibleVerseLookupApp.QbResult;
using System.Reflection.Metadata;

namespace BibleVerseLookupApp
{
    public partial class MainForm : Form
    {
        private KeyboardHook _keyboardHook;
        public MainForm()
        {
            InitializeComponent();
            _keyboardHook = new KeyboardHook();
            _keyboardHook.KeyDownEvent += _keyboardHook_KeyDownEvent;
            _keyboardHook.Start();
        }

        private void QueryBible_SqlList版本(string str)
        {
            System.Media.SystemSounds.Exclamation.Play();
            var text = str.Replace("：", ":").Replace("章", ":").Replace("篇", ":").Replace("，", ",").Replace(" ", "").Replace("节", "").Replace("到", "-").Replace("至", "-");
            text = text.Replace("诗:", "诗篇");
            var args = text.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries).ToList();

            if (args.Count != 2)
            {
                this.lbl_msg.Text = "长度必须是2";
                return;
            }

            var chapStr = System.Text.RegularExpressions.Regex.Replace(args[0], @"[^0-9]+", "");
            var chap = 0;
            if (!int.TryParse(chapStr, out chap))
            {
                this.lbl_msg.Text = "章节有误";
                return;
            }

            var chineses = args[0].Replace(chap.ToString(), "").Replace("一", "壹").Replace("二", "贰").Replace("三", "叁");
            if (string.IsNullOrEmpty(chineses))
            {
                this.lbl_msg.Text = "卷名有误";
                return;
            }

            var sec = args[1];

            this.lbl_msg.Text = $"{chineses} {chap}:{args[1]}";

            using (var db = DbContext.GetDbClient())
            {
                var bibleId = db.Queryable<BibleID>().Where(p => p.ShortName == chineses || p.FullName == chineses).First();

                if (bibleId == null)
                {
                    this.lbl_msg.Text = "卷名有误";
                    return;
                }

                string[] chapterAndVerses = sec.Split(new char[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries);

                var recordList = new List<Record>();
                foreach (var chapterAndVerse in chapterAndVerses)
                {
                    if (chapterAndVerse.Contains('-'))
                    {
                        string[] verseRange = chapterAndVerse.Split('-');
                        int startVerseSN = int.Parse(verseRange[0]); // 开始节
                        int endVerseSN = int.Parse(verseRange[1]); // 结束节

                        var bibleList = db.Queryable<Bible>().Where(p => p.VolumeSN == bibleId.SN && p.ChapterSN == chap && p.VerseSN >= startVerseSN && p.VerseSN <= endVerseSN).ToList();
                        recordList.AddRange(bibleList.Select(p => new Record()
                        {
                            sec = p.VerseSN,
                            bible_text = p.Lection
                        }));
                    }
                    else
                    {
                        int verseSN = int.Parse(chapterAndVerse);

                        var bibleList = db.Queryable<Bible>().Where(p => p.VolumeSN == bibleId.SN && p.ChapterSN == chap && p.VerseSN == verseSN).ToList();
                        recordList.AddRange(bibleList.Select(p => new Record()
                        {
                            sec = p.VerseSN,
                            bible_text = p.Lection
                        }));
                    }
                }

                var resultBuilder = new StringBuilder();

                if (rb_type1.Checked)
                {
                    resultBuilder.AppendLine($"{GetFullVolume(chineses)} {chap}:{sec}");
                    foreach (var record in recordList)
                    {
                        resultBuilder.AppendLine($"{record.sec} {record.bible_text}");
                    }
                }

                if (rb_type2.Checked)
                {
                    foreach (var record in recordList)
                    {
                        resultBuilder.AppendLine($"【{chineses}{chap}:{record.sec}】{record.bible_text}");
                    }
                }

                if (rb_type3.Checked)
                {
                    resultBuilder.Append($"{chineses}{chap}：{sec} ");
                    var rb3list = new List<string>();
                    foreach (var record in recordList)
                    {
                        rb3list.Add(Regex.Replace(Regex.Replace(record.bible_text, @"[^\w\s]", " "), @"\s+", " ").Trim());
                    }
                    resultBuilder.Append(string.Join(" ", rb3list));
                }

                if (rb_type4.Checked)
                {
                    var rb4list = new List<string>();
                    foreach (var record in recordList)
                    {
                        rb4list.Add(record.bible_text);
                    }
                    resultBuilder.Append($"“{string.Join("", rb4list).TrimStart('“').TrimEnd('”')}”");
                }

                Clipboard.SetDataObject(resultBuilder.ToString());

                this.lbl_msg.Text = "已复制到剪贴板";
                System.Media.SystemSounds.Hand.Play();

            }
        }

        private void QueryBible_信爱网API版本(string str)
        {
            System.Media.SystemSounds.Exclamation.Play();
            var text = str.Replace("：", ":").Replace("，", ",").Replace(" ", ""); ;
            var args = text.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            Console.WriteLine();
            if (args.Count != 2)
            {
                this.lbl_msg.Text = "长度必须是2";
                return;
            }

            var chapStr = System.Text.RegularExpressions.Regex.Replace(args[0], @"[^0-9]+", "");
            var chap = 0;
            if (!int.TryParse(chapStr, out chap))
            {
                this.lbl_msg.Text = "章节有误";
                return;
            }

            var chineses = args[0].Replace(chap.ToString(), "");
            if (string.IsNullOrEmpty(chineses))
            {
                this.lbl_msg.Text = "卷名有误";
                return;
            }



            var sec = args[1];

            this.lbl_msg.Text = $"{chineses} {chap}:{args[1]}";


            RestClient client = new RestClient("https://bible.fhl.net/json/qb.php");

            var request = new RestRequest();
            request.AddQueryParameter("chineses", chineses);
            request.AddQueryParameter("chap", chap);
            request.AddQueryParameter("sec", sec);
            request.AddQueryParameter("version", "nstrunv");
            //request.AddQueryParameter("version", "ncv");
            request.AddQueryParameter("strong", 0);
            request.AddQueryParameter("gb", 1);

            var res = client.Get(request);

            var result = JsonConvert.DeserializeObject<QbResult>(res.Content);

            if (result.status != "success")
            {
                Clipboard.SetDataObject(res.Content);
                this.lbl_msg.Text = "接口调用失败";
                try
                {
                    // errorSound.Load();
                    // errorSound.Play();
                }
                catch
                {
                    return;
                }
            }

            foreach (var record in result.record)
            {
                record.bible_text = Regex.Replace(UnicodeToGB(record.bible_text), "[「」『』]", m =>
                {
                    return m.Value switch
                    {
                        "「" => "“",
                        "」" => "”",
                        "『" => "‘",
                        "』" => "’",
                        _ => "",
                    };
                });
            }

            var resultBuilder = new StringBuilder();

            if (rb_type1.Checked)
            {
                resultBuilder.AppendLine($"{GetFullVolume(chineses)} {chap}:{sec}");
                foreach (var record in result.record)
                {
                    resultBuilder.AppendLine($"{record.sec} {record.bible_text}");
                }
            }

            if (rb_type2.Checked)
            {
                foreach (var record in result.record)
                {
                    resultBuilder.AppendLine($"【{chineses}{chap}:{record.sec}】{record.bible_text}");
                }
            }



            if (rb_type3.Checked)
            {
                resultBuilder.Append($"{chineses}{chap}：{sec} ");
                var rb3list = new List<string>();
                foreach (var record in result.record)
                {
                    rb3list.Add(Regex.Replace(Regex.Replace(record.bible_text, @"[^\w\s]", " "), @"\s+", " ").Trim());
                }
                resultBuilder.Append(string.Join(" ", rb3list));
            }


            if (rb_type4.Checked)
            {
                resultBuilder.Append($"{chineses}{chap}：{sec} ");
                var rb4list = new List<string>();
                foreach (var record in result.record)
                {
                    rb4list.Add(Regex.Replace(record.bible_text, @"[\p{P}]", "，"));
                }
                resultBuilder.Append(string.Join("。", rb4list));
            }

            Clipboard.SetDataObject(resultBuilder.ToString());

            this.lbl_msg.Text = "已复制到剪贴板";
            System.Media.SystemSounds.Hand.Play();
        }

        private string GetFullVolume(string str)
        {
            // 如果传入的已经是全称，则直接返回
            switch (str)
            {
                case "创世记":
                case "出埃及记":
                case "利未记":
                case "民数记":
                case "申命记":
                case "约书亚记":
                case "士师记":
                case "路得记":
                case "撒母耳记上":
                case "撒母耳记下":
                case "列王纪上":
                case "列王纪下":
                case "历代志上":
                case "历代志下":
                case "以斯拉记":
                case "尼希米记":
                case "以斯帖记":
                case "约伯记":
                case "诗篇":
                case "箴言":
                case "传道书":
                case "雅歌":
                case "以赛亚书":
                case "耶利米书":
                case "耶利米哀歌":
                case "以西结书":
                case "但以理书":
                case "何西阿书":
                case "约珥书":
                case "阿摩司书":
                case "俄巴底亚书":
                case "约拿书":
                case "弥迦书":
                case "那鸿书":
                case "哈巴谷书":
                case "西番雅书":
                case "哈该书":
                case "撒迦利亚书":
                case "玛拉基书":
                case "马太福音":
                case "马可福音":
                case "路加福音":
                case "约翰福音":
                case "使徒行传":
                case "罗马书":
                case "哥林多前书":
                case "哥林多后书":
                case "加拉太书":
                case "以弗所书":
                case "腓立比书":
                case "歌罗西书":
                case "帖撒罗尼迦前书":
                case "帖撒罗尼迦后书":
                case "提摩太前书":
                case "提摩太后书":
                case "提多书":
                case "腓利门书":
                case "希伯来书":
                case "雅各书":
                case "彼得前书":
                case "彼得后书":
                case "约翰一书":
                case "约翰二书":
                case "约翰三书":
                case "犹大书":
                case "启示录":
                    return str;

                default:
                    break;
            }

            return str switch
            {
                "创" => "创世记",
                "出" => "出埃及记",
                "利" => "利未记",
                "民" => "民数记",
                "申" => "申命记",
                "书" => "约书亚记",
                "士" => "士师记",
                "得" => "路得记",
                "撒上" => "撒母耳记上",
                "撒下" => "撒母耳记下",
                "王上" => "列王纪上",
                "王下" => "列王纪下",
                "代上" => "历代志上",
                "代下" => "历代志下",
                "拉" => "以斯拉记",
                "尼" => "尼希米记",
                "斯" => "以斯帖记",
                "伯" => "约伯记",
                "诗" => "诗篇",
                "箴" => "箴言",
                "传" => "传道书",
                "歌" => "雅歌",
                "赛" => "以赛亚书",
                "耶" => "耶利米书",
                "哀" => "耶利米哀歌",
                "结" => "以西结书",
                "但" => "但以理书",
                "何" => "何西阿书",
                "珥" => "约珥书",
                "摩" => "阿摩司书",
                "俄" => "俄巴底亚书",
                "拿" => "约拿书",
                "弥" => "弥迦书",
                "鸿" => "那鸿书",
                "哈" => "哈巴谷书",
                "番" => "西番雅书",
                "该" => "哈该书",
                "亚" => "撒迦利亚书",
                "玛" => "玛拉基书",
                "太" => "马太福音",
                "可" => "马可福音",
                "路" => "路加福音",
                "约" => "约翰福音",
                "徒" => "使徒行传",
                "罗" => "罗马书",
                "林前" => "哥林多前书",
                "林后" => "哥林多后书",
                "加" => "加拉太书",
                "弗" => "以弗所书",
                "腓" => "腓立比书",
                "西" => "歌罗西书",
                "帖前" => "帖撒罗尼迦前书",
                "帖后" => "帖撒罗尼迦后书",
                "提前" => "提摩太前书",
                "提后" => "提摩太后书",
                "多" => "提多书",
                "门" => "腓利门书",
                "来" => "希伯来书",
                "雅" => "雅各书",
                "彼前" => "彼得前书",
                "彼后" => "彼得后书",
                "约一" => "约翰一书",
                "约二" => "约翰二书",
                "约三" => "约翰三书",
                "约壹" => "约翰一书",
                "约贰" => "约翰二书",
                "约叁" => "约翰三书",
                "犹" => "犹大书",
                "启" => "启示录",
                _ => "未知卷",
            };
        }

        private string UnicodeToGB(string str)
        {
            return str;
            MatchCollection mc = Regex.Matches(str, "([\\w]+)|(\\\\u([\\w]{4}))");
            if (mc != null && mc.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                foreach (Match m2 in mc)
                {
                    string v = m2.Value;
                    if (v.StartsWith("\\"))
                    {
                        string word = v.Substring(2);
                        byte[] codes = new byte[2];
                        int code = Convert.ToInt32(word.Substring(0, 2), 16);
                        int code2 = Convert.ToInt32(word.Substring(2), 16);
                        codes[0] = (byte)code2;
                        codes[1] = (byte)code;
                        sb.Append(Encoding.Unicode.GetString(codes));
                    }
                    else
                    {
                        sb.Append(v);
                    }
                }
                return sb.ToString();
            }
            else
            {
                return str;
            }
        }

        private void _keyboardHook_KeyDownEvent(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space && (Control.ModifierKeys & Keys.Control) == Keys.Control)
            {
                var text = Clipboard.GetText();
                QueryBible_SqlList版本(text);
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _keyboardHook.Stop();
        }

        private void lbl_help_Click(object sender, EventArgs e)
        {
            new HelpForm().Show();
        }
    }
}
