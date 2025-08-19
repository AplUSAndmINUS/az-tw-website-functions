using SharedStorage.Models;
using SharedStorage.Services.BaseServices;
using SharedStorage.Services.Media;
using Utils;
using Utils.Constants;
using Utils.Extensions;

namespace SharedStorage.Services.Media.Handlers;

public class ImageHandler : MediaHandler, IMediaTypeHandler
{
  private readonly IBlobStorageService _blobStorageService;
  private readonly IThumbnailService _thumbnailService;
  private readonly IImageService _imageService;
  private readonly IAppInsightsLogger<ImageHandler> _logger;

  public override string SupportedType => "image";

  public ImageHandler(
      IBlobStorageService blobStorageService,
      IThumbnailService thumbnailService,
      IImageService imageService,
      IAppInsightsLogger<ImageHandler> logger) : base("image")
  {
    _blobStorageService = blobStorageService ?? throw new ArgumentNullException(nameof(blobStorageService));
    _thumbnailService = thumbnailService ?? throw new ArgumentNullException(nameof(thumbnailService));
    _imageService = imageService ?? throw new ArgumentNullException(nameof(imageService));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  public override async Task<MediaEntity> UploadAsync(Stream stream, string fileName, string contentType, string? authorId = null, string? contentId = null, string? relatedContentType = null)
  {
    try
    {
      _logger.LogInformation("Starting image upload for file: {FileName}", fileName);

      if (stream == null || !stream.CanRead)
        throw new ArgumentException("Stream must be readable", nameof(stream));

      if (!stream.CanSeek)
        throw new ArgumentException("Stream must be seekable for image processing", nameof(stream));

      _logger.LogInformation("Stream validation passed - Length: {Length}, Position: {Position}, CanRead: {CanRead}, CanSeek: {CanSeek}",
        stream.Length, stream.Position, stream.CanRead, stream.CanSeek);

      var mediaId = Guid.NewGuid().ToString();
      var webpFileName = Path.GetFileNameWithoutExtension(fileName) + ".webp"; // Convert to WebP
      var originalBlobName = $"images/{mediaId}/{webpFileName}";
      var thumbnailBlobName = $"images/{mediaId}/thumb_{webpFileName}";

      // Check if mock storage is enabled
      var useMockStorage = System.Environment.GetEnvironmentVariable("USE_MOCK_STORAGE")?.ToLowerInvariant() == "true";
      _logger.LogInformation("USE_MOCK_STORAGE environment variable is set to: {UseMockStorage}", useMockStorage ? "true" : "false");

      // Create the container name based on content section, explicitly passing the mock storage flag
      var containerName = ContentNameResolver.GetBlobContainerName(ContentSections.Blog, AssetType.Images, useMockStorage);

      // Store original stream in memory so we can use it multiple times - using a safer approach
      byte[] originalStreamData;
      using (var memoryStream = new MemoryStream())
      {
        await stream.CopyToAsync(memoryStream);
        originalStreamData = memoryStream.ToArray();
      }

      _logger.LogInformation("Copied stream to memory: {Length} bytes", originalStreamData.Length);

      // Validate we have actual image data
      if (originalStreamData.Length == 0)
      {
        throw new ArgumentException("Stream contains no data", nameof(stream));
      }

      if (originalStreamData.Length < 4)
      {
        throw new ArgumentException($"Stream too small to be a valid image ({originalStreamData.Length} bytes)", nameof(stream));
      }

      // Add extra safety checks before conversion
      _logger.LogInformation("Checking image data integrity before conversion");

      // Perform basic image header validation to ensure data integrity
      bool isValidImage = false;
      if (originalStreamData.Length >= 4)
      {
        // Check for common image file signatures
        if ((originalStreamData[0] == 0xFF && originalStreamData[1] == 0xD8 && originalStreamData[2] == 0xFF) || // JPEG
            (originalStreamData[0] == 0x89 && originalStreamData[1] == 0x50 && originalStreamData[2] == 0x4E && originalStreamData[3] == 0x47) || // PNG
            (originalStreamData[0] == 0x47 && originalStreamData[1] == 0x49 && originalStreamData[2] == 0x46 && originalStreamData[3] == 0x38)) // GIF
        {
          isValidImage = true;
        }
      }

      if (!isValidImage)
      {
        _logger.LogWarning("Image data doesn't have a valid header signature. This may cause conversion issues.");
      }

      // Convert and optimize the image using a fresh, clean stream
      _logger.LogInformation("Converting image to optimized WebP format");
      using var conversionStream = new MemoryStream(originalStreamData);

      // Verify the stream is properly created and seekable
      if (!conversionStream.CanRead || !conversionStream.CanSeek)
      {
        throw new InvalidOperationException("Created conversion stream is not readable or seekable");
      }

      _logger.LogInformation("Conversion stream created - Length: {Length}, Position: {Position}, CanRead: {CanRead}, CanSeek: {CanSeek}",
        conversionStream.Length, conversionStream.Position, conversionStream.CanRead, conversionStream.CanSeek);

      // Use a more conservative approach for conversion with error handling
      ImageConversionResult conversionResult;
      try
      {
        conversionResult = await _imageService.ConvertToWebPAsync(conversionStream, maxWidth: 2048, maxHeight: 2048, quality: 85, fileName: fileName);
      }
      catch (Exception ex)
      {
        _logger.LogError("Image conversion failed with standard parameters, trying fallback approach with lower quality", ex);

        // Reset the stream and try again with more conservative parameters
        conversionStream.Position = 0;
        try
        {
          conversionResult = await _imageService.ConvertToWebPAsync(conversionStream, maxWidth: 1024, maxHeight: 1024, quality: 70, fileName: fileName);
        }
        catch (Exception fallbackEx)
        {
          _logger.LogError("Fallback image conversion also failed", fallbackEx);
          throw new InvalidOperationException("Unable to process image file. The image may be corrupted or in an unsupported format.", fallbackEx);
        }
      }

      // Upload optimized image to blob storage with content relationship if provided
      _logger.LogInformation("Uploading optimized image to blob storage: {BlobName}", originalBlobName);
      _logger.LogInformation("Upload parameters - Container: {ContainerName}, ContentId: {ContentId}, RelatedContentType: {RelatedContentType}",
          containerName, contentId ?? "null", relatedContentType ?? "null");

      // Pass null for contentId and relatedContentType if they're empty strings to avoid issues
      string? safeContentId = string.IsNullOrEmpty(contentId) ? null : contentId;
      string? safeRelatedContentType = string.IsNullOrEmpty(relatedContentType) ? null : relatedContentType;

      var mediaReference = await _blobStorageService.UploadBlobAsync(containerName, originalBlobName, conversionResult.Content, safeContentId, safeRelatedContentType);

      // Generate thumbnail from the original stream data (with graceful fallback)
      string? thumbnailUrl = null;
      try
      {
        using var thumbnailStream = new MemoryStream(originalStreamData);
        var thumbnailResult = await _thumbnailService.GenerateWebPThumbnailAsync(thumbnailStream);

        // Upload thumbnail to blob storage
        _logger.LogInformation("Uploading thumbnail to blob storage: {ThumbnailBlobName}", thumbnailBlobName);
        // Pass null explicitly for content relationships for thumbnail
        var thumbnailRef = await _blobStorageService.UploadBlobAsync(containerName, thumbnailBlobName, thumbnailResult.Content, null, null);
        thumbnailUrl = thumbnailRef.ThumbnailCdnUrl;
      }
      catch (Exception thumbEx)
      {
        // Log error but continue without thumbnail
        _logger.LogWarning("Failed to generate or upload thumbnail, proceeding without it: {Error}", thumbEx.Message);
      }

      // Get image dimensions from conversion result
      var (width, height) = (conversionResult.Width, conversionResult.Height);

      // Create media entity using CDN URLs from mediaReference
      var imageEntity = new ImageEntity
      {
        Id = mediaId,
        PartitionKey = authorId ?? "system",
        RowKey = mediaId,
        AuthorId = authorId ?? "system",
        Filename = webpFileName,
        MediaType = "image",
        Url = mediaReference.CdnUrl, // Use CDN URL from MediaReference
        ThumbnailUrl = thumbnailUrl ?? mediaReference.CdnUrl, // Use thumbnail URL if available, fall back to main URL
        ContentType = "image/webp", // Always WebP after conversion
        Width = width,
        Height = height,
        UploadedAt = DateTime.UtcNow.EnsureValidStorageDate(),
        ContentId = contentId ?? string.Empty, // Set ContentId if provided
        RelatedContentType = relatedContentType ?? string.Empty // Set RelatedContentType if provided
      };

      _logger.LogInformation("Successfully uploaded image {MediaId} with dimensions {Width}x{Height}",
          mediaId, width, height);

      return imageEntity;
    }
    catch (ArgumentException ex)
    {
      _logger.LogError("Invalid argument in image upload for {FileName}: {Error}", ex, fileName, ex.Message);
      throw;
    }
    catch (InvalidOperationException ex)
    {
      _logger.LogError("Invalid operation in image upload for {FileName}: {Error}", ex, fileName, ex.Message);
      throw;
    }
    catch (Exception ex)
    {
      _logger.LogError("Failed to upload image {FileName}: {Error}", ex, fileName, ex.Message);
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
      _logger.LogInformation("Deleting image with ID: {MediaId}", id);

      // Check if mock storage is enabled
      var useMockStorage = System.Environment.GetEnvironmentVariable("USE_MOCK_STORAGE")?.ToLowerInvariant() == "true";
      _logger.LogInformation("USE_MOCK_STORAGE environment variable is set to: {UseMockStorage}", useMockStorage ? "true" : "false");

      // Delete blobs from storage (both original and thumbnail)
      var containerName = ContentNameResolver.GetBlobContainerName(ContentSections.Blog, AssetType.Images, useMockStorage);

      // For now, we'll need to implement blob enumeration to find files to delete
      // This is a simplified approach - in production you'd store the blob names in metadata
      await DeleteImageBlobsAsync(containerName, id);

      _logger.LogInformation("Successfully deleted image blobs for media ID: {MediaId}", id);
      return true;
    }
    catch (Exception ex)
    {
      _logger.LogError("Failed to delete image {MediaId}: {Error}", ex, id, ex.Message);
      return false;
    }
  }

  private async Task DeleteImageBlobsAsync(string containerName, string mediaId)
  {
    try
    {
      // Get all blobs with the media ID prefix
      var prefix = $"images/{mediaId}/";
      var blobsResult = await _blobStorageService.GetBlobsAsync(containerName, prefix);

      // Delete each blob
      foreach (var blobClient in blobsResult.Blobs)
      {
        await _blobStorageService.DeleteBlobAsync(containerName, blobClient.Name);
        _logger.LogInformation("Deleted blob: {BlobName}", blobClient.Name);
      }
    }
    catch (Exception ex)
    {
      _logger.LogError("Failed to delete image blobs for media ID {MediaId}: {Error}", ex, mediaId, ex.Message);
      throw;
    }
  }
}