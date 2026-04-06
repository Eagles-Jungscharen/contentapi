namespace EaglesJungscharen.Azure.ContentApi.Models;

public class ContentTypeConfig
{
    public required string Key { get; set; }
    public string SiteId { get; set; } = string.Empty;
    public string ListId { get; set; } = string.Empty;
    public FeedType FeedType { get; set; } = FeedType.CONTENT;
    public string? SortColumn { get; set; }
    public string ActiveColumn { get; set; } = string.Empty;
    public List<ColumnMapping> ColumnMappings { get; set; } = [];
}
