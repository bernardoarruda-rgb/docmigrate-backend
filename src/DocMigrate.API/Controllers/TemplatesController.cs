using DocMigrate.Application.DTOs.Template;
using DocMigrate.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocMigrate.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TemplatesController(ITemplateService templateService) : AuthenticatedControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<TemplateListItem>>> GetAll()
    {
        return Ok(await templateService.GetAllAsync());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TemplateResponse>> GetById(int id)
    {
        try
        {
            return Ok(await templateService.GetByIdAsync(id));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Template nao encontrado" });
        }
    }

    [HttpPost]
    public async Task<ActionResult<TemplateResponse>> Create(CreateTemplateRequest request)
    {
        var userId = await ResolveUserIdOrNullAsync();
        var response = await templateService.CreateAsync(request, userId);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TemplateResponse>> Update(int id, UpdateTemplateRequest request)
    {
        try
        {
            var userId = await ResolveUserIdOrNullAsync();
            return Ok(await templateService.UpdateAsync(id, request, userId));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Template nao encontrado" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            await templateService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Template nao encontrado" });
        }
    }
}
