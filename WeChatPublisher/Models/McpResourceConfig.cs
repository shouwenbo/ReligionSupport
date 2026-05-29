namespace WeChatPublisher.Models;

public class McpResourceConfig
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string ResourceType { get; set; } = "LocalFolder";
    public string Path { get; set; } = "";
    public string? FileFilter { get; set; } = "*.*";
    public string? Description { get; set; }
    public string CreatedAt { get; set; } = "";
    public string UpdatedAt { get; set; } = "";
}
