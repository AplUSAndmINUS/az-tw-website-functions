using SharedStorage.Services.BaseServices;
using SharedStorage.Services.Media.Handlers;
using SharedStorage.Models;
using Utils;
using Utils.Extensions;

namespace SharedStorage.Services.Media;

public interface IMediaService
{
  // Core media operations
  Task<MediaEntity> UploadMediaAsync(string mediaType, Stream stream, string fileName, string? authorId = null, string? description = null, string? altText = null, string? purpose = null, string? contentId = null, string? relatedContentType = null);
  Task<MediaEntity?> GetMediaAsync(string mediaId);
  Task<IEnumerable<MediaEntity>> GetMediaByAuthorAsync(string authorId, string? mediaType = null, int? limit = null);
  Task<IEnumerable<MediaEntity>> GetMediaByTypeAsync(string mediaType, int? limit = null);
  Task<IEnumerable<MediaEntity>> GetMediaByTypeAsync(string mediaType, int? limit, int offset);
  Task<IEnumerable<MediaEntity>> GetMediaByContentIdAsync(string contentId, string? relatedContentType = null, int? limit = null);
  Task<bool> DeleteMediaAsync(string mediaId);

  // Bulk operations
  Task<IEnumerable<MediaEntity>> GetMediaBatchAsync(string[] mediaIds);
  Task<int> DeleteMediaBatchAsync(string[] mediaIds);

  // Specialized operations
  Task<MediaEntity> UploadImageAsync(Stream stream, string fileName, string? authorId = null, string? description = null, string? altText = null, string? purpose = "coverImage", string? contentId = null, string? relatedContentType = null);
  Task<MediaEntity> UploadVideoAsync(Stream stream, string fileName, string? authorId = null, string? description = null, string? purpose = "introVideo", string? contentId = null, string? relatedContentType = null);

  // New methods for media gallery functionality
  Task<IEnumerable<MediaEntity>> GetAllMediaAsync(int? limit = null, int offset = 0);
  Task<IEnumerable<MediaEntity>> GetMediaByPlatformAsync(string platform, int? limit = null, int offset = 0);
}

public partial class MediaService : IMediaService
{
  private readonly Dictionary<string, IMediaTypeHandler> _handlers;
  private readonly ITableStorageService _tableStorageService;
  private readonly IAppInsightsLogger<MediaService> _appLogger;
  private readonly string _tableName;

  public MediaService(
    IEnumerable<IMediaTypeHandler> handlers,
    ITableStorageService tableStorageService,
    IAppInsightsLogger<MediaService> appLogger)
  {
    _handlers = handlers.ToDictionary(h => h.SupportedType.ToLowerInvariant());
    _tableStorageService = tableStorageService ?? throw new ArgumentNullException(nameof(tableStorageService));
    _appLogger = appLogger ?? throw new ArgumentNullException(nameof(appLogger));

    // Get table name from environment variable with fallback
    var useMock = System.Environment.GetEnvironmentVariable("USE_MOCK_STORAGE")?.ToLowerInvariant() == "true";
    var envTableName = System.Environment.GetEnvironmentVariable("MEDIA_TABLE_NAME");

    string resolvedTableName;
    if (!string.IsNullOrEmpty(envTableName))
    {
      // If an explicit table name is provided via environment variable, use that
      resolvedTableName = useMock ? $"mock{envTableName}" : envTableName;
    }
    else
    {
      // Otherwise use ContentNameResolver for consistent naming
      resolvedTableName = Utils.ContentNameResolver.GetTableName(Utils.Constants.ContentSections.Blog, Utils.Constants.AssetType.Media, useMock);
    }

    _tableName = SharedStorage.Validators.TableNameValidator.ValidateTableName(resolvedTableName);

    _appLogger.LogInformation("MediaService initialized with {HandlerCount} handlers using table {TableName}",
      _handlers.Count, _tableName);
  }

