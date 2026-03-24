namespace DocMigrate.Application.Interfaces;

public interface IPlainTextExtractor
{
    string? Extract(string? tiptapJson);
}
