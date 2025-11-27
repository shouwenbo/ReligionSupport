using SlideExtractor.WPF.Models;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SlideExtractor.WPF.Services;

public interface IPresentationService
{
	Task ExportImagesAsync(IReadOnlyList<SlideModel> slides, string destinationFolder, string format, CancellationToken token);
}

public class PresentationService : IPresentationService
{
	public async Task ExportImagesAsync(IReadOnlyList<SlideModel> slides, string destinationFolder, string format, CancellationToken token)
	{
		Directory.CreateDirectory(destinationFolder);
		await Task.Run(() =>
		{
			for (var i = 0; i < slides.Count; i++)
			{
				token.ThrowIfCancellationRequested();
				var target = Path.Combine(destinationFolder, $"{i + 1:D3}.{format.ToLower()}");
				File.Copy(slides[i].ImagePath, target, true);
			}
		}, token);
	}
}
