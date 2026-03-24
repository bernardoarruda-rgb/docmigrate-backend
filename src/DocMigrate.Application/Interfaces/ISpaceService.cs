using DocMigrate.Application.DTOs.Common;
using DocMigrate.Application.DTOs.Space;

namespace DocMigrate.Application.Interfaces;

public interface ISpaceService
{
    Task<List<SpaceListItem>> GetAllAsync();
    Task<PaginatedResult<SpaceListItem>> GetAllAsync(int page, int pageSize);
    Task<SpaceResponse> GetByIdAsync(int id);
    Task<SpaceResponse> CreateAsync(CreateSpaceRequest request, int? userId = null);
    Task<SpaceResponse> UpdateAsync(int id, UpdateSpaceRequest request, int? userId = null);
    Task DeleteAsync(int id);
    Task SetTagsAsync(int spaceId, List<int> tagIds);
}
