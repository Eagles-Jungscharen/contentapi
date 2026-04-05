using EaglesJungscharen.Azure.ContentApi.Models;
using EaglesJungscharen.Azure.ContentApi.Services;
using GuedesPlace.AzureTools.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EaglesJungscharen.Azure.ContentApi.Functions;

public class Info(
    ILogger<Info> logger,
    ExtendedAzureTableClientService tableClientService,
    SharepointListService sharepointListService)
{
    private readonly ILogger<Info> _logger = logger;
    private readonly TypedAzureTableClient<ContentTypeConfig> _configTableClient =
        tableClientService.GetTypedTableClient<ContentTypeConfig>();
    private readonly SharepointListService _sharepointListService = sharepointListService;

    [Function("Info")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/info")] HttpRequest req)
    {
        _logger.LogInformation("Info-Anfrage eingegangen.");

        var configs = await _configTableClient.GetAllAsync("config");

        var validationTasks = configs.Select(result => ValidateConfigAsync(result.Entity));
        var infoList = await Task.WhenAll(validationTasks);

        return new OkObjectResult(infoList);
    }

    private async Task<ContentConfigInfo> ValidateConfigAsync(ContentTypeConfig config)
    {
        var info = new ContentConfigInfo
        {
            Key = config.Key,
            FeedType = config.FeedType,
            SortColumn = config.SortColumn,
            IsValid = true
        };

        // Prüfe ob die SharePoint-Liste erreichbar ist
        var isAccessible = await _sharepointListService.IsListAccessibleAsync(config.SiteId, config.ListId);
        if (!isAccessible)
        {
            info.IsValid = false;
            info.ValidationError = $"SharePoint-Liste nicht erreichbar (SiteId: {config.SiteId}, ListId: {config.ListId})";
            return info;
        }

        // Für NEWS und AGENDA: SortColumn wird benötigt
        if (config.FeedType != FeedType.CONTENT)
        {
            if (string.IsNullOrWhiteSpace(config.SortColumn))
            {
                info.IsValid = false;
                info.ValidationError = $"SortColumn fehlt für FeedType {config.FeedType}";
                return info;
            }

            // Prüfe ob SortColumn eine Datumsspalte ist
            var isDateColumn = await _sharepointListService.IsDateColumnAsync(config.SiteId, config.ListId, config.SortColumn);
            if (!isDateColumn)
            {
                info.IsValid = false;
                info.ValidationError = $"SortColumn '{config.SortColumn}' ist keine Datumsspalte";
                return info;
            }
        }

        return info;
    }
}
