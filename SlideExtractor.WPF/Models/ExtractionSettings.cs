namespace SlideExtractor.WPF.Models;

public class ExtractionSettings
{
	public int FrameInterval { get; set; }
	public int HashThreshold { get; set; }
	public string TesseractPath { get; set; } = string.Empty;
	public string OcrLanguages { get; set; } = "eng";
	public string OutputDirectory { get; set; } = string.Empty;
	public string ImageFormat { get; set; } = "PNG";
	public int ImageQuality { get; set; } = 90;
	public bool AutoSave { get; set; }
	public string ExportQuality { get; set; } = "High";
}
