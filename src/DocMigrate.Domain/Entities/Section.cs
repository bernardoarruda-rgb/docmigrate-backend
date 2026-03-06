using DocMigrate.Domain.Common;

namespace DocMigrate.Domain.Entities;

public class Section : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public int SpaceId { get; set; }
    public Space Space { get; set; } = null!;

    public ICollection<Page> Pages { get; set; } = [];
}
