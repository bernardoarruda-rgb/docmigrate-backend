namespace DocMigrate.Application.DTOs.PageVersion;

public class PageVersionListItem
{
    public int Id { get; set; }
    public int VersionNumber { get; set; }
    public string? ChangeDescription { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
}
