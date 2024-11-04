using RestSharp;
using System.Text.RegularExpressions;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Header;

namespace AutoFileDownloaderApp
{
    public partial class MainForm : Form
    {
        private KeyboardHook _keyboardHook;
        public MainForm()
        {
            InitializeComponent();
            _keyboardHook = new KeyboardHook();
            _keyboardHook.KeyDownEvent += _keyboardHook_KeyDownEventAsync;
            _keyboardHook.Start();
            this.list_url.Items.Clear();
            this.txt_dir.Text = Properties.Settings.Default.LastFolder ?? string.Empty;
            this.txt_great_supper_cookie.Text = Properties.Settings.Default.GreatSupperCookie ?? string.Empty;
        }

        private void btn_select_dir_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "请选择一个文件夹";
                dialog.UseDescriptionForTitle = true; // 使用 Description 作为窗口标题
                dialog.ShowNewFolderButton = true;    // 允许新建文件夹

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string selectedPath = dialog.SelectedPath;
                    this.txt_dir.Text = selectedPath;
                    Properties.Settings.Default.LastFolder = selectedPath;
                    Properties.Settings.Default.Save();
                }
            }
        }

        private void _keyboardHook_KeyDownEventAsync(object? sender, KeyEventArgs e)
        {
            // 检测 Ctrl + 小键盘Add 键组合
            if (e.KeyCode == Keys.Add && (Control.ModifierKeys & Keys.Control) == Keys.Control)
            {
                var text = Clipboard.GetText();
                if (this.WindowState == FormWindowState.Minimized)
                {
                    this.WindowState = FormWindowState.Normal;
                }
                this.Activate();
                foreach (var line in text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (line.StartsWith("http://") || line.StartsWith("https://"))
                    {
                        // 取出最后一段路径，解码作为显示名
                        var uri = new Uri(line.Trim());
                        //var lastSegment = uri.Segments.Last();
                        var displayName = System.Net.WebUtility.UrlDecode(uri.AbsoluteUri);

                        this.list_url.Items.Add(new AudioItem
                        {
                            Url = line.Trim(),
                            DisplayName = displayName
                        });
                    }
                    if (line.StartsWith("title:"))
                    {
                        var title_args = line.Split(':');
                        if (title_args.Length < 2)
                        {
                            MessageBox.Show("标题格式错误，请使用 'title: 标题内容' 格式");
                            return;
                        }
                        this.txt_title.Text = line.Split(':')[1].Trim();
                    }
                }
            }
        }

        private void btn_download_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(this.txt_title.Text))
            {
                MessageBox.Show("请先填写标题。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(this.txt_dir.Text))
            {
                MessageBox.Show("请先选择文件夹。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (list_url.Items.Count == 0)
            {
                MessageBox.Show("列表中没有需要下载的链接。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Properties.Settings.Default.LastFolder = this.txt_dir.Text.Trim();
            Properties.Settings.Default.Save();

            // 将 ListBox 中的 AudioItem 提取出来
            var itemList = new List<AudioItem>();
            foreach (AudioItem item in list_url.Items)
            {
                itemList.Add(item);
            }

            // 打开 Downloader 窗口
            var downloaderForm = new Downloader(this.txt_dir.Text.Trim(), this.txt_title.Text.Trim(), itemList);
            downloaderForm.Show();  // 你也可以用 .ShowDialog() 来模态打开

            this.txt_title.Text = "";
            this.list_url.Items.Clear();
        }

        private void btn_copy_Click(object sender, EventArgs e)
        {
            var result = File.ReadAllText("getUrl.js");

            Clipboard.SetDataObject(result.ToString());

            MessageBox.Show("已将获取链接的脚本复制到剪贴板。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btn_great_supper_cd_download_Click(object sender, EventArgs e)
        {
            try
            {
                Properties.Settings.Default.GreatSupperCookie = this.txt_great_supper_cookie.Text.Trim();
                Properties.Settings.Default.Save();

                var url = "https://manna.great-supper.com/v2.0/links?category_id=55&sort=id&delay_show=1";

                var options = new RestClientOptions(url)
                {
                    ThrowOnAnyError = false,
                    FollowRedirects = true
                };

                var client = new RestClient(options);

                var request = new RestRequest("", Method.Get);

                request.AddHeader("accept", "*/*");
                request.AddHeader("x-requested-with", "XMLHttpRequest");
                request.AddHeader("referer", "https://manna.great-supper.com/v2.0/links");
                request.AddHeader("cookie", this.txt_great_supper_cookie.Text.Trim());

                // 可选：如果该接口需要 Cookie 登录认证
                // client.CookieContainer = new CookieContainer();

                MessageBox.Show("开始请求");
                var response = client.Execute(request);

                if (response.IsSuccessful)
                {
                    var linkRes = LinkHelper.SaveAndOpenLinkInfo(response.Content.ToString());
                    foreach (var item in linkRes.Data)
                    {
                        // 判断 item.Dir 是否存在 mp3 或 mp4 文件
                        if (!Directory.Exists(item.Dir) ||
                            !Directory.EnumerateFiles(item.Dir, "*.*", SearchOption.TopDirectoryOnly)
                                      .Any(file => file.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ||
                                                   file.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)))
                        {
                            var downloaderForm = new Downloader(item.Dir, item.FileName, item.FileList);
                            downloaderForm.ShowDialog();  // 阻塞式弹窗
                        }
                    }
                    MessageBox.Show("结束下载");
                }
                else
                {
                    MessageBox.Show($"请求失败: {response.StatusCode} - {response.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
