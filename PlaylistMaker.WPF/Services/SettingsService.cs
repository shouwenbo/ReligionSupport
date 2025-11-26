using System;
using System.Threading;
using PlaylistMaker.WPF.Models;

namespace PlaylistMaker.WPF.Services;

public sealed class SettingsService
{
    private readonly object _syncRoot = new();
    private AppSettingsSnapshot _snapshot = new("", ".mp3;.mp4", true, true, "播放列表", "MicaLight");

    public AppSettingsSnapshot Current => Volatile.Read(ref _snapshot);

    public void Load()
    {
        Properties.Settings.Default.Reload();
        _snapshot = CreateSnapshot();
    }

    internal void Update(Action<Properties.Settings> apply)
    {
        if (apply is null)
        {
            return;
        }

        lock (_syncRoot)
        {
            apply(Properties.Settings.Default);
            Properties.Settings.Default.Save();
            _snapshot = CreateSnapshot();
        }
    }

    private static AppSettingsSnapshot CreateSnapshot()
    {
        var s = Properties.Settings.Default;
        return new AppSettingsSnapshot(
            s.LastScanFolder ?? string.Empty,
            s.Extensions ?? string.Empty,
            s.IncludeSubdirectories,
            s.AutoOpenAfterExport,
            s.PlaylistTitle ?? "播放列表",
            s.ThemeVariant ?? "MicaLight");
    }
}
