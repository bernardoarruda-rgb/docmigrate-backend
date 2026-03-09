using DocMigrate.Application.DTOs.Page;
using DocMigrate.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DocMigrate.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PagesController(IPageService pageService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<PageListItem>>> GetAll([FromQuery] int spaceId)
        => Ok(await pageService.GetAllAsync(spaceId));

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
    public async Task<ActionResult<PageResponse>> Create(CreatePageRequest request)
    {
        try
        {
            var response = await pageService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Espaco nao encontrado" });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<PageResponse>> Update(int id, UpdatePageRequest request)
    {
        try
        {
            return Ok(await pageService.UpdateAsync(id, request));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Pagina nao encontrada" });
        }
    }

    [HttpDelete("{id}")]
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
}
