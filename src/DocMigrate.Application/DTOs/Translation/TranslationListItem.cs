namespace DocMigrate.Application.DTOs.Translation;

public class TranslationListItem
{
    public string Language { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}
