using DocMigrate.Application.DTOs.Folder;
using DocMigrate.Application.DTOs.Page;
using DocMigrate.Application.Interfaces;
using DocMigrate.Domain.Entities;
using DocMigrate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DocMigrate.Infrastructure.Services;

public class FolderService(AppDbContext context) : IFolderService
{
    public async Task<List<FolderTreeItem>> GetAllAsync(int spaceId)
    {
        var folders = await context.Folders
            .AsNoTracking()
            .Where(f => f.SpaceId == spaceId && f.DeletedAt == null)
            .OrderBy(f => f.SortOrder)
            .Select(f => new FolderTreeItem
            {
                Id = f.Id,
                SpaceId = f.SpaceId,
                Title = f.Title,
                Icon = f.Icon,
                IconColor = f.IconColor,
                ParentFolderId = f.ParentFolderId,
                Level = f.Level,
                SortOrder = f.SortOrder,
                CreatedAt = f.CreatedAt,
            })
            .ToListAsync();

        var pages = await context.Pages
            .AsNoTracking()
            .Where(p => p.SpaceId == spaceId && p.DeletedAt == null && p.FolderId != null)
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

        return BuildTree(folders, pages);
    }

    public async Task<FolderResponse> GetByIdAsync(int id)
    {
        var folder = await context.Folders
            .AsNoTracking()
            .Where(f => f.DeletedAt == null && f.Id == id)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Pasta nao encontrada");

        var breadcrumbs = await GetBreadcrumbsAsync(id);

        return new FolderResponse
        {
            Id = folder.Id,
            SpaceId = folder.SpaceId,
            Title = folder.Title,
            Icon = folder.Icon,
            IconColor = folder.IconColor,
            ParentFolderId = folder.ParentFolderId,
            Level = folder.Level,
            SortOrder = folder.SortOrder,
            Breadcrumbs = breadcrumbs,
            CreatedAt = folder.CreatedAt,
            UpdatedAt = folder.UpdatedAt,
        };
    }

    public async Task<FolderResponse> CreateAsync(CreateFolderRequest request)
    {
        var spaceExists = await context.Spaces
            .AnyAsync(s => s.Id == request.SpaceId && s.DeletedAt == null);
        if (!spaceExists)
            throw new KeyNotFoundException("Espaco nao encontrado");

        int level = 1;
        if (request.ParentFolderId.HasValue)
        {
            var parent = await context.Folders
                .Where(f => f.DeletedAt == null && f.Id == request.ParentFolderId.Value)
                .Select(f => new { f.SpaceId, f.Level })
                .FirstOrDefaultAsync()
                ?? throw new KeyNotFoundException("Pasta pai nao encontrada ou foi desativada.");

            if (parent.SpaceId != request.SpaceId)
                throw new InvalidOperationException("Pasta pai deve pertencer ao mesmo espaco.");

            if (parent.Level >= 5)
                throw new InvalidOperationException("Profundidade maxima (5 niveis) atingida.");

            level = parent.Level + 1;
        }

        var entity = new Folder
        {
            Title = request.Title,
            Icon = request.Icon,
            IconColor = request.IconColor,
            SortOrder = request.SortOrder,
            SpaceId = request.SpaceId,
            ParentFolderId = request.ParentFolderId,
            Level = level,
        };

        context.Folders.Add(entity);
        await context.SaveChangesAsync();

        return new FolderResponse
        {
            Id = entity.Id,
            SpaceId = entity.SpaceId,
            Title = entity.Title,
            Icon = entity.Icon,
            IconColor = entity.IconColor,
            ParentFolderId = entity.ParentFolderId,
            Level = entity.Level,
            SortOrder = entity.SortOrder,
            Breadcrumbs = [],
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
        };
    }

