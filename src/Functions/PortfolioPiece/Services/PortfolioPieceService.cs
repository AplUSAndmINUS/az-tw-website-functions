using Functions.PortfolioPiece.Models;
using SharedStorage.Services.ContentServices;
using SharedStorage.Services.Media;
using SharedStorage.Services.BaseServices;
using SharedStorage.Validators;
using SharedStorage.Models;
using Utils;
using Utils.Constants;
using Utils.Extensions;
using Utils.Validation;

namespace Functions.PortfolioPiece.Services;

public interface IPortfolioPieceService
{
  // Core CRUD operations
  Task<PortfolioPieceDTO?> GetPieceAsync(string slug, bool? isPublished = true);
  Task<IEnumerable<PortfolioPieceDTO>> GetPiecesAsync(string? authorSlug = null, string? category = null, bool? isPublished = true, int? limit = null);
  Task<PortfolioPieceDTO?> UpsertPieceAsync(string slug, PortfolioPieceModel model);
  Task<bool> DeletePieceAsync(string slug);

  // Media-enhanced operations
  Task<PortfolioPieceWithMediaDTO?> GetPieceWithMediaAsync(string slug, bool? isPublished = true);
  Task<IEnumerable<PortfolioPieceWithMediaDTO>> GetPiecesWithMediaAsync(string? authorSlug = null, string? category = null, bool? isPublished = true, int? limit = null);

  // Media operations
  Task<PortfolioPieceDTO?> SetFeaturedImageAsync(string slug, string mediaId);
  Task<PortfolioPieceDTO?> SetFeaturedMediaAsync(string slug, string mediaId);
  Task<PortfolioPieceDTO?> SetFeaturedVideoAsync(string slug, string mediaId);
  Task<PortfolioPieceDTO?> AddMediaReferenceAsync(string slug, string mediaId);
  Task<PortfolioPieceDTO?> RemoveMediaReferenceAsync(string slug, string mediaId);
}

public class PortfolioPieceService : ContentService<PortfolioPieceEntity, PortfolioPieceModel, PortfolioPieceDTO>, IPortfolioPieceService
{
  private readonly IMediaService _mediaService;

  public PortfolioPieceService(
    ITableStorageService tableStorageService,
    IMediaService mediaService,
    IAppInsightsLogger<ContentService<PortfolioPieceEntity, PortfolioPieceModel, PortfolioPieceDTO>> appLogger)
    : base(tableStorageService, appLogger, GetTableName())
  {
    _mediaService = mediaService ?? throw new ArgumentNullException(nameof(mediaService));
  }

  public async Task<PortfolioPieceWithMediaDTO?> GetPieceWithMediaAsync(string slug, bool? isPublished = true)
  {
    var portfolioPiece = await GetPieceAsync(slug, isPublished);
    if (portfolioPiece == null)
    {
      return null;
    }

    return await EnrichWithMediaAsync(portfolioPiece);
  }

  public async Task<IEnumerable<PortfolioPieceWithMediaDTO>> GetPiecesWithMediaAsync(string? authorSlug = null, string? category = null, bool? isPublished = true, int? limit = null)
  {
    var portfolioPieces = await GetPiecesAsync(authorSlug, category, isPublished, limit);
    if (portfolioPieces == null || !portfolioPieces.Any())
    {
      return Enumerable.Empty<PortfolioPieceWithMediaDTO>();
    }

    var result = new List<PortfolioPieceWithMediaDTO>();
    foreach (var piece in portfolioPieces)
    {
      var enrichedPiece = await EnrichWithMediaAsync(piece);
      result.Add(enrichedPiece);
    }

    return result;
  }

