using DocMigrate.Application.DTOs.Space;
using DocMigrate.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DocMigrate.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SpacesController(ISpaceService spaceService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<SpaceListItem>>> GetAll()
        => Ok(await spaceService.GetAllAsync());

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
    public async Task<ActionResult<SpaceResponse>> Create(CreateSpaceRequest request)
    {
        var response = await spaceService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<SpaceResponse>> Update(int id, UpdateSpaceRequest request)
    {
        try
        {
            return Ok(await spaceService.UpdateAsync(id, request));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Espaco nao encontrado" });
        }
    }

    [HttpDelete("{id}")]
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
}
