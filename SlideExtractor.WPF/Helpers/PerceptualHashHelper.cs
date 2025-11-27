using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Numerics;

namespace SlideExtractor.WPF.Helpers;

public static class PerceptualHashHelper
{
	public static ulong ComputeHash(string imagePath)
	{
		using var image = Image.Load<Rgba32>(imagePath);
		image.Mutate(x => x.Resize(new ResizeOptions
		{
			Size = new Size(32, 32),
			Sampler = KnownResamplers.Bicubic,
			Mode = ResizeMode.Stretch
		}).Grayscale());

		var pixels = new float[32, 32];
		for (var y = 0; y < 32; y++)
		{
			for (var x = 0; x < 32; x++)
			{
				var color = image[x, y];
				pixels[x, y] = color.R;
			}
		}

		var dct = Dct2D(pixels, 32);
		var avg = 0f;
		for (var y = 0; y < 8; y++)
		{
			for (var x = 0; x < 8; x++)
			{
				if (x != 0 || y != 0)
				{
					avg += dct[x, y];
				}
			}
		}

		avg /= 63f;
		ulong hash = 0;
		var bit = 0;
		for (var y = 0; y < 8; y++)
		{
			for (var x = 0; x < 8; x++)
			{
				if (x == 0 && y == 0) continue;
				if (dct[x, y] > avg)
				{
					hash |= 1UL << bit;
				}
				bit++;
			}
		}

		return hash;
	}

	public static int GetDistance(ulong left, ulong right) => BitOperations.PopCount(left ^ right);

	private static float[,] Dct2D(float[,] data, int size)
	{
		var result = new float[size, size];
		for (var u = 0; u < size; u++)
		{
			for (var v = 0; v < size; v++)
			{
				float sum = 0;
				for (var x = 0; x < size; x++)
				{
					for (var y = 0; y < size; y++)
					{
						sum += data[x, y] *
							   MathF.Cos(((2 * x + 1) * u * MathF.PI) / (2 * size)) *
							   MathF.Cos(((2 * y + 1) * v * MathF.PI) / (2 * size));
					}
				}

				var cu = u == 0 ? 1 / MathF.Sqrt(2) : 1;
				var cv = v == 0 ? 1 / MathF.Sqrt(2) : 1;
				result[u, v] = 0.25f * cu * cv * sum;
			}
		}

		return result;
	}
}
