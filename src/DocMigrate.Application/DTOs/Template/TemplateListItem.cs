namespace DocMigrate.Application.DTOs.Template;

public class TemplateListItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public bool IsDefault { get; set; }
    public int SortOrder { get; set; }
}
