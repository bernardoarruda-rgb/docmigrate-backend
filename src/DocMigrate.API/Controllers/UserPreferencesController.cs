using DocMigrate.Application.DTOs.UserPreference;
using DocMigrate.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocMigrate.API.Controllers;

[ApiController]
[Route("api/user-preferences")]
[Authorize]
public class UserPreferencesController(IUserPreferenceService service) : AuthenticatedControllerBase
{
    [HttpGet]
    public async Task<ActionResult<UserPreferenceResponse>> Get()
    {
        var userId = await ResolveUserIdAsync();
        return Ok(await service.GetByUserIdAsync(userId));
    }

    [HttpPut]
    public async Task<ActionResult<UserPreferenceResponse>> Update(UpdateUserPreferenceRequest request)
    {
        var userId = await ResolveUserIdAsync();
        return Ok(await service.UpdateAsync(userId, request));
    }

    [HttpDelete]
    public async Task<IActionResult> Reset()
    {
        var userId = await ResolveUserIdAsync();
        await service.ResetAsync(userId);
        return NoContent();
    }
}
