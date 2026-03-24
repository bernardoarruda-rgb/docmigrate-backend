namespace DocMigrate.Application.DTOs.Tag;

public class CreateTagRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
}
