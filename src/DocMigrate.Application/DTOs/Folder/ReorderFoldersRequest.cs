namespace DocMigrate.Application.DTOs.Folder;

public record ReorderFoldersRequest
{
    public required List<FolderOrderItem> Items { get; init; }
}

public record FolderOrderItem
{
    public required int FolderId { get; init; }
    public required int SortOrder { get; init; }
}
