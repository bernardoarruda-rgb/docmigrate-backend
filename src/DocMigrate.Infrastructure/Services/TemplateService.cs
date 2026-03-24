using DocMigrate.Application.DTOs.Template;
using DocMigrate.Application.Interfaces;
using DocMigrate.Domain.Entities;
using DocMigrate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DocMigrate.Infrastructure.Services;

public class TemplateService(AppDbContext context) : ITemplateService
{
    public async Task<List<TemplateListItem>> GetAllAsync()
    {
        return await context.Templates
            .AsNoTracking()
            .Where(t => t.DeletedAt == null)
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Title)
            .Select(t => new TemplateListItem
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                Icon = t.Icon,
                IsDefault = t.IsDefault,
                SortOrder = t.SortOrder,
            })
            .ToListAsync();
    }

    public async Task<TemplateResponse> GetByIdAsync(int id)
    {
        var entity = await context.Templates
            .AsNoTracking()
            .Include(t => t.CreatedByUser)
            .Include(t => t.UpdatedByUser)
            .Where(t => t.DeletedAt == null)
            .FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new KeyNotFoundException("Template nao encontrado");

        return MapToResponse(entity);
    }

    public async Task<TemplateResponse> CreateAsync(CreateTemplateRequest request, int? userId = null)
    {
        var entity = new Template
        {
            Title = request.Title,
            Description = request.Description,
            Icon = request.Icon,
            Content = request.Content,
            SortOrder = request.SortOrder,
        };

        entity.CreatedByUserId = userId;
        entity.UpdatedByUserId = userId;

        context.Templates.Add(entity);
        await context.SaveChangesAsync();

        await context.Entry(entity).Reference(e => e.CreatedByUser).LoadAsync();
        await context.Entry(entity).Reference(e => e.UpdatedByUser).LoadAsync();

        return MapToResponse(entity);
    }

    public async Task<TemplateResponse> UpdateAsync(int id, UpdateTemplateRequest request, int? userId = null)
    {
        var entity = await context.Templates
            .Include(t => t.CreatedByUser)
            .Include(t => t.UpdatedByUser)
            .Where(t => t.DeletedAt == null)
            .FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new KeyNotFoundException("Template nao encontrado");

        entity.Title = request.Title;
        entity.Description = request.Description;
        entity.Icon = request.Icon;
        if (request.Content is not null)
            entity.Content = request.Content;
        entity.SortOrder = request.SortOrder;
        entity.UpdatedByUserId = userId;

        await context.SaveChangesAsync();

        await context.Entry(entity).Reference(e => e.UpdatedByUser).LoadAsync();

        return MapToResponse(entity);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await context.Templates
            .Where(t => t.DeletedAt == null)
            .FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new KeyNotFoundException("Template nao encontrado");

        entity.DeletedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    private static TemplateResponse MapToResponse(Template entity) => new()
    {
        Id = entity.Id,
        Title = entity.Title,
        Description = entity.Description,
        Icon = entity.Icon,
        Content = entity.Content,
        IsDefault = entity.IsDefault,
        SortOrder = entity.SortOrder,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
        CreatedByName = entity.CreatedByUser?.Name,
        UpdatedByName = entity.UpdatedByUser?.Name,
    };
}
