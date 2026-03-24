using DocMigrate.Domain.Common;

namespace DocMigrate.Domain.Entities;

public class PageComment : BaseEntity
{
    public int PageId { get; set; }
    public Page Page { get; set; } = null!;

    public string Content { get; set; } = string.Empty;

    public int? AuthorId { get; set; }
    public User? Author { get; set; }

    public int? ParentCommentId { get; set; }
    public PageComment? ParentComment { get; set; }
    public ICollection<PageComment> Replies { get; set; } = [];
}
