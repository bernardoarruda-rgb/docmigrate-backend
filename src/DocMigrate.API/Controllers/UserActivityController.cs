namespace DocMigrate.API.Controllers;

using DocMigrate.Application.DTOs.Favorite;
using DocMigrate.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/user")]
[Authorize]
public class UserActivityController(IUserActivityService activityService) : AuthenticatedControllerBase
{
    [HttpGet("favorites")]
    public async Task<ActionResult<List<FavoritePageItem>>> GetFavorites()
    {
        var userId = await ResolveUserIdAsync();
        return Ok(await activityService.GetFavoritesAsync(userId));
    }

    [HttpGet("favorites/{pageId}/check")]
    public async Task<ActionResult<object>> IsFavorite(int pageId)
    {
        var userId = await ResolveUserIdAsync();
        var isFavorite = await activityService.IsFavoriteAsync(userId, pageId);
        return Ok(new { isFavorite });
    }

    [HttpPost("favorites/{pageId}/toggle")]
    public async Task<IActionResult> ToggleFavorite(int pageId)
    {
        try
        {
            var userId = await ResolveUserIdAsync();
            await activityService.ToggleFavoriteAsync(userId, pageId);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Pagina nao encontrada" });
        }
    }

    [HttpGet("recent")]
    public async Task<ActionResult<List<RecentPageItem>>> GetRecentPages([FromQuery] int limit = 10)
    {
        var userId = await ResolveUserIdAsync();
        return Ok(await activityService.GetRecentPagesAsync(userId, limit));
    }

    [HttpPost("visits/{pageId}")]
    public async Task<IActionResult> RecordVisit(int pageId)
    {
        var userId = await ResolveUserIdAsync();
        await activityService.RecordVisitAsync(userId, pageId);
        return NoContent();
    }
}
