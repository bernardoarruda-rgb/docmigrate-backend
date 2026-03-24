namespace DocMigrate.Domain.Entities;

public class PageFavorite
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int PageId { get; set; }
    public Page Page { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