  private async Task<PortfolioPieceWithMediaDTO> EnrichWithMediaAsync(PortfolioPieceDTO portfolioPiece)
  {
    var result = PortfolioPieceWithMediaDTO.FromPortfolioPieceDTO(portfolioPiece);

    // Get featured image if available
    if (!string.IsNullOrEmpty(portfolioPiece.FeaturedImageId))
    {
      var imageEntity = await _mediaService.GetMediaAsync(portfolioPiece.FeaturedImageId);
      if (imageEntity != null)
      {
        var imageModel = SharedStorage.Models.MediaItemMapper.ToModel(imageEntity);
        result.FeaturedImage = imageModel;
        result.MediaItems.Add(imageModel);
        result.LegacyFeaturedImage = imageEntity; // Keep legacy reference for backward compatibility
      }
    }

    // Get featured video if available
    if (!string.IsNullOrEmpty(portfolioPiece.FeaturedVideoId))
    {
      var videoEntity = await _mediaService.GetMediaAsync(portfolioPiece.FeaturedVideoId);
      if (videoEntity != null)
      {
        var videoModel = SharedStorage.Models.MediaItemMapper.ToModel(videoEntity);
        result.FeaturedVideo = videoModel;
        result.MediaItems.Add(videoModel);
        result.LegacyFeaturedVideo = videoEntity; // Keep legacy reference for backward compatibility
      }
    }

    // Get featured media if available
    if (!string.IsNullOrEmpty(portfolioPiece.FeaturedMediaId))
    {
      var mediaEntity = await _mediaService.GetMediaAsync(portfolioPiece.FeaturedMediaId);
      if (mediaEntity != null)
      {
        var mediaModel = SharedStorage.Models.MediaItemMapper.ToModel(mediaEntity);
        result.FeaturedMedia = mediaModel;
        result.MediaItems.Add(mediaModel);
        result.LegacyFeaturedMedia = mediaEntity; // Keep legacy reference for backward compatibility
      }
    }

    // Get all media references if available
    if (!string.IsNullOrEmpty(portfolioPiece.MediaReferencesJson))
    {
      try
      {
        var mediaIds = JsonHelper.Deserialize<List<string>>(portfolioPiece.MediaReferencesJson);
        if (mediaIds != null && mediaIds.Any())
        {
          foreach (var mediaId in mediaIds)
          {
            // Skip if already added as featured media
            if (mediaId == portfolioPiece.FeaturedImageId ||
                mediaId == portfolioPiece.FeaturedVideoId ||
                mediaId == portfolioPiece.FeaturedMediaId)
            {
              continue;
            }

            var mediaEntity = await _mediaService.GetMediaAsync(mediaId);
            if (mediaEntity != null)
            {
              var mediaModel = SharedStorage.Models.MediaItemMapper.ToModel(mediaEntity);
              result.MediaItems.Add(mediaModel);
            }
          }
        }
      }
      catch (Exception ex)
      {
        _appLogger.LogWarning("Failed to deserialize media references for portfolio piece {Slug}: {Error}", portfolioPiece.Slug, ex.Message);
      }
    }

    return result;
  }

  private static string GetTableName()
  {
    var useMock = System.Environment.GetEnvironmentVariable("USE_MOCK_STORAGE")?.ToLowerInvariant() == "true";
    var envTableName = System.Environment.GetEnvironmentVariable("PORTFOLIOPIECES_TABLE_NAME");

    Console.WriteLine($"DEBUG: USE_MOCK_STORAGE={useMock}, PORTFOLIOPIECES_TABLE_NAME={envTableName}");

    string tableName;
    if (!string.IsNullOrEmpty(envTableName))
    {
      // If an explicit table name is provided via environment variable, use that
      var resolvedTableName = useMock ? $"mock{envTableName}" : envTableName;
      tableName = TableNameValidator.ValidateTableName(resolvedTableName);
      Console.WriteLine($"DEBUG: Using environment variable table name. Raw={envTableName}, Resolved={resolvedTableName}, Validated={tableName}");
    }
    else
    {
      // Otherwise use ContentNameResolver for consistent naming
      var resolvedTableName = ContentNameResolver.GetTableName(ContentSections.Portfolio, null, useMock);
      tableName = TableNameValidator.ValidateTableName(resolvedTableName);
      Console.WriteLine($"DEBUG: Using ContentNameResolver. Resolved={resolvedTableName}, Validated={tableName}");
    }

    return tableName;
  }

  #region ContentService Implementation

  protected override string GetPartitionKey(string slug) => slug;
  protected override string GetRowKey(string slug) => "piece";

  protected override bool IsPublished(PortfolioPieceEntity entity) => entity.IsPublished;
  protected override string GetAuthorSlug(PortfolioPieceEntity entity) => entity.AuthorSlug;
  protected override string GetCategory(PortfolioPieceEntity entity) => entity.Category;
  protected override DateTime GetPublishDate(PortfolioPieceEntity entity) => entity.PublishDate;

  protected override PortfolioPieceDTO EntityToDto(PortfolioPieceEntity entity) => PortfolioPieceMapper.EntityToDTOStatic(entity);

