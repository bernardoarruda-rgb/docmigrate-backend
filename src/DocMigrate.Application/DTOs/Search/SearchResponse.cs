namespace DocMigrate.Application.DTOs.Search;

public record SearchResponse
{
    public IReadOnlyList<SearchResultDto> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Limit { get; init; }
    public int Offset { get; init; }
}
