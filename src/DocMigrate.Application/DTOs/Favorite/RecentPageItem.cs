namespace DocMigrate.Application.DTOs.Favorite;

public class RecentPageItem
{
    public int PageId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SpaceId { get; set; }
    public string? SpaceTitle { get; set; }
    public DateTime VisitedAt { get; set; }
}
