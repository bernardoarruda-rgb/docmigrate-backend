namespace DocMigrate.Application.DTOs.Page;

public class UpdatePageCoverRequest
{
    public string? CoverType { get; set; }
    public string? CoverValue { get; set; }
    public int? CoverPosition { get; set; }
    public string? CoverAttribution { get; set; }
}