    public async Task<FolderResponse> UpdateAsync(int id, UpdateFolderRequest request)
    {
        var entity = await context.Folders
            .Where(f => f.DeletedAt == null)
            .FirstOrDefaultAsync(f => f.Id == id)
            ?? throw new KeyNotFoundException("Pasta nao encontrada");

        entity.Title = request.Title;
        entity.Icon = request.Icon;
        entity.IconColor = request.IconColor;
        entity.SortOrder = request.SortOrder;

        if (request.ParentFolderId != entity.ParentFolderId)
        {
            if (request.ParentFolderId.HasValue)
            {
                if (request.ParentFolderId.Value == id)
                    throw new InvalidOperationException("Uma pasta nao pode ser pai de si mesma.");

                if (await IsDescendantAsync(id, request.ParentFolderId.Value))
                    throw new InvalidOperationException("Nao e possivel mover a pasta para um descendente (ciclo detectado).");

                var newParent = await context.Folders
                    .Where(f => f.DeletedAt == null && f.Id == request.ParentFolderId.Value)
                    .Select(f => new { f.SpaceId, f.Level })
                    .FirstOrDefaultAsync()
                    ?? throw new KeyNotFoundException("Pasta pai nao encontrada ou foi desativada.");

                if (newParent.SpaceId != entity.SpaceId)
                    throw new InvalidOperationException("Pasta pai deve pertencer ao mesmo espaco.");

                var maxDescendantDepth = await GetMaxDescendantDepthAsync(id);
                var newLevel = newParent.Level + 1;
                if (newLevel + maxDescendantDepth - entity.Level > 5)
                    throw new InvalidOperationException("Mover esta pasta excederia a profundidade maxima (5 niveis).");

                entity.ParentFolderId = request.ParentFolderId;
                entity.Level = newLevel;
            }
            else
            {
                entity.ParentFolderId = null;
                entity.Level = 1;
            }

            await RecalculateDescendantLevelsAsync(entity.Id, entity.Level);
        }

        await context.SaveChangesAsync();

        var breadcrumbs = await GetBreadcrumbsAsync(id);

        return new FolderResponse
        {
            Id = entity.Id,
            SpaceId = entity.SpaceId,
            Title = entity.Title,
            Icon = entity.Icon,
            IconColor = entity.IconColor,
            ParentFolderId = entity.ParentFolderId,
            Level = entity.Level,
            SortOrder = entity.SortOrder,
            Breadcrumbs = breadcrumbs,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
        };
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await context.Folders
            .Where(f => f.DeletedAt == null)
            .FirstOrDefaultAsync(f => f.Id == id)
            ?? throw new KeyNotFoundException("Pasta nao encontrada");

        var activeChildFolders = await context.Folders
            .CountAsync(f => f.ParentFolderId == id && f.DeletedAt == null);

        var activePages = await context.Pages
            .CountAsync(p => p.FolderId == id && p.DeletedAt == null);

        var totalDependents = activeChildFolders + activePages;
        if (totalDependents > 0)
        {
            var parts = new List<string>();
            if (activeChildFolders > 0) parts.Add($"{activeChildFolders} sub-pasta(s)");
            if (activePages > 0) parts.Add($"{activePages} pagina(s)");
            throw new InvalidOperationException(
                $"Nao e possivel excluir esta pasta. Existem {string.Join(" e ", parts)} vinculado(s). Remova-os primeiro.");
        }

        entity.DeletedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    public async Task ReorderAsync(int spaceId, ReorderFoldersRequest request)
    {
        var folderIds = request.Items.Select(i => i.FolderId).ToList();
        var folders = await context.Folders
            .Where(f => f.SpaceId == spaceId && f.DeletedAt == null && folderIds.Contains(f.Id))
            .ToListAsync();

        if (folders.Count != folderIds.Count)
            throw new KeyNotFoundException("Uma ou mais pastas nao encontradas");

        var parentIds = folders.Select(f => f.ParentFolderId).Distinct().ToList();
        if (parentIds.Count > 1)
            throw new ArgumentException("Todas as pastas devem pertencer ao mesmo pai para reordenar.");

        foreach (var item in request.Items)
        {
            var folder = folders.First(f => f.Id == item.FolderId);
            folder.SortOrder = item.SortOrder;
        }

        await context.SaveChangesAsync();
    }

    public async Task<List<BreadcrumbItem>> GetBreadcrumbsAsync(int folderId)
    {
        var folders = await context.Folders
            .AsNoTracking()
            .Where(f => f.DeletedAt == null)
            .Select(f => new { f.Id, f.Title, f.ParentFolderId })
            .ToListAsync();

        var lookup = folders.ToDictionary(f => f.Id);
        var breadcrumbs = new List<BreadcrumbItem>();
        var currentId = (int?)folderId;

        while (currentId.HasValue && lookup.TryGetValue(currentId.Value, out var folder))
        {
            breadcrumbs.Add(new BreadcrumbItem { Id = folder.Id, Title = folder.Title });
            currentId = folder.ParentFolderId;
        }

        breadcrumbs.Reverse();
        return breadcrumbs;
    }

    private async Task<bool> IsDescendantAsync(int ancestorId, int potentialDescendantId)
    {
        var allFolders = await context.Folders
            .AsNoTracking()
            .Where(f => f.DeletedAt == null)
            .Select(f => new { f.Id, f.ParentFolderId })
            .ToListAsync();

        var lookup = allFolders.ToDictionary(f => f.Id, f => f.ParentFolderId);
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

    private async Task<int> GetMaxDescendantDepthAsync(int folderId)
    {
        var allFolders = await context.Folders
            .AsNoTracking()
            .Where(f => f.DeletedAt == null)
            .Select(f => new { f.Id, f.ParentFolderId, f.Level })
            .ToListAsync();

        var childrenLookup = allFolders
            .Where(f => f.ParentFolderId.HasValue)
            .GroupBy(f => f.ParentFolderId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var maxLevel = 0;
        var queue = new Queue<int>();
        queue.Enqueue(folderId);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            if (!childrenLookup.TryGetValue(currentId, out var children)) continue;
            foreach (var child in children)
            {
                if (child.Level > maxLevel) maxLevel = child.Level;
                queue.Enqueue(child.Id);
            }
        }

        return maxLevel;
    }

    private async Task RecalculateDescendantLevelsAsync(int folderId, int newLevel)
    {
        var allFolders = await context.Folders
            .Where(f => f.DeletedAt == null)
            .ToListAsync();

        var childrenLookup = allFolders
            .Where(f => f.ParentFolderId.HasValue)
            .GroupBy(f => f.ParentFolderId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var queue = new Queue<(int Id, int Level)>();
        queue.Enqueue((folderId, newLevel));

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

    private static List<FolderTreeItem> BuildTree(List<FolderTreeItem> folders, List<PageListItem> pages)
    {
        var folderMap = folders.ToDictionary(f => f.Id);
        var roots = new List<FolderTreeItem>();

        var pagesByFolder = pages
            .Where(p => p.FolderId.HasValue)
            .GroupBy(p => p.FolderId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderBy(p => p.SortOrder).ToList());

        foreach (var folder in folders)
        {
            if (pagesByFolder.TryGetValue(folder.Id, out var folderPages))
                folder.Pages = folderPages;

            if (folder.ParentFolderId.HasValue && folderMap.TryGetValue(folder.ParentFolderId.Value, out var parent))
                parent.ChildFolders.Add(folder);
            else
                roots.Add(folder);
        }

        return roots;
    }
}
