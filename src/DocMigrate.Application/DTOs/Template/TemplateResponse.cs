namespace DocMigrate.Application.DTOs.Template;

public class TemplateResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string? Content { get; set; }
    public bool IsDefault { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CreatedByName { get; set; }
    public string? UpdatedByName { get; set; }
}