  public async Task<MediaEntity> UploadMediaAsync(string mediaType, Stream stream, string fileName, string? authorId = null, string? description = null, string? altText = null, string? purpose = null, string? contentId = null, string? relatedContentType = null)
  {
    _appLogger.LogInformation("Uploading media of type {MediaType}, file: {FileName}", mediaType, fileName);

    if (string.IsNullOrWhiteSpace(mediaType))
      throw new ArgumentException("Media type is required", nameof(mediaType));

    if (stream == null || !stream.CanRead)
      throw new ArgumentException("Stream must be readable", nameof(stream));

    if (string.IsNullOrWhiteSpace(fileName))
      throw new ArgumentException("File name is required", nameof(fileName));

    var normalizedType = mediaType.ToLowerInvariant();

    if (!_handlers.TryGetValue(normalizedType, out var handler))
    {
      _appLogger.LogError("Unsupported media type: {MediaType}", new InvalidOperationException($"Unsupported media type: {mediaType}"), mediaType);
      throw new InvalidOperationException($"Unsupported media type: {mediaType}");
    }

    try
    {
      // Use the appropriate handler to process and upload the media
      var mediaEntity = await handler.UploadAsync(stream, fileName, GetContentType(fileName), authorId, contentId, relatedContentType);

      // Set additional metadata
      if (!string.IsNullOrWhiteSpace(description))
        mediaEntity.Description = description;

      if (!string.IsNullOrWhiteSpace(altText))
        mediaEntity.AltText = altText;

      if (!string.IsNullOrWhiteSpace(purpose))
        mediaEntity.Purpose = purpose;

      // Save metadata to table storage
      await _tableStorageService.UpsertEntityAsync(_tableName, mediaEntity);

      _appLogger.LogInformation("Successfully uploaded media {MediaId} of type {MediaType}",
        mediaEntity.Id, mediaType);

      return mediaEntity;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Failed to upload media of type {MediaType}: {Error}", ex, mediaType, ex.Message);
      throw;
    }
  }

  public async Task<MediaEntity?> GetMediaAsync(string mediaId)
  {
    if (string.IsNullOrWhiteSpace(mediaId))
      throw new ArgumentException("Media ID is required", nameof(mediaId));

    try
    {
      // For now, we'll search across all partitions since we don't know the author
      // In a real implementation, you might want to index by media ID
      var filter = $"Id eq '{mediaId}'";
      var result = await _tableStorageService.GetEntitiesAsync(_tableName, filter, 1);

      if (!result.Entities.Any())
      {
        _appLogger.LogInformation("Media not found: {MediaId}", mediaId);
        return null;
      }

      var entityData = result.Entities.First();
      var mediaEntity = ConvertToMediaEntity(entityData);

      _appLogger.LogInformation("Retrieved media: {MediaId}", mediaId);
      return mediaEntity;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Failed to get media {MediaId}: {Error}", ex, mediaId, ex.Message);
      throw;
    }
  }

  public async Task<IEnumerable<MediaEntity>> GetMediaByAuthorAsync(string authorId, string? mediaType = null, int? limit = null)
  {
    if (string.IsNullOrWhiteSpace(authorId))
      throw new ArgumentException("Author ID is required", nameof(authorId));

    try
    {
      var filters = new List<string> { $"AuthorId eq '{authorId}'" };

      if (!string.IsNullOrWhiteSpace(mediaType))
        filters.Add($"MediaType eq '{mediaType.ToLowerInvariant()}'");

      var filter = string.Join(" and ", filters);
      var pageSize = Math.Min(limit ?? 50, 100); // Cap at 100 for performance

      var result = await _tableStorageService.GetEntitiesAsync(_tableName, filter, pageSize);
      var entities = result.Entities.Select(ConvertToMediaEntity).ToList();

      _appLogger.LogInformation("Retrieved {Count} media items for author {AuthorId}",
        entities.Count, authorId);

      return entities;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Failed to get media for author {AuthorId}: {Error}", ex, authorId, ex.Message);
      throw;
    }
  }

  public async Task<IEnumerable<MediaEntity>> GetMediaByTypeAsync(string mediaType, int? limit = null)
  {
    return await GetMediaByTypeAsync(mediaType, limit, 0);
  }

