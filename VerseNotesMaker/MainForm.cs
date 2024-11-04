using System.Text.RegularExpressions;
using System.Text;
using static VerseNotesMaker.QbResult;
using Xceed.Words.NET;
using System.Windows.Forms;
using System.Reflection.Metadata;
using Xceed.Document.NET;
using System.Diagnostics;

namespace VerseNotesMaker
{
    public partial class MainForm : Form
    {
        public string publicPath = @"F:\传道 & 公众号文案\";
        public string readPath = @"F:\个人 & 文档\读经感悟\";

        public string publicPathCyx = @"F:\传道 & 公众号文案\爱与祝福同行\";
        public string publicPathCyxImageRepository = @"F:\传道 & 美图\插图素材\";
        public string publicPathCyxImage = @"F:\传道 & 公众号文案\爱与祝福同行\随机图片50张\";

        public MainForm()
        {
            InitializeComponent();
        }

        private string QueryBible_SqlList版本(string str, int type = 1)
        {
            System.Media.SystemSounds.Exclamation.Play();
            var text = str.Replace("：", ":").Replace("章", ":").Replace("篇", ":").Replace("，", ",").Replace(" ", "").Replace("节", "").Replace("到", "-").Replace("至", "-");
            text = text.Replace("诗:", "诗篇");
            var args = text.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries).ToList();

            if (args.Count != 2)
            {
                return "";
            }

            var chapStr = System.Text.RegularExpressions.Regex.Replace(args[0], @"[^0-9]+", "");
            var chap = 0;
            if (!int.TryParse(chapStr, out chap))
            {
                return "";
            }

            var chineses = args[0].Replace(chap.ToString(), "").Replace("一", "壹").Replace("二", "贰").Replace("三", "叁");
            if (string.IsNullOrEmpty(chineses))
            {
                return "";
            }

            var sec = args[1];

            using (var db = DbContext.GetDbClient())
            {
                var bibleId = db.Queryable<BibleID>().Where(p => p.ShortName == chineses || p.FullName == chineses).First();

                if (bibleId == null)
                {
                    return "";
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

                if (type == 1)
                {
                    resultBuilder.AppendLine($"{GetFullVolume(chineses)} {chap}:{sec}");
                    foreach (var record in recordList)
                    {
                        resultBuilder.AppendLine($"{record.sec} {record.bible_text}");
                    }
                }

                if (type == 4)
                {
                    var rb4list = new List<string>();
                    foreach (var record in recordList)
                    {
                        rb4list.Add(record.bible_text);
                    }
                    resultBuilder.Append($"{string.Join("", rb4list).TrimStart('“').TrimEnd('”')}");
                }

                System.Media.SystemSounds.Hand.Play();
                return resultBuilder.ToString();
                //Clipboard.SetDataObject(resultBuilder.ToString());
            }
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

        private void txt_chapter_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                var verse_content = QueryBible_SqlList版本(this.txt_chapter.Text, 4);
                this.txt_verse.Text = verse_content;
            }
        }

        private void btn_maker_Click(object sender, EventArgs e)
        {
            #region 文本验证

            var fields = new (TextBox TextBox, string ErrorMessage)[]
            {
                (txt_title, "请输入标题"),
                (txt_desc, "请输入简介"),
                (txt_chapter, "请输入章节"),
                (txt_verse, "请输入经文"),
                (txt_content, "请输入内容"),
                (txt_tag1, "请输入标签1"),
                (txt_tag2, "请输入标签2"),
                (txt_tag3, "请输入标签3"),
                (txt_tag4, "请输入标签4"),
                (txt_tag5, "请输入标签5")
            };

            foreach (var (textBox, errorMessage) in fields)
            {
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    lbl_status.Text = errorMessage;
                    return;
                }
            }

            #endregion

            string publicFilePath = $@"{publicPath}{this.txt_title.Text.Trim()}.docx";
            string publicFilePathCyx = $@"{publicPathCyx}{this.txt_title.Text.Trim()}.docx";
            var readFileName = $"{this.txt_chapter.Text.Trim().Replace(":", "：").Replace(",", "，")} {this.txt_title.Text.Trim()}";
            string readFilePath = $@"{readPath}{readFileName}.txt";

            var contentList = this.txt_content.Text.Split("\r\n", StringSplitOptions.RemoveEmptyEntries).Where(p => !string.IsNullOrEmpty(p)).ToList();

