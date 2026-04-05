namespace EaglesJungscharen.Azure.ContentApi.Models;

public class ContentTypeConfig
{
    public required string Key { get; set; }
    public string SiteId { get; set; } = string.Empty;
    public string ListId { get; set; } = string.Empty;
}
