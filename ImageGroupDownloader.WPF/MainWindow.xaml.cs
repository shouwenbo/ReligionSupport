using ImageGroupDownloader.WPF.Models;
using System.IO;
using System.Net.Http;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;

namespace ImageGroupDownloader.WPF
{
    public partial class MainWindow : Window
    {
        private string WatchFolder = string.Empty;
        private readonly Regex regex = new Regex(@"^(?<name>.+?)\s+(?<num>\d+)\.(?<ext>jpg|jpeg|png|bmp|gif|webp)$", RegexOptions.IgnoreCase);
        private readonly string[] SupportedExtensions = { "jpg", "jpeg", "png", "bmp", "gif", "webp" };
        private readonly string workDir;
        private List<ImageGroup> _allGroups = new List<ImageGroup>();

        public MainWindow()
        {
            InitializeComponent();

            workDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "work");
            Directory.CreateDirectory(workDir);

            LoadWatchFolder();
            RefreshList();

            OutputSourceFilesToTxt();
        }

        private void LoadWatchFolder()
        {
            txtWatchFolder.Text = Properties.Settings.Default.WatchFolder;
            WatchFolder = Properties.Settings.Default.WatchFolder;
        }

        private void SaveWatchFolder()
        {
            Properties.Settings.Default.WatchFolder = WatchFolder;
            Properties.Settings.Default.Save();
        }

        private string PickFolder()
        {
            var dlg = new System.Windows.Forms.FolderBrowserDialog(); // 如果要完全 WPF，可以改成 WindowsAPICodePack 的 CommonOpenFileDialog
            dlg.Description = "请选择文件夹";
            dlg.ShowNewFolderButton = true;

            var result = dlg.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
                return dlg.SelectedPath;
            return null;
        }

        private void BtnSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            var folder = PickFolder();
            if (!string.IsNullOrEmpty(folder))
            {
                WatchFolder = folder;
                txtWatchFolder.Text = WatchFolder;
                SaveWatchFolder();
                RefreshList();
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshList();
        }

        private void RefreshList()
        {
            if (string.IsNullOrWhiteSpace(WatchFolder) || !Directory.Exists(WatchFolder)) return;

            var files = Directory.EnumerateFiles(WatchFolder)
                .Select(f => Path.GetFileName(f))
                .Where(f => regex.IsMatch(f))
                .Select(f =>
                {
                    var m = regex.Match(f);
                    return new { Name = m.Groups["name"].Value.Trim(), Num = int.Parse(m.Groups["num"].Value) };
                }).ToList();

            _allGroups = files.GroupBy(x => x.Name)
                              .Select(g => new ImageGroup
                              {
                                  Name = g.Key,
                                  Count = g.Count()
                              })
                              .OrderByDescending(x => x.Count)
                              .ToList();

            dgGroups.ItemsSource = _allGroups;
        }

        private void TxtNameSearch_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            var text = txtNameSearch.Text.Trim();

            // 取消选中行
            dgGroups.SelectedItem = null;

            if (string.IsNullOrEmpty(text))
            {
                dgGroups.ItemsSource = _allGroups;
            }
            else
            {
                var filtered = _allGroups
                    .Where(g => g.Name.Contains(text, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                dgGroups.ItemsSource = filtered;
            }
        }

        private async void BtnDownload_Click(object sender, RoutedEventArgs e)
        {
            var url = txtDownloadUrl.Text.Trim();
            var inputName = txtNameSearch.Text.Trim();

            if (string.IsNullOrWhiteSpace(url) && string.IsNullOrWhiteSpace(inputName))
            {
                MessageBox.Show("请输入下载地址或名称。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(WatchFolder) || !Directory.Exists(WatchFolder))
            {
                MessageBox.Show("请选择有效的监控文件夹。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 判断是选中分组还是新增分组
            string name;
            if (dgGroups.SelectedItem is ImageGroup selectedGroup)
            {
                name = selectedGroup.Name;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(inputName))
                {
                    MessageBox.Show("请在搜索框输入名称以创建新分组。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 弹窗确认新增分组
                var result = MessageBox.Show($"未选中任何分组，是否要新增分组 \"{inputName}\"？",
                                             "确认新增分组", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return;

                name = inputName;
            }

            int nextIndex = 1;
            var existing = Directory.EnumerateFiles(WatchFolder)
                .Select(f => Path.GetFileName(f))
                .Where(f => regex.IsMatch(f))
                .Select(f =>
                {
                    var m = regex.Match(f);
                    return new { Name = m.Groups["name"].Value.Trim(), Num = int.Parse(m.Groups["num"].Value), Ext = m.Groups["ext"].Value };
                })
                .Where(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (existing.Any())
                nextIndex = existing.Max(x => x.Num) + 1;

            string ext = "jpg";
            var path = Path.Combine(WatchFolder, $"{name} {nextIndex}.{ext}");

            try
            {
                pbDownload.Visibility = Visibility.Visible;
                pbDownload.Value = 0;

                if (!string.IsNullOrWhiteSpace(url))
                {
                    using var resp = await HttpUtil.GetWithRetryAsync(url);
                    resp.EnsureSuccessStatusCode();

                    ext = Path.GetExtension(new Uri(url).AbsolutePath).TrimStart('.').ToLower();
                    if (!SupportedExtensions.Contains(ext)) ext = "jpg";
                    path = Path.Combine(WatchFolder, $"{name} {nextIndex}.{ext}");

                    var totalBytes = resp.Content.Headers.ContentLength ?? -1L;
                    var canReportProgress = totalBytes != -1;

                    using var stream = await resp.Content.ReadAsStreamAsync();
                    using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                    var buffer = new byte[8192];
                    long totalRead = 0;
                    int read;
                    while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fs.WriteAsync(buffer, 0, read);
                        totalRead += read;
                        if (canReportProgress)
                            pbDownload.Value = totalRead * 100.0 / totalBytes;
                    }
                }
                else
                {
                    // 创建空文件
                    File.WriteAllBytes(path, Array.Empty<byte>());
                    pbDownload.Value = 100;
                }

                RefreshList();

                txtDownloadUrl.Text = string.Empty;
                txtNameSearch.Text = string.Empty;

                MessageBox.Show($"已保存: {Path.GetFileName(path)}", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("操作失败: " + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                pbDownload.Visibility = Visibility.Collapsed;
                pbDownload.Value = 0;
            }
        }


        #region 输出源码到txt
        private void OutputSourceFilesToTxt()
        {
            try
            {
                string[] files =
                {
                    @"F:\code\ReligionSupport\ImageGroupDownloader.WPF\MainWindow.xaml",
                    @"F:\code\ReligionSupport\ImageGroupDownloader.WPF\MainWindow.xaml.cs",
                    @"F:\code\ReligionSupport\ImageGroupDownloader.WPF\HttpUtil.cs",
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
                // Log($"源码已输出到 {outputPath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"输出源码失败: {ex.Message}");
            }
        }
        #endregion
    }
}
