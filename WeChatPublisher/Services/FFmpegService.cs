using System.Diagnostics;
using System.Text.Json;

namespace WeChatPublisher.Services;

public class FFmpegService
{
    private readonly string _ffmpegPath;
    private readonly string _ffprobePath;
    private readonly string _logPath;

    public FFmpegService(string? ffmpegPath = null, string? ffprobePath = null, string? logPath = null)
    {
        _ffmpegPath = ffmpegPath ?? AppSettings.Instance.FfmpegPath;
        _ffprobePath = ffprobePath ?? AppSettings.Instance.FfprobePath;
        _logPath = logPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.log");
    }

    public async Task RunAsync(string arguments, Action<string>? log = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("无法启动FFmpeg");
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var msg = $"FFmpeg exit code {process.ExitCode}: {error}";
            log?.Invoke(msg);
            File.AppendAllText(_logPath, $"[{DateTime.Now}] {arguments}\n{output}\n{error}\n---\n");
            throw new InvalidOperationException(msg);
        }

        log?.Invoke(output);
    }

    public async Task<double> GetDurationAsync(string filePath)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _ffprobePath,
            Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{filePath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("无法启动FFprobe");
        var output = (await process.StandardOutput.ReadToEndAsync()).Trim();
        await process.WaitForExitAsync();
        return double.TryParse(output, out var d) ? d : 0;
    }

    public async Task<VideoInfo> GetVideoInfoAsync(string filePath)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _ffprobePath,
            Arguments = $"-v error -select_streams v:0 -show_entries stream=width,height,r_frame_rate,sample_aspect_ratio,duration -of json \"{filePath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("无法启动FFprobe");
        var json = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        using var doc = JsonDocument.Parse(json);
        var stream = doc.RootElement.GetProperty("streams")[0];
        var fpsParts = stream.GetProperty("r_frame_rate").GetString()?.Split('/') ?? ["30", "1"];

        return new VideoInfo
        {
            Width = stream.GetProperty("width").GetInt32(),
            Height = stream.GetProperty("height").GetInt32(),
            Fps = double.Parse(fpsParts[0]) / double.Parse(fpsParts[1]),
            Duration = stream.TryGetProperty("duration", out var d)
                ? double.Parse(d.GetString() ?? "0") : 0,
            SarNum = stream.TryGetProperty("sample_aspect_ratio", out var sar)
                ? int.Parse((sar.GetString() ?? "1:1").Split(':')[0]) : 1,
            SarDen = stream.TryGetProperty("sample_aspect_ratio", out var sarden)
                ? int.Parse((sarden.GetString() ?? "1:1").Split(':').Last()) : 1
        };
    }

    public static string EscapeDrawText(string input)
    {
        return input.Replace("'", "'\\\\\\''")
                    .Replace(":", "\\\\:")
                    .Replace("\\", "/")
                    .Replace("%", "\\\\%");
    }
}

public class VideoInfo
{
    public int Width { get; set; }
    public int Height { get; set; }
    public double Fps { get; set; }
    public double Duration { get; set; }
    public int SarNum { get; set; } = 1;
    public int SarDen { get; set; } = 1;
}
