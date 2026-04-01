using DocMigrate.Application.DTOs.Unsplash;

namespace DocMigrate.Application.Interfaces;

public interface IUnsplashService
{
    Task<UnsplashSearchResponse> SearchAsync(string query, int page = 1, int perPage = 12);
}
