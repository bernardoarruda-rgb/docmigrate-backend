namespace DocMigrate.Application.DTOs.Translation;

public class UpdateTranslationRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Content { get; set; }
}
