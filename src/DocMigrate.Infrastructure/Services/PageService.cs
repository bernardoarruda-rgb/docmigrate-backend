using DocMigrate.Application.DTOs.Common;
using DocMigrate.Application.DTOs.Page;
using DocMigrate.Application.Interfaces;
using DocMigrate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Page = DocMigrate.Domain.Entities.Page;

namespace DocMigrate.Infrastructure.Services;

public class PageService(
    AppDbContext context,
    IPlainTextExtractor plainTextExtractor,
    IFileService fileService,
    IPageTranslationService pageTranslationService,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<PageService> logger) : IPageService
{
    private const int LockTimeoutMinutes = 30;
    public async Task<List<PageListItem>> GetAllAsync(int spaceId)
    {
        return await context.Pages
            .AsNoTracking()
            .Where(p => p.SpaceId == spaceId && p.DeletedAt == null)
            .OrderBy(p => p.SortOrder)
            .Select(p => new PageListItem
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                SortOrder = p.SortOrder,
                SpaceId = p.SpaceId,
                FolderId = p.FolderId,
                Icon = p.Icon,
                IconColor = p.IconColor,
                BackgroundColor = p.BackgroundColor,
                CreatedAt = p.CreatedAt,
                Language = p.Language,
                CoverType = p.CoverType,
                CoverValue = p.CoverValue,
            })
            .ToListAsync();
    }

    public async Task<PaginatedResult<PageListItem>> GetAllAsync(int spaceId, int page, int pageSize, int? folderId = null)
    {
        var query = context.Pages
            .AsNoTracking()
            .Where(p => p.SpaceId == spaceId && p.DeletedAt == null);

        if (folderId.HasValue)
            query = query.Where(p => p.FolderId == folderId.Value);

        query = query.OrderBy(p => p.SortOrder);

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PageListItem
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                SortOrder = p.SortOrder,
                SpaceId = p.SpaceId,
                FolderId = p.FolderId,
                Icon = p.Icon,
                IconColor = p.IconColor,
                BackgroundColor = p.BackgroundColor,
                CreatedAt = p.CreatedAt,
                Language = p.Language,
                CoverType = p.CoverType,
                CoverValue = p.CoverValue,
            })
            .ToListAsync();

        return new PaginatedResult<PageListItem>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<PageResponse> GetByIdAsync(int id)
    {
        var entity = await context.Pages
            .AsNoTracking()
            .Include(p => p.CreatedByUser)
            .Include(p => p.UpdatedByUser)
            .Where(p => p.DeletedAt == null)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new KeyNotFoundException("Pagina nao encontrada");

        var response = MapToResponse(entity);
        if (entity.FolderId.HasValue)
            response.FolderBreadcrumbs = await GetFolderBreadcrumbsAsync(entity.FolderId.Value);
        return response;
    }

    public async Task<PageResponse> CreateAsync(CreatePageRequest request, int? userId = null)
    {
        var spaceExists = await context.Spaces
            .Where(s => s.DeletedAt == null)
            .AnyAsync(s => s.Id == request.SpaceId);

        if (!spaceExists)
            throw new KeyNotFoundException("Espaco nao encontrado");

        if (request.FolderId.HasValue)
        {
            var folder = await context.Folders
                .Where(f => f.DeletedAt == null && f.Id == request.FolderId.Value)
                .Select(f => new { f.SpaceId })
                .FirstOrDefaultAsync()
                ?? throw new KeyNotFoundException("Pasta nao encontrada ou foi desativada.");

            if (folder.SpaceId != request.SpaceId)
                throw new InvalidOperationException("Pasta deve pertencer ao mesmo espaco.");
        }

        var entity = new Page
        {
            Title = request.Title,
            Description = request.Description,
            Content = request.Content,
            SortOrder = request.SortOrder,
            SpaceId = request.SpaceId,
            FolderId = request.FolderId,
            Icon = request.Icon,
            IconColor = request.IconColor,
            BackgroundColor = request.BackgroundColor,
            Language = request.Language ?? "pt-BR",
            CoverType = request.CoverType,
            CoverValue = request.CoverValue,
            CoverPosition = request.CoverPosition ?? 50,
            CoverAttribution = request.CoverAttribution,
            ContentWidth = request.ContentWidth ?? "normal",
        };

        entity.PlainText = plainTextExtractor.Extract(entity.Content);

        entity.CreatedByUserId = userId;
        entity.UpdatedByUserId = userId;

        context.Pages.Add(entity);
        await context.SaveChangesAsync();

        await context.Entry(entity).Reference(e => e.CreatedByUser).LoadAsync();
        await context.Entry(entity).Reference(e => e.UpdatedByUser).LoadAsync();

        return MapToResponse(entity);
    }

    public async Task<PageResponse> UpdateAsync(int id, UpdatePageRequest request, int? userId = null)
    {
        var entity = await context.Pages
            .Include(p => p.CreatedByUser)
            .Include(p => p.UpdatedByUser)
            .Where(p => p.DeletedAt == null)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new KeyNotFoundException("Pagina nao encontrada");

        // Handle folder change (move page)
        if (request.FolderId != entity.FolderId)
        {
            if (request.FolderId.HasValue)
            {
                var newFolder = await context.Folders
                    .Where(f => f.DeletedAt == null && f.Id == request.FolderId.Value)
                    .Select(f => new { f.SpaceId })
                    .FirstOrDefaultAsync()
                    ?? throw new KeyNotFoundException("Pasta nao encontrada ou foi desativada.");

                if (newFolder.SpaceId != entity.SpaceId)
                    throw new InvalidOperationException("Pasta deve pertencer ao mesmo espaco.");
            }

            entity.FolderId = request.FolderId;
        }

        entity.Title = request.Title;
        entity.Description = request.Description;
        if (request.Content is not null)
        {
            entity.Content = request.Content;
            entity.PlainText = plainTextExtractor.Extract(entity.Content);
        }
        entity.SortOrder = request.SortOrder;
        entity.Icon = request.Icon;
        entity.IconColor = request.IconColor;
        entity.BackgroundColor = request.BackgroundColor;
        entity.CoverType = request.CoverType;
        entity.CoverValue = request.CoverValue;
        if (request.CoverPosition.HasValue) entity.CoverPosition = request.CoverPosition.Value;
        entity.CoverAttribution = request.CoverAttribution;
        if (request.ContentWidth is not null) entity.ContentWidth = request.ContentWidth;
        if (request.Language is not null) entity.Language = request.Language;
        entity.UpdatedByUserId = userId;

        await context.SaveChangesAsync();

        await pageTranslationService.MarkOutdatedAsync(id);

        await context.Entry(entity).Reference(e => e.UpdatedByUser).LoadAsync();

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

        await pageTranslationService.SoftDeleteByPageAsync(id);
    }

    public async Task ReorderAsync(int spaceId, ReorderPagesRequest request)
    {
        var spaceExists = await context.Spaces
            .Where(s => s.DeletedAt == null)
            .AnyAsync(s => s.Id == spaceId);

        if (!spaceExists)
            throw new KeyNotFoundException("Espaco nao encontrado");

        var pageIds = request.Items.Select(i => i.PageId).ToList();

        var pages = await context.Pages
            .Where(p => p.SpaceId == spaceId && p.DeletedAt == null && pageIds.Contains(p.Id))
            .ToListAsync();

        if (pages.Count != pageIds.Count)
            throw new ArgumentException("Uma ou mais paginas nao pertencem a este espaco ou nao existem");

        // Validate all pages share the same folder (reorder only within same folder)
        var folderIds = pages.Select(p => p.FolderId).Distinct().ToList();
        if (folderIds.Count > 1)
            throw new ArgumentException("Todas as paginas devem pertencer a mesma pasta para reordenar.");

        foreach (var item in request.Items)
        {
            var page = pages.First(p => p.Id == item.PageId);
            page.SortOrder = item.SortOrder;
        }

        await context.SaveChangesAsync();
    }

    public async Task<bool> AcquireLockAsync(int pageId, string userId)
    {
        var entity = await context.Pages
            .Where(p => p.DeletedAt == null)
            .FirstOrDefaultAsync(p => p.Id == pageId)
            ?? throw new KeyNotFoundException("Pagina nao encontrada");

        var lockExpiry = DateTime.UtcNow.AddMinutes(-LockTimeoutMinutes);
        var isAvailable = entity.LockedBy == null
            || entity.LockedBy == userId
            || entity.LockedAt < lockExpiry;

        if (!isAvailable)
            return false;

        entity.LockedBy = userId;
        entity.LockedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ReleaseLockAsync(int pageId, string userId)
    {
        var entity = await context.Pages
            .Where(p => p.DeletedAt == null)
            .FirstOrDefaultAsync(p => p.Id == pageId)
            ?? throw new KeyNotFoundException("Pagina nao encontrada");

        if (entity.LockedBy != userId)
            return false;

        // Create a version snapshot when releasing the lock (user finished editing)
        if (!string.IsNullOrEmpty(entity.Content))
        {
            var lastVersion = await context.PageVersions
                .Where(v => v.PageId == pageId && v.DeletedAt == null)
                .MaxAsync(v => (int?)v.VersionNumber) ?? 0;

            int.TryParse(userId, out var userIdInt);

            var version = new Domain.Entities.PageVersion
            {
                PageId = pageId,
                VersionNumber = lastVersion + 1,
                Content = entity.Content,
                PlainText = entity.PlainText ?? plainTextExtractor.Extract(entity.Content),
                ChangeDescription = null,
                CreatedByUserId = userIdInt > 0 ? userIdInt : null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            context.PageVersions.Add(version);
        }

        entity.LockedBy = null;
        entity.LockedAt = null;
        await context.SaveChangesAsync();

        // Mark existing translations as outdated (no auto-translate — user triggers manually)
        _ = Task.Run(async () =>
        {
            using var scope = serviceScopeFactory.CreateScope();
            var translationService = scope.ServiceProvider.GetRequiredService<IPageTranslationService>();
            try { await translationService.MarkOutdatedAsync(pageId); }
            catch (Exception ex) { logger.LogError(ex, "MarkOutdated failed for page {PageId}", pageId); }
        });

        return true;
    }

    public async Task AutosaveContentAsync(int pageId, string lockUserId, string content, int? userId = null)
    {
        var entity = await context.Pages
            .Where(p => p.DeletedAt == null)
            .FirstOrDefaultAsync(p => p.Id == pageId)
            ?? throw new KeyNotFoundException("Pagina nao encontrada");

        if (entity.LockedBy != lockUserId)
            throw new InvalidOperationException("Voce nao possui o lock desta pagina.");

        entity.Content = content;
        entity.PlainText = plainTextExtractor.Extract(content);
        entity.LockedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedByUserId = userId;
        await context.SaveChangesAsync();
    }

    public async Task SetTagsAsync(int pageId, List<int> tagIds)
    {
        var entity = await context.Pages
            .Include(p => p.Tags)
            .Where(p => p.DeletedAt == null)
            .FirstOrDefaultAsync(p => p.Id == pageId)
            ?? throw new KeyNotFoundException("Pagina nao encontrada");

        var tags = await context.Tags
            .Where(t => t.DeletedAt == null && tagIds.Contains(t.Id))
            .ToListAsync();

        entity.Tags.Clear();
        foreach (var tag in tags)
            entity.Tags.Add(tag);

        await context.SaveChangesAsync();
    }

    public async Task UpdateCoverAsync(int id, UpdatePageCoverRequest request)
    {
        var entity = await context.Pages
            .Where(p => p.DeletedAt == null)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new KeyNotFoundException("Pagina nao encontrada");

        entity.CoverType = request.CoverType;
        entity.CoverValue = request.CoverValue;
        if (request.CoverPosition.HasValue) entity.CoverPosition = request.CoverPosition.Value;
        entity.CoverAttribution = request.CoverAttribution;
        entity.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
    }

    public async Task UpdateWidthAsync(int id, UpdatePageWidthRequest request)
    {
        var entity = await context.Pages
            .Where(p => p.DeletedAt == null)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new KeyNotFoundException("Pagina nao encontrada");

        entity.ContentWidth = request.ContentWidth;
        entity.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
    }

    public async Task<string> UploadCoverAsync(int id, Stream fileStream, string fileName, string contentType)
    {
        var entity = await context.Pages
            .Where(p => p.DeletedAt == null)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new KeyNotFoundException("Pagina nao encontrada");

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var objectName = $"covers/page-{id}-{Guid.NewGuid()}{ext}";
        var url = await fileService.UploadAsync(fileStream, objectName, contentType);

        entity.CoverType = "image";
        entity.CoverValue = url;
        entity.CoverAttribution = null;
        entity.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return url;
    }

    private async Task<List<BreadcrumbItem>> GetFolderBreadcrumbsAsync(int folderId)
    {
        var breadcrumbs = new List<BreadcrumbItem>();
        var currentId = (int?)folderId;
        var visited = new HashSet<int>();

        while (currentId.HasValue)
        {
            if (!visited.Add(currentId.Value)) break;

            var folder = await context.Folders
                .AsNoTracking()
                .Where(f => f.Id == currentId.Value && f.DeletedAt == null)
                .Select(f => new { f.Id, f.Title, f.ParentFolderId })
                .FirstOrDefaultAsync();

            if (folder == null) break;

            breadcrumbs.Insert(0, new BreadcrumbItem { Id = folder.Id, Title = folder.Title });
            currentId = folder.ParentFolderId;
        }

        return breadcrumbs;
    }

    private static PageResponse MapToResponse(Page entity) => new()
    {
        Id = entity.Id,
        Title = entity.Title,
        Description = entity.Description,
        Content = entity.Content,
        SortOrder = entity.SortOrder,
        SpaceId = entity.SpaceId,
        FolderId = entity.FolderId,
        Icon = entity.Icon,
        IconColor = entity.IconColor,
        BackgroundColor = entity.BackgroundColor,
        LockedBy = entity.LockedBy,
        LockedAt = entity.LockedAt,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
        CreatedByName = entity.CreatedByUser?.Name,
        UpdatedByName = entity.UpdatedByUser?.Name,
        Language = entity.Language,
        CoverType = entity.CoverType,
        CoverValue = entity.CoverValue,
        CoverPosition = entity.CoverPosition,
        CoverAttribution = entity.CoverAttribution,
        ContentWidth = entity.ContentWidth,
    };

    public async Task<List<HeadingDto>> GetHeadingsAsync(int pageId)
    {
        var entity = await context.Pages
            .AsNoTracking()
            .Where(p => p.DeletedAt == null)
            .FirstOrDefaultAsync(p => p.Id == pageId)
            ?? throw new KeyNotFoundException("Pagina nao encontrada");

        return ExtractHeadings(entity.Content);
    }

    private static List<HeadingDto> ExtractHeadings(string? tiptapJson)
    {
        if (string.IsNullOrWhiteSpace(tiptapJson))
            return [];

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(tiptapJson);
            var headings = new List<HeadingDto>();
            var slugCounts = new Dictionary<string, int>();
            WalkForHeadings(doc.RootElement, headings, slugCounts);
            return headings;
        }
        catch (System.Text.Json.JsonException)
        {
            return [];
        }
    }

    private static void WalkForHeadings(
        System.Text.Json.JsonElement node,
        List<HeadingDto> headings,
        Dictionary<string, int> slugCounts)
    {
        if (node.TryGetProperty("type", out var typeProp)
            && typeProp.GetString() == "heading"
            && node.TryGetProperty("attrs", out var attrs)
            && attrs.TryGetProperty("level", out var levelProp))
        {
            var text = ExtractTextFromNode(node);
            if (!string.IsNullOrWhiteSpace(text))
            {
                var baseSlug = Slugify(text);
                var slug = DeduplicateSlug(baseSlug, slugCounts);
                headings.Add(new HeadingDto
                {
                    Id = slug,
                    Text = text,
                    Level = levelProp.GetInt32(),
                });
            }
        }

        if (node.TryGetProperty("content", out var content)
            && content.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (var child in content.EnumerateArray())
            {
                WalkForHeadings(child, headings, slugCounts);
            }
        }
    }

    private static string ExtractTextFromNode(System.Text.Json.JsonElement node)
    {
        var sb = new System.Text.StringBuilder();
        ExtractTextRecursive(node, sb);
        return sb.ToString().Trim();
    }

    private static void ExtractTextRecursive(System.Text.Json.JsonElement node, System.Text.StringBuilder sb)
    {
        if (node.TryGetProperty("text", out var text)
            && text.ValueKind == System.Text.Json.JsonValueKind.String)
        {
            sb.Append(text.GetString());
        }

        if (node.TryGetProperty("content", out var content)
            && content.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (var child in content.EnumerateArray())
            {
                ExtractTextRecursive(child, sb);
            }
        }
    }

    private static string Slugify(string text)
    {
        var normalized = text
            .ToLowerInvariant()
            .Normalize(System.Text.NormalizationForm.FormD);

        var sb = new System.Text.StringBuilder();
        foreach (var c in normalized)
        {
            var category = char.GetUnicodeCategory(c);
            if (category != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                sb.Append(char.IsLetterOrDigit(c) ? c : '-');
            }
        }

        return sb.ToString().Trim('-');
    }

    private static string DeduplicateSlug(string baseSlug, Dictionary<string, int> slugCounts)
    {
        if (!slugCounts.TryGetValue(baseSlug, out var count))
        {
            slugCounts[baseSlug] = 1;
            return baseSlug;
        }

        slugCounts[baseSlug] = count + 1;
        return $"{baseSlug}-{count}";
    }
}
