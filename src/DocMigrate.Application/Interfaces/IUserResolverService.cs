using System.Security.Claims;

namespace DocMigrate.Application.Interfaces;

public interface IUserResolverService
{
    Task<int> ResolveUserIdAsync(string keycloakId, ClaimsPrincipal? principal = null);
    Task<int?> ResolveUserIdOrNullAsync(string? keycloakId, ClaimsPrincipal? principal = null);
}
