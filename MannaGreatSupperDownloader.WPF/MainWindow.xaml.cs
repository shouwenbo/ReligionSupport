using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;

namespace MannaGreatSupperDownloader.WPF
{
    public partial class MainWindow : Window
    {
        private readonly HttpClient _http = new HttpClient();
        private readonly string workDir;

        public MainWindow()
        {
            InitializeComponent();
            workDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "work");
            if (Directory.Exists(workDir))
            {
                DirectoryInfo di = new DirectoryInfo(workDir);
                foreach (var file in di.GetFiles()) file.Delete();
                foreach (var dir in di.GetDirectories()) dir.Delete(true);
            }
            Directory.CreateDirectory(workDir);
            LoadSettings();
            OutputSourceFilesToTxt();
        }

        private void LoadSettings()
        {
            TbUsername.Text = Properties.Settings.Default.Username;
            TbPassword.Password = Properties.Settings.Default.Password;
            TbToken.Text = Properties.Settings.Default.Token;
            TbOutputFolder.Text = Properties.Settings.Default.OutputFolder;
        }

        private void SaveSettings()
        {
            Properties.Settings.Default.Username = TbUsername.Text;
            Properties.Settings.Default.Password = TbPassword.Password;
            Properties.Settings.Default.Token = TbToken.Text;
            Properties.Settings.Default.OutputFolder = TbOutputFolder.Text;
            Properties.Settings.Default.Save();
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = TbUsername.Text.Trim();
            string password = TbPassword.Password.Trim();
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                System.Windows.MessageBox.Show("请输入账号和密码。");
                return;
            }

