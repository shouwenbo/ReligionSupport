namespace WeChatPublisher.Models;

public class VideoDraft
{
    public int Id { get; set; }
    public string? TitleWord1 { get; set; }
    public string? TitleWord2 { get; set; }
    public string? Content { get; set; }
    public string? Verse { get; set; }
    public string? VerseContent { get; set; }
    public string? Summary { get; set; }
    public string? AudioPath { get; set; }
    public string? SrtPath { get; set; }
    public string? VideoPath { get; set; }
    public string? OutputFolder { get; set; }
    public string Status { get; set; } = "draft";
    public string CreatedAt { get; set; } = "";
    public string UpdatedAt { get; set; } = "";
}
