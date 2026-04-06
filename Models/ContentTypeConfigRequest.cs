namespace EaglesJungscharen.Azure.ContentApi.Models;

public class ContentTypeConfigRequest
{
    public required string Key { get; set; }
    public required string SiteId { get; set; }
    public required string ListId { get; set; }
    public FeedType FeedType { get; set; } = FeedType.CONTENT;
    public string? SortColumn { get; set; }
    public required string ActiveColumn { get; set; }
    public required List<ColumnMapping> ColumnMappings { get; set; }
}
