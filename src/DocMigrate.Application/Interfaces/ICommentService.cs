namespace DocMigrate.Application.Interfaces;

using DocMigrate.Application.DTOs.Comment;

public interface ICommentService
{
    Task<List<CommentResponse>> GetCommentsAsync(int pageId);
    Task<CommentResponse> GetByIdAsync(int commentId);
    Task<CommentResponse> CreateAsync(int pageId, CreateCommentRequest request, int? userId);
    Task<CommentResponse> UpdateAsync(int commentId, UpdateCommentRequest request, int? userId);
    Task DeleteAsync(int commentId, int? userId);
    Task<int> GetCommentCountAsync(int pageId);
}
