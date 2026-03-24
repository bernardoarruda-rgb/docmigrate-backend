using DocMigrate.Domain.Common;

namespace DocMigrate.Domain.Entities;

public class PageVersion : BaseEntity
{
    public int PageId { get; set; }
    public Page Page { get; set; } = null!;

    public int VersionNumber { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? PlainText { get; set; }

    public int? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }

    public string? ChangeDescription { get; set; }
}
