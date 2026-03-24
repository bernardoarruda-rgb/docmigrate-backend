using DocMigrate.Application.DTOs.Common;
using DocMigrate.Application.DTOs.Page;

namespace DocMigrate.Application.Interfaces;

public interface IPageService
{
    Task<List<PageListItem>> GetAllAsync(int spaceId);
    Task<PaginatedResult<PageListItem>> GetAllAsync(int spaceId, int page, int pageSize);
    Task<PageResponse> GetByIdAsync(int id);
    Task<PageResponse> CreateAsync(CreatePageRequest request, int? userId = null);
    Task<PageResponse> UpdateAsync(int id, UpdatePageRequest request, int? userId = null);
    Task DeleteAsync(int id);
    Task ReorderAsync(int spaceId, ReorderPagesRequest request);
    Task<bool> AcquireLockAsync(int pageId, string userId);
    Task<bool> ReleaseLockAsync(int pageId, string userId);
    Task AutosaveContentAsync(int pageId, string lockUserId, string content, int? userId = null);
    Task SetTagsAsync(int pageId, List<int> tagIds);
    Task<List<HeadingDto>> GetHeadingsAsync(int pageId);
}
