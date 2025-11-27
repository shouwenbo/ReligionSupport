using OpenCvSharp;
using SlideExtractor.WPF.Models;
using SlideExtractor.WPF.Helpers;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace SlideExtractor.WPF.Services;

public record FrameExtractionProgress(int Extracted, int Total, string Stage);

public interface IFrameExtractionService
{
	Task<IReadOnlyList<SlideModel>> ExtractAsync(
		VideoSourceModel source,
		ExtractionSettings settings,
		IProgress<FrameExtractionProgress> progress,
		CancellationToken cancellationToken);
}

public class FrameExtractionService : IFrameExtractionService
{
	private readonly ILoggingService _logger;

	public FrameExtractionService(ILoggingService logger) => _logger = logger;

	public async Task<IReadOnlyList<SlideModel>> ExtractAsync(
		VideoSourceModel source,
		ExtractionSettings settings,
		IProgress<FrameExtractionProgress> progress,
		CancellationToken cancellationToken)
	{
		return await Task.Run(() =>
		{
			var interval = Math.Max(1, settings.FrameInterval);
			var results = new List<SlideModel>();
			using var capture = new VideoCapture(source.FilePath);
			if (!capture.IsOpened())
			{
				throw new InvalidOperationException("无法打开视频文件。");
			}

			var totalFrames = (int)capture.FrameCount;
			var estimatedSamples = Math.Max(1, (int)Math.Ceiling(totalFrames / (double)interval));
			var frame = new Mat();
			var extractorTemp = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "SlideExtractor", Guid.NewGuid().ToString()));
			var savedCount = 0;

			for (var frameIndex = 0; frameIndex < totalFrames; frameIndex += interval)
			{
				cancellationToken.ThrowIfCancellationRequested();

				capture.Set(VideoCaptureProperties.PosFrames, frameIndex);
				if (!capture.Read(frame) || frame.Empty())
				{
					break;
				}

				var pngPath = Path.Combine(extractorTemp.FullName, $"frame_{frameIndex:D6}.png");
				Cv2.ImWrite(pngPath, frame);
				results.Add(new SlideModel
				{
					FrameIndex = frameIndex,
					ImagePath = pngPath,
					ThumbnailPath = pngPath,
					Timestamp = TimeSpan.FromSeconds(frameIndex / Math.Max(0.01, capture.Fps))
				});

				savedCount++;
				progress.Report(new FrameExtractionProgress(savedCount, estimatedSamples, "提取帧"));
			}

			progress.Report(new FrameExtractionProgress(savedCount, estimatedSamples, "提取完成"));
			_logger.Info($"按间隔 {interval} (≈{Math.Round(interval / capture.Fps, 2)} 秒) 仅提取 {savedCount} 帧，自临时目录 {extractorTemp.FullName}");
			return results;
		}, cancellationToken);
	}
}
