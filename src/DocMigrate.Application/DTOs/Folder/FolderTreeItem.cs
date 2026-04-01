using DocMigrate.Application.DTOs.Page;

namespace DocMigrate.Application.DTOs.Folder;

public class FolderTreeItem
{
    public int Id { get; set; }
    public int SpaceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? IconColor { get; set; }
    public int? ParentFolderId { get; set; }
    public int Level { get; set; } = 1;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<FolderTreeItem> ChildFolders { get; set; } = [];
    public List<PageListItem> Pages { get; set; } = [];
}
