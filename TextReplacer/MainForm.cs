using Newtonsoft.Json;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace TextReplacer
{
    public partial class MainForm : Form
    {
        public string config_path = @"F:\code\ReligionSupport\TextReplacer\config.json";
        private KeyboardHook _keyboardHook;
        public MainForm()
        {
            InitializeComponent();
            this.AcceptButton = this.btn_add_config;
            _keyboardHook = new KeyboardHook();
            _keyboardHook.KeyDownEvent += _keyboardHook_KeyDownEvent;
            _keyboardHook.Start();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            if (!File.Exists(config_path))
            {
                MessageBox.Show("配置文件不存在");
                Application.Exit();
            }

            // var json = File.ReadAllText(config_path);
            // var replacements = JsonConvert.DeserializeObject<List<Replacement>>(json);
            // var cs = replacements
            // .Select(p => p.To)
            // .Where(p => p.Length <= 6)
            // .Distinct()
            // .ToList();
            // 
            // // 每行一个元素
            // File.WriteAllLines(@"F:\code\ReligionSupport\TextReplacer\to.csv", cs);
            // 
            // Console.WriteLine("CSV 文件已生成完成！");
        }

        private void btn_show_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("notepad.exe", config_path);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开文件时发生错误: {ex.Message}");
            }
        }

        private void btn_add_config_Click(object sender, EventArgs e)
        {
            var find = this.txt_find.Text.Trim();
            var to = this.txt_to.Text.Trim();
            if (string.IsNullOrWhiteSpace(find))
            {
                MessageBox.Show("查找的内容为空");
                return;
            }
            if (string.IsNullOrWhiteSpace(to))
            {
                MessageBox.Show("替换的内容为空");
                return;
            }
            var json = File.ReadAllText(config_path);
            var replacements = JsonConvert.DeserializeObject<List<Replacement>>(json);

            var existing = replacements.Find(r => r.From == find);
            if (existing != null)
            {
                existing.To = to;
            }
            else
            {
                replacements.Add(new Replacement() { From = find, To = to });
            }

            replacements.Sort((a, b) =>
            {
                int lengthComparison = b.From.Length.CompareTo(a.From.Length);
                if (lengthComparison != 0)
                {
                    return lengthComparison; // 长度优先
                }
                return string.Compare(a.From, b.From, StringComparison.Ordinal); // 字母顺序
            });

            var sortedJson = JsonConvert.SerializeObject(replacements, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(config_path, sortedJson);

            this.txt_find.Clear();
            this.txt_to.Clear();
        }

        private void btn_replace_Click(object sender, EventArgs e)
        {
            var text = this.rich_maintext.Text;
            if (ckb_capture.Checked)
            {
                ReplaceCaptureMode(text);
            }
            else
            {
                var (result, replacementCount, loopWords) = ProcessAndReplaceText(text, config_path, ckb_srt.Checked);
                new Result(result, replacementCount, loopWords).Show();
                this.rich_maintext.Clear();
            }
        }

        private void _keyboardHook_KeyDownEvent(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.LWin && (Control.ModifierKeys & Keys.Control) == Keys.Control)
            {
                var text = Clipboard.GetText();
                if (this.WindowState == FormWindowState.Minimized)
                {
                    this.WindowState = FormWindowState.Normal;
                }
                this.Activate();
                this.txt_find.Text = text;
                this.txt_to.Focus();
            }
        }

        private static (string result, Dictionary<string, int> replacementCount, HashSet<string> loopWords) ProcessAndReplaceText(string originalText, string configPath, bool handleSubtitles = false)
        {
            var result = originalText;

            if (handleSubtitles)
            {
                #region 处理网易见外字幕
                var text_split_wy = originalText.Split("\n", StringSplitOptions.RemoveEmptyEntries);
                if (text_split_wy.Any(p => Regex.IsMatch(p, @"^\d{2}:\d{2}:\d{2},\d{3} --> \d{2}:\d{2}:\d{2},\d{3}$")))
                {
                    var list = new List<string>();
                    foreach (var text_item in text_split_wy)
                    {
                        if (Regex.IsMatch(text_item, @"[\u4e00-\u9fa5]"))
                        {
                            list.AddRange(text_item.Split(" ", StringSplitOptions.RemoveEmptyEntries));
                        }
                    }
                    result = string.Join("，", list);
                }
                #endregion

                #region 处理钉钉闪记字幕
                var text_split_dd = originalText.Split("\n", StringSplitOptions.RemoveEmptyEntries);
                if (text_split_dd.Any(p => Regex.IsMatch(p, @"^\d{2}:\d{2}:\d{2}\r?$") || Regex.IsMatch(p, @"^[\u4e00-\u9fa5]+ \d{2}:\d{2}:\d{2}\r?$")))
                {
                    var list = new List<string>();
                    foreach (var text_item in text_split_dd)
                    {
                        if (Regex.IsMatch(text_item, @"[\u4e00-\u9fa5]") && !text_item.Contains("寿文博"))
                        {
                            list.AddRange(text_item.Split(" ", StringSplitOptions.RemoveEmptyEntries));
                        }
                    }
                    result = string.Join("，", list);
                }
                #endregion

                #region 标准 SRT 字幕格式处理
                if (Regex.IsMatch(originalText, @"^\d+\r?\n\d{2}:\d{2}:\d{2},\d{3}", RegexOptions.Multiline))
                {
                    var cleanedText = Regex.Replace(
                        originalText,
                        @"^\d+\r?\n|\d{2}:\d{2}:\d{2},\d{3} --> \d{2}:\d{2}:\d{2},\d{3}\r?\n",
                        "",
                        RegexOptions.Multiline);

                    cleanedText = Regex.Replace(cleanedText, @"^\s*\r?\n", "", RegexOptions.Multiline);

                    var lines = cleanedText
                        .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                        .Where(line => Regex.IsMatch(line, @"[\u4e00-\u9fa5]"))
                        .Select(line =>
                        {
                            var trimmed = line.Trim();
                            if (!Regex.IsMatch(trimmed, @"[。！？；：，…\.,!?;:]"))
                                return trimmed + "，";
                            return trimmed;
                        });

                    result = string.Join("", lines);
                }
                #endregion
            }

            #region 加载替换词库
            var json = File.ReadAllText(configPath);
            var replacements = JsonConvert.DeserializeObject<List<Replacement>>(json);
            var replacementDict = replacements.ToDictionary(r => r.From, r => r.To);
            #endregion

            #region 链式替换逻辑
            var replacementCount = new Dictionary<string, int>();
            var pattern = string.Join("|", replacementDict.Keys.OrderByDescending(k => k.Length).Select(Regex.Escape));
            var regex = new Regex(pattern);
            int maxIterations = 10;
            int iteration = 0;
            string previous;
            var history = new List<string>();
            var loopWords = new HashSet<string>();

            do
            {
                previous = result;

                result = regex.Replace(result, match =>
                {
                    string matchedValue = match.Value;
                    if (replacementDict.TryGetValue(matchedValue, out string? toValue))
                    {
                        if (replacementCount.ContainsKey(matchedValue))
                            replacementCount[matchedValue]++;
                        else
                            replacementCount[matchedValue] = 1;

                        return toValue;
                    }
                    return matchedValue;
                });

                if (history.Count > 0 && history[history.Count - 1] == result) break;

                if (history.Contains(result))
                {
                    foreach (var kvp in replacementDict)
                    {
                        string a = kvp.Key;
                        string b = kvp.Value;
                        if (replacementDict.ContainsKey(b) && replacementDict[b] == a)
                        {
                            loopWords.Add(a);
                            loopWords.Add(b);
                        }
                    }
                    break;
                }

                history.Add(result);
                iteration++;

            } while (iteration < maxIterations && result != previous);
            #endregion

            return (result, replacementCount, loopWords);
        }

        private async void btn_batch_replace_Click(object sender, EventArgs e)
        {
            await Task.Run(() =>
            {
                BatchReplaceFiles(@"F:\", config_path, lbl_batch_progress);
            });
        }
        
        private static void BatchReplaceFiles(string rootPath, string configPath, Label statusLabel)
        {
            var files = FindTranscribedTxtFiles(rootPath);
            int total = files.Count;

            for (int i = 0; i < total; i++)
            {
                string file = files[i];

                try
                {
                    string content = File.ReadAllText(file);

                    var (newText, _, _) = ProcessAndReplaceText(content, configPath, handleSubtitles: false);

                    // 写回文件（或另存为备份）
                    File.WriteAllText(file, newText);

                    // 更新 UI 线程上的 Label
                    statusLabel.Invoke(new Action(() =>
                    {
                        statusLabel.Text = $"已处理 {i + 1} / {total} 个文件：{Path.GetFileName(file)}";
                    }));
                }
                catch (Exception ex)
                {
                    statusLabel.Invoke(new Action(() =>
                    {
                        statusLabel.Text = $"错误：{Path.GetFileName(file)}，{ex.Message}";
                    }));
                }
            }

            statusLabel.Invoke(new Action(() =>
            {
                statusLabel.Text = $"全部完成，共处理 {total} 个文件。";
            }));
        }
        
        private static List<string> FindTranscribedTxtFiles(string rootPath)
        {
            var result = new List<string>();

            try
            {
                result.AddRange(Directory.GetFiles(rootPath, "*transcribed*.txt"));

                foreach (var dir in Directory.GetDirectories(rootPath))
                {
                    try
                    {
                        result.AddRange(FindTranscribedTxtFiles(dir));
                    }
                    catch (UnauthorizedAccessException) { }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"遍历失败：{rootPath}\n原因：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            return result;
        }

        private void ReplaceCaptureMode(string text)
        {
            //text = Regex.Replace(text, @"\s+", "");
            text = text.Replace(" ", "");
            text = text
            .Replace("(", "（")
            .Replace(",", "，")
            .Replace("~", "-")
            .Replace("?", "？")
            .Replace("!", "！")
            .Replace("\"", "■")
            .Replace("·", "■")
            .Replace(">", "▶")
            .Replace(")", "）")
            .Replace(":", "：");
            for (int i = 0; i <= 9; i++)
            {
                text = text.Replace(i + "：", i + ":");
            }

            new Result(text).Show();
            this.rich_maintext.Clear();
        }
    }
}
