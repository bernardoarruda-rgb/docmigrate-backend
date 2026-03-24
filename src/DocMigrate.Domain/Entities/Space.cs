using DocMigrate.Domain.Common;
using NpgsqlTypes;

namespace DocMigrate.Domain.Entities;

public class Space : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string? IconColor { get; set; }
    public string? BackgroundColor { get; set; }

    public NpgsqlTsVector? SearchVector { get; set; }
    public ICollection<Tag> Tags { get; set; } = [];

    public int? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
    public int? UpdatedByUserId { get; set; }
    public User? UpdatedByUser { get; set; }

    public ICollection<Page> Pages { get; set; } = [];
}
