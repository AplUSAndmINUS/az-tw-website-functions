using SharedStorage.Services;
using Utils;
using Utils.Validation;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Core; // Add this
using Microsoft.Azure.Functions.Worker; // Add this for WorkerOptions

public class Program
{
    public static void Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                var configuration = context.Configuration;
                var storageAccountName = configuration["StorageAccountName"]
                    ?? Environment.GetEnvironmentVariable("StorageAccountName")
                    ?? "aztwwebsitestorage"; // Default value if not set

                if (string.IsNullOrWhiteSpace(storageAccountName))
                    throw new InvalidOperationException("Missing 'StorageAccountName' in environment or config.");

                // Register these services first so they can be injected into BlobStorageService
                services.AddSingleton<IImageService, ImageConversionService>();
                services.AddSingleton<IThumbnailService, ThumbnailService>();

                // Register BlobStorageService
                services.AddSingleton<IBlobStorageService>(sp =>
                {
                    var logger = sp.GetRequiredService<IAppInsightsLogger<BlobStorageService>>();
                    var imageConversionService = sp.GetRequiredService<IImageService>();
                    var thumbnailService = sp.GetRequiredService<IThumbnailService>();

                    return new BlobStorageService(storageAccountName!, logger, imageConversionService, thumbnailService);
                });

                // Register TableStorageService
                services.AddSingleton<ITableStorageService>(sp =>
                {
                    var logger = sp.GetRequiredService<IAppInsightsLogger<TableStorageService>>();
                    return new TableStorageService(storageAccountName!, logger);
                });

                // Register APIKeyValidator
                services.AddSingleton<IAPIKeyValidator>(sp =>
                {
                    var validApiKey = configuration["X_API_ENVIRONMENT_KEY"]
                        ?? Environment.GetEnvironmentVariable("X_API_ENVIRONMENT_KEY");

                    if (string.IsNullOrWhiteSpace(validApiKey))
                        throw new InvalidOperationException("Missing X_API_ENVIRONMENT_KEY in configuration.");

                    var appLogger = sp.GetRequiredService<IAppInsightsLogger<ApiKeyValidator>>();
                    return new ApiKeyValidator(validApiKey, appLogger);
                });                // Register Author Service
                services.AddSingleton<Functions.Authors.Services.IAuthorService, Functions.Authors.Services.AuthorService>();

                // Register Application Insights telemetry
                services.AddApplicationInsightsTelemetryWorkerService();

                // Register AppInsightsLogger
                services.AddSingleton(typeof(IAppInsightsLogger<>), typeof(AppInsightsLogger<>));

            })
            .ConfigureFunctionsWorkerDefaults()
            .Build();

        Console.WriteLine("az_tw_website_functions function app is starting...");

        host.Run();
    }
}