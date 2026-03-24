using DocMigrate.Application.DTOs.Common;
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
            .AsNoTracking()
            .Where(s => s.DeletedAt == null)
            .OrderBy(s => s.Title)
            .Select(s => new SpaceListItem
            {
                Id = s.Id,
                Title = s.Title,
                Description = s.Description,
                Icon = s.Icon,
                IconColor = s.IconColor,
                BackgroundColor = s.BackgroundColor,
                PageCount = s.Pages.Count(p => p.DeletedAt == null),
                CreatedAt = s.CreatedAt,
            })
            .ToListAsync();
    }

    public async Task<PaginatedResult<SpaceListItem>> GetAllAsync(int page, int pageSize)
    {
        var query = context.Spaces
            .AsNoTracking()
            .Where(s => s.DeletedAt == null)
            .OrderBy(s => s.Title);

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SpaceListItem
            {
                Id = s.Id,
                Title = s.Title,
                Description = s.Description,
                Icon = s.Icon,
                IconColor = s.IconColor,
                BackgroundColor = s.BackgroundColor,
                PageCount = s.Pages.Count(p => p.DeletedAt == null),
                CreatedAt = s.CreatedAt,
            })
            .ToListAsync();

        return new PaginatedResult<SpaceListItem>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<SpaceResponse> GetByIdAsync(int id)
    {
        var entity = await context.Spaces
            .AsNoTracking()
            .Include(s => s.CreatedByUser)
            .Include(s => s.UpdatedByUser)
            .Where(s => s.DeletedAt == null)
            .FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new KeyNotFoundException("Espaco nao encontrado");

        return MapToResponse(entity);
    }

    public async Task<SpaceResponse> CreateAsync(CreateSpaceRequest request, int? userId = null)
    {
        var entity = new Space
        {
            Title = request.Title,
            Description = request.Description,
            Icon = request.Icon,
            IconColor = request.IconColor,
            BackgroundColor = request.BackgroundColor,
        };

        entity.CreatedByUserId = userId;
        entity.UpdatedByUserId = userId;

        context.Spaces.Add(entity);
        await context.SaveChangesAsync();

        await context.Entry(entity).Reference(e => e.CreatedByUser).LoadAsync();
        await context.Entry(entity).Reference(e => e.UpdatedByUser).LoadAsync();

        return MapToResponse(entity);
    }

    public async Task<SpaceResponse> UpdateAsync(int id, UpdateSpaceRequest request, int? userId = null)
    {
        var entity = await context.Spaces
            .Include(s => s.CreatedByUser)
            .Include(s => s.UpdatedByUser)
            .Where(s => s.DeletedAt == null)
            .FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new KeyNotFoundException("Espaco nao encontrado");

        entity.Title = request.Title;
        entity.Description = request.Description;
        entity.Icon = request.Icon;
        entity.IconColor = request.IconColor;
        entity.BackgroundColor = request.BackgroundColor;
        entity.UpdatedByUserId = userId;

        await context.SaveChangesAsync();

        await context.Entry(entity).Reference(e => e.UpdatedByUser).LoadAsync();

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

    public async Task SetTagsAsync(int spaceId, List<int> tagIds)
    {
        var entity = await context.Spaces
            .Include(s => s.Tags)
            .Where(s => s.DeletedAt == null)
            .FirstOrDefaultAsync(s => s.Id == spaceId)
            ?? throw new KeyNotFoundException("Espaco nao encontrado");

        var tags = await context.Tags
            .Where(t => t.DeletedAt == null && tagIds.Contains(t.Id))
            .ToListAsync();

        entity.Tags.Clear();
        foreach (var tag in tags)
            entity.Tags.Add(tag);

        await context.SaveChangesAsync();
    }

    private static SpaceResponse MapToResponse(Space entity) => new()
    {
        Id = entity.Id,
        Title = entity.Title,
        Description = entity.Description,
        Icon = entity.Icon,
        IconColor = entity.IconColor,
        BackgroundColor = entity.BackgroundColor,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
        CreatedByName = entity.CreatedByUser?.Name,
        UpdatedByName = entity.UpdatedByUser?.Name,
    };
}
