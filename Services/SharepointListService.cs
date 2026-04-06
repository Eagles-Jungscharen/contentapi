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

    /// <summary>
    /// Prüft ob eine Spalte in der angegebenen Liste vom Typ Boolean ist.
    /// </summary>
    public async Task<bool> IsBooleanColumnAsync(string siteId, string listId, string columnName)
    {
        var response = await _graphClient
            .Sites[siteId]
            .Lists[listId]
            .Columns
            .GetAsync();

        bool isBooleanColumn = false;

        var pageIterator = PageIterator<ColumnDefinition, ColumnDefinitionCollectionResponse>.CreatePageIterator(
            _graphClient,
            response!,
            column =>
            {
                if (string.Equals(column.Name, columnName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(column.DisplayName, columnName, StringComparison.OrdinalIgnoreCase))
                {
                    isBooleanColumn = column.Boolean != null;
                    return false;
                }
                return true;
            });

        await pageIterator.IterateAsync();

        return isBooleanColumn;
    }

    /// <summary>
    /// Prüft ob eine Spalte in der angegebenen Liste vom Typ Datum ist.
    /// </summary>
    public async Task<bool> IsDateColumnAsync(string siteId, string listId, string columnName)
    {
        var response = await _graphClient
            .Sites[siteId]
            .Lists[listId]
            .Columns
            .GetAsync();

        bool isDateColumn = false;

        var pageIterator = PageIterator<ColumnDefinition, ColumnDefinitionCollectionResponse>.CreatePageIterator(
            _graphClient,
            response!,
            column =>
            {
                if (string.Equals(column.Name, columnName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(column.DisplayName, columnName, StringComparison.OrdinalIgnoreCase))
                {
                    isDateColumn = column.DateTime != null;
                    return false;
                }
                return true;
            });

        await pageIterator.IterateAsync();

        return isDateColumn;
    }

    /// <summary>
    /// Prüft ob die SharePoint-Liste erreichbar und zugänglich ist.
    /// </summary>
    public async Task<bool> IsListAccessibleAsync(string siteId, string listId)
    {
        try
        {
            await _graphClient
                .Sites[siteId]
                .Lists[listId]
                .GetAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
