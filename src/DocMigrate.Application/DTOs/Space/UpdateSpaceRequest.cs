namespace DocMigrate.Application.DTOs.Space;

public class UpdateSpaceRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string? IconColor { get; set; }
    public string? BackgroundColor { get; set; }
}
