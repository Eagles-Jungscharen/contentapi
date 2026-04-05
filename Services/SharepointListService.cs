using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace EaglesJungscharen.Azure.ContentApi.Services;

public class SharepointListService(GraphServiceClient graphClient)
{
    private readonly GraphServiceClient _graphClient = graphClient;

    public async Task<List<Dictionary<string, object?>>> GetListItemsAsync(string siteId, string listId)
    {
        var result = new List<Dictionary<string, object?>>();

        var response = await _graphClient
            .Sites[siteId]
            .Lists[listId]
            .Items
            .GetAsync(config =>
            {
                config.QueryParameters.Expand = ["fields"];
            });

        var pageIterator = PageIterator<ListItem, ListItemCollectionResponse>.CreatePageIterator(
            _graphClient,
            response!,
            item =>
            {
                var fields = item.Fields?.AdditionalData;
                if (fields != null)
                {
                    result.Add(new Dictionary<string, object?>(fields));
                }
                return true;
            });

        await pageIterator.IterateAsync();

        return result;
    }
}
