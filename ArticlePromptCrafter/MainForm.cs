using System.Text;
using System.Text.RegularExpressions;
using Xceed.Document.NET;
using Xceed.Words.NET;
using static System.Net.Mime.MediaTypeNames;

namespace ArticlePromptCrafter
{
    public partial class MainForm : Form
    {
        private const string DocxFolderPath = @"F:\传道 & 公众号文案\";

        public MainForm()
        {
            InitializeComponent();
        }

        private void btn_create_Click(object sender, EventArgs e)
        {
            string folderPath = @"F:\传道 & 公众号文案";
            var files = Directory.GetFiles(folderPath, "*.docx")
                                 .Where(f => !Path.GetFileName(f).StartsWith("~$"))
                                 .ToList();

            if (files.Count == 0)
            {
                MessageBox.Show("未找到有效的 docx 文件！");
                return;
            }

            var random = new Random();
            var selectedFile = files[random.Next(files.Count)];

            string title = "", desc = "", verse = "", content = "";

            try
            {
                using var doc = DocX.Load(selectedFile);
                var paragraphs = doc.Paragraphs.Select(p => p.Text.Trim()).Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
                var text = string.Join("\n", paragraphs);

                var match = Regex.Match(text, @"^标题：(.*)\n简介：(.*)\n(.*)\n([\s\S]*?)\n#(.+)$");
                if (!match.Success)
                {
                    MessageBox.Show("文件格式不符合规范，请检查标题、简介、经文和内容的格式。");
                    return;
                }

                title = match.Groups[1].Value.Trim();
                desc = match.Groups[2].Value.Trim();
                verse = match.Groups[3].Value.Trim();
                content = match.Groups[4].Value.Trim();
            }
            catch (Exception ex)
            {
                MessageBox.Show("读取 docx 出错：" + ex.Message);
                return;
            }

            // 构造 Prompt
            string prompt = BuildPrompt(title, desc, verse, content);

            Clipboard.SetDataObject(prompt);

            this.lbl_status.Text = $"已将生成的 Prompt 复制到剪贴板！ {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
        }

        private string BuildPrompt(string title, string desc, string verse, string content)
        {
            return $@"
你是一位公众号文案创作者，擅长用温暖、柔和、真诚的文字打动人心。
本公众号的主题为“爱与祝福同行”，文章整体风格应带有温暖、柔和、真诚的色调，传递希望、安慰、连接、鼓励的情感，但不要刻意强调“爱与祝福”字样，而是在不经意间自然流露。
我会提供一篇文章的四个部分，包括标题、简介、经文和正文内容（见文末占位符）。请按以下三阶段严格执行、按序输出，禁止跳过，且三个阶段不得向我提问 —— 自动深度思考并为每一步作出最优选择，完成后再进入下一阶段。

最重要的导出要求（必须遵守）：
1.最终导出的内容必须在最开头放置经文原文（用户提供的经文），不可以放在正文中间或末尾。
2.最终导出必须包含一段简介（1–2句）并显著呈现。
3.最终导出必须包含 五个两字标签（格式为：#xx #xx #xx #xx #xx），并放在正文结尾或显眼位置。
4.三个阶段都不得向用户提问或请求确认；在每个阶段内直接给出最佳结果并自动过渡到下一阶段，直至完成第三阶段的成品输出。

----

阶段说明（按顺序执行 —— 不做提问）

第一阶段 — 思路解析（不要写成品）
在不向用户提问的前提下，阅读并分析用户提供的【标题】【简介】【经文】【内容】，输出如下内容（仅此，不写成品）：
1.用五到八句话总结原文的核心思想、情绪基调、读者受众
2.列出需要保留的关键信息与细节（要点式列出）。
3.标出需要替换或软化的宗教/敏感用语，并给出明确替换词表（例如：神→父亲/源头；耶稣→老师/榜样；圣灵→内心引导 等）。
4.给出三种可选的文章重构思路（每项一短句描述，如：故事化叙述、情绪递进、场景化描写），并自动选出最适合原文本的一个思路（并说明选此思路的理由，1–2句）。
注意：本阶段只做分析与选择，不写正文草稿。

第二阶段 — 结构设计（仍不要写成品）
基于第一阶段的结论，自动设计并输出下列内容（不要向用户提问）：
1.提供三个新标题及对应三个不同风格的简介（每组标题+简介均为一行，风格要区分明显：如“温情生活型 / 诗意感悟型 / 生活哲思型”）。
2.规划全文的段落结构（明确段落数，建议不超过6段），并逐段说明每段核心信息与情绪走向（每段1–2句）。
3.确认经文的使用方式：如果用户提供的经文清晰可辨，则直接采用该章节（并在最终输出最开头展示原文）；若用户提供经文不完整或无法识别，则自动匹配最贴切的一节圣经章节并只列出章节编号（例如：赛7:14）。
4.明确在成品中经文放置位置（最开头）、简介放置位置、以及五个两字标签放置位置。

第三阶段 — 成品创作
按第二阶段设计的结构，自动完成整篇文章的写作并输出最终成品（直接输出成品，不提问），必须满足以下规则：
1.经文原文（用户提供）必须放在最开头部分（原文不改写，若需生活化解读可另起段落进行解释）。
2.文章正文须为深度改写，用全新句式和表达方式呈现，语言温暖、细腻、真诚，传递希望、安慰、鼓励的氛围，但不要明显重复或刻意强调“爱与祝福”字样。
3.在文章末尾以“愿xxxx”形式收尾（xxxx为结合全文的真诚祝愿，完整一句）。
4.在最终成品中显著包含：简介（1–2句）和五个两字标签（格式：#xx #xx #xx #xx #xx）。标签须由两个汉字组成，且与文章主题相关。
5.请在文章开头单独写一段简介（1-2句话），并于文章结尾附上5个两个字标签，格式为 #xx #xx #xx #xx #xx

---

【标题】：{title}

【简介】：{desc}

【经文】：{verse}

【内容】：{content}

---

请使用以上Prompt 执行写作任务，记住：经文必须放最前面，最终导出要含简介与五个两字标签，三个阶段不得提问并需自动做出最优选择且按序输出。
";
        }

