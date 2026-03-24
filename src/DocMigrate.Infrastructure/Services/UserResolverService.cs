using System.Security.Claims;
using DocMigrate.Application.Interfaces;
using DocMigrate.Domain.Entities;
using DocMigrate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DocMigrate.Infrastructure.Services;

public class UserResolverService(AppDbContext context) : IUserResolverService
{
    public async Task<int> ResolveUserIdAsync(string keycloakId, ClaimsPrincipal? principal = null)
    {
        var user = await GetOrProvisionAsync(keycloakId, principal);
        return user.Id;
    }

    public async Task<int?> ResolveUserIdOrNullAsync(string? keycloakId, ClaimsPrincipal? principal = null)
    {
        if (string.IsNullOrEmpty(keycloakId))
            return null;

        var user = await GetOrProvisionAsync(keycloakId, principal);
        return user.Id;
    }

    private async Task<User> GetOrProvisionAsync(string keycloakId, ClaimsPrincipal? principal)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.KeycloakId == keycloakId && u.DeletedAt == null);

        if (user is not null)
            return user;

        var name = ExtractClaim(principal, "name", "preferred_username", ClaimTypes.Name) ?? "Usuario";
        var email = ExtractClaim(principal, ClaimTypes.Email, "email") ?? $"{keycloakId}@docmigrate.local";

        user = new User
        {
            KeycloakId = keycloakId,
            Name = name,
            Email = email,
            Role = "viewer",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return user;
    }

    private static string? ExtractClaim(ClaimsPrincipal? principal, params string[] claimTypes)
    {
        if (principal is null)
            return null;

        foreach (var type in claimTypes)
        {
            var value = principal.FindFirst(type)?.Value;
            if (!string.IsNullOrEmpty(value))
                return value;
        }

        return null;
    }
}
