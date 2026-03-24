namespace DocMigrate.Application.DTOs.Comment;

public class CommentResponse
{
    public int Id { get; set; }
    public int PageId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? AuthorName { get; set; }
    public int? AuthorId { get; set; }
    public int? ParentCommentId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<CommentResponse> Replies { get; set; } = [];
}
