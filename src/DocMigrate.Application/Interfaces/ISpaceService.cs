using DocMigrate.Application.DTOs.Space;

namespace DocMigrate.Application.Interfaces;

public interface ISpaceService
{
    Task<List<SpaceListItem>> GetAllAsync();
    Task<SpaceResponse> GetByIdAsync(int id);
    Task<SpaceResponse> CreateAsync(CreateSpaceRequest request);
    Task<SpaceResponse> UpdateAsync(int id, UpdateSpaceRequest request);
    Task DeleteAsync(int id);
}
