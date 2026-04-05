using EaglesJungscharen.Azure.ContentApi.Models;
using EaglesJungscharen.Azure.ContentApi.Services;
using GuedesPlace.AzureTools.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EaglesJungscharen.Azure.ContentApi;

public class Content(
    ILogger<Content> logger,
    ExtendedAzureTableClientService tableClientService,
    SharepointListService sharepointListService)
{
    private readonly ILogger<Content> _logger = logger;
    private readonly TypedAzureTableClient<ContentTypeConfig> _configTableClient = tableClientService.GetTypedTableClient<ContentTypeConfig>();
    private readonly SharepointListService _sharepointListService = sharepointListService;

    [Function("Content")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "content/{short}")] HttpRequest req,
        string @short)
    {
        _logger.LogInformation("Content request for short: {Short}", @short);

        var configResult = await _configTableClient.GetByIdAsync(@short, "config");
        if (configResult is null)
        {
            _logger.LogWarning("No configuration found for short: {Short}", @short);
            return new NotFoundResult();
        }

        var items = await _sharepointListService.GetListItemsAsync(
            configResult.Entity.SiteId,
            configResult.Entity.ListId);

        var result = ApplyFeedType(items, configResult.Entity);

        return new OkObjectResult(result);
    }

    private static List<Dictionary<string, object?>> ApplyFeedType(
        List<Dictionary<string, object?>> items,
        ContentTypeConfig config)
    {
        if (config.FeedType == FeedType.CONTENT)
            return items;

        var sortColumn = config.SortColumn!;
        var now = DateTimeOffset.UtcNow;

        var withDates = items
            .Select(item => (
                item,
                date: TryGetDateTimeOffset(item.GetValueOrDefault(sortColumn), out var dt) ? dt : (DateTimeOffset?)null))
            .Where(x => x.date.HasValue);

        if (config.FeedType == FeedType.NEWS)
        {
            return withDates
                .Where(x => x.date!.Value < now)
                .OrderByDescending(x => x.date!.Value)
                .Select(x => x.item)
                .ToList();
        }
        else // AGENDA
        {
            return withDates
                .Where(x => x.date!.Value >= now)
                .OrderBy(x => x.date!.Value)
                .Select(x => x.item)
                .ToList();
        }
    }

    private static bool TryGetDateTimeOffset(object? value, out DateTimeOffset result)
    {
        result = default;
        if (value is null) return false;
        var str = value is System.Text.Json.JsonElement je ? je.GetString() : value?.ToString();
        return str is not null && DateTimeOffset.TryParse(str, out result);
    }
}