  public async Task<IEnumerable<MediaEntity>> GetMediaByTypeAsync(string mediaType, int? limit = null, int offset = 0)
  {
    if (string.IsNullOrWhiteSpace(mediaType))
      throw new ArgumentException("Media type is required", nameof(mediaType));

    try
    {
      var filter = $"MediaType eq '{mediaType.ToLowerInvariant()}'";
      var pageSize = Math.Min(limit ?? 50, 100);

      // Get entities with offset support
      var result = await _tableStorageService.GetEntitiesAsync(_tableName, filter, pageSize + offset);
      var entities = result.Entities.Skip(offset).Take(pageSize).Select(ConvertToMediaEntity).ToList();

      _appLogger.LogInformation("Retrieved {Count} media items of type {MediaType}",
        entities.Count, mediaType);

      return entities;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Failed to get media of type {MediaType}: {Error}", ex, mediaType, ex.Message);
      throw;
    }
  }

  public async Task<IEnumerable<MediaEntity>> GetMediaByContentIdAsync(string contentId, string? relatedContentType = null, int? limit = null)
  {
    if (string.IsNullOrWhiteSpace(contentId))
      throw new ArgumentException("Content ID is required", nameof(contentId));

    try
    {
      var filters = new List<string> { $"ContentId eq '{contentId}'" };

      if (!string.IsNullOrWhiteSpace(relatedContentType))
        filters.Add($"RelatedContentType eq '{relatedContentType}'");

      var filter = string.Join(" and ", filters);
      var pageSize = Math.Min(limit ?? 50, 100);

      var result = await _tableStorageService.GetEntitiesAsync(_tableName, filter, pageSize);
      var entities = result.Entities.Select(ConvertToMediaEntity).ToList();

      _appLogger.LogInformation("Retrieved {Count} media items for content ID {ContentId}",
        entities.Count, contentId);

      return entities;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Failed to get media for content ID {ContentId}: {Error}", ex, contentId, ex.Message);
      throw;
    }
  }

  public async Task<bool> DeleteMediaAsync(string mediaId)
  {
    if (string.IsNullOrWhiteSpace(mediaId))
      throw new ArgumentException("Media ID is required", nameof(mediaId));

    try
    {
      // First get the media entity to determine the handler and get storage info
      var mediaEntity = await GetMediaAsync(mediaId);
      if (mediaEntity == null)
      {
        _appLogger.LogWarning("Media not found for deletion: {MediaId}", mediaId);
        return false;
      }

      // Use the appropriate handler to delete the physical file
      if (_handlers.TryGetValue(mediaEntity.MediaType.ToLowerInvariant(), out var handler))
      {
        await handler.DeleteAsync(mediaId);
      }

      // Delete metadata from table storage
      await _tableStorageService.DeleteEntityAsync(_tableName, mediaEntity.PartitionKey, mediaEntity.RowKey);

      _appLogger.LogInformation("Successfully deleted media: {MediaId}", mediaId);
      return true;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Failed to delete media {MediaId}: {Error}", ex, mediaId, ex.Message);
      return false;
    }
  }

  public async Task<IEnumerable<MediaEntity>> GetMediaBatchAsync(string[] mediaIds)
  {
    if (mediaIds == null || mediaIds.Length == 0)
      return Enumerable.Empty<MediaEntity>();

    var results = new List<MediaEntity>();

    // Process in batches for better performance
    foreach (var mediaId in mediaIds)
    {
      var media = await GetMediaAsync(mediaId);
      if (media != null)
        results.Add(media);
    }

    _appLogger.LogInformation("Retrieved {Count} of {Total} requested media items",
      results.Count, mediaIds.Length);

    return results;
  }

  public async Task<int> DeleteMediaBatchAsync(string[] mediaIds)
  {
    if (mediaIds == null || mediaIds.Length == 0)
      return 0;

    var deletedCount = 0;

    foreach (var mediaId in mediaIds)
    {
      if (await DeleteMediaAsync(mediaId))
        deletedCount++;
    }

    _appLogger.LogInformation("Deleted {Count} of {Total} requested media items",
      deletedCount, mediaIds.Length);

    return deletedCount;
  }

  public async Task<MediaEntity> UploadImageAsync(Stream stream, string fileName, string? authorId = null, string? description = null, string? altText = null, string? purpose = "coverImage", string? contentId = null, string? relatedContentType = null)
  {
    return await UploadMediaAsync("image", stream, fileName, authorId, description, altText, purpose, contentId, relatedContentType);
  }

