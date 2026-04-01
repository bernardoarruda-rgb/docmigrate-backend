using DocMigrate.Application.DTOs.Folder;
using DocMigrate.Application.DTOs.Page;

namespace DocMigrate.Application.Interfaces;

public interface IFolderService
{
    Task<List<FolderTreeItem>> GetAllAsync(int spaceId);
    Task<FolderResponse> GetByIdAsync(int id);
    Task<FolderResponse> CreateAsync(CreateFolderRequest request);
    Task<FolderResponse> UpdateAsync(int id, UpdateFolderRequest request);
    Task DeleteAsync(int id);
    Task ReorderAsync(int spaceId, ReorderFoldersRequest request);
    Task<List<BreadcrumbItem>> GetBreadcrumbsAsync(int folderId);
}
