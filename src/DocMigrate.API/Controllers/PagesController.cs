using DocMigrate.Application.DTOs.Common;
using DocMigrate.Application.DTOs.Page;
using DocMigrate.Application.DTOs.Tag;
using DocMigrate.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocMigrate.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PagesController(IPageService pageService) : AuthenticatedControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResult<PageListItem>>> GetAll(
        [FromQuery] int spaceId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (spaceId < 1)
            return BadRequest(new { message = "O identificador do espaco deve ser maior ou igual a 1." });

        if (page < 1)
            return BadRequest(new { message = "O numero da pagina deve ser maior ou igual a 1." });

        if (pageSize < 1 || pageSize > 100)
            return BadRequest(new { message = "O tamanho da pagina deve estar entre 1 e 100." });

        return Ok(await pageService.GetAllAsync(spaceId, page, pageSize));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PageResponse>> GetById(int id)
    {
        try
        {
            return Ok(await pageService.GetByIdAsync(id));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Pagina nao encontrada" });
        }
    }

    [HttpPost]
    [Authorize(Policy = "EditorOnly")]
    public async Task<ActionResult<PageResponse>> Create(CreatePageRequest request)
    {
        try
        {
            var userId = await ResolveUserIdOrNullAsync();
            var response = await pageService.CreateAsync(request, userId);
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Espaco nao encontrado" });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "EditorOnly")]
    public async Task<ActionResult<PageResponse>> Update(int id, UpdatePageRequest request)
    {
        try
        {
            var userId = await ResolveUserIdOrNullAsync();
            return Ok(await pageService.UpdateAsync(id, request, userId));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Pagina nao encontrada" });
        }
    }

    [HttpPut("reorder/{spaceId:int}")]
    [Authorize(Policy = "EditorOnly")]
    public async Task<IActionResult> Reorder(int spaceId, [FromBody] ReorderPagesRequest request)
    {
        try
        {
            await pageService.ReorderAsync(spaceId, request);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Espaco nao encontrado" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "EditorOnly")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            await pageService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Pagina nao encontrada" });
        }
    }

    [HttpPost("{id}/lock")]
    [Authorize(Policy = "EditorOnly")]
    public async Task<IActionResult> AcquireLock(int id)
    {
        try
        {
            var userId = GetUserId();
            var acquired = await pageService.AcquireLockAsync(id, userId);
            if (!acquired)
                return Conflict(new { message = "Pagina esta sendo editada por outro usuario." });
            return Ok(new { locked = true });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Pagina nao encontrada" });
        }
    }

    [HttpDelete("{id}/lock")]
    [Authorize(Policy = "EditorOnly")]
    public async Task<IActionResult> ReleaseLock(int id)
    {
        try
        {
            var userId = GetUserId();
            await pageService.ReleaseLockAsync(id, userId);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Pagina nao encontrada" });
        }
    }

    [HttpPatch("{id}/autosave")]
    [Authorize(Policy = "EditorOnly")]
    public async Task<IActionResult> Autosave(int id, [FromBody] AutosavePageRequest request)
    {
        try
        {
            var lockUserId = GetUserId();
            var authorUserId = await ResolveUserIdOrNullAsync();
            await pageService.AutosaveContentAsync(id, lockUserId, request.Content, authorUserId);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Pagina nao encontrada" });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id}/tags")]
    [Authorize(Policy = "EditorOnly")]
    public async Task<ActionResult> SetTags(int id, [FromBody] SetTagsRequest request)
    {
        try
        {
            await pageService.SetTagsAsync(id, request.TagIds);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Pagina nao encontrada" });
        }
    }

    [HttpGet("{id}/headings")]
    public async Task<ActionResult<List<Application.DTOs.Page.HeadingDto>>> GetHeadings(int id)
    {
        try
        {
            return Ok(await pageService.GetHeadingsAsync(id));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Pagina nao encontrada" });
        }
    }

    [HttpGet("{id}/lock-status")]
    public async Task<IActionResult> GetLockStatus(int id)
    {
        try
        {
            var page = await pageService.GetByIdAsync(id);
            var isLocked = page.LockedBy != null
                && page.LockedAt.HasValue
                && page.LockedAt.Value > DateTime.UtcNow.AddMinutes(-30);
            return Ok(new { isLocked, lockedBy = isLocked ? page.LockedBy : null });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Pagina nao encontrada" });
        }
    }
}
