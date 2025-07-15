using SharedStorage.Models;
using SharedStorage.Services.BaseServices;
using SharedStorage.Services.Media;
using Utils;
using Utils.Constants;

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

      // Create the container name based on content section
      var containerName = ContentNameResolver.GetBlobContainerName(ContentSections.Blog, AssetType.Images);

      // Store original stream in memory so we can use it multiple times
      using var memoryStream = new MemoryStream();
      await stream.CopyToAsync(memoryStream);
      var originalStreamData = memoryStream.ToArray();

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

      var conversionResult = await _imageService.ConvertToWebPAsync(conversionStream, maxWidth: 2048, maxHeight: 2048, quality: 85);

      // Upload optimized image to blob storage with content relationship if provided
      _logger.LogInformation("Uploading optimized image to blob storage: {BlobName}", originalBlobName);
      var mediaReference = await _blobStorageService.UploadBlobAsync(containerName, originalBlobName, conversionResult.Content, contentId, relatedContentType);

      // Generate thumbnail from the original stream data
      using var thumbnailStream = new MemoryStream(originalStreamData);
      var thumbnailResult = await _thumbnailService.GenerateWebPThumbnailAsync(thumbnailStream);

      // Upload thumbnail to blob storage
      _logger.LogInformation("Uploading thumbnail to blob storage: {ThumbnailBlobName}", thumbnailBlobName);
      await _blobStorageService.UploadBlobAsync(containerName, thumbnailBlobName, thumbnailResult.Content);

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
        ThumbnailUrl = mediaReference.ThumbnailCdnUrl, // Use thumbnail CDN URL from MediaReference
        ContentType = "image/webp", // Always WebP after conversion
        Width = width,
        Height = height,
        UploadedAt = DateTime.UtcNow,
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

      // Delete blobs from storage (both original and thumbnail)
      var containerName = ContentNameResolver.GetBlobContainerName(ContentSections.Blog, AssetType.Images);

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