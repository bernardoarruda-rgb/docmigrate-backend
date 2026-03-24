using DocMigrate.Application.DTOs.Translation;
using DocMigrate.Application.Interfaces;
using DocMigrate.Domain.Entities;
using DocMigrate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocMigrate.Infrastructure.Services;

public class PageTranslationService(
    AppDbContext context,
    ITranslationProvider translationProvider,
    IPlainTextExtractor plainTextExtractor,
    TiptapTranslationHelper tiptapTranslationHelper,
    ILogger<PageTranslationService> logger) : IPageTranslationService
{
    private static readonly HashSet<string> SupportedLanguages = new(StringComparer.OrdinalIgnoreCase) { "pt-BR", "en", "es" };

    public async Task<List<TranslationListItem>> GetTranslationsAsync(int pageId)
    {
        await EnsurePageExistsAsync(pageId);

        return await context.PageTranslations
            .AsNoTracking()
            .Where(t => t.PageId == pageId && t.DeletedAt == null)
            .Select(t => new TranslationListItem
            {
                Language = t.Language,
                Status = t.Status,
                UpdatedAt = t.UpdatedAt,
            })
            .ToListAsync();
    }

    public async Task<TranslationResponse> GetTranslationAsync(int pageId, string language)
    {
        await EnsurePageExistsAsync(pageId);
        ValidateLanguage(language);

        var entity = await context.PageTranslations
            .AsNoTracking()
            .Include(t => t.TranslatedByUser)
            .Where(t => t.PageId == pageId && t.Language == language && t.DeletedAt == null)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Traducao nao encontrada para o idioma '{language}'");

        return MapToResponse(entity);
    }

    public async Task<TranslationResponse> GenerateTranslationAsync(int pageId, string language, int? userId = null)
    {
        var page = await EnsurePageExistsAsync(pageId);
        ValidateTargetLanguage(language, page.Language);

        // Check if active non-outdated translation already exists
        var existing = await context.PageTranslations
            .Include(t => t.TranslatedByUser)
            .Where(t => t.PageId == pageId && t.Language == language && t.DeletedAt == null)
            .FirstOrDefaultAsync();

        if (existing is not null && existing.Status != "desatualizada")
            throw new InvalidOperationException($"Ja existe uma traducao ativa para o idioma '{language}' nesta pagina");

        // Translate title
        var titleResult = await translationProvider.TranslateTextAsync(page.Title, page.Language, language);
        if (!titleResult.Success)
            throw new ApplicationException("Falha ao gerar traducao automatica. Tente novamente mais tarde.");

        // Translate description
        string? translatedDescription = null;
        if (!string.IsNullOrEmpty(page.Description))
        {
            var descResult = await translationProvider.TranslateTextAsync(page.Description, page.Language, language);
            if (descResult.Success) translatedDescription = descResult.TranslatedText;
        }

        // Translate content (Tiptap JSON)
        string? translatedContent = null;
        if (!string.IsNullOrEmpty(page.Content))
        {
            translatedContent = await tiptapTranslationHelper.TranslateContentAsync(
                page.Content, page.Language, language, translationProvider);
        }

        var plainText = plainTextExtractor.Extract(translatedContent);

        if (existing is not null)
        {
            // Re-generate outdated translation
            existing.Title = titleResult.TranslatedText;
            existing.Description = translatedDescription;
            existing.Content = translatedContent;
            existing.PlainText = plainText;
            existing.Status = "automatica";
            existing.TranslatedByUserId = userId;
            existing.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            return MapToResponse(existing);
        }

        // Create new translation
        var entity = new PageTranslation
        {
            PageId = pageId,
            Language = language,
            Title = titleResult.TranslatedText,
            Description = translatedDescription,
            Content = translatedContent,
            PlainText = plainText,
            Status = "automatica",
            TranslatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        context.PageTranslations.Add(entity);
        await context.SaveChangesAsync();

        await context.Entry(entity).Reference(e => e.TranslatedByUser).LoadAsync();
        return MapToResponse(entity);
    }

    public async Task<TranslationResponse> UpdateTranslationAsync(int pageId, string language, UpdateTranslationRequest request, int? userId = null)
    {
        await EnsurePageExistsAsync(pageId);
        ValidateLanguage(language);

        var entity = await context.PageTranslations
            .Include(t => t.TranslatedByUser)
            .Where(t => t.PageId == pageId && t.Language == language && t.DeletedAt == null)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Traducao nao encontrada para o idioma '{language}'");

        entity.Title = request.Title;
        entity.Description = request.Description;
        if (request.Content is not null)
        {
            entity.Content = request.Content;
            entity.PlainText = plainTextExtractor.Extract(request.Content);
        }
        entity.Status = "revisada";
        entity.TranslatedByUserId = userId;

        await context.SaveChangesAsync();

        await context.Entry(entity).Reference(e => e.TranslatedByUser).LoadAsync();
        return MapToResponse(entity);
    }

    public async Task DeleteTranslationAsync(int pageId, string language)
    {
        ValidateLanguage(language);

        var entity = await context.PageTranslations
            .Where(t => t.PageId == pageId && t.Language == language && t.DeletedAt == null)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Traducao nao encontrada para o idioma '{language}'");

        entity.DeletedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    // Use loop-based approach (InMemory-compatible for tests)
    public async Task MarkOutdatedAsync(int pageId)
    {
        var translations = await context.PageTranslations
            .Where(t => t.PageId == pageId && t.DeletedAt == null && t.Status != "desatualizada")
            .ToListAsync();

        foreach (var t in translations)
        {
            t.Status = "desatualizada";
            t.UpdatedAt = DateTime.UtcNow;
        }

        if (translations.Count > 0)
            await context.SaveChangesAsync();
    }

    // Use loop-based approach (InMemory-compatible for tests)
    public async Task SoftDeleteByPageAsync(int pageId)
    {
        var translations = await context.PageTranslations
            .Where(t => t.PageId == pageId && t.DeletedAt == null)
            .ToListAsync();

        foreach (var t in translations)
            t.DeletedAt = DateTime.UtcNow;

        if (translations.Count > 0)
            await context.SaveChangesAsync();
    }

    public async Task AutoTranslateAsync(int pageId, int? userId = null)
    {
        var page = await context.Pages
            .AsNoTracking()
            .Where(p => p.DeletedAt == null)
            .FirstOrDefaultAsync(p => p.Id == pageId);

        if (page is null) return;

        var sourceHash = ComputeSourceHash(page.Title, page.Description, page.Content);
        var targetLanguages = SupportedLanguages.Where(l => !string.Equals(l, page.Language, StringComparison.OrdinalIgnoreCase));

        foreach (var targetLang in targetLanguages)
        {
            try
            {
                var existing = await context.PageTranslations
                    .Where(t => t.PageId == pageId && t.Language == targetLang && t.DeletedAt == null)
                    .FirstOrDefaultAsync();

                if (existing is not null && existing.SourceHash == sourceHash)
                    continue; // Hash unchanged, skip

                if (existing is not null && existing.Status == "revisada")
                {
                    // Preserve human work — just mark as outdated
                    existing.Status = "desatualizada";
                    existing.UpdatedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync();
                    continue;
                }

                // Translate title
                var titleResult = await translationProvider.TranslateTextAsync(page.Title, page.Language, targetLang);
                if (!titleResult.Success)
                {
                    logger.LogWarning("Translation to {Lang} failed for page {PageId}: {Error}", targetLang, pageId, titleResult.Error);
                    continue;
                }

                // Translate description
                string? translatedDescription = null;
                if (!string.IsNullOrEmpty(page.Description))
                {
                    var descResult = await translationProvider.TranslateTextAsync(page.Description, page.Language, targetLang);
                    if (descResult.Success) translatedDescription = descResult.TranslatedText;
                }

                // Translate content (Tiptap JSON)
                string? translatedContent = null;
                if (!string.IsNullOrEmpty(page.Content))
                {
                    translatedContent = await tiptapTranslationHelper.TranslateContentAsync(
                        page.Content, page.Language, targetLang, translationProvider);
                }

                var plainText = plainTextExtractor.Extract(translatedContent);

                if (existing is not null)
                {
                    existing.Title = titleResult.TranslatedText;
                    existing.Description = translatedDescription;
                    existing.Content = translatedContent;
                    existing.PlainText = plainText;
                    existing.Status = "automatica";
                    existing.SourceHash = sourceHash;
                    existing.TranslatedByUserId = userId;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    context.PageTranslations.Add(new PageTranslation
                    {
                        PageId = pageId,
                        Language = targetLang,
                        Title = titleResult.TranslatedText,
                        Description = translatedDescription,
                        Content = translatedContent,
                        PlainText = plainText,
                        Status = "automatica",
                        SourceHash = sourceHash,
                        TranslatedByUserId = userId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                    });
                }

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "AutoTranslate failed for page {PageId}, language {Lang}", pageId, targetLang);
                continue;
            }
        }
    }

    private async Task<Page> EnsurePageExistsAsync(int pageId)
    {
        return await context.Pages
            .AsNoTracking()
            .Where(p => p.DeletedAt == null)
            .FirstOrDefaultAsync(p => p.Id == pageId)
            ?? throw new KeyNotFoundException("Pagina nao encontrada");
    }

    private static void ValidateLanguage(string language)
    {
        if (string.Equals(language, "pt-BR", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Nao e possivel criar traducao para o idioma original (PT-BR).");

        if (!SupportedLanguages.Contains(language))
            throw new ArgumentException($"Idioma '{language}' nao e suportado. Idiomas disponiveis: en, es.");
    }

    private static void ValidateTargetLanguage(string targetLang, string pageLanguage)
    {
        ValidateLanguage(targetLang);
        if (string.Equals(targetLang, pageLanguage, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"Nao e possivel criar traducao para o idioma original da pagina ({pageLanguage}).");
    }

    private static string ComputeSourceHash(string? title, string? description, string? content)
    {
        var input = $"{title ?? ""}|{description ?? ""}|{content ?? ""}";
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }

    private static TranslationResponse MapToResponse(PageTranslation entity) => new()
    {
        Id = entity.Id,
        PageId = entity.PageId,
        Language = entity.Language,
        Title = entity.Title,
        Description = entity.Description,
        Content = entity.Content,
        Status = entity.Status,
        TranslatedByName = entity.TranslatedByUser?.Name,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
    };
}
