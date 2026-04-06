using Azure.Identity;
using EaglesJungscharen.Azure.ContentApi.Models;
using EaglesJungscharen.Azure.ContentApi.Services;
using GuedesPlace.AzureTools.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Graph;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

var storageConnectionString = builder.Configuration["AzureWebJobsStorage"]
    ?? throw new InvalidOperationException("AzureWebJobsStorage connection string is not configured.");

var tableClientService = new ExtendedAzureTableClientService(storageConnectionString);
var configTableClient = tableClientService.CreateAndRegisterTableClient<ContentTypeConfig>("ContentConfig");

builder.Services.AddSingleton(tableClientService);

builder.Services.AddSingleton(_ =>
{
    var credential = new DefaultAzureCredential();
    return new GraphServiceClient(credential, ["https://graph.microsoft.com/.default"]);
});

builder.Services.AddSingleton<SharepointListService>();

builder.Build().Run();
