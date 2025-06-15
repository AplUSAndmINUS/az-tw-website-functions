using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using az_tw_website_functions.SharedStorage;
using Utils;
using System.Reflection.Metadata;
using Microsoft.Identity.Client.Extensions.Msal;

var builder = FunctionsApplication.CreateBuilder(args);

// Add configuration and services
var configuration = builder.Configuration;
var storageAccountName = configuration["StorageAccountName"];

// Register storage services
builder.Services.AddSingleton<IBlobStorageService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<BlobStorageService>>();

    try
    {
        logger.LogInformation("Creating blob storage client for {StorageAccount}", storageAccountName ?? "unknown");
        StorageAccountValidator.ValidateStorageAccountName(storageAccountName!);

        return new BlobStorageService(storageAccountName!, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to create blob storage client for {StorageAccount}", storageAccountName ?? "unknown");
        throw;
    }
});

builder.Services.AddSingleton<ITableStorageService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<TableStorageService>>();

    try
    {
        logger.LogInformation("Creating table storage client for {StorageAccount}", storageAccountName ?? "unknown");
        StorageAccountValidator.ValidateStorageAccountName(storageAccountName!);

        return new TableStorageService(storageAccountName!, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to create table storage client for {StorageAccount}", storageAccountName ?? "unknown");
        throw;
    }
});

// Register custom logger
builder.Services.AddSingleton<AppInsightsLogger>();

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
