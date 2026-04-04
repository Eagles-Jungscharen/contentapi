using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EaglesJungscharen.Azure.ContentApi;

public class Content
{
    private readonly ILogger<Content> _logger;

    public Content(ILogger<Content> logger)
    {
        _logger = logger;
    }

    [Function("Content")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }
}