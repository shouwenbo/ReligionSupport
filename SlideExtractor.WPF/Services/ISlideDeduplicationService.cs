using SlideExtractor.WPF.Helpers;
using SlideExtractor.WPF.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SlideExtractor.WPF.Services;

public record DeduplicationProgress(int Processed, int Total);

public interface ISlideDeduplicationService
{
	Task<IReadOnlyList<SlideModel>> DeduplicateAsync(
		IReadOnlyList<SlideModel> frames,
		int hashThreshold,
		IProgress<DeduplicationProgress> progress,
		CancellationToken cancellationToken);
}

public class SlideDeduplicationService : ISlideDeduplicationService
{
	public async Task<IReadOnlyList<SlideModel>> DeduplicateAsync(
		IReadOnlyList<SlideModel> frames,
		int hashThreshold,
		IProgress<DeduplicationProgress> progress,
		CancellationToken cancellationToken)
	{
		return await Task.Run(() =>
		{
			var hashes = new List<ulong>();
			var result = new List<SlideModel>();
			for (var i = 0; i < frames.Count; i++)
			{
				cancellationToken.ThrowIfCancellationRequested();
				var frame = frames[i];
				var hash = PerceptualHashHelper.ComputeHash(frame.ImagePath);
				var isUnique = true;
				foreach (var existing in hashes)
				{
					if (PerceptualHashHelper.GetDistance(hash, existing) <= hashThreshold)
					{
						isUnique = false;
						break;
					}
				}

				if (isUnique)
				{
					result.Add(frame);
					hashes.Add(hash);
				}

				progress.Report(new DeduplicationProgress(i + 1, frames.Count));
			}

			return result;
		}, cancellationToken);
	}
}
