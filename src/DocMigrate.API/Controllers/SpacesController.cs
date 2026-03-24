using DocMigrate.Application.DTOs.Common;
using DocMigrate.Application.DTOs.Space;
using DocMigrate.Application.DTOs.Tag;
using DocMigrate.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocMigrate.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SpacesController(ISpaceService spaceService) : AuthenticatedControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResult<SpaceListItem>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1)
            return BadRequest(new { message = "O numero da pagina deve ser maior ou igual a 1." });

        if (pageSize < 1 || pageSize > 100)
            return BadRequest(new { message = "O tamanho da pagina deve estar entre 1 e 100." });

        return Ok(await spaceService.GetAllAsync(page, pageSize));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SpaceResponse>> GetById(int id)
    {
        try
        {
            return Ok(await spaceService.GetByIdAsync(id));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Espaco nao encontrado" });
        }
    }

    [HttpPost]
    [Authorize(Policy = "EditorOnly")]
    public async Task<ActionResult<SpaceResponse>> Create(CreateSpaceRequest request)
    {
        var userId = await ResolveUserIdOrNullAsync();
        var response = await spaceService.CreateAsync(request, userId);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "EditorOnly")]
    public async Task<ActionResult<SpaceResponse>> Update(int id, UpdateSpaceRequest request)
    {
        try
        {
            var userId = await ResolveUserIdOrNullAsync();
            return Ok(await spaceService.UpdateAsync(id, request, userId));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Espaco nao encontrado" });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "EditorOnly")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            await spaceService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Espaco nao encontrado" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}/tags")]
    [Authorize(Policy = "EditorOnly")]
    public async Task<ActionResult> SetTags(int id, [FromBody] SetTagsRequest request)
    {
        try
        {
            await spaceService.SetTagsAsync(id, request.TagIds);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Espaco nao encontrado" });
        }
    }
}
