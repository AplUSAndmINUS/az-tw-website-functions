using SharedStorage.Models;
using SharedStorage.Services.BaseServices;
using SharedStorage.Services.Media.Platforms;
using Utils;
using Utils.Extensions;

namespace SharedStorage.Services.Media;

/// <summary>
/// Service for managing media gallery operations across multiple platforms
/// </summary>
public interface IMediaGalleryService
{
  Task<IEnumerable<MediaEntity>> GetAllMediaAsync(string? authorId = null, int? limit = null);
  Task<IEnumerable<MediaEntity>> GetMediaByMediumAsync(string mediaType, string? authorId = null, int? limit = null);
  Task<IEnumerable<MediaEntity>> GetMediaByPlatformAsync(string platform, string? authorId = null, int? limit = null);
  Task<int> SyncAllPlatformsAsync(string authorId);
  Task<int> SyncPlatformAsync(string platform, string authorId);
  Task<IEnumerable<string>> GetSupportedPlatformsAsync();
}

public class MediaGalleryService : IMediaGalleryService
{
  private readonly IEnumerable<IPlatformMediaAdapter> _platformAdapters;
  private readonly ITableStorageService _tableStorageService;
  private readonly IAppInsightsLogger<MediaGalleryService> _appLogger;
  private readonly string _tableName;
  private readonly Dictionary<string, IPlatformMediaAdapter> _adapterMap;

  public MediaGalleryService(
    IEnumerable<IPlatformMediaAdapter> platformAdapters,
    ITableStorageService tableStorageService,
    IAppInsightsLogger<MediaGalleryService> appLogger)
  {
    _platformAdapters = platformAdapters ?? throw new ArgumentNullException(nameof(platformAdapters));
    _tableStorageService = tableStorageService ?? throw new ArgumentNullException(nameof(tableStorageService));
    _appLogger = appLogger ?? throw new ArgumentNullException(nameof(appLogger));

    // Create adapter lookup map
    _adapterMap = _platformAdapters.ToDictionary(a => a.PlatformName.ToLowerInvariant());

    // Get table name from environment variable with fallback
    var useMock = System.Environment.GetEnvironmentVariable("USE_MOCK_STORAGE")?.ToLowerInvariant() == "true";
    var envTableName = System.Environment.GetEnvironmentVariable("MEDIA_TABLE_NAME") ?? "media";
    
    _tableName = useMock ? $"mock{envTableName}" : envTableName;
    _tableName = SharedStorage.Validators.TableNameValidator.ValidateTableName(_tableName);

    _appLogger.LogInformation("MediaGalleryService initialized with {AdapterCount} platform adapters using table {TableName}",
      _adapterMap.Count, _tableName);
  }

  public async Task<IEnumerable<MediaEntity>> GetAllMediaAsync(string? authorId = null, int? limit = null)
  {
    _appLogger.LogInformation("Getting all media - AuthorId: {AuthorId}, Limit: {Limit}", authorId, limit);

    try
    {
      var filters = new List<string>();
      
      if (!string.IsNullOrWhiteSpace(authorId))
        filters.Add($"AuthorId eq '{authorId}'");

      var filter = filters.Any() ? string.Join(" and ", filters) : null;
      var pageSize = Math.Min(limit ?? 100, 200); // Cap at 200 for performance

      var result = await _tableStorageService.GetEntitiesAsync(_tableName, filter, pageSize);
      var entities = result.Entities.Select(ConvertToMediaEntity).ToList();

      _appLogger.LogInformation("Retrieved {Count} media items", entities.Count);
      return entities;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Failed to get all media: {Error}", ex, ex.Message);
      throw;
    }
  }

  public async Task<IEnumerable<MediaEntity>> GetMediaByMediumAsync(string mediaType, string? authorId = null, int? limit = null)
  {
    if (string.IsNullOrWhiteSpace(mediaType))
      throw new ArgumentException("Media type is required", nameof(mediaType));

    _appLogger.LogInformation("Getting media by medium - MediaType: {MediaType}, AuthorId: {AuthorId}, Limit: {Limit}", 
      mediaType, authorId, limit);

    try
    {
      var filters = new List<string> { $"MediaType eq '{mediaType.ToLowerInvariant()}'" };
      
      if (!string.IsNullOrWhiteSpace(authorId))
        filters.Add($"AuthorId eq '{authorId}'");

      var filter = string.Join(" and ", filters);
      var pageSize = Math.Min(limit ?? 100, 200);

      var result = await _tableStorageService.GetEntitiesAsync(_tableName, filter, pageSize);
      var entities = result.Entities.Select(ConvertToMediaEntity).ToList();

      _appLogger.LogInformation("Retrieved {Count} media items for medium {MediaType}", entities.Count, mediaType);
      return entities;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Failed to get media by medium {MediaType}: {Error}", ex, mediaType, ex.Message);
      throw;
    }
  }

  public async Task<IEnumerable<MediaEntity>> GetMediaByPlatformAsync(string platform, string? authorId = null, int? limit = null)
  {
    if (string.IsNullOrWhiteSpace(platform))
      throw new ArgumentException("Platform is required", nameof(platform));

    _appLogger.LogInformation("Getting media by platform - Platform: {Platform}, AuthorId: {AuthorId}, Limit: {Limit}", 
      platform, authorId, limit);

    try
    {
      var filters = new List<string> { $"Platform eq '{platform.ToLowerInvariant()}'" };
      
      if (!string.IsNullOrWhiteSpace(authorId))
        filters.Add($"AuthorId eq '{authorId}'");

      var filter = string.Join(" and ", filters);
      var pageSize = Math.Min(limit ?? 100, 200);

      var result = await _tableStorageService.GetEntitiesAsync(_tableName, filter, pageSize);
      var entities = result.Entities.Select(ConvertToMediaEntity).ToList();

      _appLogger.LogInformation("Retrieved {Count} media items for platform {Platform}", entities.Count, platform);
      return entities;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Failed to get media by platform {Platform}: {Error}", ex, platform, ex.Message);
      throw;
    }
  }

