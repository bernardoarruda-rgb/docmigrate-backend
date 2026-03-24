namespace DocMigrate.Application.DTOs.Comment;

public class CreateCommentRequest
{
    public string Content { get; set; } = string.Empty;
    public int? ParentCommentId { get; set; }
}
