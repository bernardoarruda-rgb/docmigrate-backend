using DocMigrate.Application.DTOs.Reference;
using DocMigrate.Application.Interfaces;
using DocMigrate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DocMigrate.Infrastructure.Services;

public class ReferenceService(AppDbContext context) : IReferenceService
{
    public async Task<CheckReferencesResponse> CheckAsync(CheckReferencesRequest request)
    {
        var existingPageIds = request.PageIds.Count > 0
            ? await context.Pages
                .AsNoTracking()
                .Where(p => request.PageIds.Contains(p.Id) && p.DeletedAt == null)
                .Select(p => p.Id)
                .ToListAsync()
            : [];

        var existingSpaceIds = request.SpaceIds.Count > 0
            ? await context.Spaces
                .AsNoTracking()
                .Where(s => request.SpaceIds.Contains(s.Id) && s.DeletedAt == null)
                .Select(s => s.Id)
                .ToListAsync()
            : [];

        return new CheckReferencesResponse
        {
            ExistingPageIds = existingPageIds,
            ExistingSpaceIds = existingSpaceIds,
        };
    }
}
