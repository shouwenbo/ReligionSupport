using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using MediaColor = System.Windows.Media.Color;

namespace PlaylistMaker.WPF.Services;

public sealed class ThemeService
{
    private readonly IReadOnlyDictionary<string, ThemePalette> _palettes;

    public ThemeService()
    {
        _palettes = new Dictionary<string, ThemePalette>(StringComparer.OrdinalIgnoreCase)
        {
            ["Aurora"] = new ThemePalette(
                MediaColor.FromRgb(5, 25, 45),
                MediaColor.FromRgb(18, 38, 67),
                MediaColor.FromRgb(224, 247, 250),
                MediaColor.FromRgb(16, 185, 129)),
            ["MicaDark"] = new ThemePalette(
                MediaColor.FromRgb(15, 18, 34),
                MediaColor.FromRgb(25, 30, 52),
                MediaColor.FromRgb(226, 232, 240),
                MediaColor.FromRgb(248, 196, 31)),
            ["MicaLight"] = new ThemePalette(
                MediaColor.FromRgb(248, 250, 252),
                MediaColor.FromRgb(255, 255, 255),
                MediaColor.FromRgb(15, 23, 42),
                MediaColor.FromRgb(79, 70, 229))
        };

        ThemeOptions = _palettes.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public IReadOnlyList<string> ThemeOptions { get; }

    public void ApplyTheme(string? themeKey)
    {
        if (System.Windows.Application.Current is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(themeKey) || !_palettes.TryGetValue(themeKey, out var palette))
        {
            palette = _palettes["MicaLight"];
        }

        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            UpdateBrush("BaseBackgroundBrush", palette.BaseBackground);
            UpdateBrush("CardBackgroundBrush", palette.CardBackground);
            UpdateBrush("PrimaryForegroundBrush", palette.Foreground);
            UpdateBrush("SecondaryForegroundBrush", MediaColor.FromArgb(160, palette.Foreground.R, palette.Foreground.G, palette.Foreground.B));
            UpdateBrush("AccentBrush", palette.Accent);
        });
    }

    private static void UpdateBrush(string key, MediaColor color)
    {
        if (System.Windows.Application.Current is null)
        {
            return;
        }

        if (System.Windows.Application.Current.Resources[key] is SolidColorBrush brush)
        {
            if (brush.IsFrozen)
            {
                var replacement = new SolidColorBrush(color);
                replacement.Freeze();
                System.Windows.Application.Current.Resources[key] = replacement;
            }
            else
            {
                brush.Color = color;
            }
        }
        else
        {
            var newBrush = new SolidColorBrush(color);
            newBrush.Freeze();
            System.Windows.Application.Current.Resources[key] = newBrush;
        }
    }

    private sealed record ThemePalette(MediaColor BaseBackground, MediaColor CardBackground, MediaColor Foreground, MediaColor Accent);
}
