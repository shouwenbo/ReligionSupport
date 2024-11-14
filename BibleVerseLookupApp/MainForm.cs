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

        private void QueryBible_SqlList�汾(string str)
        {
            System.Media.SystemSounds.Exclamation.Play();
            var text = str.Replace("��", ":").Replace("��", ",").Replace(" ", ""); ;
            var args = text.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries).ToList();

            if (args.Count != 2)
            {
                this.lbl_msg.Text = "���ȱ�����2";
                return;
            }

            var chapStr = System.Text.RegularExpressions.Regex.Replace(args[0], @"[^0-9]+", "");
            var chap = 0;
            if (!int.TryParse(chapStr, out chap))
            {
                this.lbl_msg.Text = "�½�����";
                return;
            }

            var chineses = args[0].Replace(chap.ToString(), "");
            if (string.IsNullOrEmpty(chineses))
            {
                this.lbl_msg.Text = "��������";
                return;
            }

            var sec = args[1];

            this.lbl_msg.Text = $"{chineses} {chap}:{args[1]}";

            using (var db = DbContext.GetDbClient())
            {
                var bibleId = db.Queryable<BibleID>().Where(p => p.ShortName == chineses || p.FullName == chineses).First();

                if (bibleId == null)
                {
                    this.lbl_msg.Text = "��������";
                    return;
                }

                string[] chapterAndVerses = sec.Split(new char[] { ',', '��' }, StringSplitOptions.RemoveEmptyEntries);

                var recordList = new List<Record>();
                foreach (var chapterAndVerse in chapterAndVerses)
                {
                    if (chapterAndVerse.Contains('-'))
                    {
                        string[] verseRange = chapterAndVerse.Split('-');
                        int startVerseSN = int.Parse(verseRange[0]);
                        int endVerseSN = int.Parse(verseRange[1]);

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
                        resultBuilder.AppendLine($"��{chineses}{chap}:{record.sec}��{record.bible_text}");
                    }
                }

                if (rb_type3.Checked)
                {
                    resultBuilder.Append($"{chineses}{chap}��{sec} ");
                    var rb3list = new List<string>();
                    foreach (var record in recordList)
                    {
                        rb3list.Add(Regex.Replace(Regex.Replace(record.bible_text, @"[^\w\s]", " "), @"\s+", " ").Trim());
                    }
                    resultBuilder.Append(string.Join(" ", rb3list));
                }

                Clipboard.SetDataObject(resultBuilder.ToString());

                this.lbl_msg.Text = "�Ѹ��Ƶ�������";
                System.Media.SystemSounds.Hand.Play();

            }
        }

        private void QueryBible_�Ű���API�汾(string str)
        {
            System.Media.SystemSounds.Exclamation.Play();
            var text = str.Replace("��", ":").Replace("��", ",").Replace(" ", ""); ;
            var args = text.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            Console.WriteLine();
            if (args.Count != 2)
            {
                this.lbl_msg.Text = "���ȱ�����2";
                return;
            }

            var chapStr = System.Text.RegularExpressions.Regex.Replace(args[0], @"[^0-9]+", "");
            var chap = 0;
            if (!int.TryParse(chapStr, out chap))
            {
                this.lbl_msg.Text = "�½�����";
                return;
            }

            var chineses = args[0].Replace(chap.ToString(), "");
            if (string.IsNullOrEmpty(chineses))
            {
                this.lbl_msg.Text = "��������";
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
                this.lbl_msg.Text = "�ӿڵ���ʧ��";
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
                record.bible_text = Regex.Replace(UnicodeToGB(record.bible_text), "[��������]", m =>
                {
                    return m.Value switch
                    {
                        "��" => "��",
                        "��" => "��",
                        "��" => "��",
                        "��" => "��",
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
                    resultBuilder.AppendLine($"��{chineses}{chap}:{record.sec}��{record.bible_text}");
                }
            }



            if (rb_type3.Checked)
            {
                resultBuilder.Append($"{chineses}{chap}��{sec} ");
                var rb3list = new List<string>();
                foreach (var record in result.record)
                {
                    rb3list.Add(Regex.Replace(Regex.Replace(record.bible_text, @"[^\w\s]", " "), @"\s+", " ").Trim());
                }
                resultBuilder.Append(string.Join(" ", rb3list));
            }

            Clipboard.SetDataObject(resultBuilder.ToString());

            this.lbl_msg.Text = "�Ѹ��Ƶ�������";
            System.Media.SystemSounds.Hand.Play();
        }

        private string GetFullVolume(string str)
        {
            // ���������Ѿ���ȫ�ƣ���ֱ�ӷ���
            switch (str)
            {
                case "������":
                case "��������":
                case "��δ��":
                case "������":
                case "������":
                case "Լ���Ǽ�":
                case "ʿʦ��":
                case "·�ü�":
                case "��ĸ������":
                case "��ĸ������":
                case "��������":
                case "��������":
                case "����־��":
                case "����־��":
                case "��˹����":
                case "��ϣ�׼�":
                case "��˹����":
                case "Լ����":
                case "ʫƪ":
                case "����":
                case "������":
                case "�Ÿ�":
                case "��������":
                case "Ү������":
                case "Ү���װ���":
                case "��������":
                case "��������":
                case "��������":
                case "Լ����":
                case "��Ħ˾��":
                case "��͵�����":
                case "Լ����":
                case "������":
                case "�Ǻ���":
                case "���͹���":
                case "��������":
                case "������":
                case "����������":
                case "��������":
                case "��̫����":
                case "��ɸ���":
                case "·�Ӹ���":
                case "Լ������":
                case "ʹͽ�д�":
                case "������":
                case "���ֶ�ǰ��":
                case "���ֶ����":
                case "����̫��":
                case "�Ը�����":
                case "��������":
                case "��������":
                case "����������ǰ��":
                case "���������Ⱥ���":
                case "��Ħ̫ǰ��":
                case "��Ħ̫����":
                case "�����":
                case "��������":
                case "ϣ������":
                case "�Ÿ���":
                case "�˵�ǰ��":
                case "�˵ú���":
                case "Լ��һ��":
                case "Լ������":
                case "Լ������":
                case "�̴���":
                case "��ʾ¼":
                    return str;

                default:
                    break;
            }

            return str switch
            {
                "��" => "������",
                "��" => "��������",
                "��" => "��δ��",
                "��" => "������",
                "��" => "������",
                "��" => "Լ���Ǽ�",
                "ʿ" => "ʿʦ��",
                "��" => "·�ü�",
                "����" => "��ĸ������",
                "����" => "��ĸ������",
                "����" => "��������",
                "����" => "��������",
                "����" => "����־��",
                "����" => "����־��",
                "��" => "��˹����",
                "��" => "��ϣ�׼�",
                "˹" => "��˹����",
                "��" => "Լ����",
                "ʫ" => "ʫƪ",
                "��" => "����",
                "��" => "������",
                "��" => "�Ÿ�",
                "��" => "��������",
                "Ү" => "Ү������",
                "��" => "Ү���װ���",
                "��" => "��������",
                "��" => "��������",
                "��" => "��������",
                "��" => "Լ����",
                "Ħ" => "��Ħ˾��",
                "��" => "��͵�����",
                "��" => "Լ����",
                "��" => "������",
                "��" => "�Ǻ���",
                "��" => "���͹���",
                "��" => "��������",
                "��" => "������",
                "��" => "����������",
                "��" => "��������",
                "̫" => "��̫����",
                "��" => "��ɸ���",
                "·" => "·�Ӹ���",
                "Լ" => "Լ������",
                "ͽ" => "ʹͽ�д�",
                "��" => "������",
                "��ǰ" => "���ֶ�ǰ��",
                "�ֺ�" => "���ֶ����",
                "��" => "����̫��",
                "��" => "�Ը�����",
                "��" => "��������",
                "��" => "��������",
                "��ǰ" => "����������ǰ��",
                "����" => "���������Ⱥ���",
                "��ǰ" => "��Ħ̫ǰ��",
                "���" => "��Ħ̫����",
                "��" => "�����",
                "��" => "��������",
                "��" => "ϣ������",
                "��" => "�Ÿ���",
                "��ǰ" => "�˵�ǰ��",
                "�˺�" => "�˵ú���",
                "Լһ" => "Լ��һ��",
                "Լ��" => "Լ������",
                "Լ��" => "Լ������",
                "��" => "�̴���",
                "��" => "��ʾ¼",
                _ => "δ֪��",
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
                QueryBible_SqlList�汾(text);
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _keyboardHook.Stop();
        }
    }
}
