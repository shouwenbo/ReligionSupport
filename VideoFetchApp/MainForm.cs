using System.Diagnostics;
using YoutubeExplode;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Header;

namespace VideoFetchApp
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            this.txt_dir.Text = Properties.Settings.Default.LastFolder ?? string.Empty;
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

        private void btn_download_Click(object sender, EventArgs e)
        {
            string url = this.txt_video_link.Text.Trim();
            if (string.IsNullOrWhiteSpace(url))
            {
                MessageBox.Show("请输入 YouTube 视频链接！");
                return;
            }

            using var saveDialog = new SaveFileDialog
            {
                Filter = "MP4 Video|*.mp4",
                FileName = "output.mp4"
            };

            if (saveDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            try
            {
                this.btn_download.Enabled = false;

                var downloaderForm = new YoutubeDownloader(this.txt_video_link.Text, saveDialog.FileName);
                downloaderForm.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show("出错：" + ex.Message);
            }
            finally
            {
                this.btn_download.Enabled = true;
            }
        }

        private void btn_show_log_Click(object sender, EventArgs e)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "log.txt",
                UseShellExecute = true
            };
            Process.Start(psi);
        }
    }
}