  protected override PortfolioPieceEntity ModelToEntity(PortfolioPieceModel model)
  {
    var entity = new PortfolioPieceEntity
    {
      Id = model.Id ?? Guid.NewGuid().ToString(),
      Title = model.Title,
      Content = model.Content,
      AuthorSlug = model.AuthorSlug,
      Category = model.Category,
      Status = model.Status,
      PublishDate = model.PublishDate.EnsureValidStorageDate(),
      LastModified = DateTime.UtcNow,
      TagsJson = System.Text.Json.JsonSerializer.Serialize(model.TagsList),
      FeaturedImageId = model.FeaturedImageId,
      FeaturedMediaId = model.FeaturedMediaId,
      FeaturedVideoId = model.FeaturedVideoId,
      MediaReferencesJson = model.MediaReferencesJson,
      Description = model.Description,
      Slug = model.Slug
    };

    // Use consistent slug-based partitioning
    entity.PartitionKey = GetPartitionKey(model.Slug);
    entity.RowKey = GetRowKey(model.Slug);

    return entity;
  }

  protected override PortfolioPieceEntity? ConvertTableEntityToTEntity(Azure.Data.Tables.TableEntity tableEntity)
  {
    return ConvertToEntity(tableEntity);
  }

  protected override void UpdateEntityFromModel(PortfolioPieceEntity entity, PortfolioPieceModel model)
  {
    entity.Title = model.Title;
    entity.Content = model.Content;
    entity.AuthorSlug = model.AuthorSlug;
    entity.Category = model.Category;
    entity.Status = model.Status;

    entity.PublishDate = model.PublishDate.EnsureValidStorageDate();
    entity.LastModified = DateTime.UtcNow;
    entity.TagsJson = System.Text.Json.JsonSerializer.Serialize(model.TagsList);
    entity.FeaturedImageId = model.FeaturedImageId;
    entity.FeaturedMediaId = model.FeaturedMediaId;
    entity.FeaturedVideoId = model.FeaturedVideoId;
    entity.MediaReferencesJson = model.MediaReferencesJson;
    entity.Description = model.Description;
  }

  protected override void ValidateModel(PortfolioPieceModel model)
  {
    if (string.IsNullOrWhiteSpace(model.Title))
      throw new ArgumentException("Title is required", nameof(model.Title));

    if (string.IsNullOrWhiteSpace(model.Slug))
      throw new ArgumentException("Slug is required", nameof(model.Slug));

    if (string.IsNullOrWhiteSpace(model.AuthorSlug))
      throw new ArgumentException("AuthorSlug is required", nameof(model.AuthorSlug));

    if (string.IsNullOrWhiteSpace(model.Content))
      throw new ArgumentException("Content is required", nameof(model.Content));

    if (string.IsNullOrWhiteSpace(model.Category))
      throw new ArgumentException("Category is required", nameof(model.Category));
  }

  #endregion

  #region IPortfolioPieceService Implementation

  public async Task<PortfolioPieceDTO?> GetPieceAsync(string slug, bool? isPublished = true)
  {
    return await GetBySlugAsync(slug, isPublished);
  }

  public async Task<IEnumerable<PortfolioPieceDTO>> GetPiecesAsync(string? authorSlug = null, string? category = null, bool? isPublished = true, int? limit = null)
  {
    if (isPublished == true)
    {
      return await GetPublishedContentAsync(authorSlug, category, limit);
    }

    // Fix: If isPublished is false, get all content and filter for unpublished only
    // If isPublished is null, get all content without filtering
    try
    {
      var filters = new List<string>();

      if (!string.IsNullOrWhiteSpace(authorSlug))
        filters.Add($"AuthorSlug eq '{authorSlug}'");

      if (!string.IsNullOrWhiteSpace(category))
        filters.Add($"Category eq '{category}'");

      // Add published status filter when isPublished is explicitly false
      if (isPublished == false)
        filters.Add("IsPublished eq false");

      var filter = filters.Any() ? string.Join(" and ", filters) : null;
      var pageSize = Math.Min(limit ?? 50, 100);

      var result = await _tableStorageService.GetEntitiesAsync(_tableName, filter, pageSize);
      var entities = result.Entities.Select(e => ConvertTableEntityToTEntity(e))
                          .Where(e => e != null)
                          .OrderByDescending(e => GetPublishDate(e!))
                          .Select(e => EntityToDto(e!))
                          .ToList();

      _appLogger.LogInformation("Retrieved {Count} portfolio pieces", entities.Count);
      return entities;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Failed to get portfolio pieces: {Error}", ex, ex.Message);
      return Enumerable.Empty<PortfolioPieceDTO>();
    }
  }

