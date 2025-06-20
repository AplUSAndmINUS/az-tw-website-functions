using SharedStorage.Services;
using Utils;
using Utils.Validation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace az_tw_website_functions.Functions
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices((context, services) =>
                {
                    var configuration = context.Configuration;
                    var storageAccountName = configuration["StorageAccountName"];

                    if (string.IsNullOrWhiteSpace(storageAccountName))
                        throw new InvalidOperationException("Missing StorageAccountName in configuration.");
                    
                    // Register these services first
                    services.AddSingleton<IImageService, ImageConversionService>();
                    services.AddSingleton<IThumbnailService, ThumbnailService>();

                    services.AddSingleton<IBlobStorageService>(sp =>
                    {
                        var logger = sp.GetRequiredService<ILogger<BlobStorageService>>();
                        var imageConversionService = sp.GetRequiredService<IImageService>();
                        var thumbnailService = sp.GetRequiredService<IThumbnailService>();

                        return new BlobStorageService(storageAccountName!, logger, imageConversionService, thumbnailService);
                    });

                    services.AddSingleton<ITableStorageService>(sp =>
                    {
                        var logger = sp.GetRequiredService<ILogger<TableStorageService>>();
                        return new TableStorageService(storageAccountName!, logger);
                    });
                    
                    services.AddSingleton<IAPIKeyValidator>(sp =>
                    {
                        var validApiKey = configuration["X_API_ENVIRONMENT_KEY"];
                        if (string.IsNullOrWhiteSpace(validApiKey))
                            throw new InvalidOperationException("Missing X_API_ENVIRONMENT_KEY in configuration.");

                        return new ApiKeyValidator(validApiKey);
                    });

                    services.AddSingleton<AppInsightsLogger>();

                    services.AddApplicationInsightsTelemetryWorkerService();
                })
                .Build();

            Console.WriteLine("az_tw_website_functions function app is starting...");

            host.Run();
        }
    }
}
