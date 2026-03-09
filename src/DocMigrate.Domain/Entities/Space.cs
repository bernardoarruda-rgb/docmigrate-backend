using DocMigrate.Domain.Common;

namespace DocMigrate.Domain.Entities;

public class Space : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<Page> Pages { get; set; } = [];
}
