using DocMigrate.Application.DTOs.Search;
using DocMigrate.Application.DTOs.Tag;
using DocMigrate.Application.Interfaces;
using DocMigrate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DocMigrate.Infrastructure.Services;

public class SearchService(AppDbContext context) : ISearchService
{
    private const int MaxLimit = 100;

    public async Task<SearchResponse> SearchAsync(string query, string? type = null, int? spaceId = null,
        List<int>? tagIds = null, int limit = 20, int offset = 0, string? language = null)
    {
        limit = Math.Clamp(limit, 1, MaxLimit);
        offset = Math.Max(offset, 0);

        var results = new List<SearchResultDto>();
        var totalCount = 0;

        if (type is null or "space")
        {
            var (spaceResults, spaceCount) = await SearchSpacesAsync(query, spaceId, tagIds, limit, offset);
            results.AddRange(spaceResults);
            totalCount += spaceCount;
        }

        if (type is null or "page")
        {
            var (pageResults, pageCount) = await SearchPagesAsync(query, spaceId, tagIds, limit, offset);
            results.AddRange(pageResults);
            totalCount += pageCount;
        }

        if (!string.IsNullOrEmpty(language))
        {
            var translationResults = await SearchTranslationsAsync(query, language, limit);
            results.AddRange(translationResults);
            totalCount += translationResults.Count;
        }

        results = results.OrderByDescending(r => r.Rank).ToList();

        await LoadTagsAsync(results);

        return new SearchResponse
        {
            Items = results.Take(limit).ToList().AsReadOnly(),
            TotalCount = totalCount,
            Limit = limit,
            Offset = offset,
        };
    }

    private async Task<(List<SearchResultDto> Results, int Count)> SearchSpacesAsync(
        string query, int? spaceId, List<int>? tagIds, int limit, int offset)
    {
        var ftsResults = await SearchSpacesFtsAsync(query, spaceId, tagIds, limit, offset);
        if (ftsResults.Results.Count > 0)
            return ftsResults;

        return await SearchSpacesIlikeAsync(query, spaceId, tagIds, limit, offset);
    }

    private async Task<(List<SearchResultDto> Results, int Count)> SearchPagesAsync(
        string query, int? spaceId, List<int>? tagIds, int limit, int offset)
    {
        var ftsResults = await SearchPagesFtsAsync(query, spaceId, tagIds, limit, offset);
        if (ftsResults.Results.Count > 0)
            return ftsResults;

        return await SearchPagesIlikeAsync(query, spaceId, tagIds, limit, offset);
    }

    private async Task<(List<SearchResultDto> Results, int Count)> SearchSpacesFtsAsync(
        string query, int? spaceId, List<int>? tagIds, int limit, int offset)
    {
        var baseQuery = context.Spaces
            .AsNoTracking()
            .Where(s => s.DeletedAt == null)
            .Where(s => s.SearchVector!.Matches(EF.Functions.PlainToTsQuery("portuguese", query)));

        if (spaceId.HasValue)
            baseQuery = baseQuery.Where(s => s.Id == spaceId.Value);

        if (tagIds is { Count: > 0 })
            baseQuery = baseQuery.Where(s => s.Tags.Any(t => tagIds.Contains(t.Id)));

        var count = await baseQuery.CountAsync();

        var results = await baseQuery
            .Select(s => new SearchResultDto
            {
                Id = s.Id,
                Title = s.Title,
                Description = s.Description,
                Type = "space",
                SpaceId = null,
                SpaceTitle = null,
                Icon = s.Icon,
                IconColor = s.IconColor,
                Rank = s.SearchVector!.Rank(EF.Functions.PlainToTsQuery("portuguese", query)),
            })
            .OrderByDescending(r => r.Rank)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

        return (results, count);
    }

    private async Task<(List<SearchResultDto> Results, int Count)> SearchPagesFtsAsync(
        string query, int? spaceId, List<int>? tagIds, int limit, int offset)
    {
        var baseQuery = context.Pages
            .AsNoTracking()
            .Where(p => p.DeletedAt == null)
            .Where(p => p.Space.DeletedAt == null)
            .Where(p => p.SearchVector!.Matches(EF.Functions.PlainToTsQuery("portuguese", query)));

        if (spaceId.HasValue)
            baseQuery = baseQuery.Where(p => p.SpaceId == spaceId.Value);

        if (tagIds is { Count: > 0 })
            baseQuery = baseQuery.Where(p => p.Tags.Any(t => tagIds.Contains(t.Id)));

        var count = await baseQuery.CountAsync();

        var results = await baseQuery
            .Select(p => new SearchResultDto
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                Type = "page",
                SpaceId = p.SpaceId,
                SpaceTitle = p.Space.Title,
                Icon = p.Icon,
                IconColor = p.IconColor,
                Rank = p.SearchVector!.Rank(EF.Functions.PlainToTsQuery("portuguese", query)),
            })
            .OrderByDescending(r => r.Rank)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

