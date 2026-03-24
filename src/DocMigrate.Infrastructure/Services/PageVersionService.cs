using DocMigrate.Application.DTOs.PageVersion;
using DocMigrate.Application.Interfaces;
using DocMigrate.Domain.Entities;
using DocMigrate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DocMigrate.Infrastructure.Services;

public class PageVersionService(AppDbContext context, IPlainTextExtractor plainTextExtractor) : IPageVersionService
{
    public async Task<List<PageVersionListItem>> GetVersionsAsync(int pageId)
    {
        return await context.PageVersions
            .AsNoTracking()
            .Include(v => v.CreatedByUser)
            .Where(v => v.PageId == pageId && v.DeletedAt == null)
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => new PageVersionListItem
            {
                Id = v.Id,
                VersionNumber = v.VersionNumber,
                ChangeDescription = v.ChangeDescription,
                CreatedByName = v.CreatedByUser != null ? v.CreatedByUser.Name : null,
                CreatedAt = v.CreatedAt,
            })
            .ToListAsync();
    }

    public async Task<PageVersionResponse> GetVersionAsync(int pageId, int versionNumber)
    {
        var version = await context.PageVersions
            .AsNoTracking()
            .Include(v => v.CreatedByUser)
            .Where(v => v.PageId == pageId && v.VersionNumber == versionNumber && v.DeletedAt == null)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Versao nao encontrada");

        return MapToResponse(version);
    }

    public async Task CreateVersionAsync(int pageId, string content, string? changeDescription, int? userId)
    {
        var page = await context.Pages
            .Where(p => p.Id == pageId && p.DeletedAt == null)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Pagina nao encontrada");

        var lastVersion = await context.PageVersions
            .Where(v => v.PageId == pageId && v.DeletedAt == null)
            .MaxAsync(v => (int?)v.VersionNumber) ?? 0;

        var version = new PageVersion
        {
            PageId = pageId,
            VersionNumber = lastVersion + 1,
            Content = content,
            PlainText = plainTextExtractor.Extract(content),
            ChangeDescription = changeDescription,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        context.PageVersions.Add(version);
        await context.SaveChangesAsync();
    }

    public async Task<PageVersionResponse> RestoreVersionAsync(int pageId, int versionNumber, int? userId)
    {
        var version = await context.PageVersions
            .Include(v => v.CreatedByUser)
            .Where(v => v.PageId == pageId && v.VersionNumber == versionNumber && v.DeletedAt == null)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Versao nao encontrada");

        var page = await context.Pages
            .Where(p => p.Id == pageId && p.DeletedAt == null)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Pagina nao encontrada");

        // Save current content as a new version before restoring
        await CreateVersionAsync(pageId, page.Content ?? string.Empty, "Backup antes de restaurar", userId);

        // Restore
        page.Content = version.Content;
        page.PlainText = version.PlainText ?? plainTextExtractor.Extract(version.Content);
        page.UpdatedAt = DateTime.UtcNow;
        if (userId != null) page.UpdatedByUserId = userId;

        await context.SaveChangesAsync();

        return MapToResponse(version);
    }

    private static PageVersionResponse MapToResponse(PageVersion v) => new()
    {
        Id = v.Id,
        PageId = v.PageId,
        VersionNumber = v.VersionNumber,
        Content = v.Content,
        ChangeDescription = v.ChangeDescription,
        CreatedByName = v.CreatedByUser?.Name,
        CreatedAt = v.CreatedAt,
    };
}
