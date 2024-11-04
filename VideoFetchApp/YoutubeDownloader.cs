using System.Data;
using System.Diagnostics;
using YoutubeExplode;

namespace VideoFetchApp
{
    public partial class YoutubeDownloader : Form
    {
        public YoutubeDownloader(string url, string outputPath)
        {
            InitializeComponent();

            Task.Run(() =>
            {
                var thread = new Thread(async () =>
                {
                    try
                    {
                        this.bar_download.Style = ProgressBarStyle.Marquee;

                        this.lbl_status.Text = "正在下载视频，请稍候...";
                        var youtube = new YoutubeClient();

                        this.lbl_status.Text = "正在获取视频信息...";
                        var video = await youtube.Videos.GetAsync(url);

                        this.lbl_status.Text = $"视频标题：{video.Title}";
                        this.lbl_status.Text = $"视频时长：{video.Duration?.ToString() ?? "未知"}";
                        this.lbl_status.Text = $"视频作者：{video.Author.ChannelTitle}";

                        this.lbl_status.Text = "正在获取视频流信息...";
                        var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);

                        this.lbl_status.Text = "正在选择最高质量的视频和音频流...";
                        var videoStreamInfo = streamManifest
                            .GetVideoOnlyStreams()
                            .OrderByDescending(s => s.VideoQuality.MaxHeight)
                            .First();

                        this.lbl_status.Text = "正在选择最高质量的音频流...";
                        var audioStreamInfo = streamManifest
                            .GetAudioOnlyStreams()
                            .OrderByDescending(s => s.Bitrate)
                            .First();

                        this.lbl_status.Text = "正在创建视频临时文件...";
                        var tempVideoPath = Path.GetTempFileName() + "." + videoStreamInfo.Container.Name;

                        this.lbl_status.Text = "正在创建音频临时文件...";
                        var tempAudioPath = Path.GetTempFileName() + "." + audioStreamInfo.Container.Name;

                        this.lbl_status.Text = "正在下载视频流...";
                        await youtube.Videos.Streams.DownloadAsync(videoStreamInfo, tempVideoPath);

                        this.lbl_status.Text = "正在下载音频流...";
                        await youtube.Videos.Streams.DownloadAsync(audioStreamInfo, tempAudioPath);

                        this.lbl_status.Text = "正在合并视频和音频流...";
                        string ffmpegPath = @"D:\Program Files\ffmpeg-master-latest-win64-gpl\bin\ffmpeg.exe";
                        string arguments = $"-i \"{tempVideoPath}\" -i \"{tempAudioPath}\" -c:v copy -c:a aac -y \"{outputPath}\"";

                        this.lbl_status.Text = "正在执行 FFmpeg 命令...";
                        using var process = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = ffmpegPath,
                                Arguments = arguments,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                UseShellExecute = false,
                                CreateNoWindow = true,
                            }
                        };

                        this.lbl_status.Text = "正在启动 FFmpeg 进程...";
                        process.Start();

                        this.lbl_status.Text = "正在等待 FFmpeg 进程完成...";
                        string output = await process.StandardError.ReadToEndAsync();

                        this.lbl_status.Text = "视频和音频合并完成！";
                        process.WaitForExit();

                        this.lbl_status.Text = "正在删除临时视频文件...";
                        File.Delete(tempVideoPath);

                        this.lbl_status.Text = "正在清理音频临时文件...";
                        File.Delete(tempAudioPath);

                        this.bar_download.Style = ProgressBarStyle.Blocks;
                    }
                    catch (Exception ex)
                    {
                        AppendLog($"错误: \r\nUrl：{url}\r\nOutput：{outputPath}\r\nErrorMessage：{ex.Message}\r\nStackTrace：{ex.StackTrace}");
                    }
                    finally
                    {
                        // ★★★ 下载完成后自动关闭窗口 ★★★
                        this.Invoke(new Action(() =>
                        {
                            this.lbl_status.Text = "所有下载已完成，窗口将自动关闭。";
                            this.Close();
                        }));
                    }
                });

                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();
            });
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
