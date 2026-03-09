namespace DocMigrate.Application.DTOs.Space;

public class SpaceListItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int PageCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
