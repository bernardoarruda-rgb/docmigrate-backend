namespace DocMigrate.Application.DTOs.Translation;

public class TranslationResponse
{
    public int Id { get; set; }
    public int PageId { get; set; }
    public string Language { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Content { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? TranslatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
