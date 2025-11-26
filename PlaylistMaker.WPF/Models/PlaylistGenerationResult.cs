namespace PlaylistMaker.WPF.Models;

public sealed class PlaylistGenerationResult
{
    public PlaylistGenerationResult(int fileCount, string outputPath)
    {
        FileCount = fileCount;
        OutputPath = outputPath;
    }

    public int FileCount { get; }
    public string OutputPath { get; }
}
