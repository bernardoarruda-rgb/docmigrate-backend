using DocMigrate.Domain.Common;

namespace DocMigrate.Domain.Entities;

public class PageTranslation : BaseEntity
{
    public int PageId { get; set; }
    public string Language { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Content { get; set; }
    public string? PlainText { get; set; }
    public string Status { get; set; } = "automatica";
    public string? SourceHash { get; set; }
    public int? TranslatedByUserId { get; set; }

    // Navigation
    public Page Page { get; set; } = null!;
    public User? TranslatedByUser { get; set; }
}
