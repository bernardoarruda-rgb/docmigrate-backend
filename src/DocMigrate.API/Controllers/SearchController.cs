using DocMigrate.Application.DTOs.Search;
using DocMigrate.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocMigrate.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SearchController(ISearchService searchService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<SearchResponse>> Search(
        [FromQuery(Name = "q")] string? query,
        [FromQuery] string? type,
        [FromQuery] int? spaceId,
        [FromQuery(Name = "tags")] List<int>? tagIds,
        [FromQuery] int limit = 20,
        [FromQuery] int offset = 0,
        [FromQuery] string? language = null)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest(new { message = "O parametro de busca e obrigatorio" });

        if (query.Trim().Length < 2)
            return BadRequest(new { message = "A busca deve ter pelo menos 2 caracteres" });

        if (type is not null and not "space" and not "page")
            return BadRequest(new { message = "Tipo invalido. Use 'space' ou 'page'" });

        if (limit < 1 || limit > 100)
            return BadRequest(new { message = "Limite deve estar entre 1 e 100" });

        if (offset < 0)
            return BadRequest(new { message = "Offset deve ser maior ou igual a 0" });

        return Ok(await searchService.SearchAsync(query.Trim(), type, spaceId, tagIds, limit, offset, language));
    }
}