  public async Task<PortfolioPieceDTO?> UpsertPieceAsync(string slug, PortfolioPieceModel model)
  {
    return await UpsertAsync(slug, model);
  }

  public async Task<bool> DeletePieceAsync(string slug)
  {
    return await DeleteAsync(slug);
  }

  #endregion

  #region Media Operations

  public async Task<PortfolioPieceDTO?> SetFeaturedImageAsync(string slug, string mediaId)
  {
    var piece = await GetBySlugAsync(slug, false); // Get regardless of published status
    if (piece == null) return null;

    // Verify media exists
    var mediaEntity = await _mediaService.GetMediaAsync(mediaId);
    if (mediaEntity == null)
    {
      _appLogger.LogWarning("Media with ID {MediaId} not found when setting featured image for portfolio piece {Slug}", mediaId, slug);
      return null;
    }

    // Create model from existing DTO to update
    var model = PortfolioPieceMapper.FromDTOStatic(piece);
    model.FeaturedImageId = mediaId;

    // Ensure media reference integrity
    await EnsureMediaReferenceIntegrityAsync(slug, mediaId, "image");

    return await UpsertAsync(slug, model);
  }

  public async Task<PortfolioPieceDTO?> SetFeaturedMediaAsync(string slug, string mediaId)
  {
    var piece = await GetBySlugAsync(slug, false); // Get regardless of published status
    if (piece == null) return null;

    // Verify media exists
    var mediaEntity = await _mediaService.GetMediaAsync(mediaId);
    if (mediaEntity == null)
    {
      _appLogger.LogWarning("Media with ID {MediaId} not found when setting featured media for portfolio piece {Slug}", mediaId, slug);
      return null;
    }

    // Create model from existing DTO to update
    var model = PortfolioPieceMapper.FromDTOStatic(piece);
    model.FeaturedMediaId = mediaId;

    // Ensure media reference integrity
    await EnsureMediaReferenceIntegrityAsync(slug, mediaId, mediaEntity.MediaType);

    return await UpsertAsync(slug, model);
  }

  public async Task<PortfolioPieceDTO?> AddMediaReferenceAsync(string slug, string mediaId)
  {
    var piece = await GetBySlugAsync(slug, false); // Get regardless of published status
    if (piece == null) return null;

    // Verify media exists
    var mediaEntity = await _mediaService.GetMediaAsync(mediaId);
    if (mediaEntity == null)
    {
      _appLogger.LogWarning("Media with ID {MediaId} not found when adding media reference to portfolio piece {Slug}", mediaId, slug);
      return null;
    }

    // Create model from existing DTO to update
    var model = PortfolioPieceMapper.FromDTOStatic(piece);

    // Get current media references
    var mediaIds = string.IsNullOrEmpty(model.MediaReferencesJson)
      ? new List<string>()
      : JsonHelper.Deserialize<List<string>>(model.MediaReferencesJson) ?? new List<string>();

    // Add if not already present
    if (!mediaIds.Contains(mediaId))
    {
      mediaIds.Add(mediaId);
      model.MediaReferencesJson = JsonHelper.Serialize(mediaIds);

      // Ensure media reference integrity
      await EnsureMediaReferenceIntegrityAsync(slug, mediaId, mediaEntity.MediaType);

      return await UpsertAsync(slug, model);
    }

    return piece; // Return unchanged if already exists
  }

  public async Task<PortfolioPieceDTO?> RemoveMediaReferenceAsync(string slug, string mediaId)
  {
    var piece = await GetBySlugAsync(slug, false); // Get regardless of published status
    if (piece == null) return null;

    // Create model from existing DTO to update
    var model = PortfolioPieceMapper.FromDTOStatic(piece);

    // Get current media references
    var mediaIds = string.IsNullOrEmpty(model.MediaReferencesJson)
      ? new List<string>()
      : JsonHelper.Deserialize<List<string>>(model.MediaReferencesJson) ?? new List<string>();

    // Remove if present
    if (mediaIds.Contains(mediaId))
    {
      mediaIds.Remove(mediaId);
      model.MediaReferencesJson = JsonHelper.Serialize(mediaIds);

      // Also clear any featured references
      if (model.FeaturedImageId == mediaId) model.FeaturedImageId = null;
      if (model.FeaturedMediaId == mediaId) model.FeaturedMediaId = null;
      if (model.FeaturedVideoId == mediaId) model.FeaturedVideoId = null;

      // Remove media reference metadata
      await RemoveMediaReferenceMetadataAsync(slug, mediaId);

      return await UpsertAsync(slug, model);
    }

    return piece; // Return unchanged if not found
  }

