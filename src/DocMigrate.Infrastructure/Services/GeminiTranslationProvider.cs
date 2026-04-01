using System.Text;
using System.Text.Json;
using DocMigrate.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace DocMigrate.Infrastructure.Services;

public class GeminiTranslationProvider(IHttpClientFactory httpClientFactory, IConfiguration configuration) : ITranslationProvider
{
    private static readonly Dictionary<string, string> LanguageNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["pt-BR"] = "Brazilian Portuguese",
        ["en"] = "English",
        ["es"] = "Spanish",
    };

    public async Task<TranslationResult> TranslateTextAsync(string text, string fromLang, string toLang)
    {
        var apiKey = configuration["GeminiTranslation:ApiKey"];
        var model = configuration["GeminiTranslation:Model"] ?? "gemini-2.5-flash";

        var fromName = LanguageNames.GetValueOrDefault(fromLang, fromLang);
        var toName = LanguageNames.GetValueOrDefault(toLang, toLang);

        var prompt = $"You are a professional translator for internal technical documentation. " +
            $"Translate the following text from {fromName} to {toName}. " +
            "Rules: 1) Keep technical terms in English (deploy, branch, pipeline, commit, merge, pull request, release, rollback, sprint, etc). " +
            "2) Maintain the same tone and formality. " +
            "3) Return ONLY the translated text, no explanations. " +
            "4) CRITICAL: If the text contains segment markers like <<1>>, <<2>>, <<3>> etc, you MUST preserve them EXACTLY as they appear, in the same positions between text segments. Never remove, modify, or reorder these markers." +
            $"\n\nText to translate:\n{text}";

        var requestBody = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            },
            generationConfig = new { temperature = 0.1 },
        };

        try
        {
            var client = httpClientFactory.CreateClient("GeminiTranslation");
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("x-goog-api-key", apiKey);
            request.Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                return new TranslationResult("", false, $"Gemini API error ({response.StatusCode}): {errorBody}");
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var translatedText = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return new TranslationResult(translatedText?.Trim() ?? "", true);
        }
        catch (Exception ex)
        {
            return new TranslationResult("", false, $"Translation failed: {ex.Message}");
        }
    }
}
