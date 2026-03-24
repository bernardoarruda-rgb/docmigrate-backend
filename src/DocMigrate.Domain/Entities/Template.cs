using DocMigrate.Domain.Common;

namespace DocMigrate.Domain.Entities;

public class Template : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string? Content { get; set; }
    public bool IsDefault { get; set; }
    public int SortOrder { get; set; }

    public int? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
    public int? UpdatedByUserId { get; set; }
    public User? UpdatedByUser { get; set; }
}
