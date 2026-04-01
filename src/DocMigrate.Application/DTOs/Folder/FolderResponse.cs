using DocMigrate.Application.DTOs.Page;

namespace DocMigrate.Application.DTOs.Folder;

public class FolderResponse
{
    public int Id { get; set; }
    public int SpaceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? IconColor { get; set; }
    public int? ParentFolderId { get; set; }
    public int Level { get; set; } = 1;
    public int SortOrder { get; set; }
    public List<BreadcrumbItem> Breadcrumbs { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
