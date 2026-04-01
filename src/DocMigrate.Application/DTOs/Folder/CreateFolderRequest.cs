namespace DocMigrate.Application.DTOs.Folder;

public class CreateFolderRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? IconColor { get; set; }
    public int SortOrder { get; set; }
    public int SpaceId { get; set; }
    public int? ParentFolderId { get; set; }
}
