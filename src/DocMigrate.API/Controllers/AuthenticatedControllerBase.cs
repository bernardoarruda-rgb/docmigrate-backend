using System.Security.Claims;
using DocMigrate.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DocMigrate.API.Controllers;

public abstract class AuthenticatedControllerBase : ControllerBase
{
    private const string DevFallbackUserId = "1";
    private const string UserIdHeader = "X-User-Id";

    protected string GetUserId()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst("sub")?.Value;

        if (!string.IsNullOrEmpty(userId))
            return userId;

        var environment = HttpContext.RequestServices.GetRequiredService<IHostEnvironment>();

        if (environment.IsDevelopment())
        {
            var headerValue = Request.Headers[UserIdHeader].FirstOrDefault();
            return string.IsNullOrEmpty(headerValue) ? DevFallbackUserId : headerValue;
        }

        throw new UnauthorizedAccessException("Usuario nao autenticado.");
    }

    protected async Task<int> ResolveUserIdAsync()
    {
        var keycloakId = GetUserId();
        var resolver = HttpContext.RequestServices.GetRequiredService<IUserResolverService>();
        return await resolver.ResolveUserIdAsync(keycloakId, User);
    }

    protected async Task<int?> ResolveUserIdOrNullAsync()
    {
        try
        {
            var keycloakId = GetUserId();
            var resolver = HttpContext.RequestServices.GetRequiredService<IUserResolverService>();
            return await resolver.ResolveUserIdOrNullAsync(keycloakId, User);
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }
}
