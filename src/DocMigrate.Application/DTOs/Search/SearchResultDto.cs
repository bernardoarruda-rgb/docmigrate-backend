using DocMigrate.Application.DTOs.Tag;

namespace DocMigrate.Application.DTOs.Search;

public record SearchResultDto
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Type { get; init; } = string.Empty;
    public int? SpaceId { get; init; }
    public string? SpaceTitle { get; init; }
    public string? Icon { get; init; }
    public string? IconColor { get; init; }
    public string? Snippet { get; init; }
    public float Rank { get; set; }
    public string? Language { get; set; }
    public List<TagListItem> Tags { get; set; } = [];
}
