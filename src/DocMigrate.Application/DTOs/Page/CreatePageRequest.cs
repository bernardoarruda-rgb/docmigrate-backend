namespace DocMigrate.Application.DTOs.Page;

public class CreatePageRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Content { get; set; }
    public int SortOrder { get; set; }
    public int SpaceId { get; set; }
}
