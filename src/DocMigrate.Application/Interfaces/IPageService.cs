using DocMigrate.Application.DTOs.Page;

namespace DocMigrate.Application.Interfaces;

public interface IPageService
{
    Task<List<PageListItem>> GetAllAsync(int spaceId);
    Task<PageResponse> GetByIdAsync(int id);
    Task<PageResponse> CreateAsync(CreatePageRequest request);
    Task<PageResponse> UpdateAsync(int id, UpdatePageRequest request);
    Task DeleteAsync(int id);
}
