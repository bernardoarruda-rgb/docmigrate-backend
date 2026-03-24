using System.Text;
using System.Text.Json;
using DocMigrate.Application.Interfaces;

namespace DocMigrate.Infrastructure.Services;

public class TiptapPlainTextExtractor : IPlainTextExtractor
{
    public string? Extract(string? tiptapJson)
    {
        if (string.IsNullOrWhiteSpace(tiptapJson))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(tiptapJson);
            var sb = new StringBuilder();
            WalkNodes(doc.RootElement, sb);
            var result = sb.ToString().Trim();
            return result.Length > 0 ? result : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void WalkNodes(JsonElement node, StringBuilder sb)
    {
        if (node.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
        {
            sb.Append(text.GetString());
            sb.Append(' ');
        }

        if (node.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in content.EnumerateArray())
            {
                WalkNodes(child, sb);
            }
        }
    }
}
