using DocMigrate.Domain.Common;

namespace DocMigrate.Domain.Entities;

public class Page : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Content { get; set; }
    public int SortOrder { get; set; }

    public int SectionId { get; set; }
    public Section Section { get; set; } = null!;
}
