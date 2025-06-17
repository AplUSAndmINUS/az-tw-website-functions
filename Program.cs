using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.ApplicationInsights.WorkerService;
using SharedStorage.Services;
using Utils;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;
        var storageAccountName = configuration["StorageAccountName"];

        if (string.IsNullOrWhiteSpace(storageAccountName))
            throw new InvalidOperationException("Missing StorageAccountName in configuration.");

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

        services.AddSingleton<AppInsightsLogger>();

        services
            .AddApplicationInsightsTelemetryWorkerService();
    })
    .Build();

Console.WriteLine("BlogPosts function app is starting...");

host.Run();
