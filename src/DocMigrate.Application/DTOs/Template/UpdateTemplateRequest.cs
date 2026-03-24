namespace DocMigrate.Application.DTOs.Template;

public class UpdateTemplateRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string? Content { get; set; }
    public int SortOrder { get; set; }
}
