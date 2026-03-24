using DocMigrate.Domain.Common;

namespace DocMigrate.Domain.Entities;

public class Tag : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }

    public ICollection<Page> Pages { get; set; } = [];
    public ICollection<Space> Spaces { get; set; } = [];
}