  public async Task<PortfolioPieceDTO?> SetFeaturedVideoAsync(string slug, string mediaId)
  {
    var piece = await GetBySlugAsync(slug, false); // Get regardless of published status
    if (piece == null) return null;

    // Verify media exists
    var mediaEntity = await _mediaService.GetMediaAsync(mediaId);
    if (mediaEntity == null)
    {
      _appLogger.LogWarning("Media with ID {MediaId} not found when setting featured video for portfolio piece {Slug}", mediaId, slug);
      return null;
    }

    // Create model from existing DTO to update
    var model = PortfolioPieceMapper.FromDTOStatic(piece);
    model.FeaturedVideoId = mediaId;

    // Ensure media reference integrity
    await EnsureMediaReferenceIntegrityAsync(slug, mediaId, "video");

    return await UpsertAsync(slug, model);
  }
  #endregion

  #region Helper Methods

  private PortfolioPieceEntity? ConvertToEntity(Azure.Data.Tables.TableEntity tableEntity)
  {
    try
    {
      var entity = new PortfolioPieceEntity
      {
        PartitionKey = tableEntity.PartitionKey,
        RowKey = tableEntity.RowKey,
        Timestamp = tableEntity.Timestamp,
        ETag = tableEntity.ETag
      };

      // Map properties with null checks and type conversions
      if (tableEntity.TryGetValue("Id", out var idValue))
        entity.Id = idValue?.ToString() ?? Guid.NewGuid().ToString();

      if (tableEntity.TryGetValue("Title", out var titleValue))
        entity.Title = titleValue?.ToString() ?? string.Empty;

      if (tableEntity.TryGetValue("AuthorSlug", out var authorSlugValue))
        entity.AuthorSlug = authorSlugValue?.ToString() ?? string.Empty;

      if (tableEntity.TryGetValue("Description", out var descriptionValue))
        entity.Description = descriptionValue?.ToString() ?? string.Empty;

      if (tableEntity.TryGetValue("Content", out var contentValue))
        entity.Content = contentValue?.ToString() ?? string.Empty;

      if (tableEntity.TryGetValue("Slug", out var slugValue))
        entity.Slug = slugValue?.ToString() ?? string.Empty;

      if (tableEntity.TryGetValue("Category", out var categoryValue))
        entity.Category = categoryValue?.ToString() ?? string.Empty;

      if (tableEntity.TryGetValue("Status", out var statusValue))
        entity.Status = statusValue?.ToString() ?? "Draft";

      if (tableEntity.TryGetValue("FeaturedImageId", out var featuredImageIdValue))
        entity.FeaturedImageId = featuredImageIdValue?.ToString();

      if (tableEntity.TryGetValue("FeaturedMediaId", out var featuredMediaIdValue))
        entity.FeaturedMediaId = featuredMediaIdValue?.ToString();

      if (tableEntity.TryGetValue("FeaturedVideoId", out var featuredVideoIdValue))
        entity.FeaturedVideoId = featuredVideoIdValue?.ToString();

      if (tableEntity.TryGetValue("MediaReferencesJson", out var mediaReferencesJsonValue))
        entity.MediaReferencesJson = mediaReferencesJsonValue?.ToString() ?? "[]";

      if (tableEntity.TryGetValue("PublishDate", out var publishDateValue))
      {
        if (publishDateValue is DateTimeOffset publishDateOffset)
          entity.PublishDate = publishDateOffset.DateTime;
        else if (DateTime.TryParse(publishDateValue?.ToString(), out var publishDate))
          entity.PublishDate = publishDate;
      }

      if (tableEntity.TryGetValue("LastModified", out var lastModifiedValue))
      {
        if (lastModifiedValue is DateTimeOffset lastModifiedOffset)
          entity.LastModified = lastModifiedOffset.DateTime;
        else if (DateTime.TryParse(lastModifiedValue?.ToString(), out var lastModified))
          entity.LastModified = lastModified;
      }

      if (tableEntity.TryGetValue("TagsJson", out var tagsJsonValue))
        entity.TagsJson = tagsJsonValue?.ToString() ?? "[]";

      return entity;
    }
    catch (Exception ex)
    {
      _appLogger.LogWarning("Failed to convert TableEntity to PortfolioPieceEntity: {Error}", ex.Message);
      return null;
    }
  }

