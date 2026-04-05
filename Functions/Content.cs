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
    private readonly TypedAzureTableClient<ContentTypeConfig> _configTableClient = tableClientService.CreateAndRegisterTableClient<ContentTypeConfig>("ContentConfig");
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

        return new OkObjectResult(items);
    }
}
