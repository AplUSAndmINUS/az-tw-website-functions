using SharedStorage.Models;
using SharedStorage.Services.BaseServices;
using SharedStorage.Services.Media;
using Utils;
using Utils.Constants;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Webp;
using Utils.Extensions;

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

  public override async Task<MediaEntity> UploadAsync(Stream stream, string fileName, string contentType, string? authorId = null, string? contentId = null, string? relatedContentType = null)
  {
    try
    {
      _logger.LogInformation("Starting video upload for file: {FileName}", fileName);

      if (stream == null || !stream.CanRead)
        throw new ArgumentException("Stream must be readable", nameof(stream));

      // Store original stream in memory so we can use it multiple times
      using var memoryStream = new MemoryStream();
      await stream.CopyToAsync(memoryStream);
      memoryStream.Position = 0;

      _logger.LogInformation("Copied video stream to memory: {Length} bytes", memoryStream.Length);

      // Get video metadata first to determine resolution and conversion needs
      var videoMetadata = await _videoThumbnailService.GetVideoMetadataAsync(memoryStream);
      memoryStream.Position = 0;

      // Convert video to MP4 if it's not already (this would call FFmpeg in a real implementation)
      var mp4FileName = Path.ChangeExtension(fileName, ".mp4");
      var mediaId = Guid.NewGuid().ToString();
      var videoBlobName = $"videos/{mediaId}/{mp4FileName}";
      var thumbnailBlobName = $"videos/{mediaId}/thumb_{Path.GetFileNameWithoutExtension(mp4FileName)}.webp";

      _logger.LogInformation("Original video dimensions: {Width}x{Height}", videoMetadata.Width, videoMetadata.Height);

      // Determine if video should be resized based on width
      int targetWidth = videoMetadata.Width;
      int targetHeight = videoMetadata.Height;
      bool needsResize = false;

      if (videoMetadata.Width >= 1000)
      {
        // Upscale to 1920x1080 for high quality videos
        targetWidth = 1920;
        targetHeight = 1080;
        needsResize = true;
        _logger.LogInformation("Video will be upscaled to 1920x1080");
      }
      else if (videoMetadata.Width > 700 && videoMetadata.Width < 1000)
      {
        // Medium quality videos get upscaled to 1080p
        targetWidth = 1280;
        targetHeight = 720;
        needsResize = true;
        _logger.LogInformation("Video will be upscaled to 720p");
      }
      else
      {
        // Small videos stay as-is
        _logger.LogInformation("Video resolution will be preserved at {Width}x{Height}", targetWidth, targetHeight);
      }

      // In a production implementation, here's where we'd call FFmpeg to convert the video
      // For now, we'll simulate the conversion by just setting the content type

      // Check if mock storage is enabled
      var useMockStorage = System.Environment.GetEnvironmentVariable("USE_MOCK_STORAGE")?.ToLowerInvariant() == "true";
      _logger.LogInformation("USE_MOCK_STORAGE environment variable is set to: {UseMockStorage}", useMockStorage ? "true" : "false");

      // Create the container name based on content section, explicitly passing the mock storage flag
      var containerName = ContentNameResolver.GetBlobContainerName(ContentSections.Blog, AssetType.Video, useMockStorage);

      // Upload video to blob storage with content relationship if provided
      _logger.LogInformation("Uploading video to blob storage: {BlobName}", videoBlobName);
      var mediaReference = await _blobStorageService.UploadBlobAsync(containerName, videoBlobName, memoryStream, contentId, relatedContentType);

      // Generate video thumbnail (placeholder for now)
      // In production, this would extract an actual frame from the video
      var thumbnailResult = await _videoThumbnailService.CreatePlaceholderThumbnailAsync();
      await _blobStorageService.UploadBlobAsync(containerName, thumbnailBlobName, thumbnailResult.Content);

      // Create video entity using CDN URLs from mediaReference
      var videoEntity = new VideoEntity
      {
        Id = mediaId,
        PartitionKey = authorId ?? "system",
        RowKey = mediaId,
        AuthorId = authorId ?? "system",
        Filename = mp4FileName, // Always use MP4 filename
        MediaType = "video",
        Url = mediaReference.CdnUrl, // Use CDN URL from MediaReference
        ThumbnailUrl = mediaReference.ThumbnailCdnUrl, // Use thumbnail CDN URL from MediaReference
        ContentType = "video/mp4", // Always MP4 content type
        Width = needsResize ? targetWidth : videoMetadata.Width, // Use target width if resized
        Height = needsResize ? targetHeight : videoMetadata.Height, // Use target height if resized
        UploadedAt = DateTime.UtcNow.EnsureValidStorageDate(),
        Resolution = needsResize ? $"{targetWidth}x{targetHeight}" : $"{videoMetadata.Width}x{videoMetadata.Height}",
        VidPurpose = "introVideo", // Default purpose
        ContentId = contentId ?? string.Empty, // Set ContentId if provided
        RelatedContentType = relatedContentType ?? string.Empty // Set RelatedContentType if provided
      };

      // Log detailed video processing information
      _logger.LogInformation(
          "Successfully uploaded video {MediaId} with file {FileName}. Original format: {OriginalFormat}, " +
          "Original size: {OriginalSize}, Final size: {FinalSize}, Content-Type: {ContentType}",
          mediaId,
          mp4FileName,
          GetVideoMimeType(fileName),
          $"{videoMetadata.Width}x{videoMetadata.Height}",
          videoEntity.Resolution,
          videoEntity.ContentType);

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

      // Check if mock storage is enabled
      var useMockStorage = System.Environment.GetEnvironmentVariable("USE_MOCK_STORAGE")?.ToLowerInvariant() == "true";
      _logger.LogInformation("USE_MOCK_STORAGE environment variable is set to: {UseMockStorage}", useMockStorage ? "true" : "false");

      // Delete video blob from storage
      var containerName = ContentNameResolver.GetBlobContainerName(ContentSections.Blog, AssetType.Video, useMockStorage);

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

  private static string GetVideoResolution(int width, int height)
  {
    // Determine resolution category based on width
    if (width >= 1920 && height >= 1080)
      return "1080p";
    else if (width >= 1280 && height >= 720)
      return "720p";
    else if (width >= 854 && height >= 480)
      return "480p";
    else if (width >= 640 && height >= 360)
      return "360p";
    else
      return $"{width}x{height}";
  }

  private static string GetVideoMimeType(string fileName)
  {
    // Get MIME type based on file extension
    return Path.GetExtension(fileName).ToLowerInvariant() switch
    {
      ".mp4" => "video/mp4",
      ".mpeg" => "video/mpeg",
      ".mpg" => "video/mpeg",
      ".avi" => "video/x-msvideo",
      ".mov" => "video/quicktime",
      ".qt" => "video/quicktime",
      ".wmv" => "video/x-ms-wmv",
      ".flv" => "video/x-flv",
      ".3gp" => "video/3gpp",
      ".mkv" => "video/x-matroska",
      ".webm" => "video/webm",
      _ => "video/mp4" // Default to MP4
    };
  }
}