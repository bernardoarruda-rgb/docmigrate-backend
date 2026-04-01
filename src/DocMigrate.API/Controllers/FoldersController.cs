using DocMigrate.Application.DTOs.Folder;
using DocMigrate.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocMigrate.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FoldersController(IFolderService folderService) : ControllerBase
{
    [HttpGet("space/{spaceId:int}")]
    public async Task<ActionResult<List<FolderTreeItem>>> GetAll(int spaceId)
    {
        if (spaceId < 1)
            return BadRequest(new { message = "O identificador do espaco deve ser maior ou igual a 1." });

        return Ok(await folderService.GetAllAsync(spaceId));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<FolderResponse>> GetById(int id)
    {
        try
        {
            return Ok(await folderService.GetByIdAsync(id));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Pasta nao encontrada" });
        }
    }

    [HttpPost]
    [Authorize(Policy = "EditorOnly")]
    public async Task<ActionResult<FolderResponse>> Create(CreateFolderRequest request)
    {
        try
        {
            var response = await folderService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "EditorOnly")]
    public async Task<ActionResult<FolderResponse>> Update(int id, UpdateFolderRequest request)
    {
        try
        {
            return Ok(await folderService.UpdateAsync(id, request));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "EditorOnly")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            await folderService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Pasta nao encontrada" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("reorder/{spaceId:int}")]
    [Authorize(Policy = "EditorOnly")]
    public async Task<IActionResult> Reorder(int spaceId, [FromBody] ReorderFoldersRequest request)
    {
        try
        {
            await folderService.ReorderAsync(spaceId, request);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
