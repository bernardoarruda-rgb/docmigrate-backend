using DocMigrate.Application.DTOs.Tag;

namespace DocMigrate.Application.Interfaces;

public interface ITagService
{
    Task<List<TagListItem>> GetAllAsync();
    Task<TagResponse> GetByIdAsync(int id);
    Task<TagResponse> CreateAsync(CreateTagRequest request);
    Task<TagResponse> UpdateAsync(int id, UpdateTagRequest request);
    Task DeleteAsync(int id);
    Task<List<TagListItem>> SearchAsync(string query);
}
