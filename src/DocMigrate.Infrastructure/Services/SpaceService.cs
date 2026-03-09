using DocMigrate.Application.DTOs.Space;
using DocMigrate.Application.Interfaces;
using DocMigrate.Domain.Entities;
using DocMigrate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DocMigrate.Infrastructure.Services;

public class SpaceService(AppDbContext context) : ISpaceService
{
    public async Task<List<SpaceListItem>> GetAllAsync()
    {
        return await context.Spaces
            .Where(s => s.DeletedAt == null)
            .OrderBy(s => s.Title)
            .Select(s => new SpaceListItem
            {
                Id = s.Id,
                Title = s.Title,
                Description = s.Description,
                PageCount = s.Pages.Count(p => p.DeletedAt == null),
                CreatedAt = s.CreatedAt,
            })
            .ToListAsync();
    }

    public async Task<SpaceResponse> GetByIdAsync(int id)
    {
        var entity = await context.Spaces
            .Where(s => s.DeletedAt == null)
            .FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new KeyNotFoundException("Espaco nao encontrado");

        return MapToResponse(entity);
    }

    public async Task<SpaceResponse> CreateAsync(CreateSpaceRequest request)
    {
        var entity = new Space
        {
            Title = request.Title,
            Description = request.Description,
        };

        context.Spaces.Add(entity);
        await context.SaveChangesAsync();

        return MapToResponse(entity);
    }

    public async Task<SpaceResponse> UpdateAsync(int id, UpdateSpaceRequest request)
    {
        var entity = await context.Spaces
            .Where(s => s.DeletedAt == null)
            .FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new KeyNotFoundException("Espaco nao encontrado");

        entity.Title = request.Title;
        entity.Description = request.Description;

        await context.SaveChangesAsync();

        return MapToResponse(entity);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await context.Spaces
            .Where(s => s.DeletedAt == null)
            .FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new KeyNotFoundException("Espaco nao encontrado");

        var hasPages = await context.Pages
            .AnyAsync(p => p.SpaceId == id && p.DeletedAt == null);

        if (hasPages)
            throw new InvalidOperationException(
                "Nao e possivel excluir este espaco. Existem paginas vinculadas. Remova-as primeiro.");

        entity.DeletedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    private static SpaceResponse MapToResponse(Space entity) => new()
    {
        Id = entity.Id,
        Title = entity.Title,
        Description = entity.Description,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
    };
}
