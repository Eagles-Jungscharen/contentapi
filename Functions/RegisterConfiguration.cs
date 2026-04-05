using System.Text.Json;
using EaglesJungscharen.Azure.ContentApi.Models;
using GuedesPlace.AzureTools.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EaglesJungscharen.Azure.ContentApi;

public class RegisterConfiguration(
    ILogger<RegisterConfiguration> logger,
    ExtendedAzureTableClientService tableClientService)
{
    private readonly ILogger<RegisterConfiguration> _logger = logger;
    private readonly TypedAzureTableClient<ContentTypeConfig> _configTableClient = tableClientService.CreateAndRegisterTableClient<ContentTypeConfig>("ContentConfig");

    [Function("RegisterConfiguration")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Admin, "post", "delete", Route = "admin/registerConfiguration")] HttpRequest req)
    {
        if (req.Method.Equals("DELETE", StringComparison.OrdinalIgnoreCase))
        {
            return await HandleDeleteAsync(req);
        }

        return await HandlePostAsync(req);
    }

    // Legt eine neue Konfiguration an oder überschreibt eine bestehende (mit ?update=true)
    private async Task<IActionResult> HandlePostAsync(HttpRequest req)
    {
        ContentTypeConfig? config;
        try
        {
            config = await req.ReadFromJsonAsync<ContentTypeConfig>();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning("Ungültiger JSON-Body: {Message}", ex.Message);
            return new BadRequestObjectResult("Ungültiger JSON-Body.");
        }

        if (config is null
            || string.IsNullOrWhiteSpace(config.Key)
            || string.IsNullOrWhiteSpace(config.SiteId)
            || string.IsNullOrWhiteSpace(config.ListId))
        {
            return new BadRequestObjectResult("Die Felder Key, SiteId und ListId sind Pflichtfelder.");
        }

        var existing = await _configTableClient.GetByIdAsync(config.Key, "config");
        if (existing is not null)
        {
            var update = req.Query["update"].ToString();
            if (!string.Equals(update, "true", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Konfiguration '{Key}' existiert bereits. ?update=true fehlt.", config.Key);
                return new ConflictObjectResult(
                    $"Konfiguration '{config.Key}' existiert bereits. Zum Überschreiben ?update=true setzen.");
            }
        }

        await _configTableClient.InsertOrReplaceAsync(config.Key, "config", config);
        _logger.LogInformation("Konfiguration '{Key}' gespeichert.", config.Key);
        return new StatusCodeResult(StatusCodes.Status201Created);
    }

    // Löscht eine bestehende Konfiguration per ?short=<kurzname>
    private async Task<IActionResult> HandleDeleteAsync(HttpRequest req)
    {
        var @short = req.Query["short"].ToString();
        if (string.IsNullOrWhiteSpace(@short))
        {
            return new BadRequestObjectResult("Der Query-Parameter 'short' ist ein Pflichtfeld.");
        }

        var existing = await _configTableClient.GetByIdAsync(@short, "config");
        if (existing is null)
        {
            _logger.LogWarning("Konfiguration '{Short}' nicht gefunden.", @short);
            return new NotFoundResult();
        }

        await _configTableClient.DeleteEntityAsync(@short, "config");
        _logger.LogInformation("Konfiguration '{Short}' gelöscht.", @short);
        return new StatusCodeResult(StatusCodes.Status204NoContent);
    }
}
