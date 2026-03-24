namespace DocMigrate.Infrastructure.Services;

using DocMigrate.Application.DTOs.Favorite;
using DocMigrate.Application.Interfaces;
using DocMigrate.Domain.Entities;
using DocMigrate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class UserActivityService(AppDbContext context) : IUserActivityService
{
    private const int MaxRecentPages = 50;

    public async Task<List<FavoritePageItem>> GetFavoritesAsync(int userId)
    {
        return await context.PageFavorites
            .AsNoTracking()
            .Include(f => f.Page)
                .ThenInclude(p => p.Space)
            .Where(f => f.UserId == userId && f.Page.DeletedAt == null)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new FavoritePageItem
            {
                PageId = f.PageId,
                Title = f.Page.Title,
                Description = f.Page.Description,
                SpaceId = f.Page.SpaceId,
                SpaceTitle = f.Page.Space.Title,
                FavoritedAt = f.CreatedAt,
            })
            .ToListAsync();
    }

    public async Task<bool> IsFavoriteAsync(int userId, int pageId)
    {
        return await context.PageFavorites
            .AsNoTracking()
            .AnyAsync(f => f.UserId == userId && f.PageId == pageId);
    }

    public async Task ToggleFavoriteAsync(int userId, int pageId)
    {
        var existing = await context.PageFavorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.PageId == pageId);

        if (existing != null)
        {
            context.PageFavorites.Remove(existing);
        }
        else
        {
            var pageExists = await context.Pages.AnyAsync(p => p.Id == pageId && p.DeletedAt == null);
            if (!pageExists) throw new KeyNotFoundException("Pagina nao encontrada");

            context.PageFavorites.Add(new PageFavorite
            {
                UserId = userId,
                PageId = pageId,
                CreatedAt = DateTime.UtcNow,
            });
        }

        await context.SaveChangesAsync();
    }

    public async Task<List<RecentPageItem>> GetRecentPagesAsync(int userId, int limit = 10)
    {
        var latestVisitIds = context.PageVisits
            .Where(v => v.UserId == userId)
            .GroupBy(v => v.PageId)
            .Select(g => g.Max(v => v.Id));

        return await context.PageVisits
            .AsNoTracking()
            .Include(v => v.Page)
                .ThenInclude(p => p.Space)
            .Where(v => latestVisitIds.Contains(v.Id) && v.Page.DeletedAt == null)
            .OrderByDescending(v => v.VisitedAt)
            .Take(limit)
            .Select(v => new RecentPageItem
            {
                PageId = v.PageId,
                Title = v.Page.Title,
                Description = v.Page.Description,
                SpaceId = v.Page.SpaceId,
                SpaceTitle = v.Page.Space.Title,
                VisitedAt = v.VisitedAt,
            })
            .ToListAsync();
    }

    public async Task RecordVisitAsync(int userId, int pageId)
    {
        context.PageVisits.Add(new PageVisit
        {
            UserId = userId,
            PageId = pageId,
            VisitedAt = DateTime.UtcNow,
        });

        var oldVisits = await context.PageVisits
            .Where(v => v.UserId == userId)
            .OrderByDescending(v => v.VisitedAt)
            .Skip(MaxRecentPages)
            .ToListAsync();

        if (oldVisits.Count > 0)
        {
            context.PageVisits.RemoveRange(oldVisits);
        }

        await context.SaveChangesAsync();
    }
}
