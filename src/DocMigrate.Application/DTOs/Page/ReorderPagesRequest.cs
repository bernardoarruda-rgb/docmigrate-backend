namespace DocMigrate.Application.DTOs.Page;

public record ReorderPagesRequest
{
    public required List<PageOrderItem> Items { get; init; }
}

public record PageOrderItem
{
    public required int PageId { get; init; }
    public required int SortOrder { get; init; }
}