        #region 检查文件是否符合规范

        private void CheckFolder()
        {
            string folderPath = @"F:\传道 & 公众号文案";

            if (!Directory.Exists(folderPath))
            {
                MessageBox.Show("文件夹不存在！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var docxFiles = Directory
                .GetFiles(folderPath, "*.docx", SearchOption.TopDirectoryOnly)
                .Where(f => !Path.GetFileName(f).StartsWith("~$"))
                .ToList();

            string pattern = @"^标题：(.*)\n简介：(.*)\n(.*)\n([\s\S]*?)\n#(.+)$";


            var invalidFiles = new List<string>();
            // int previewCount = 0;

            foreach (var file in docxFiles)
            {
                try
                {
                    string text;
                    using (var doc = DocX.Load(file))
                    {
                        text = doc.Text.Trim();

                        var paragraphs = doc.Paragraphs.Select(p => p.Text.Trim()).Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
                        text = string.Join("\n", paragraphs);
                    }

                    var match = Regex.Match(text, pattern);
                    if (!match.Success)
                    {
                        invalidFiles.Add(Path.GetFileName(file));
                        continue;
                    }

                    string title = match.Groups[1].Value.Trim();
                    string summary = match.Groups[2].Value.Trim();
                    string verse = match.Groups[3].Value.Trim();
                    string body = match.Groups[4].Value.Trim();
                    string tags = match.Groups[5].Value.Trim();

                    Console.WriteLine($"✅ 文件：{Path.GetFileName(file)}");
                    Console.WriteLine($"标题：{title}");
                    Console.WriteLine($"简介：{summary}");
                    Console.WriteLine($"经文：{verse}");
                    Console.WriteLine($"内容预览：{(body.Length > 200 ? body.Substring(0, 200) + "..." : body)}");
                    Console.WriteLine($"标签：{tags}");
                    Console.WriteLine(new string('-', 60));

                    // if (previewCount++ < 5)
                    // {
                    //     string preview = $"标题：{title}\n\n简介：{summary}\n\n经文：{verse}\n\n标签：{tags}";
                    //     MessageBox.Show(preview, $"文件预览：{Path.GetFileName(file)}", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    // }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"读取文件失败：{file}\n{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            if (invalidFiles.Any())
            {
                string msg = "以下文件格式不符合规范：\n\n" + string.Join("\n", invalidFiles);
                MessageBox.Show(msg, "格式校验不通过", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                MessageBox.Show("全部文件格式符合规范！", "格式校验", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        #endregion
    }
}
