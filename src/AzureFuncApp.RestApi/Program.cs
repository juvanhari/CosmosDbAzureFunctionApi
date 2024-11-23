using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.Services.AddSingleton(s =>
{
    var accessKey = builder.Configuration.GetValue<string>("CosmosDBAccessKey");
    var endpoint = builder.Configuration.GetValue<string>("CosmosDBEndPoint");
    var database = builder.Configuration.GetValue<string>("CosmosDBDatabase");

    // Initialize CosmosClientOptions with desired settings
    CosmosClientOptions options = new CosmosClientOptions()
    {
        ConnectionMode = ConnectionMode.Gateway, // or other modes like Direct, etc.
        ConsistencyLevel = ConsistencyLevel.Session // or other levels like Eventual, Strong
    };

    return new CosmosClient(endpoint, accessKey, options);
});

builder.ConfigureFunctionsWebApplication();

// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
// builder.Services
//     .AddApplicationInsightsTelemetryWorkerService()
//     .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
