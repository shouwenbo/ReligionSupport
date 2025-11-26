using System.Collections.Generic;

namespace PlaylistMaker.WPF.Models;

public sealed class PlaylistGenerationRequest
{
    public required string FolderPath { get; init; }
    public required IReadOnlyCollection<string> Extensions { get; init; }
    public bool IncludeSubdirectories { get; init; }
    public string PlaylistTitle { get; init; } = "播放列表";
}