            if (this.cbx_cyx.Checked) // 爱与祝福之年特殊模式
            {
                if (File.Exists(publicFilePathCyx))
                {
                    this.lbl_status.Text = $"文件已存在：{publicFilePathCyx}";
                    return;
                }

                MakePublicAccount(publicFilePathCyx, contentList);

                Process.Start(new ProcessStartInfo
                {
                    FileName = publicFilePathCyx,
                    UseShellExecute = true
                });

                CopyRandomImages(publicPathCyxImageRepository, publicPathCyxImage);
            }
            else // 普通模式
            {
                if (File.Exists(publicFilePath))
                {
                    this.lbl_status.Text = $"文件已存在：{publicFilePath}";
                    return;
                }

                if (File.Exists(readFilePath))
                {
                    this.lbl_status.Text = $"文件已存在：{publicFilePath}";
                    return;
                }

                MakePublicAccount(publicFilePath, contentList);
                MakeReadNote(readFileName, readFilePath, contentList);

                Process.Start(new ProcessStartInfo
                {
                    FileName = publicFilePath,
                    UseShellExecute = true
                });

                Process.Start(new ProcessStartInfo
                {
                    FileName = readFilePath,
                    UseShellExecute = true
                });
            }

            this.lbl_status.Text = "生成完成";

            this.txt_title.Clear();
            this.txt_desc.Clear();
            this.txt_chapter.Clear();
            this.txt_verse.Clear();
            this.txt_content.Clear();
            this.txt_tag1.Clear();
            this.txt_tag2.Clear();
            this.txt_tag3.Clear();
            this.txt_tag4.Clear();
            this.txt_tag5.Clear();
        }

        private void MakePublicAccount(string publicFilePath, List<string> contentList)
        {
            string inputFilePath = @"template\公众号文案模板.docx";

            using (DocX document = DocX.Load(inputFilePath))
            {
                document.ReplaceText(new StringReplaceTextOptions() { SearchValue = "{{标题}}", NewValue = this.txt_title.Text.Trim() });
                document.ReplaceText(new StringReplaceTextOptions() { SearchValue = "{{简介}}", NewValue = this.txt_desc.Text.Trim() });
                document.ReplaceText(new StringReplaceTextOptions() { SearchValue = "{{章节}}", NewValue = this.txt_chapter.Text.Trim() });
                document.ReplaceText(new StringReplaceTextOptions() { SearchValue = "{{经文}}", NewValue = this.txt_verse.Text.Trim() });
                document.ReplaceText(new StringReplaceTextOptions() { SearchValue = "{{内容}}", NewValue = string.Join("\r\n\r\n", contentList) });
                document.ReplaceText(new StringReplaceTextOptions() { SearchValue = "{{标签1}}", NewValue = this.txt_tag1.Text.Trim() });
                document.ReplaceText(new StringReplaceTextOptions() { SearchValue = "{{标签2}}", NewValue = this.txt_tag2.Text.Trim() });
                document.ReplaceText(new StringReplaceTextOptions() { SearchValue = "{{标签3}}", NewValue = this.txt_tag3.Text.Trim() });
                document.ReplaceText(new StringReplaceTextOptions() { SearchValue = "{{标签4}}", NewValue = this.txt_tag4.Text.Trim() });
                document.ReplaceText(new StringReplaceTextOptions() { SearchValue = "{{标签5}}", NewValue = this.txt_tag5.Text.Trim() });
                document.SaveAs(publicFilePath);
            }
        }

        private void MakeReadNote(string readFileName, string readFilePath, List<string> contentList)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(readFileName);
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(QueryBible_SqlList版本(this.txt_chapter.Text.Trim()).Trim());
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(string.Join("\r\n", contentList));

            File.WriteAllText(readFilePath, stringBuilder.ToString());
        }

        /// <summary>
        /// 从源文件夹随机选取50张图片（不包含子目录），清空目标文件夹后复制过去。
        /// </summary>
        /// <param name="sourceFolder">源图片文件夹路径</param>
        /// <param name="destinationFolder">目标文件夹路径</param>
        public static void CopyRandomImages(string sourceFolder, string destinationFolder)
        {
            if (!Directory.Exists(sourceFolder))
                throw new DirectoryNotFoundException($"源文件夹不存在: {sourceFolder}");

            // 获取所有图片文件（常见扩展名）
            var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
            var allImages = Directory.GetFiles(sourceFolder)
                                     .Where(f => imageExtensions.Contains(Path.GetExtension(f).ToLower()))
                                     .ToList();

            if (allImages.Count == 0)
                throw new Exception("源文件夹中没有找到任何图片文件。");

            // 随机选取最多50张图片
            var random = new Random();
            var selectedImages = allImages.OrderBy(x => random.Next()).Take(50).ToList();

            // 清空目标文件夹（强制删除，不留在回收站）
            if (Directory.Exists(destinationFolder))
            {
                foreach (var file in Directory.GetFiles(destinationFolder))
                {
                    File.SetAttributes(file, FileAttributes.Normal); // 清除只读
                    File.Delete(file);
                }
            }
            else
            {
                Directory.CreateDirectory(destinationFolder);
            }

            // 复制图片
            foreach (var imagePath in selectedImages)
            {
                var fileName = Path.GetFileName(imagePath);
                var destPath = Path.Combine(destinationFolder, fileName);
                File.Copy(imagePath, destPath, overwrite: true);
            }
        }
    }
}