            try
            {
                BtnLogin.IsEnabled = false;
                BtnLogin.Content = "正在登录...";

                var requestBody = new { username, password };
                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

                var response = await _http.PostAsync("https://manna3.great-supper.com/prod-api/app/login", content);
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("code", out var code) && code.GetInt32() == 200)
                {
                    string token = root.GetProperty("token").GetString();
                    TbToken.Text = token;
                    SaveSettings();
                    System.Windows.MessageBox.Show("登录成功，Token 已保存！");
                }
                else
                {
                    string msg = root.TryGetProperty("msg", out var m) ? m.GetString() : "未知错误";
                    System.Windows.MessageBox.Show($"登录失败：{msg}");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"请求出错：{ex.Message}");
            }
            finally
            {
                BtnLogin.IsEnabled = true;
                BtnLogin.Content = "获取 Token";
            }
        }

        private void BtnSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (!string.IsNullOrEmpty(TbOutputFolder.Text)) dialog.SelectedPath = TbOutputFolder.Text;
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TbOutputFolder.Text = dialog.SelectedPath;
                SaveSettings();
            }
        }

        private async void BtnFetchArticle_Click(object sender, RoutedEventArgs e)
        {
            string token = TbToken.Text.Trim();
            string id = TbArticleID.Text.Trim();
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(id))
            {
                System.Windows.MessageBox.Show("请先填写 Token 和文章ID");
                return;
            }

            try
            {
                _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                var response = await _http.GetAsync($"https://manna3.great-supper.com/prod-api/app/article/{id}");
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.GetProperty("code").GetInt32() == 200)
                {
                    var data = root.GetProperty("data");
                    TbArticleTitle.Text = data.GetProperty("title").GetString();
                    string contentHtml = data.GetProperty("content").GetString();

                    // 提取音频和图片链接
                    var matches = new List<string>();
                    matches.AddRange(Regex.Matches(contentHtml, "<audio.*?src=\"(.*?)\"").Cast<Match>().Select(m => m.Groups[1].Value));
                    matches.AddRange(Regex.Matches(contentHtml, "<img.*?src=\"(.*?)\"").Cast<Match>().Select(m => m.Groups[1].Value));

                    LbArticleContent.Items.Clear();
                    foreach (var m in matches) LbArticleContent.Items.Add(m);
                }
                else
                {
                    string msg = root.TryGetProperty("msg", out var m) ? m.GetString() : "未知错误";
                    System.Windows.MessageBox.Show($"获取失败：{msg}");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"请求失败：{ex.Message}");
            }
        }
        private async void BtnDownload_Click(object sender, RoutedEventArgs e)
        {
            if (LbArticleContent.Items.Count == 0)
            {
                MessageBox.Show("没有内容可下载");
                return;
            }
            string folder = TbOutputFolder.Text.Trim();
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            {
                MessageBox.Show("请先选择有效的输出文件夹");
                return;
            }

            string title = TbArticleTitle.Text.Trim();
            string targetFolder = folder;

            // ✅ 如果选中“按标题创建子文件夹”，则在输出文件夹下创建子文件夹
            if (CbCreateFolder.IsChecked == true)
            {
                targetFolder = Path.Combine(folder, title);
                Directory.CreateDirectory(targetFolder);
            }

            PbDownload.Value = 0;
            PbDownload.Maximum = LbArticleContent.Items.Count;

            int audioIndex = 1;
            int imageIndex = 1;

            foreach (var item in LbArticleContent.Items)
            {
                string url = item.ToString();

                if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    url = "https://cdn.great-supper.com/" + url;

                string ext = Path.GetExtension(url);
                string filename = "";

                if (url.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
                {
                    filename = (audioIndex == 1 && LbArticleContent.Items.Cast<string>().Count(i => i.EndsWith(".mp3")) == 1)
                        ? $"{title}{ext}"
                        : $"{title}({audioIndex}){ext}";
                    audioIndex++;
                }
                else
                {
                    filename = (imageIndex == 1 && LbArticleContent.Items.Cast<string>().Count(i => !i.EndsWith(".mp3")) == 1)
                        ? $"{title}{ext}"
                        : $"{title}①".Replace("①", $"{"①②③④⑤⑥⑦⑧⑨"[imageIndex - 1]}") + ext;
                    imageIndex++;
                }

                try
                {
                    var bytes = await _http.GetByteArrayAsync(url);
                    await File.WriteAllBytesAsync(Path.Combine(targetFolder, filename), bytes);
                }
                catch { }

                PbDownload.Value += 1;
            }

            MessageBox.Show("下载完成！");
            System.Diagnostics.Process.Start("explorer.exe", targetFolder);
        }



        // 批量获取文章ID列表（按 articleCategoryId 自动分页）
        private async void BtnFetchBatchArticles_Click(object sender, RoutedEventArgs e)
        {
            string token = TbToken.Text.Trim();
            if (string.IsNullOrEmpty(token))
            {
                MessageBox.Show("请先填写 Token");
                return;
            }

            // 获取文章分类ID
            if (!int.TryParse(TbArticleCategoryID.Text.Trim(), out int articleCategoryId))
            {
                MessageBox.Show("请输入有效的文章列表ID（分类ID）");
                return;
            }

            try
            {
                _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                LbBatchArticles.Items.Clear();
                int pageSize = 20; // 每页数量，可根据接口调整
                int pageNum = 1;
                bool hasMore = true;

                while (hasMore)
                {
                    var response = await _http.GetAsync($"https://manna3.great-supper.com/prod-api/app/article/list?pageNum={pageNum}&pageSize={pageSize}&articleCategoryId={articleCategoryId}");
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (root.GetProperty("code").GetInt32() != 200)
                    {
                        MessageBox.Show($"接口返回错误，code={root.GetProperty("code").GetInt32()}");
                        break;
                    }

                    var rows = root.GetProperty("rows");
                    if (rows.GetArrayLength() == 0)
                    {
                        hasMore = false;
                        break;
                    }

                    foreach (var row in rows.EnumerateArray())
                    {
                        int id = row.GetProperty("id").GetInt32();
                        string title = row.GetProperty("title").GetString();
                        LbBatchArticles.Items.Add($"{id} - {title}");
                    }

                    pageNum++;
                }

                // MessageBox.Show($"共获取 {LbBatchArticles.Items.Count} 条文章ID");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取失败：{ex.Message}");
            }
        }


        // 单篇文章下载（异步版，接收进度条）
        private async Task DownloadArticleAsync(string title, List<string> contentList, string outputFolder, bool createFolder, System.Windows.Controls.ProgressBar contentProgressBar)
        {
            if (contentList.Count == 0) return;
            string targetFolder = outputFolder;
            if (createFolder)
            {
                targetFolder = Path.Combine(outputFolder, title);
                Directory.CreateDirectory(targetFolder);
            }

            contentProgressBar.Value = 0;
            contentProgressBar.Maximum = contentList.Count;

            int audioIndex = 1;
            int imageIndex = 1;

            foreach (var url in contentList)
            {
                string realUrl = url.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? url : "https://cdn.great-supper.com/" + url;
                string ext = Path.GetExtension(realUrl);
                string filename = "";

                if (realUrl.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
                {
                    filename = (audioIndex == 1 && contentList.Count(i => i.EndsWith(".mp3")) == 1)
                        ? $"{title}{ext}"
                        : $"{title}({audioIndex}){ext}";
                    audioIndex++;
                }
                else
                {
                    filename = (imageIndex == 1 && contentList.Count(i => !i.EndsWith(".mp3")) == 1)
                        ? $"{title}{ext}"
                        : $"{title}①".Replace("①", $"{"①②③④⑤⑥⑦⑧⑨"[imageIndex - 1]}") + ext;
                    imageIndex++;
                }

                try
                {
                    var bytes = await _http.GetByteArrayAsync(realUrl);
                    await File.WriteAllBytesAsync(Path.Combine(targetFolder, filename), bytes);
                }
                catch { }

                contentProgressBar.Value += 1;
            }
        }



        // 单篇文章获取（异步版）返回元组
        private async Task<(string Title, List<string> ContentList)> FetchArticleAsync(string token, string id)
        {
            string title = "";
            var contentList = new List<string>();

            try
            {
                _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                var response = await _http.GetAsync($"https://manna3.great-supper.com/prod-api/app/article/{id}");
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.GetProperty("code").GetInt32() == 200)
                {
                    var data = root.GetProperty("data");
                    title = data.GetProperty("title").GetString();
                    string contentHtml = data.GetProperty("content").GetString();

                    contentList.AddRange(Regex.Matches(contentHtml, "<audio.*?src=\"(.*?)\"").Cast<Match>().Select(m => m.Groups[1].Value));
                    contentList.AddRange(Regex.Matches(contentHtml, "<img.*?src=\"(.*?)\"").Cast<Match>().Select(m => m.Groups[1].Value));
                }
            }
            catch { }

            return (title, contentList);
        }


        private async void BtnBatchDownload_Click(object sender, RoutedEventArgs e)
        {
            if (LbBatchArticles.Items.Count == 0)
            {
                MessageBox.Show("没有文章可下载");
                return;
            }

            string folder = TbOutputFolder.Text.Trim();
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            {
                MessageBox.Show("请先选择有效的输出文件夹");
                return;
            }

            PbBatchArticleProgress.Value = 0;
            PbBatchArticleProgress.Maximum = LbBatchArticles.Items.Count;

            string token = TbToken.Text.Trim();
            bool createFolder = CbBatchCreateFolder.IsChecked == true;

            foreach (var item in LbBatchArticles.Items)
            {
                string s = item.ToString();
                int idx = s.IndexOf(" - ");
                if (idx < 0) continue;

                string id = s.Substring(0, idx);
                string title = s.Substring(idx + 3);

                // 获取文章内容
                var (fetchedTitle, contentList) = await FetchArticleAsync(TbToken.Text.Trim(), id);
                string finalTitle = string.IsNullOrEmpty(fetchedTitle) ? title : fetchedTitle;

                // 下载文章
                await DownloadArticleAsync(finalTitle, contentList, folder, createFolder, PbBatchContentProgress);

                PbBatchArticleProgress.Value += 1; // 更新整体进度
                PbBatchContentProgress.Value = 0;   // 重置当前文章进度

                await Task.Delay(100); // 避免UI卡顿
            }

            MessageBox.Show("批量下载完成！");
        }




        #region 输出源码到txt
        private void OutputSourceFilesToTxt()
        {
            try
            {
                string[] files =
                {
                    @"F:\code\ReligionSupport\MannaGreatSupperDownloader.WPF\MainWindow.xaml",
                    @"F:\code\ReligionSupport\MannaGreatSupperDownloader.WPF\MainWindow.xaml.cs",
                };

                var sb = new StringBuilder();
                sb.AppendLine($"请严格以我提供的代码为基础，在其之上进行修改：");
                foreach (var file in files)
                {
                    if (File.Exists(file))
                    {
                        sb.AppendLine($"===== 文件: {file} =====");
                        sb.AppendLine(File.ReadAllText(file));
                        sb.AppendLine();
                    }
                    else
                        sb.AppendLine($"===== 文件未找到: {file} =====\n");
                }

                string outputPath = Path.Combine(workDir, "AllSourceOutput.txt");
                File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"输出源码失败: {ex.Message}");
            }
        }
        #endregion

    }
}
