namespace DocMigrate.Application.DTOs.Tag;

public class UpdateTagRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
}
