using DocMigrate.Domain.Common;

namespace DocMigrate.Domain.Entities;

public class Folder : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? IconColor { get; set; }
    public int SortOrder { get; set; }
    public int Level { get; set; } = 1;

    // Relationships
    public int SpaceId { get; set; }
    public Space Space { get; set; } = null!;

    public int? ParentFolderId { get; set; }
    public Folder? ParentFolder { get; set; }
    public ICollection<Folder> ChildFolders { get; set; } = [];

    public ICollection<Page> Pages { get; set; } = [];
}
