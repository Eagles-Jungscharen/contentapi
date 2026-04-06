namespace EaglesJungscharen.Azure.ContentApi.Models;

public class ContentConfigInfo
{
    public required string Key { get; set; }
    public FeedType FeedType { get; set; }
    public string? SortColumn { get; set; }
    public string? ActiveColumn { get; set; }
    public List<ColumnMapping> ColumnMappings { get; set; } = [];
    public bool IsValid { get; set; }
    public string? ValidationError { get; set; }
}
