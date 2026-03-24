namespace DocMigrate.Application.DTOs.PageVersion;

public class PageVersionResponse
{
    public int Id { get; set; }
    public int PageId { get; set; }
    public int VersionNumber { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ChangeDescription { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
}
