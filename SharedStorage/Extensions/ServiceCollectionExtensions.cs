using Microsoft.Extensions.DependencyInjection;
using SharedStorage.Services.BaseServices;
using SharedStorage.Services.Media;
using SharedStorage.Services.Media.Handlers;
using SharedStorage.Services.Email;
using SharedStorage.Environment;
using Utils;

namespace SharedStorage.Extensions;

public static class ServiceCollectionExtensions
{
  /// <summary>
  /// Adds media services and handlers to the dependency injection container
  /// </summary>
  public static IServiceCollection AddMediaServices(this IServiceCollection services)
  {
    // No need to register storage services here as they should already be registered by AddStorageServices
    // We'll just check if they exist and only register if they don't
    if (services.All(s => s.ServiceType != typeof(IBlobStorageService)))
    {
      // Fall back to registering them here if for some reason they weren't registered
      services.AddStorageServices();
    }

    // Register media conversion services
    services.AddSingleton<IThumbnailService, ThumbnailService>();
    services.AddSingleton<IImageService, ImageConversionService>();
    services.AddSingleton<IVideoThumbnailService, BasicVideoThumbnailService>();
    services.AddSingleton<IDocumentConversionService, DocumentConversionService>();

    // Register media handlers
    services.AddSingleton<IMediaTypeHandler, ImageHandler>();
    services.AddSingleton<IMediaTypeHandler, VideoHandler>();
    services.AddSingleton<IMediaTypeHandler, DocumentHandler>();

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

    // Register email service
    services.AddScoped<IEmailService, EmailService>();

    // Register environment services
    services.AddScoped<IAppMode, DefaultAppMode>();

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
      // Look for StorageAccountName first, then fall back to AZURE_STORAGE_ACCOUNT_NAME for compatibility
      var storageAccountName = System.Environment.GetEnvironmentVariable("StorageAccountName")
              ?? System.Environment.GetEnvironmentVariable("AZURE_STORAGE_ACCOUNT_NAME")
              ?? throw new InvalidOperationException("Storage account name environment variable is required (StorageAccountName or AZURE_STORAGE_ACCOUNT_NAME)");
      var logger = provider.GetRequiredService<IAppInsightsLogger<BlobStorageService>>();
      return new BlobStorageService(storageAccountName, logger);
    });

    services.AddSingleton<ITableStorageService>(provider =>
    {
      // Look for StorageAccountName first, then fall back to AZURE_STORAGE_ACCOUNT_NAME for compatibility
      var storageAccountName = System.Environment.GetEnvironmentVariable("StorageAccountName")
              ?? System.Environment.GetEnvironmentVariable("AZURE_STORAGE_ACCOUNT_NAME")
              ?? throw new InvalidOperationException("Storage account name environment variable is required (StorageAccountName or AZURE_STORAGE_ACCOUNT_NAME)");
      var logger = provider.GetRequiredService<IAppInsightsLogger<TableStorageService>>();
      return new TableStorageService(storageAccountName, logger);
    });

    return services;
  }
}
