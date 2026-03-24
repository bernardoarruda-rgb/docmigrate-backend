using DocMigrate.Application.DTOs.Translation;

namespace DocMigrate.Application.Interfaces;

public interface IPageTranslationService
{
    Task<List<TranslationListItem>> GetTranslationsAsync(int pageId);
    Task<TranslationResponse> GetTranslationAsync(int pageId, string language);
    Task<TranslationResponse> GenerateTranslationAsync(int pageId, string language, int? userId = null);
    Task<TranslationResponse> UpdateTranslationAsync(int pageId, string language, UpdateTranslationRequest request, int? userId = null);
    Task DeleteTranslationAsync(int pageId, string language);
    Task MarkOutdatedAsync(int pageId);
    Task SoftDeleteByPageAsync(int pageId);
    Task AutoTranslateAsync(int pageId, int? userId = null);
}
