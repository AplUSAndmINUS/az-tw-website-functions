using Azure.Storage.Blobs;
using SharedStorage.Services.BaseServices;
using Utils;

namespace SharedStorage.Services.MediaServices;

public interface IMediaService
{
  Task UploadImageAsync(string slug, string imageUrl, string? description = null);
  Task UploadMediaAsync(string slug, string mediaUrl, string? description = null);
}

public abstract class MediaService : IMediaService
{
  protected readonly IBlobStorageService _blobStorageService;
  protected readonly IAppInsightsLogger<MediaService> _appLogger;

  protected MediaService(IBlobStorageService blobStorageService, IAppInsightsLogger<MediaService> appLogger)
  {
    _blobStorageService = blobStorageService ?? throw new ArgumentNullException(nameof(blobStorageService));
    _appLogger = appLogger ?? throw new ArgumentNullException(nameof(appLogger));
  }

  public abstract Task UploadImageAsync(string slug, string imageUrl, string? description = null);
  public abstract Task UploadMediaAsync(string slug, string mediaUrl, string? description = null);
}