  public async Task<MediaEntity> UploadVideoAsync(Stream stream, string fileName, string? authorId = null, string? description = null, string? purpose = "introVideo", string? contentId = null, string? relatedContentType = null)
  {
    return await UploadMediaAsync("video", stream, fileName, authorId, description, null, purpose, contentId, relatedContentType);
  }

  private MediaEntity ConvertToMediaEntity(Azure.Data.Tables.TableEntity tableEntity)
  {
    // Convert the generic TableEntity back to our MediaEntity
    // This is a simplified conversion - you might want to use AutoMapper or similar
    return new MediaEntity
    {
      Id = tableEntity.GetString("Id") ?? string.Empty,
      PartitionKey = tableEntity.PartitionKey,
      RowKey = tableEntity.RowKey,
      Timestamp = tableEntity.Timestamp,
      ETag = tableEntity.ETag,
      AuthorId = tableEntity.GetString("AuthorId") ?? string.Empty,
      Filename = tableEntity.GetString("Filename") ?? string.Empty,
      MediaType = tableEntity.GetString("MediaType") ?? string.Empty,
      Purpose = tableEntity.GetString("Purpose") ?? string.Empty,
      Url = tableEntity.GetString("Url") ?? string.Empty,
      Description = tableEntity.GetString("Description") ?? string.Empty,
      AltText = tableEntity.GetString("AltText") ?? string.Empty,
      ThumbnailUrl = tableEntity.GetString("ThumbnailUrl") ?? string.Empty,
      ContentType = tableEntity.GetString("ContentType") ?? string.Empty,
      ContentId = tableEntity.GetString("ContentId"),
      RelatedContentType = tableEntity.GetString("RelatedContentType"),
      Width = tableEntity.GetInt32("Width") ?? 0,
      Height = tableEntity.GetInt32("Height") ?? 0,
      UploadedAt = (tableEntity.GetDateTime("UploadedAt") ?? DateTime.UtcNow).EnsureUtc()
    };
  }

  private static string GetContentType(string fileName)
  {
    var extension = Path.GetExtension(fileName).ToLowerInvariant();
    return extension switch
    {
      ".jpg" or ".jpeg" => "image/jpeg",
      ".png" => "image/png",
      ".gif" => "image/gif",
      ".webp" => "image/webp",
      ".mp4" => "video/mp4",
      ".mov" => "video/quicktime",
      ".avi" => "video/x-msvideo",
      ".wmv" => "video/x-ms-wmv",
      _ => "application/octet-stream"
    };
  }

  public async Task<IEnumerable<MediaEntity>> GetAllMediaAsync(int? limit = null, int offset = 0)
  {
    try
    {
      // Get all media from table storage with pagination
      var pageSize = Math.Min(limit ?? 50, 100); // Cap at 100 for performance
      
      // For now, we'll get all entities and then apply offset/limit
      // In a production scenario, you'd want proper pagination at the storage level
      var result = await _tableStorageService.GetEntitiesAsync(_tableName, null, pageSize + offset);
      var entities = result.Entities.Skip(offset).Take(pageSize).Select(ConvertToMediaEntity).ToList();

      _appLogger.LogInformation("Retrieved {Count} total media entities", entities.Count);
      return entities;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Failed to get all media: {Error}", ex, ex.Message);
      throw;
    }
  }

  public async Task<IEnumerable<MediaEntity>> GetMediaByPlatformAsync(string platform, int? limit = null, int offset = 0)
  {
    if (string.IsNullOrWhiteSpace(platform))
      throw new ArgumentException("Platform is required", nameof(platform));

    try
    {
      var filter = $"Platform eq '{platform}'";
      var pageSize = Math.Min(limit ?? 50, 100);

      // For now, we'll get all matching entities and then apply offset/limit
      var result = await _tableStorageService.GetEntitiesAsync(_tableName, filter, pageSize + offset);
      var entities = result.Entities.Skip(offset).Take(pageSize).Select(ConvertToMediaEntity).ToList();

      _appLogger.LogInformation("Retrieved {Count} media entities from platform {Platform}", entities.Count, platform);
      return entities;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Failed to get media by platform {Platform}: {Error}", ex, platform, ex.Message);
      throw;
    }
  }
}