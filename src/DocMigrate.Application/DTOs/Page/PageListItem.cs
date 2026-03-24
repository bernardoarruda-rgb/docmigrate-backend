namespace DocMigrate.Application.DTOs.Page;

public class PageListItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public int SpaceId { get; set; }
    public string? Icon { get; set; }
    public string? IconColor { get; set; }
    public string? BackgroundColor { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Language { get; set; } = "pt-BR";
}