  /// <summary>
  /// Ensures that metadata linking a media item to a portfolio piece is properly maintained.
  /// Creates entries in the appropriate metadata tables (e.g., mockportfolioimagesmetadata, mockportfoliovideometadata)
  /// and updates the media entity with ContentId and RelatedContentType.
  /// </summary>
  private async Task EnsureMediaReferenceIntegrityAsync(string portfolioSlug, string mediaId, string? mediaType = null)
  {
    try
    {
      // Update the media entity to link back to this portfolio piece
      var mediaEntity = await _mediaService.GetMediaAsync(mediaId);
      if (mediaEntity != null)
      {
        // Use the MediaService extension method to associate media with content
        await _mediaService.AssociateMediaWithContentAsync(mediaId, portfolioSlug, "portfolio");

        // Determine media type if not provided
        if (string.IsNullOrEmpty(mediaType))
        {
          mediaType = mediaEntity.MediaType?.ToLowerInvariant() ?? "media";
        }
      }

      // Create metadata entry in the appropriate table
      var metadataTableName = GetMediaMetadataTableName(mediaType ?? "media");
      var metadataEntity = new Azure.Data.Tables.TableEntity(portfolioSlug, mediaId)
      {
        ["ContentType"] = "portfolio",
        ["MediaId"] = mediaId,
        ["ContentId"] = portfolioSlug,
        ["CreatedAt"] = DateTime.UtcNow.EnsureValidStorageDate(),
        ["MediaType"] = mediaType ?? "media"
      };

      await _tableStorageService.UpsertEntityAsync(metadataTableName, metadataEntity);

      _appLogger.LogInformation("Media reference integrity ensured for portfolio piece {Slug} and media {MediaId}", portfolioSlug, mediaId);
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Failed to ensure media reference integrity for portfolio piece {Slug} and media {MediaId}", ex, portfolioSlug, mediaId);
    }
  }

  /// <summary>
  /// Removes metadata entries linking a media item to a portfolio piece when the relationship is removed
  /// </summary>
  private async Task RemoveMediaReferenceMetadataAsync(string portfolioSlug, string mediaId, string? mediaType = null)
  {
    try
    {
      // Get the media entity to determine type if needed
      if (string.IsNullOrEmpty(mediaType))
      {
        var mediaEntity = await _mediaService.GetMediaAsync(mediaId);
        mediaType = mediaEntity?.MediaType?.ToLowerInvariant() ?? "media";
      }

      // Remove metadata entry from the appropriate table
      var metadataTableName = GetMediaMetadataTableName(mediaType ?? "media");
      await _tableStorageService.DeleteEntityAsync(metadataTableName, portfolioSlug, mediaId);

      _appLogger.LogInformation("Media reference metadata removed for portfolio piece {Slug} and media {MediaId}", portfolioSlug, mediaId);
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Failed to remove media reference metadata for portfolio piece {Slug} and media {MediaId}", ex, portfolioSlug, mediaId);
    }
  }

  /// <summary>
  /// Gets the appropriate metadata table name based on media type
  /// </summary>
  private string GetMediaMetadataTableName(string mediaType)
  {
    var useMock = System.Environment.GetEnvironmentVariable("USE_MOCK_STORAGE")?.ToLowerInvariant() == "true";
    var envTableName = System.Environment.GetEnvironmentVariable("PORTFOLIO_METADATA_TABLE_NAME");

    switch (mediaType?.ToLowerInvariant())
    {
      case "image":
        if (!string.IsNullOrEmpty(envTableName))
        {
          return useMock ? $"mock{envTableName}images" : $"{envTableName}images";
        }
        else
        {
          return ContentNameResolver.GetTableName(ContentSections.Portfolio, AssetType.Images, useMock);
        }

      case "video":
        if (!string.IsNullOrEmpty(envTableName))
        {
          return useMock ? $"mock{envTableName}video" : $"{envTableName}video";
        }
        else
        {
          return ContentNameResolver.GetTableName(ContentSections.Portfolio, AssetType.Video, useMock);
        }

      default:
        // Default to general media metadata table
        if (!string.IsNullOrEmpty(envTableName))
        {
          return useMock ? $"mock{envTableName}media" : $"{envTableName}media";
        }
        else
        {
          return ContentNameResolver.GetTableName(ContentSections.Portfolio, AssetType.Media, useMock);
        }
    }
  }

  #endregion
}
