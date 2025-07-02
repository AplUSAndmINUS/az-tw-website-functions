using Microsoft.Extensions.DependencyInjection;
using SharedStorage.Services.BaseServices;
using SharedStorage.Services.Media;
using SharedStorage.Services.Media.Handlers;
using SharedStorage.Services.MediaServices;
using Utils;

namespace SharedStorage.Extensions;

public static class ServiceCollectionExtensions
{
  /// <summary>
  /// Adds media services and handlers to the dependency injection container
  /// </summary>
  public static IServiceCollection AddMediaServices(this IServiceCollection services)
  {
    // Register base storage services (if not already registered by AddStorageServices)
    services.AddSingleton<IBlobStorageService>(provider =>
    {
      var storageAccountName = System.Environment.GetEnvironmentVariable("StorageAccountName")
              ?? throw new InvalidOperationException("StorageAccountName environment variable is required");
      var logger = provider.GetRequiredService<IAppInsightsLogger<BlobStorageService>>();
      return new BlobStorageService(storageAccountName, logger);
    });

    services.AddSingleton<ITableStorageService>(provider =>
    {
      var storageAccountName = System.Environment.GetEnvironmentVariable("StorageAccountName")
              ?? throw new InvalidOperationException("StorageAccountName environment variable is required");
      var logger = provider.GetRequiredService<IAppInsightsLogger<TableStorageService>>();
      return new TableStorageService(storageAccountName, logger);
    });

    // Register media conversion services
    services.AddSingleton<IThumbnailService, ThumbnailService>();
    services.AddSingleton<IImageService, ImageConversionService>();
    services.AddSingleton<IVideoThumbnailService, BasicVideoThumbnailService>();

    // Register media handlers
    services.AddSingleton<IMediaTypeHandler, ImageHandler>();
    services.AddSingleton<IMediaTypeHandler, VideoHandler>();

    // Register main media service
    services.AddSingleton<IMediaService>(provider =>
    {
      var handlers = provider.GetServices<IMediaTypeHandler>();
      var tableStorage = provider.GetRequiredService<ITableStorageService>();
      var logger = provider.GetRequiredService<IAppInsightsLogger<MediaService>>();
      return new MediaService(handlers, tableStorage, logger);
    });

    return services;
  }

  /// <summary>
  /// Adds content services to the dependency injection container
  /// </summary>
  public static IServiceCollection AddContentServices(this IServiceCollection services)
  {
    // Register shared content services here if any
    // Function-specific services should be registered in Functions.Extensions

    return services;
  }

  /// <summary>
  /// Adds the core storage services needed by content and media services
  /// </summary>
  public static IServiceCollection AddStorageServices(this IServiceCollection services)
  {
    // Register base storage services
    services.AddSingleton<IBlobStorageService>(provider =>
    {
      var storageAccountName = System.Environment.GetEnvironmentVariable("AZURE_STORAGE_ACCOUNT_NAME")
              ?? throw new InvalidOperationException("AZURE_STORAGE_ACCOUNT_NAME environment variable is required");
      var logger = provider.GetRequiredService<IAppInsightsLogger<BlobStorageService>>();
      return new BlobStorageService(storageAccountName, logger);
    });

    services.AddSingleton<ITableStorageService>(provider =>
    {
      var storageAccountName = System.Environment.GetEnvironmentVariable("AZURE_STORAGE_ACCOUNT_NAME")
              ?? throw new InvalidOperationException("AZURE_STORAGE_ACCOUNT_NAME environment variable is required");
      var logger = provider.GetRequiredService<IAppInsightsLogger<TableStorageService>>();
      return new TableStorageService(storageAccountName, logger);
    });

    return services;
  }
}
