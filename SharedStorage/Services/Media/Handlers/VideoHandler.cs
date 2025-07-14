using SharedStorage.Models;
using SharedStorage.Services.BaseServices;
using SharedStorage.Services.Media;
using Utils;
using Utils.Constants;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Webp;

namespace SharedStorage.Services.Media.Handlers;

public class VideoHandler : MediaHandler, IMediaTypeHandler
{
  private readonly IBlobStorageService _blobStorageService;
  private readonly IVideoThumbnailService _videoThumbnailService;
  private readonly IAppInsightsLogger<VideoHandler> _logger;

  public override string SupportedType => "video";

  public VideoHandler(
      IBlobStorageService blobStorageService,
      IVideoThumbnailService videoThumbnailService,
      IAppInsightsLogger<VideoHandler> logger) : base("video")
  {
    _blobStorageService = blobStorageService ?? throw new ArgumentNullException(nameof(blobStorageService));
    _videoThumbnailService = videoThumbnailService ?? throw new ArgumentNullException(nameof(videoThumbnailService));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  public override async Task<MediaEntity> UploadAsync(Stream stream, string fileName, string contentType, string? authorId = null)
  {
    try
    {
      _logger.LogInformation("Starting video upload for file: {FileName}", fileName);

      if (stream == null || !stream.CanRead)
        throw new ArgumentException("Stream must be readable", nameof(stream));

      var mediaId = Guid.NewGuid().ToString();
      var videoBlobName = $"videos/{mediaId}/{fileName}";
      var thumbnailBlobName = $"videos/{mediaId}/thumb_{Path.GetFileNameWithoutExtension(fileName)}.webp";

      // Create the container name based on content section
      var containerName = ContentNameResolver.GetBlobContainerName(ContentSections.Blog, AssetType.Video);

      // Upload video to blob storage
      _logger.LogInformation("Uploading video to blob storage: {BlobName}", videoBlobName);
      var mediaReference = await _blobStorageService.UploadBlobAsync(containerName, videoBlobName, stream);

      // Generate video thumbnail (placeholder for now)
      // In production, this would extract an actual frame from the video
      var thumbnailResult = await _videoThumbnailService.CreatePlaceholderThumbnailAsync();
      var thumbnailReference = await _blobStorageService.UploadBlobAsync(containerName, thumbnailBlobName, thumbnailResult.Content);

      // Get video metadata
      var videoMetadata = await _videoThumbnailService.GetVideoMetadataAsync(stream);

      // Create video entity
      var videoEntity = new VideoEntity
      {
        Id = mediaId,
        PartitionKey = authorId ?? "system",
        RowKey = mediaId,
        AuthorId = authorId ?? "system",
        Filename = fileName,
        MediaType = "video",
        Url = mediaReference.CdnUrl,
        ThumbnailUrl = thumbnailReference.CdnUrl,
        ContentType = contentType,
        Width = videoMetadata.Width,
        Height = videoMetadata.Height,
        UploadedAt = DateTime.UtcNow,
        Resolution = $"{videoMetadata.Width}x{videoMetadata.Height}",
        VidPurpose = "introVideo" // Default purpose
      };

      _logger.LogInformation("Successfully uploaded video {MediaId} with file {FileName}",
          mediaId, fileName);

      return videoEntity;
    }
    catch (Exception ex)
    {
      _logger.LogError("Failed to upload video {FileName}: {Error}", ex, fileName, ex.Message);
      throw;
    }
  }

  public override Task<MediaEntity> GetAsync(string id)
  {
    // This method is handled by the MediaService itself via table storage
    // Handlers are primarily for upload/delete operations with blob storage
    throw new NotSupportedException("Get operations are handled by MediaService directly");
  }

  public override Task<IEnumerable<MediaEntity>> GetAllAsync(string? authorSlug = null, int? limit = null)
  {
    // This method is handled by the MediaService itself via table storage
    // Handlers are primarily for upload/delete operations with blob storage
    throw new NotSupportedException("Get operations are handled by MediaService directly");
  }

  public override async Task<bool> DeleteAsync(string id)
  {
    try
    {
      _logger.LogInformation("Deleting video with ID: {MediaId}", id);

      // Delete video blob from storage
      var containerName = ContentNameResolver.GetBlobContainerName(ContentSections.Blog, AssetType.Video);

      // Delete all blobs associated with this video
      await DeleteVideoBlobsAsync(containerName, id);

      _logger.LogInformation("Successfully deleted video blobs for media ID: {MediaId}", id);
      return true;
    }
    catch (Exception ex)
    {
      _logger.LogError("Failed to delete video {MediaId}: {Error}", ex, id, ex.Message);
      return false;
    }
  }

  private async Task DeleteVideoBlobsAsync(string containerName, string mediaId)
  {
    try
    {
      // Get all blobs with the media ID prefix
      var prefix = $"videos/{mediaId}/";
      var blobsResult = await _blobStorageService.GetBlobsAsync(containerName, prefix);

      // Delete each blob
      foreach (var blobClient in blobsResult.Blobs)
      {
        await _blobStorageService.DeleteBlobAsync(containerName, blobClient.Name);
        _logger.LogInformation("Deleted video blob: {BlobName}", blobClient.Name);
      }
    }
    catch (Exception ex)
    {
      _logger.LogError("Failed to delete video blobs for media ID {MediaId}: {Error}", ex, mediaId, ex.Message);
      throw;
    }
  }

  private static string GetVideoResolution(string contentType)
  {
    // Simple resolution mapping based on content type
    // In production, you'd analyze the video file to get actual resolution
    return contentType switch
    {
      "video/mp4" => "1080p",
      "video/webm" => "720p",
      "video/quicktime" => "1080p",
      _ => "720p"
    };
  }
}