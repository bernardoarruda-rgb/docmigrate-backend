using DocMigrate.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocMigrate.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "EditorOnly")]
public class UnsplashController(IUnsplashService unsplashService) : ControllerBase
{
    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string q,
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 12)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { message = "Termo de busca e obrigatorio." });

        if (page < 1) page = 1;
        if (perPage < 1 || perPage > 30) perPage = 12;

        var result = await unsplashService.SearchAsync(q, page, perPage);
        return Ok(result);
    }
}
