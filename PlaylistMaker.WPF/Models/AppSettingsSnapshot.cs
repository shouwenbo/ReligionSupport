namespace PlaylistMaker.WPF.Models;

public sealed record AppSettingsSnapshot(
    string LastScanFolder,
    string Extensions,
    bool IncludeSubdirectories,
    bool AutoOpenAfterExport,
    string PlaylistTitle,
    string ThemeVariant);
