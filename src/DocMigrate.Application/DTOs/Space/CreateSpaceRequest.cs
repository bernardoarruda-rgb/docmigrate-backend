namespace DocMigrate.Application.DTOs.Space;

public class CreateSpaceRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string? IconColor { get; set; }
    public string? BackgroundColor { get; set; }
}
