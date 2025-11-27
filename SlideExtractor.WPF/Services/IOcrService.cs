using SlideExtractor.WPF.Models;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tesseract;

namespace SlideExtractor.WPF.Services;

public readonly record struct OcrProgress(int Processed, int Total);
public readonly record struct OcrResult(bool Success, string? ErrorMessage);

public interface IOcrService
{
	Task<OcrResult> PerformOcrAsync(
		IReadOnlyList<SlideModel> slides,
		ExtractionSettings settings,
		IProgress<OcrProgress> progress,
		CancellationToken cancellationToken);
}

public class OcrService : IOcrService
{
	private readonly ILoggingService _logger;

	public OcrService(ILoggingService logger) => _logger = logger;

	public async Task<OcrResult> PerformOcrAsync(
		IReadOnlyList<SlideModel> slides,
		ExtractionSettings settings,
		IProgress<OcrProgress> progress,
		CancellationToken cancellationToken)
	{
		return await Task.Run(() =>
		{
			if (slides.Count == 0)
			{
				return new OcrResult(true, null);
			}

			if (string.IsNullOrWhiteSpace(settings.TesseractPath) || !File.Exists(settings.TesseractPath))
			{
				var msg = "未配置有效的 Tesseract 可执行文件，请在 OCR 设置中重新选择 (Error-1)。";
				_logger.Warn(msg);
				return new OcrResult(false, msg);
			}

			var tessDir = Path.GetDirectoryName(settings.TesseractPath)!;
			var tessDataPath = Path.Combine(tessDir, "tessdata");
			if (!Directory.Exists(tessDataPath))
			{
				var msg = "找不到 tessdata 目录，请确认 Tesseract 安装完整 (Error-1)。";
				_logger.Warn(msg);
				return new OcrResult(false, msg);
			}

			TesseractEnviornment.CustomSearchPath = tessDir;

			try
			{
				using var engine = new TesseractEngine(
					tessDir,
					string.IsNullOrWhiteSpace(settings.OcrLanguages) ? "eng" : settings.OcrLanguages,
					EngineMode.Default);

				for (var i = 0; i < slides.Count; i++)
				{
					cancellationToken.ThrowIfCancellationRequested();
					try
					{
						using var pix = Pix.LoadFromFile(slides[i].ImagePath);
						using var page = engine.Process(pix);
						slides[i].OcrText = page.GetText().Trim();
					}
					catch (Exception ex)
					{
						_logger.Warn($"OCR 处理第 {i + 1} 张失败：{ex.Message}");
					}

					progress.Report(new OcrProgress(i + 1, slides.Count));
				}

				return new OcrResult(true, null);
			}
			catch (Exception ex)
			{
				var msg = $"OCR 初始化失败：{ex.Message}";
				_logger.Error(msg, ex);
				return new OcrResult(false, msg);
			}
		}, cancellationToken);
	}
}
