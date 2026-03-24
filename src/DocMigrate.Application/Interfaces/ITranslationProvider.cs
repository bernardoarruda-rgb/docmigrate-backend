namespace DocMigrate.Application.Interfaces;

public interface ITranslationProvider
{
    Task<TranslationResult> TranslateTextAsync(string text, string fromLang, string toLang);
}

public record TranslationResult(string TranslatedText, bool Success, string? Error = null);
