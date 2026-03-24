namespace DocMigrate.Infrastructure.Services;

using DocMigrate.Application.DTOs.Comment;
using DocMigrate.Application.Interfaces;
using DocMigrate.Domain.Entities;
using DocMigrate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class CommentService(AppDbContext context) : ICommentService
{
    public async Task<List<CommentResponse>> GetCommentsAsync(int pageId)
    {
        var comments = await context.PageComments
            .AsNoTracking()
            .Include(c => c.Author)
            .Include(c => c.Replies.Where(r => r.DeletedAt == null))
                .ThenInclude(r => r.Author)
            .Where(c => c.PageId == pageId && c.ParentCommentId == null)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return comments.Select(MapToResponse).ToList();
    }

    public async Task<CommentResponse> GetByIdAsync(int commentId)
    {
        var comment = await context.PageComments
            .AsNoTracking()
            .Include(c => c.Author)
            .Include(c => c.Replies.Where(r => r.DeletedAt == null))
                .ThenInclude(r => r.Author)
            .FirstOrDefaultAsync(c => c.Id == commentId)
            ?? throw new KeyNotFoundException("Comentario nao encontrado");

        return MapToResponse(comment);
    }

    public async Task<CommentResponse> CreateAsync(int pageId, CreateCommentRequest request, int? userId)
    {
        var pageExists = await context.Pages.AnyAsync(p => p.Id == pageId && p.DeletedAt == null);
        if (!pageExists) throw new KeyNotFoundException("Pagina nao encontrada");

        if (request.ParentCommentId.HasValue)
        {
            var parentExists = await context.PageComments
                .AnyAsync(c => c.Id == request.ParentCommentId.Value && c.PageId == pageId);
            if (!parentExists) throw new KeyNotFoundException("Comentario pai nao encontrado");
        }

        var comment = new PageComment
        {
            PageId = pageId,
            Content = request.Content,
            AuthorId = userId,
            ParentCommentId = request.ParentCommentId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        context.PageComments.Add(comment);
        await context.SaveChangesAsync();

        var created = await context.PageComments
            .Include(c => c.Author)
            .FirstAsync(c => c.Id == comment.Id);

        return MapToResponse(created);
    }

    public async Task<CommentResponse> UpdateAsync(int commentId, UpdateCommentRequest request, int? userId)
    {
        var comment = await context.PageComments
            .Include(c => c.Author)
            .FirstOrDefaultAsync(c => c.Id == commentId)
            ?? throw new KeyNotFoundException("Comentario nao encontrado");

        if (comment.AuthorId != userId)
            throw new UnauthorizedAccessException("Apenas o autor pode editar este comentario");

        comment.Content = request.Content;
        comment.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return MapToResponse(comment);
    }

    public async Task DeleteAsync(int commentId, int? userId)
    {
        var comment = await context.PageComments
            .Include(c => c.Replies)
            .FirstOrDefaultAsync(c => c.Id == commentId)
            ?? throw new KeyNotFoundException("Comentario nao encontrado");

        if (comment.AuthorId != userId)
            throw new UnauthorizedAccessException("Apenas o autor pode excluir este comentario");

        comment.DeletedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    public async Task<int> GetCommentCountAsync(int pageId)
    {
        return await context.PageComments
            .AsNoTracking()
            .Where(c => c.PageId == pageId)
            .CountAsync();
    }

    private static CommentResponse MapToResponse(PageComment c) => new()
    {
        Id = c.Id,
        PageId = c.PageId,
        Content = c.Content,
        AuthorName = c.Author?.Name,
        AuthorId = c.AuthorId,
        ParentCommentId = c.ParentCommentId,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt,
        Replies = c.Replies
            .Where(r => r.DeletedAt == null)
            .OrderBy(r => r.CreatedAt)
            .Select(r => new CommentResponse
            {
                Id = r.Id,
                PageId = r.PageId,
                Content = r.Content,
                AuthorName = r.Author?.Name,
                AuthorId = r.AuthorId,
                ParentCommentId = r.ParentCommentId,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                Replies = [],
            })
            .ToList(),
    };
}
