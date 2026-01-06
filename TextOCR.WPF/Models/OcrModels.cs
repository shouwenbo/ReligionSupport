namespace TextOCR.WPF.Models;

public class OcrConfig
{
    public string VideoPath { get; set; } = string.Empty;
    public int SubtitleHeight { get; set; } = 80;
    public bool FromTop { get; set; } = false;
    public double FrameInterval { get; set; } = 0.5;
    public int PreviewSecond { get; set; } = 30;
    public string? TimeRange { get; set; }
    public string OutputPath { get; set; } = "output_subtitles.txt";
}

public class OcrResult
{
    public bool Success { get; set; }
    public string OutputFilePath { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public TimeSpan ProcessingTime { get; set; }
}

public class VideoInfo
{
    public int Width { get; set; }
    public int Height { get; set; }
    public double Fps { get; set; }
    public int TotalFrames { get; set; }
    public TimeSpan Duration { get; set; }
}

public class PreviewResult
{
    public byte[]? FrameImage { get; set; }
    public byte[]? SubtitleRegionImage { get; set; }
    public string RecognizedText { get; set; } = string.Empty;
}
