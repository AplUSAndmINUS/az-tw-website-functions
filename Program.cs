using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using az_tw_website_functions.SharedStorage;
using System.Reflection.Metadata;

var builder = FunctionsApplication.CreateBuilder(args);

// Add configuration and services
var configuration = builder.Configuration;
var storageAccountName = configuration["StorageAccountName"];

if (string.IsNullOrEmpty(storageAccountName))
{
    throw new ArgumentException("Storage account name cannot be null or empty.", nameof(storageAccountName));
}
if (storageAccountName.Length < 3 || storageAccountName.Length > 24)
{
    throw new ArgumentException("Storage account name must be between 3 and 24 characters long.", nameof(storageAccountName));
}

builder.Services.AddSingleton<IBlobStorageService>(sp =>
{
    new BlobStorageService(storageAccountName);
    return new BlobStorageService(storageAccountName);
});

builder.Services.AddSingleton<ITableStorageService>(sp =>
{
    new TableStorageService(storageAccountName);
    return new TableStorageService(storageAccountName);
});

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
