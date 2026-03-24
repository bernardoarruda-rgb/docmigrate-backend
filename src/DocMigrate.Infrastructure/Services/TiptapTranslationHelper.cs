using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using DocMigrate.Application.Interfaces;

namespace DocMigrate.Infrastructure.Services;

public class TiptapTranslationHelper
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

        var sb = new StringBuilder();
        for (var i = 0; i < textNodes.Count; i++)
        {
            if (i > 0) sb.Append($"<<{i}>>");
            sb.Append(textNodes[i]["text"]?.GetValue<string>() ?? "");
        }

        var result = await provider.TranslateTextAsync(sb.ToString(), fromLang, toLang);
        if (!result.Success)
            return tiptapJson;

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

        return doc.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
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
