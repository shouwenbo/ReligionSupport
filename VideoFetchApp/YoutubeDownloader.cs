using System.Data;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace VideoFetchApp
{
    public partial class YoutubeDownloader : Form
    {
        private readonly string _url;
        private readonly string _outputPath;

        public YoutubeDownloader(string url, string outputPath)
        {
            InitializeComponent();
            _url = url;
            _outputPath = outputPath;

            // 等待窗口加载完成后再启动下载
            this.Load += YoutubeDownloader_Load;
        }

        private void YoutubeDownloader_Load(object? sender, EventArgs e)
        {
            // 窗口已加载，现在可以安全地启动下载任务
            Task.Run(() =>
            {
                var thread = new Thread(() =>
                {
                    try
                    {
                        this.Invoke(new Action(() =>
                        {
                            this.bar_download.Style = ProgressBarStyle.Marquee;
                            this.lbl_status.Text = "正在准备下载...";
                        }));

                        // 使用 yt-dlp 下载（更可靠）
                        DownloadWithYtDlp(_url, _outputPath);
                    }
                    catch (Exception ex)
                    {
                        AppendLog($"错误: \r\nUrl：{_url}\r\nOutput：{_outputPath}\r\nErrorMessage：{ex.Message}\r\nStackTrace：{ex.StackTrace}");
                        
                        if (this.IsHandleCreated && !this.IsDisposed)
                        {
                            this.Invoke(new Action(() =>
                            {
                                MessageBox.Show($"下载失败：{ex.Message}\n\n详细信息请查看 log.txt", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }));
                        }
                    }
                    finally
                    {
                        if (this.IsHandleCreated && !this.IsDisposed)
                        {
                            this.Invoke(new Action(() =>
                            {
                                this.lbl_status.Text = "下载完成，窗口将自动关闭。";
                                this.bar_download.Style = ProgressBarStyle.Blocks;
                                this.Close();
                            }));
                        }
                    }
                });

                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();
            });
        }

        private void DownloadWithYtDlp(string url, string outputPath)
        {
            // 查找 yt-dlp.exe 路径
            string ytDlpPath = FindYtDlp();

            if (string.IsNullOrEmpty(ytDlpPath))
            {
                throw new FileNotFoundException(
                    "未找到 yt-dlp.exe！\n\n" +
                    "程序集成的 yt-dlp 文件可能丢失或损坏。\n" +
                    "请确保 Assets 文件夹中包含 yt-dlp.exe 文件，\n" +
                    "或重新下载完整的程序包。"
                );
            }

            // 查找 FFmpeg 路径
            string ffmpegPath = FindFfmpeg();
            if (string.IsNullOrEmpty(ffmpegPath))
            {
                throw new FileNotFoundException(
                    "未找到 ffmpeg.exe！\n\n" +
                    "程序集成的 ffmpeg 文件可能丢失或损坏。\n" +
                    "请确保 Assets 文件夹中包含 ffmpeg.exe 文件，\n" +
                    "或重新下载完整的程序包。"
                );
            }

            if (this.IsHandleCreated && !this.IsDisposed)
            {
                this.Invoke(new Action(() =>
                {
                    this.lbl_status.Text = $"使用 yt-dlp 下载: {Path.GetFileName(outputPath)}";
                }));
            }

            // yt-dlp 命令参数 - 添加合并选项和 ffmpeg 路径
            string arguments = $"--newline --no-warnings " +
                              $"--ffmpeg-location \"{ffmpegPath}\" " +
                              $"--merge-output-format mp4 " +
                              $"--format \"bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]/best\" " +
                              $"--output \"{outputPath}\" \"{url}\""; 

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ytDlpPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    StandardErrorEncoding = System.Text.Encoding.UTF8
                }
            };

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    // 解析下载进度
                    var match = Regex.Match(e.Data, @"\[download\]\s+(\d+\.?\d*)%");
                    if (match.Success)
                    {
                        if (float.TryParse(match.Groups[1].Value, out float progress))
                        {
                            if (this.IsHandleCreated && !this.IsDisposed)
                            {
                                this.Invoke(new Action(() =>
                                {
                                    this.lbl_status.Text = $"下载进度: {progress:F1}%";
                                    if (this.bar_download.Style == ProgressBarStyle.Marquee)
                                    {
                                        this.bar_download.Style = ProgressBarStyle.Blocks;
                                    }
                                    this.bar_download.Value = Math.Min((int)progress, 100);
                                }));
                            }
                        }
                    }
                    else
                    {
                        if (this.IsHandleCreated && !this.IsDisposed)
                        {
                            this.Invoke(new Action(() =>
                            {
                                this.lbl_status.Text = e.Data;
                            }));
                        }
                    }
                    AppendLog($"[yt-dlp] {e.Data}");
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    AppendLog($"[yt-dlp ERROR] {e.Data}");
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new Exception($"yt-dlp 下载失败，退出代码: {process.ExitCode}");
            }

            if (this.IsHandleCreated && !this.IsDisposed)
            {
                this.Invoke(new Action(() =>
                {
                    this.lbl_status.Text = "下载完成！";
                    this.bar_download.Value = 100;
                }));
            }
        }

        private string FindYtDlp()
        {
            // 1. 检查程序所在目录下的 Assets 文件夹（集成的 yt-dlp）
            string assetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "yt-dlp.exe");
            if (File.Exists(assetsPath))
                return assetsPath;

            // 2. 检查程序所在目录（发布后会被复制到根目录）
            string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "yt-dlp.exe");
            if (File.Exists(localPath))
                return localPath;

            // 3. 检查系统 PATH（备用方案）
            string? pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (pathEnv != null)
            {
                foreach (string path in pathEnv.Split(';'))
                {
                    string ytDlpPath = Path.Combine(path.Trim(), "yt-dlp.exe");
                    if (File.Exists(ytDlpPath))
                        return ytDlpPath;
                }
            }

            return string.Empty;
        }

        private string FindFfmpeg()
        {
            // 1. 检查程序所在目录下的 Assets 文件夹（集成的 ffmpeg）
            string assetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "ffmpeg.exe");
            if (File.Exists(assetsPath))
                return assetsPath;

            // 2. 检查程序所在目录（发布后会被复制到根目录）
            string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");
            if (File.Exists(localPath))
                return localPath;

            // 3. 检查系统 PATH（备用方案）
            string? pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (pathEnv != null)
            {
                foreach (string path in pathEnv.Split(';'))
                {
                    string ffmpegPath = Path.Combine(path.Trim(), "ffmpeg.exe");
                    if (File.Exists(ffmpegPath))
                        return ffmpegPath;
                }
            }

            return string.Empty;
        }

        public static void AppendLog(string message)
        {
            string logFilePath = "log.txt";
            string timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string logEntry = $"[{timeStamp}] {message}\n";

            try
            {
                File.AppendAllText(logFilePath, logEntry);
            }
            catch (Exception ex)
            {
                // 如果连写日志也失败，这里可以考虑报警或忽略
                MessageBox.Show($"写日志失败: {ex.Message}", "日志错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