  public async Task<int> SyncAllPlatformsAsync(string authorId)
  {
    if (string.IsNullOrWhiteSpace(authorId))
      throw new ArgumentException("Author ID is required", nameof(authorId));

    _appLogger.LogInformation("Syncing all platforms for author: {AuthorId}", authorId);

    var totalSynced = 0;
    var tasks = _adapterMap.Values.Select(async adapter =>
    {
      try
      {
        var synced = await SyncPlatformAsync(adapter.PlatformName, authorId);
        _appLogger.LogInformation("Synced {Count} items from {Platform}", synced, adapter.PlatformName);
        return synced;
      }
      catch (Exception ex)
      {
        _appLogger.LogError("Failed to sync platform {Platform} for author {AuthorId}: {Error}", 
          ex, adapter.PlatformName, authorId, ex.Message);
        return 0;
      }
    });

    var results = await Task.WhenAll(tasks);
    totalSynced = results.Sum();

    _appLogger.LogInformation("Synced {TotalCount} total items across all platforms for author {AuthorId}", 
      totalSynced, authorId);
    
    return totalSynced;
  }

  public async Task<int> SyncPlatformAsync(string platform, string authorId)
  {
    if (string.IsNullOrWhiteSpace(platform))
      throw new ArgumentException("Platform is required", nameof(platform));
      
    if (string.IsNullOrWhiteSpace(authorId))
      throw new ArgumentException("Author ID is required", nameof(authorId));

    var normalizedPlatform = platform.ToLowerInvariant();
    
    if (!_adapterMap.TryGetValue(normalizedPlatform, out var adapter))
    {
      _appLogger.LogWarning("No adapter found for platform: {Platform}", platform);
      return 0;
    }

    _appLogger.LogInformation("Syncing platform {Platform} for author {AuthorId}", platform, authorId);

    try
    {
      // Validate connection first
      if (!await adapter.ValidateConnectionAsync())
      {
        _appLogger.LogWarning("Connection validation failed for platform {Platform}", platform);
        return 0;
      }

      // Fetch recent media from platform
      var mediaItems = await adapter.FetchRecentMediaAsync(authorId);
      var syncedCount = 0;

      foreach (var mediaItem in mediaItems)
      {
        try
        {
          // Check if item already exists by ExternalId
          var existingFilter = $"ExternalId eq '{mediaItem.ExternalId}' and Platform eq '{normalizedPlatform}'";
          var existingResult = await _tableStorageService.GetEntitiesAsync(_tableName, existingFilter, 1);
          
          if (existingResult.Entities.Any())
          {
            // Update existing item
            var existingEntity = existingResult.Entities.First();
            mediaItem.PartitionKey = existingEntity.PartitionKey;
            mediaItem.RowKey = existingEntity.RowKey;
            mediaItem.ETag = existingEntity.ETag;
            mediaItem.LastSyncedAt = DateTime.UtcNow;
            
            await _tableStorageService.UpsertEntityAsync(_tableName, mediaItem);
            _appLogger.LogInformation("Updated existing media item {ExternalId} from {Platform}", 
              mediaItem.ExternalId, platform);
          }
          else
          {
            // Create new item
            await _tableStorageService.UpsertEntityAsync(_tableName, mediaItem);
            _appLogger.LogInformation("Created new media item {ExternalId} from {Platform}", 
              mediaItem.ExternalId, platform);
          }
          
          syncedCount++;
        }
        catch (Exception ex)
        {
          _appLogger.LogError("Failed to sync media item {ExternalId} from {Platform}: {Error}", 
            ex, mediaItem.ExternalId, platform, ex.Message);
        }
      }

      _appLogger.LogInformation("Successfully synced {Count} items from {Platform} for author {AuthorId}", 
        syncedCount, platform, authorId);
      
      return syncedCount;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Failed to sync platform {Platform} for author {AuthorId}: {Error}", 
        ex, platform, authorId, ex.Message);
      throw;
    }
  }

  public async Task<IEnumerable<string>> GetSupportedPlatformsAsync()
  {
    await Task.CompletedTask; // Make async for future extensibility
    return _adapterMap.Keys;
  }

  private MediaEntity ConvertToMediaEntity(Azure.Data.Tables.TableEntity tableEntity)
  {
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
      UploadedAt = (tableEntity.GetDateTime("UploadedAt") ?? DateTime.UtcNow).EnsureUtc(),
      
      // External platform properties
      Platform = tableEntity.GetString("Platform") ?? string.Empty,
      ExternalId = tableEntity.GetString("ExternalId") ?? string.Empty,
      ExternalUrl = tableEntity.GetString("ExternalUrl") ?? string.Empty,
      EmbedCode = tableEntity.GetString("EmbedCode") ?? string.Empty,
      ExternalCreatedAt = tableEntity.GetDateTime("ExternalCreatedAt")?.EnsureUtc(),
      LastSyncedAt = tableEntity.GetDateTime("LastSyncedAt")?.EnsureUtc(),
      PlatformMetadata = tableEntity.GetString("PlatformMetadata") ?? string.Empty,
      LikeCount = tableEntity.GetInt32("LikeCount") ?? 0,
      ShareCount = tableEntity.GetInt32("ShareCount") ?? 0,
      ViewCount = tableEntity.GetInt32("ViewCount") ?? 0,
      Tags = tableEntity.GetString("Tags") ?? string.Empty
    };
  }
}