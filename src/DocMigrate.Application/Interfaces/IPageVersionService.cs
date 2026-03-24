using DocMigrate.Application.DTOs.PageVersion;

namespace DocMigrate.Application.Interfaces;

public interface IPageVersionService
{
    Task<List<PageVersionListItem>> GetVersionsAsync(int pageId);
    Task<PageVersionResponse> GetVersionAsync(int pageId, int versionNumber);
    Task CreateVersionAsync(int pageId, string content, string? changeDescription, int? userId);
    Task<PageVersionResponse> RestoreVersionAsync(int pageId, int versionNumber, int? userId);
}
