namespace DocMigrate.Application.DTOs.Reference;

public class CheckReferencesResponse
{
    public List<int> ExistingPageIds { get; set; } = [];
    public List<int> ExistingSpaceIds { get; set; } = [];
}
