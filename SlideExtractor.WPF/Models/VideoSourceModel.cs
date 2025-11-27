using System;

namespace SlideExtractor.WPF.Models;

public class VideoSourceModel
{
	public string FilePath { get; init; } = string.Empty;
	public int TotalFrames { get; init; }
	public double Fps { get; init; }
	public TimeSpan Duration => Fps <= 0 ? TimeSpan.Zero : TimeSpan.FromSeconds(TotalFrames / Fps);
	public int Width { get; init; }
	public int Height { get; init; }
	public long FileSizeBytes { get; init; }
}