        return (results, count);
    }

    private async Task<(List<SearchResultDto> Results, int Count)> SearchSpacesIlikeAsync(
        string query, int? spaceId, List<int>? tagIds, int limit, int offset)
    {
        var pattern = $"%{query}%";

        var baseQuery = context.Spaces
            .AsNoTracking()
            .Where(s => s.DeletedAt == null)
            .Where(s =>
                EF.Functions.ILike(s.Title, pattern) ||
                (s.Description != null && EF.Functions.ILike(s.Description, pattern)));

        if (spaceId.HasValue)
            baseQuery = baseQuery.Where(s => s.Id == spaceId.Value);

        if (tagIds is { Count: > 0 })
            baseQuery = baseQuery.Where(s => s.Tags.Any(t => tagIds.Contains(t.Id)));

        var count = await baseQuery.CountAsync();

        var results = await baseQuery
            .OrderBy(s => EF.Functions.ILike(s.Title, pattern) ? 0 : 1)
            .ThenBy(s => s.Title)
            .Skip(offset)
            .Take(limit)
            .Select(s => new SearchResultDto
            {
                Id = s.Id,
                Title = s.Title,
                Description = s.Description,
                Type = "space",
                SpaceId = null,
                SpaceTitle = null,
                Icon = s.Icon,
                IconColor = s.IconColor,
                Rank = 0,
            })
            .ToListAsync();

        return (results, count);
    }

    private async Task<(List<SearchResultDto> Results, int Count)> SearchPagesIlikeAsync(
        string query, int? spaceId, List<int>? tagIds, int limit, int offset)
    {
        var pattern = $"%{query}%";

        var baseQuery = context.Pages
            .AsNoTracking()
            .Where(p => p.DeletedAt == null)
            .Where(p => p.Space.DeletedAt == null)
            .Where(p =>
                EF.Functions.ILike(p.Title, pattern) ||
                (p.Description != null && EF.Functions.ILike(p.Description, pattern)) ||
                (p.PlainText != null && EF.Functions.ILike(p.PlainText, pattern)));

        if (spaceId.HasValue)
            baseQuery = baseQuery.Where(p => p.SpaceId == spaceId.Value);

        if (tagIds is { Count: > 0 })
            baseQuery = baseQuery.Where(p => p.Tags.Any(t => tagIds.Contains(t.Id)));

        var count = await baseQuery.CountAsync();

        var results = await baseQuery
            .OrderBy(p => EF.Functions.ILike(p.Title, pattern) ? 0 : 1)
            .ThenBy(p => p.Title)
            .Skip(offset)
            .Take(limit)
            .Select(p => new SearchResultDto
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                Type = "page",
                SpaceId = p.SpaceId,
                SpaceTitle = p.Space.Title,
                Icon = p.Icon,
                IconColor = p.IconColor,
                Rank = 0,
            })
            .ToListAsync();

        return (results, count);
    }

    private async Task<List<SearchResultDto>> SearchTranslationsAsync(string query, string language, int limit)
    {
        var pattern = $"%{query}%";

        return await context.PageTranslations
            .AsNoTracking()
            .Where(t => t.DeletedAt == null)
            .Where(t => t.Language == language)
            .Where(t => t.Page.DeletedAt == null)
            .Where(t =>
                EF.Functions.ILike(t.Title, pattern) ||
                (t.Description != null && EF.Functions.ILike(t.Description, pattern)) ||
                (t.PlainText != null && EF.Functions.ILike(t.PlainText, pattern)))
            .Select(t => new SearchResultDto
            {
                Id = t.PageId,
                Title = t.Title,
                Description = t.Description,
                Type = "page",
                SpaceId = t.Page.SpaceId,
                SpaceTitle = t.Page.Space.Title,
                Icon = t.Page.Icon,
                IconColor = t.Page.IconColor,
                Language = t.Language,
                Rank = 0,
            })
            .Take(limit)
            .ToListAsync();
    }

    private async Task LoadTagsAsync(List<SearchResultDto> results)
    {
        var pageIds = results.Where(r => r.Type == "page").Select(r => r.Id).ToList();
        var spaceIds = results.Where(r => r.Type == "space").Select(r => r.Id).ToList();

        var pageTags = pageIds.Count > 0
            ? await context.Pages
                .AsNoTracking()
                .Where(p => pageIds.Contains(p.Id))
                .SelectMany(p => p.Tags.Where(t => t.DeletedAt == null).Select(t => new { PageId = p.Id, Tag = new TagListItem { Id = t.Id, Name = t.Name, Color = t.Color } }))
                .ToListAsync()
            : [];

        var spaceTags = spaceIds.Count > 0
            ? await context.Spaces
                .AsNoTracking()
                .Where(s => spaceIds.Contains(s.Id))
                .SelectMany(s => s.Tags.Where(t => t.DeletedAt == null).Select(t => new { SpaceId = s.Id, Tag = new TagListItem { Id = t.Id, Name = t.Name, Color = t.Color } }))
                .ToListAsync()
            : [];

        foreach (var result in results)
        {
            if (result.Type == "page")
                result.Tags.AddRange(pageTags.Where(pt => pt.PageId == result.Id).Select(pt => pt.Tag));
            else
                result.Tags.AddRange(spaceTags.Where(st => st.SpaceId == result.Id).Select(st => st.Tag));
        }
    }
}
