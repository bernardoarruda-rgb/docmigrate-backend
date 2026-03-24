using DocMigrate.Application.DTOs.PageVersion;
using DocMigrate.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocMigrate.API.Controllers;

[ApiController]
[Route("api/pages/{pageId}/versions")]
[Authorize]
public class PageVersionsController(IPageVersionService versionService) : AuthenticatedControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<PageVersionListItem>>> GetVersions(int pageId)
    {
        try
        {
            return Ok(await versionService.GetVersionsAsync(pageId));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Pagina nao encontrada" });
        }
    }

    [HttpGet("{versionNumber}")]
    public async Task<ActionResult<PageVersionResponse>> GetVersion(int pageId, int versionNumber)
    {
        try
        {
            return Ok(await versionService.GetVersionAsync(pageId, versionNumber));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Versao nao encontrada" });
        }
    }

    [HttpPost("{versionNumber}/restore")]
    [Authorize(Policy = "EditorOnly")]
    public async Task<ActionResult<PageVersionResponse>> RestoreVersion(int pageId, int versionNumber)
    {
        try
        {
            var userId = await ResolveUserIdOrNullAsync();
            return Ok(await versionService.RestoreVersionAsync(pageId, versionNumber, userId));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Versao nao encontrada" });
        }
    }
}
