using DocMigrate.Application.DTOs.Template;

namespace DocMigrate.Application.Interfaces;

public interface ITemplateService
{
    Task<List<TemplateListItem>> GetAllAsync();
    Task<TemplateResponse> GetByIdAsync(int id);
    Task<TemplateResponse> CreateAsync(CreateTemplateRequest request, int? userId = null);
    Task<TemplateResponse> UpdateAsync(int id, UpdateTemplateRequest request, int? userId = null);
    Task DeleteAsync(int id);
}
