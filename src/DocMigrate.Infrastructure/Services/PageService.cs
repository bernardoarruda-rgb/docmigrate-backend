using DocMigrate.Application.DTOs.Page;
using DocMigrate.Application.Interfaces;
using DocMigrate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Page = DocMigrate.Domain.Entities.Page;

namespace DocMigrate.Infrastructure.Services;

public class PageService(AppDbContext context) : IPageService
{
    public async Task<List<PageListItem>> GetAllAsync(int spaceId)
    {
        return await context.Pages
            .Where(p => p.SpaceId == spaceId && p.DeletedAt == null)
            .OrderBy(p => p.SortOrder)
            .Select(p => new PageListItem
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                SortOrder = p.SortOrder,
                SpaceId = p.SpaceId,
                CreatedAt = p.CreatedAt,
            })
            .ToListAsync();
    }

    public async Task<PageResponse> GetByIdAsync(int id)
    {
        var entity = await context.Pages
            .Where(p => p.DeletedAt == null)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new KeyNotFoundException("Pagina nao encontrada");

        return MapToResponse(entity);
    }

    public async Task<PageResponse> CreateAsync(CreatePageRequest request)
    {
        var spaceExists = await context.Spaces
            .Where(s => s.DeletedAt == null)
            .AnyAsync(s => s.Id == request.SpaceId);

        if (!spaceExists)
            throw new KeyNotFoundException("Espaco nao encontrado");

        var entity = new Page
        {
            Title = request.Title,
            Description = request.Description,
            Content = request.Content,
            SortOrder = request.SortOrder,
            SpaceId = request.SpaceId,
        };

        context.Pages.Add(entity);
        await context.SaveChangesAsync();

        return MapToResponse(entity);
    }

    public async Task<PageResponse> UpdateAsync(int id, UpdatePageRequest request)
    {
        var entity = await context.Pages
            .Where(p => p.DeletedAt == null)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new KeyNotFoundException("Pagina nao encontrada");

        entity.Title = request.Title;
        entity.Description = request.Description;
        entity.Content = request.Content;
        entity.SortOrder = request.SortOrder;

        await context.SaveChangesAsync();

        return MapToResponse(entity);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await context.Pages
            .Where(p => p.DeletedAt == null)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new KeyNotFoundException("Pagina nao encontrada");

        entity.DeletedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    private static PageResponse MapToResponse(Page entity) => new()
    {
        Id = entity.Id,
        Title = entity.Title,
        Description = entity.Description,
        Content = entity.Content,
        SortOrder = entity.SortOrder,
        SpaceId = entity.SpaceId,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
    };
}
