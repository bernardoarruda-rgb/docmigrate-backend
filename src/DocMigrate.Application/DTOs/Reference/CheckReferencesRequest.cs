namespace DocMigrate.Application.DTOs.Reference;

public class CheckReferencesRequest
{
    public List<int> PageIds { get; set; } = [];
    public List<int> SpaceIds { get; set; } = [];
}
