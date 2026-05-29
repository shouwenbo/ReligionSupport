using SqlSugar;

namespace WeChatPublisher.Models;

[SugarTable("PromptTemplates")]
public class PromptTemplate
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Category { get; set; } = "Article";
    public string SystemPrompt { get; set; } = "";
    public string UserPromptTemplate { get; set; } = "";
    public string? Description { get; set; }
    public int IsActive { get; set; } = 1;
    public int SortOrder { get; set; }
    public string CreatedAt { get; set; } = "";
    public string UpdatedAt { get; set; } = "";
}
