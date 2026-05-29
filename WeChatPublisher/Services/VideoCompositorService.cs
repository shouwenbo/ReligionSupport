using WeChatPublisher.Models;

namespace WeChatPublisher.Services;

public class VideoCompositorService
{
    private readonly FFmpegService _ffmpeg;
    private readonly Action<string>? _log;

    public VideoCompositorService(FFmpegService ffmpeg, Action<string>? log = null)
    {
        _ffmpeg = ffmpeg;
        _log = log;
    }

    public string? PickRandomVideo(string folderPath)
    {
        if (!Directory.Exists(folderPath)) return null;
        var files = Directory.GetFiles(folderPath)
            .Where(f => f.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)
                     || f.EndsWith(".mov", StringComparison.OrdinalIgnoreCase)
                     || f.EndsWith(".mkv", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (files.Count == 0) return null;
        return files[Random.Shared.Next(files.Count)];
    }

    public string? PickRandomAudio(string folderPath)
    {
        if (!Directory.Exists(folderPath)) return null;
        var files = Directory.GetFiles(folderPath)
            .Where(f => f.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (files.Count == 0) return null;
        return files[Random.Shared.Next(files.Count)];
    }

    public async Task<string> GenerateVideoAsync(VideoDraft draft, string videoFolder, string musicFolder,
        string? coverPath, int extraSeconds, string outputFolder)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "WeChatPublisher");
        Directory.CreateDirectory(tempDir);

        // Pick random background
        var bgVideo = PickRandomVideo(videoFolder)
            ?? throw new InvalidOperationException("背景视频文件夹为空");
        var bgMusic = PickRandomAudio(musicFolder);

        // Get TTS audio
        var audioPath = draft.AudioPath
            ?? throw new InvalidOperationException("请先生成TTS音频");

        // Get subtitle
        var srtPath = draft.SrtPath;

        // Get video info
        var info = await _ffmpeg.GetVideoInfoAsync(bgVideo);
        double audioDuration = await _ffmpeg.GetDurationAsync(audioPath);
        double targetDuration = audioDuration + extraSeconds;

        // Speed scale
        double speedScale = info.Duration / targetDuration;
        if (speedScale < 0.5 || speedScale > 3) speedScale = 1;

        int width = AppSettings.Instance.VideoOutputWidth;
        int height = AppSettings.Instance.VideoOutputHeight;

        var title1 = EscapeDrawText(draft.TitleWord1 ?? "");
        var title2 = EscapeDrawText(draft.TitleWord2 ?? "");
        var verseText = EscapeDrawText(draft.VerseContent ?? "");

        // Build filter complex
        var filters = new List<string>
        {
            $"[0:v]setpts={speedScale}*PTS[v0]",
            $"[v0]scale=-1:{height}[v1]",
            $"[v1]crop={width}:{height}[v2]"
        };

        // Title text
        filters.Add($"[v2]drawtext=text='{title1}':fontsize=100:fontcolor=#FFDE00:bordercolor=#3C5C37:borderw=6:x=(w-text_w)/2:y=60[v3]");
        filters.Add($"[v3]drawtext=text='{title2}':fontsize=100:fontcolor=#FFDE00:bordercolor=#3C5C37:borderw=6:x=(w-text_w)/2:y=180[v4]");

        // Subtitles
        if (srtPath != null)
        {
            filters.Add($"[v4]subtitles='{EscapeDrawText(srtPath)}':force_style='FontSize=18,FontName=Microsoft YaHei,Alignment=2,MarginV=50'[v5]");
        }
        else
        {
            filters.Add($"[v4]null[v5]");
        }

        // Verse text at the end
        if (!string.IsNullOrWhiteSpace(verseText))
        {
            filters.Add($"[v5]drawtext=text='{verseText}':fontsize=24:fontcolor=white:bordercolor=black:borderw=3:x=(w-text_w)/2:y=(h-text_h)/2:enable='between(t,{audioDuration},{targetDuration})'[vout]");
        }
        else
        {
            filters.Add($"[v5]null[vout]");
        }

        filters.Add("[vout]format=yuv420p[v]");

        var tempVideo = Path.Combine(tempDir, "temp_video.mp4");
        var filterComplex = string.Join(",", filters);

        var ffArgs = $"-i \"{bgVideo}\" -i \"{audioPath}\" "
                   + (bgMusic != null ? $"-i \"{bgMusic}\" " : "")
                   + $"-filter_complex \"{filterComplex}\" "
                   + $"-map \"[v]\" -map 1:a "
                   + $"-c:v libx264 -preset veryfast -crf 20 -c:a aac -b:a 192k "
                   + $"-t {targetDuration} -y \"{tempVideo}\"";

        await _ffmpeg.RunAsync(ffArgs, _log);

        // Output folder
        var title = $"{title1} {title2}";
        var outputDir = Path.Combine(outputFolder, title);
        Directory.CreateDirectory(outputDir);

        var finalPath = Path.Combine(outputDir, $"{title}.mp4");
        File.Copy(tempVideo, finalPath, true);

        // Write companion text file
        var txtPath = Path.Combine(outputDir, $"{title} 文本.txt");
        var txtContent = $"标题: {title}\n\n内容:\n{draft.Content}\n\n经文: {draft.Verse}\n{draft.VerseContent}\n\n简介: {draft.Summary}";
        File.WriteAllText(txtPath, txtContent);

        _log?.Invoke($"视频已生成: {finalPath}");
        return finalPath;
    }

    private static string EscapeDrawText(string text)
    {
        return text.Replace("'", "'\\\\''").Replace(":", "\\\\:")
                   .Replace("\\", "/").Replace("%", "\\\\%");
    }
}
