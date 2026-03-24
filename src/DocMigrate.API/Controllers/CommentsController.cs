namespace DocMigrate.API.Controllers;

using DocMigrate.Application.DTOs.Comment;
using DocMigrate.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/pages/{pageId}/comments")]
[Authorize]
public class CommentsController(ICommentService commentService) : AuthenticatedControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CommentResponse>>> GetComments(int pageId)
    {
        return Ok(await commentService.GetCommentsAsync(pageId));
    }

    [HttpGet("count")]
    public async Task<ActionResult<object>> GetCount(int pageId)
    {
        var count = await commentService.GetCommentCountAsync(pageId);
        return Ok(new { count });
    }

    [HttpPost]
    public async Task<ActionResult<CommentResponse>> Create(int pageId, CreateCommentRequest request)
    {
        try
        {
            var userId = await ResolveUserIdOrNullAsync();
            var response = await commentService.CreateAsync(pageId, request, userId);
            return CreatedAtAction(nameof(GetComments), new { pageId }, response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("{commentId}")]
    public async Task<ActionResult<CommentResponse>> Update(int pageId, int commentId, UpdateCommentRequest request)
    {
        try
        {
            var userId = await ResolveUserIdOrNullAsync();
            return Ok(await commentService.UpdateAsync(commentId, request, userId));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Comentario nao encontrado" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpDelete("{commentId}")]
    public async Task<IActionResult> Delete(int pageId, int commentId)
    {
        try
        {
            var userId = await ResolveUserIdOrNullAsync();
            await commentService.DeleteAsync(commentId, userId);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Comentario nao encontrado" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
