using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using DocMigrate.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace DocMigrate.Infrastructure.Services;

public class TiptapTranslationHelper(ILogger<TiptapTranslationHelper> logger)
{
    private static readonly HashSet<string> SkipNodeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "codeBlock"
    };

    private static readonly HashSet<string> SkipMarkTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "code"
    };

    public async Task<string> TranslateContentAsync(
        string tiptapJson, string fromLang, string toLang, ITranslationProvider provider)
    {
        if (string.IsNullOrWhiteSpace(tiptapJson))
            return tiptapJson;

        var doc = JsonNode.Parse(tiptapJson);
        if (doc is null) return tiptapJson;

        var textNodes = new List<JsonNode>();
        CollectTextNodes(doc, textNodes);

        if (textNodes.Count == 0)
            return tiptapJson;

        logger.LogInformation("[TiptapTranslation] {NodeCount} text nodes coletados, traduzindo em chunks {From}->{To}",
            textNodes.Count, fromLang, toLang);

        // Translate in chunks (balance between efficiency and reliability)
        const int chunkSize = 50;
        var chunks = new List<List<JsonNode>>();
        for (var i = 0; i < textNodes.Count; i += chunkSize)
            chunks.Add(textNodes.GetRange(i, Math.Min(chunkSize, textNodes.Count - i)));

        var totalCalls = 0;
        foreach (var chunk in chunks)
        {
            if (await TryBatchTranslateAsync(chunk, fromLang, toLang, provider))
            {
                totalCalls++;
            }
            else
            {
                // Fallback: translate this chunk node-by-node
                logger.LogWarning("[TiptapTranslation] Chunk batch falhou ({ChunkSize} nodes), fallback node-by-node", chunk.Count);
                await TranslateNodeByNodeAsync(chunk, fromLang, toLang, provider);
                totalCalls += chunk.Count;
            }
        }

        logger.LogInformation("[TiptapTranslation] Concluido: {Calls} API calls para {NodeCount} nodes ({ChunkCount} chunks)",
            totalCalls, textNodes.Count, chunks.Count);
        return doc.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    private async Task<bool> TryBatchTranslateAsync(
        List<JsonNode> textNodes, string fromLang, string toLang, ITranslationProvider provider)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < textNodes.Count; i++)
        {
            if (i > 0) sb.Append($"<<{i}>>");
            sb.Append(textNodes[i]["text"]?.GetValue<string>() ?? "");
        }

        var result = await provider.TranslateTextAsync(sb.ToString(), fromLang, toLang);
        if (!result.Success)
        {
            logger.LogWarning("[TiptapTranslation] Batch: provider retornou erro: {Error}", result.Error);
            return false;
        }

        // Validate that markers were preserved in the response
        var expectedMarkerCount = textNodes.Count - 1;
        if (expectedMarkerCount > 0)
        {
            var foundMarkers = 0;
            for (var i = 1; i < textNodes.Count; i++)
            {
                if (result.TranslatedText.Contains($"<<{i}>>", StringComparison.Ordinal))
                    foundMarkers++;
            }

            logger.LogInformation("[TiptapTranslation] Batch: {Found}/{Expected} markers preservados",
                foundMarkers, expectedMarkerCount);

            // If less than 80% of markers were preserved, batch failed
            if (foundMarkers < expectedMarkerCount * 0.8)
                return false;
        }

        var parts = new List<string> { result.TranslatedText };
        for (var i = 1; i < textNodes.Count; i++)
        {
            var marker = $"<<{i}>>";
            var lastPart = parts[^1];
            var idx = lastPart.IndexOf(marker, StringComparison.Ordinal);
            if (idx >= 0)
            {
                parts[^1] = lastPart[..idx];
                parts.Add(lastPart[(idx + marker.Length)..]);
            }
            else
            {
                parts.Add(textNodes[i]["text"]?.GetValue<string>() ?? "");
            }
        }

        for (var i = 0; i < textNodes.Count && i < parts.Count; i++)
        {
            textNodes[i]["text"] = parts[i];
        }

        return true;
    }

    private static async Task TranslateNodeByNodeAsync(
        List<JsonNode> textNodes, string fromLang, string toLang, ITranslationProvider provider)
    {
        foreach (var node in textNodes)
        {
            var originalText = node["text"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(originalText))
                continue;

            var result = await provider.TranslateTextAsync(originalText, fromLang, toLang);
            if (result.Success)
                node["text"] = result.TranslatedText;
        }
    }

    private static void CollectTextNodes(JsonNode node, List<JsonNode> textNodes)
    {
        if (node is JsonObject obj)
        {
            var nodeType = obj["type"]?.GetValue<string>();

            if (nodeType is not null && SkipNodeTypes.Contains(nodeType))
                return;

            if (obj.ContainsKey("text") && !obj.ContainsKey("content"))
            {
                var marks = obj["marks"]?.AsArray();
                var hasCodeMark = marks?.Any(m =>
                {
                    var markType = m?["type"]?.GetValue<string>();
                    return markType is not null && SkipMarkTypes.Contains(markType);
                }) ?? false;

                if (!hasCodeMark)
                {
                    textNodes.Add(obj);
                }
                return;
            }

            if (obj["content"] is JsonArray content)
            {
                foreach (var child in content)
                {
                    if (child is not null)
                        CollectTextNodes(child, textNodes);
                }
            }
        }
        else if (node is JsonArray arr)
        {
            foreach (var child in arr)
            {
                if (child is not null)
                    CollectTextNodes(child, textNodes);
            }
        }
    }
}
