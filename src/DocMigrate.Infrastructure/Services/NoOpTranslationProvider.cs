using DocMigrate.Application.Interfaces;

namespace DocMigrate.Infrastructure.Services;

public class NoOpTranslationProvider : ITranslationProvider
{
    public Task<TranslationResult> TranslateTextAsync(string text, string fromLang, string toLang)
    {
        // Dev placeholder: prefix with [AUTO-{lang}] so it's clear this is not a real translation
        // Real provider (DeepL/Google) plugged later via DI config
        var prefixed = $"[AUTO-{toLang}] {text}";
        return Task.FromResult(new TranslationResult(prefixed, true));
    }
}
