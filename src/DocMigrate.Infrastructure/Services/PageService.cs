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
                ParentPageId = p.ParentPageId,
                Level = p.Level,
                HasChildren = p.ChildPages.Any(c => c.DeletedAt == null),
                Icon = p.Icon,
                IconColor = p.IconColor,
                BackgroundColor = p.BackgroundColor,
                CreatedAt = p.CreatedAt,
                Language = p.Language,
            })
            .ToListAsync();
    }

    public async Task<PaginatedResult<PageListItem>> GetAllAsync(int spaceId, int page, int pageSize)
    {
        var query = context.Pages
            .AsNoTracking()
            .Where(p => p.SpaceId == spaceId && p.DeletedAt == null)
            .OrderBy(p => p.SortOrder);

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
                ParentPageId = p.ParentPageId,
                Level = p.Level,
                HasChildren = p.ChildPages.Any(c => c.DeletedAt == null),
                Icon = p.Icon,
                IconColor = p.IconColor,
                BackgroundColor = p.BackgroundColor,
                CreatedAt = p.CreatedAt,
                Language = p.Language,
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
        response.Breadcrumbs = await GetBreadcrumbsAsync(id);
        return response;
    }

    public async Task<PageResponse> CreateAsync(CreatePageRequest request, int? userId = null)
    {
        var spaceExists = await context.Spaces
            .Where(s => s.DeletedAt == null)
            .AnyAsync(s => s.Id == request.SpaceId);

        if (!spaceExists)
            throw new KeyNotFoundException("Espaco nao encontrado");

        int level = 1;
        if (request.ParentPageId.HasValue)
        {
            var parent = await context.Pages
                .Where(p => p.DeletedAt == null && p.Id == request.ParentPageId.Value)
                .Select(p => new { p.SpaceId, p.Level })
                .FirstOrDefaultAsync()
                ?? throw new KeyNotFoundException("Pagina pai nao encontrada ou foi desativada.");

            if (parent.SpaceId != request.SpaceId)
                throw new InvalidOperationException("Pagina pai deve pertencer ao mesmo espaco.");

            if (parent.Level >= 5)
                throw new InvalidOperationException("Profundidade maxima (5 niveis) atingida.");

            level = parent.Level + 1;
        }

        var entity = new Page
        {
            Title = request.Title,
            Description = request.Description,
            Content = request.Content,
            SortOrder = request.SortOrder,
            SpaceId = request.SpaceId,
            ParentPageId = request.ParentPageId,
            Level = level,
            Icon = request.Icon,
            IconColor = request.IconColor,
            BackgroundColor = request.BackgroundColor,
            Language = request.Language ?? "pt-BR",
        };

        entity.PlainText = plainTextExtractor.Extract(entity.Content);

        entity.CreatedByUserId = userId;
        entity.UpdatedByUserId = userId;

        context.Pages.Add(entity);
        await context.SaveChangesAsync();

        await context.Entry(entity).Reference(e => e.CreatedByUser).LoadAsync();
        await context.Entry(entity).Reference(e => e.UpdatedByUser).LoadAsync();

        // Fire-and-forget: auto-translate new page
        _ = Task.Run(async () =>
        {
            using var scope = serviceScopeFactory.CreateScope();
            var translationService = scope.ServiceProvider.GetRequiredService<IPageTranslationService>();
            try { await translationService.AutoTranslateAsync(entity.Id, userId); }
            catch (Exception ex) { logger.LogError(ex, "Auto-translate failed for page {PageId}", entity.Id); }
        });

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

        // Handle parent change (move page)
        if (request.ParentPageId != entity.ParentPageId)
        {
            if (request.ParentPageId.HasValue)
            {
                if (request.ParentPageId.Value == id)
                    throw new InvalidOperationException("Uma pagina nao pode ser pai de si mesma.");

                var newParent = await context.Pages
                    .Where(p => p.DeletedAt == null && p.Id == request.ParentPageId.Value)
                    .Select(p => new { p.SpaceId, p.Level })
                    .FirstOrDefaultAsync()
                    ?? throw new KeyNotFoundException("Pagina pai nao encontrada ou foi desativada.");

                if (newParent.SpaceId != entity.SpaceId)
                    throw new InvalidOperationException("Pagina pai deve pertencer ao mesmo espaco.");

                if (newParent.Level >= 5)
                    throw new InvalidOperationException("Profundidade maxima (5 niveis) atingida.");

                // Cycle detection: ensure new parent is not a descendant
                if (await IsDescendantAsync(id, request.ParentPageId.Value))
                    throw new InvalidOperationException("Nao e possivel mover a pagina para um descendente (ciclo detectado).");

                entity.ParentPageId = request.ParentPageId;
                entity.Level = newParent.Level + 1;

                // Recalculate descendant levels
                await RecalculateDescendantLevelsAsync(entity.Id, entity.Level);
            }
            else
            {
                entity.ParentPageId = null;
                entity.Level = 1;
                await RecalculateDescendantLevelsAsync(entity.Id, 1);
            }
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
        if (request.Language is not null) entity.Language = request.Language;
        entity.UpdatedByUserId = userId;

        await context.SaveChangesAsync();

        await pageTranslationService.MarkOutdatedAsync(id);

        // Fire-and-forget: auto-translate after metadata update
        _ = Task.Run(async () =>
        {
            using var scope = serviceScopeFactory.CreateScope();
            var translationService = scope.ServiceProvider.GetRequiredService<IPageTranslationService>();
            try { await translationService.AutoTranslateAsync(id, userId); }
            catch (Exception ex) { logger.LogError(ex, "Auto-translate failed for page {PageId}", id); }
        });

        await context.Entry(entity).Reference(e => e.UpdatedByUser).LoadAsync();

        return MapToResponse(entity);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await context.Pages
            .Where(p => p.DeletedAt == null)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new KeyNotFoundException("Pagina nao encontrada");

        var now = DateTime.UtcNow;
        entity.DeletedAt = now;

        // Cascade soft-delete all descendants
        var descendantIds = await GetDescendantIdsAsync(id);
        if (descendantIds.Count > 0)
        {
            var descendants = await context.Pages
                .Where(p => descendantIds.Contains(p.Id) && p.DeletedAt == null)
                .ToListAsync();
            foreach (var desc in descendants)
                desc.DeletedAt = now;
        }

        await context.SaveChangesAsync();

        // Soft delete translations for this page and all descendants
        await pageTranslationService.SoftDeleteByPageAsync(id);
        foreach (var descId in descendantIds)
            await pageTranslationService.SoftDeleteByPageAsync(descId);
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

        // Validate all pages share the same parent (reorder only within siblings)
        var parentIds = pages.Select(p => p.ParentPageId).Distinct().ToList();
        if (parentIds.Count > 1)
            throw new ArgumentException("Todas as paginas devem pertencer ao mesmo pai para reordenar.");

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

        // Fire-and-forget: auto-translate after editing session
        _ = Task.Run(async () =>
        {
            using var scope = serviceScopeFactory.CreateScope();
            var translationService = scope.ServiceProvider.GetRequiredService<IPageTranslationService>();
            try { await translationService.AutoTranslateAsync(pageId); }
            catch (Exception ex) { logger.LogError(ex, "Auto-translate failed for page {PageId}", pageId); }
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

    private async Task<bool> IsDescendantAsync(int ancestorId, int potentialDescendantId)
    {
        var ancestorChain = await context.Pages
            .AsNoTracking()
            .Where(p => p.DeletedAt == null)
            .Select(p => new { p.Id, p.ParentPageId })
            .ToListAsync();

        var lookup = ancestorChain.ToDictionary(p => p.Id, p => p.ParentPageId);
        var currentId = (int?)potentialDescendantId;
        var visited = new HashSet<int>();

        while (currentId.HasValue)
        {
            if (!visited.Add(currentId.Value)) return false;
            if (currentId.Value == ancestorId) return true;
            lookup.TryGetValue(currentId.Value, out currentId);
        }

        return false;
    }

    private async Task<List<int>> GetDescendantIdsAsync(int pageId)
    {
        var allPages = await context.Pages
            .AsNoTracking()
            .Where(p => p.DeletedAt == null)
            .Select(p => new { p.Id, p.ParentPageId })
            .ToListAsync();

        var childrenLookup = allPages
            .Where(p => p.ParentPageId.HasValue)
            .GroupBy(p => p.ParentPageId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(p => p.Id).ToList());

        var descendants = new List<int>();
        var queue = new Queue<int>();
        queue.Enqueue(pageId);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            if (!childrenLookup.TryGetValue(currentId, out var childIds)) continue;
            foreach (var childId in childIds)
            {
                descendants.Add(childId);
                queue.Enqueue(childId);
            }
        }

        return descendants;
    }

    private async Task RecalculateDescendantLevelsAsync(int pageId, int newParentLevel)
    {
        var descendantIds = await GetDescendantIdsAsync(pageId);
        if (descendantIds.Count == 0) return;

        var descendants = await context.Pages
            .Where(p => descendantIds.Contains(p.Id))
            .ToListAsync();

        var childrenLookup = descendants
            .Where(p => p.ParentPageId.HasValue)
            .GroupBy(p => p.ParentPageId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var queue = new Queue<(int Id, int Level)>();
        queue.Enqueue((pageId, newParentLevel));

        while (queue.Count > 0)
        {
            var (currentId, currentLevel) = queue.Dequeue();
            if (!childrenLookup.TryGetValue(currentId, out var children)) continue;
            foreach (var child in children)
            {
                child.Level = currentLevel + 1;
                queue.Enqueue((child.Id, child.Level));
            }
        }
    }

    private static PageResponse MapToResponse(Page entity) => new()
    {
        Id = entity.Id,
        Title = entity.Title,
        Description = entity.Description,
        Content = entity.Content,
        SortOrder = entity.SortOrder,
        SpaceId = entity.SpaceId,
        ParentPageId = entity.ParentPageId,
        Level = entity.Level,
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

    public async Task<List<BreadcrumbItem>> GetBreadcrumbsAsync(int pageId)
    {
        var breadcrumbs = new List<BreadcrumbItem>();
        var currentId = (int?)pageId;
        var visited = new HashSet<int>();

        while (currentId.HasValue)
        {
            if (!visited.Add(currentId.Value)) break;

            var page = await context.Pages
                .AsNoTracking()
                .Where(p => p.Id == currentId.Value && p.DeletedAt == null)
                .Select(p => new { p.Id, p.Title, p.ParentPageId })
                .FirstOrDefaultAsync();

            if (page == null) break;

            breadcrumbs.Insert(0, new BreadcrumbItem { Id = page.Id, Title = page.Title });
            currentId = page.ParentPageId;
        }

        return breadcrumbs;
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
