using DocMigrate.Application.DTOs.Tag;
using DocMigrate.Application.Interfaces;
using DocMigrate.Domain.Entities;
using DocMigrate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DocMigrate.Infrastructure.Services;

public class TagService(AppDbContext context) : ITagService
{
    public async Task<List<TagListItem>> GetAllAsync()
    {
        return await context.Tags
            .AsNoTracking()
            .Where(t => t.DeletedAt == null)
            .OrderBy(t => t.Name)
            .Select(t => new TagListItem
            {
                Id = t.Id,
                Name = t.Name,
                Color = t.Color,
            })
            .ToListAsync();
    }

    public async Task<TagResponse> GetByIdAsync(int id)
    {
        var entity = await context.Tags
            .AsNoTracking()
            .Where(t => t.DeletedAt == null)
            .FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new KeyNotFoundException("Tag nao encontrada");

        return MapToResponse(entity);
    }

    public async Task<TagResponse> CreateAsync(CreateTagRequest request)
    {
        var nameExists = await context.Tags
            .Where(t => t.DeletedAt == null)
            .AnyAsync(t => EF.Functions.ILike(t.Name, request.Name));

        if (nameExists)
            throw new InvalidOperationException("Ja existe uma tag com este nome.");

        var entity = new Tag
        {
            Name = request.Name,
            Color = request.Color,
        };

        context.Tags.Add(entity);
        await context.SaveChangesAsync();

        return MapToResponse(entity);
    }

    public async Task<TagResponse> UpdateAsync(int id, UpdateTagRequest request)
    {
        var entity = await context.Tags
            .Where(t => t.DeletedAt == null)
            .FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new KeyNotFoundException("Tag nao encontrada");

        var nameExists = await context.Tags
            .Where(t => t.DeletedAt == null && t.Id != id)
            .AnyAsync(t => EF.Functions.ILike(t.Name, request.Name));

        if (nameExists)
            throw new InvalidOperationException("Ja existe uma tag com este nome.");

        entity.Name = request.Name;
        entity.Color = request.Color;

        await context.SaveChangesAsync();

        return MapToResponse(entity);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await context.Tags
            .Where(t => t.DeletedAt == null)
            .Include(t => t.Pages.Where(p => p.DeletedAt == null))
            .Include(t => t.Spaces.Where(s => s.DeletedAt == null))
            .FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new KeyNotFoundException("Tag nao encontrada");

        var usageCount = entity.Pages.Count + entity.Spaces.Count;
        if (usageCount > 0)
            throw new InvalidOperationException(
                $"Nao e possivel excluir esta tag. Existem {usageCount} item(ns) vinculado(s). Remova-os primeiro.");

        entity.DeletedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    public async Task<List<TagListItem>> SearchAsync(string query)
    {
        var pattern = $"%{query}%";
        return await context.Tags
            .AsNoTracking()
            .Where(t => t.DeletedAt == null)
            .Where(t => EF.Functions.ILike(t.Name, pattern))
            .OrderBy(t => t.Name)
            .Take(10)
            .Select(t => new TagListItem
            {
                Id = t.Id,
                Name = t.Name,
                Color = t.Color,
            })
            .ToListAsync();
    }

    private static TagResponse MapToResponse(Tag entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Color = entity.Color,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
    };
}
