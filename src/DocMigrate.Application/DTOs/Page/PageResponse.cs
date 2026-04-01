namespace DocMigrate.Application.DTOs.Page;

public class PageResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Content { get; set; }
    public int SortOrder { get; set; }
    public int SpaceId { get; set; }
    public int? FolderId { get; set; }
    public List<BreadcrumbItem> FolderBreadcrumbs { get; set; } = [];
    public string? Icon { get; set; }
    public string? IconColor { get; set; }
    public string? BackgroundColor { get; set; }
    public string? LockedBy { get; set; }
    public DateTime? LockedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CreatedByName { get; set; }
    public string? UpdatedByName { get; set; }
    public string Language { get; set; } = "pt-BR";
    public string? CoverType { get; set; }
    public string? CoverValue { get; set; }
    public int CoverPosition { get; set; } = 50;
    public string? CoverAttribution { get; set; }
    public string ContentWidth { get; set; } = "normal";
}
