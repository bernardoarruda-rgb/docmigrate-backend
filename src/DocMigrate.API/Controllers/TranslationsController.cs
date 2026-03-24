using DocMigrate.Application.DTOs.Translation;
using DocMigrate.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocMigrate.API.Controllers;

[ApiController]
[Route("api/pages/{pageId:int}/translations")]
[Authorize]
public class TranslationsController(IPageTranslationService translationService) : AuthenticatedControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<TranslationListItem>>> GetAll(int pageId)
    {
        try
        {
            var result = await translationService.GetTranslationsAsync(pageId);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Pagina nao encontrada" });
        }
    }

    [HttpGet("{lang}")]
    public async Task<ActionResult<TranslationResponse>> Get(int pageId, string lang)
    {
        try
        {
            var result = await translationService.GetTranslationAsync(pageId, lang);
            return Ok(result);
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

    [HttpPost("{lang}")]
    [Authorize(Policy = "EditorOnly")]
    public async Task<ActionResult<TranslationResponse>> Generate(int pageId, string lang)
    {
        try
        {
            var userId = await ResolveUserIdOrNullAsync();
            var result = await translationService.GenerateTranslationAsync(pageId, lang, userId);
            return CreatedAtAction(nameof(Get), new { pageId, lang }, result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Pagina nao encontrada" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ApplicationException)
        {
            return StatusCode(502, new { message = "Falha ao gerar traducao automatica. Tente novamente mais tarde." });
        }
    }

    [HttpPut("{lang}")]
    [Authorize(Policy = "EditorOnly")]
    public async Task<ActionResult<TranslationResponse>> Update(int pageId, string lang, UpdateTranslationRequest request)
    {
        try
        {
            var userId = await ResolveUserIdOrNullAsync();
            var result = await translationService.UpdateTranslationAsync(pageId, lang, request, userId);
            return Ok(result);
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

    [HttpDelete("{lang}")]
    [Authorize(Policy = "EditorOnly")]
    public async Task<IActionResult> Delete(int pageId, string lang)
    {
        try
        {
            await translationService.DeleteTranslationAsync(pageId, lang);
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
