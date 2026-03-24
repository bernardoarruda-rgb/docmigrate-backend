namespace DocMigrate.Application.Interfaces;

using DocMigrate.Application.DTOs.Favorite;

public interface IUserActivityService
{
    // Favorites
    Task<List<FavoritePageItem>> GetFavoritesAsync(int userId);
    Task<bool> IsFavoriteAsync(int userId, int pageId);
    Task ToggleFavoriteAsync(int userId, int pageId);

    // Recent pages
    Task<List<RecentPageItem>> GetRecentPagesAsync(int userId, int limit = 10);
    Task RecordVisitAsync(int userId, int pageId);
}
