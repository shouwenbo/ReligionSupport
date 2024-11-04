using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Threading.Tasks;

namespace AutoClipper
{
    public class VideoInfo
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public double Duration { get; set; }
        public double Fps { get; set; }
        public int SarNum { get; set; } = 1;
        public int SarDen { get; set; } = 1;
    }

    static class FFmpegRunner
    {
        private static readonly string LogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.log");

        public static void InitializeLog()
        {
            try
            {
                File.WriteAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 日志初始化\n");
            }
            catch { }
        }

        public static string EscapeFfmpegText(string input)
        {
            if (input == null) return string.Empty;
            return input.Replace("\\", "\\\\").Replace(":", "\\:").Replace("'", "\\'").Replace("\"", "\\\"");
        }

        public static void GenerateDefaultCover(string path)
        {
            var folder = Path.GetDirectoryName(path) ?? ".";
            Directory.CreateDirectory(folder!);
            using var bmp = new System.Drawing.Bitmap(1080, 1440);
            using var g = System.Drawing.Graphics.FromImage(bmp);
            g.Clear(System.Drawing.Color.FromArgb(34, 49, 63));
            using var font = new System.Drawing.Font("KaiTi", 72, System.Drawing.FontStyle.Bold);
            var brush = System.Drawing.Brushes.White;
            var text = "默认封面";
            var sz = g.MeasureString(text, font);
            g.DrawString(text, font, brush, (bmp.Width - sz.Width) / 2f, (bmp.Height - sz.Height) / 2f);
            bmp.Save(path, System.Drawing.Imaging.ImageFormat.Jpeg);
        }

        public static Task RunAsync(string ffmpegExe, string arguments, Action<string>? log = null)
        {
            return Task.Run(() =>
            {
                try
                {
                    log?.Invoke("FFmpeg: " + arguments);
                    AppendLog($"FFmpeg: {arguments}");

                    var match = Regex.Match(arguments, "\"([^\"]+)\"$");
                    if (match.Success)
                    {
                        string outDir = Path.GetDirectoryName(match.Groups[1].Value)!;
                        Directory.CreateDirectory(outDir);
                    }

                    var psi = new ProcessStartInfo(ffmpegExe, arguments)
                    {
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var p = Process.Start(psi)!;
                    p.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) { log?.Invoke(e.Data); AppendLog(e.Data); } };
                    p.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) { log?.Invoke(e.Data); AppendLog(e.Data); } };
                    p.BeginOutputReadLine();
                    p.BeginErrorReadLine();
                    p.WaitForExit();
                    if (p.ExitCode != 0) throw new Exception($"ffmpeg 返回非 0：{p.ExitCode}");
                }
                catch (Exception ex)
                {
                    AppendLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 错误: {ex}");
                    log?.Invoke("FFmpeg 执行出错，详情已保存到 ffmpeg.log");
                    throw;
                }
            });
        }

        public static async Task<double> GetDurationAsync(string ffprobeExe, string path)
        {
            var psi = new ProcessStartInfo(ffprobeExe, $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{path}\"")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi)!;
            var outp = await p.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
            p.WaitForExit();
            if (double.TryParse(outp.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d)) return d;
            throw new Exception("无法解析时长: " + outp);
        }


        public static async Task<VideoInfo> GetVideoInfoAsync(string ffprobeExe, string path)
        {
            var psi = new ProcessStartInfo(ffprobeExe, $"-v error -print_format json -show_streams \"{path}\"")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var p = Process.Start(psi)!;
            var outp = await p.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
            p.WaitForExit();

            try
            {
                using var doc = JsonDocument.Parse(outp);
                var videoStream = doc.RootElement.GetProperty("streams").EnumerateArray()
                    .FirstOrDefault(s => s.GetProperty("codec_type").GetString() == "video");

                if (videoStream.ValueKind == JsonValueKind.Undefined) throw new Exception("没有视频流");

                int width = videoStream.GetProperty("width").GetInt32();
                int height = videoStream.GetProperty("height").GetInt32();

                // 获取帧率
                double fps = 30;
                if (videoStream.TryGetProperty("r_frame_rate", out var fpsProp))
                {
                    var parts = fpsProp.GetString()?.Split('/');
                    if (parts?.Length == 2 &&
                        double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var num) &&
                        double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var den))
                    {
                        fps = den != 0 ? num / den : 30;
                    }
                }

                // 获取SAR
                int sarNum = 1, sarDen = 1;
                if (videoStream.TryGetProperty("sample_aspect_ratio", out var sarProp))
                {
                    var parts = sarProp.GetString()?.Split(':');
                    if (parts?.Length == 2 &&
                        int.TryParse(parts[0], out var n) &&
                        int.TryParse(parts[1], out var d))
                    {
                        sarNum = n; sarDen = d;
                    }
                }

                double duration = 0;
                if (videoStream.TryGetProperty("duration", out var durProp))
                    double.TryParse(durProp.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out duration);
                else
                {
                    var formatDur = doc.RootElement.GetProperty("format").GetProperty("duration").GetString();
                    double.TryParse(formatDur, NumberStyles.Any, CultureInfo.InvariantCulture, out duration);
                }

                return new VideoInfo
                {
                    Width = width,
                    Height = height,
                    Fps = fps,
                    SarNum = sarNum,
                    SarDen = sarDen,
                    Duration = duration
                };
            }
            catch (Exception ex)
            {
                throw new Exception("解析视频信息失败: " + ex.Message + "\n" + outp);
            }
        }


        private static void AppendLog(string text)
        {
            try
            {
                File.AppendAllText(LogPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {text}\n");
            }
            catch { }
        }
    }
}
