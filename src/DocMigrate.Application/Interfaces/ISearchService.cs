using DocMigrate.Application.DTOs.Search;

namespace DocMigrate.Application.Interfaces;

public interface ISearchService
{
    Task<SearchResponse> SearchAsync(string query, string? type = null, int? spaceId = null,
        List<int>? tagIds = null, int limit = 20, int offset = 0, string? language = null);
}
