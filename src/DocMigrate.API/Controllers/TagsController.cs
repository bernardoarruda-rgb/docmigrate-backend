using DocMigrate.Application.DTOs.Tag;
using DocMigrate.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocMigrate.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TagsController(ITagService tagService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<TagListItem>>> GetAll()
    {
        return Ok(await tagService.GetAllAsync());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TagResponse>> GetById(int id)
    {
        try
        {
            return Ok(await tagService.GetByIdAsync(id));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Tag nao encontrada" });
        }
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<TagListItem>>> Search([FromQuery(Name = "q")] string? query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 1)
            return Ok(new List<TagListItem>());

        return Ok(await tagService.SearchAsync(query.Trim()));
    }

    [HttpPost]
    [Authorize(Policy = "EditorOnly")]
    public async Task<ActionResult<TagResponse>> Create(CreateTagRequest request)
    {
        try
        {
            var response = await tagService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "EditorOnly")]
    public async Task<ActionResult<TagResponse>> Update(int id, UpdateTagRequest request)
    {
        try
        {
            return Ok(await tagService.UpdateAsync(id, request));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Tag nao encontrada" });
        }
        catch (InvalidOperationException ex)
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
            await tagService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Tag nao encontrada" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
