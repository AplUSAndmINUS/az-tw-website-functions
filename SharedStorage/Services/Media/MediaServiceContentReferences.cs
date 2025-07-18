using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SharedStorage.Models;
using SharedStorage.Services.Media;
using Azure.Data.Tables;
using Azure;
using Utils.Extensions;

namespace SharedStorage.Services.Media;

/// <summary>
/// Extension implementation for MediaService to handle content references
/// This class adds content reference tracking to MediaService
/// </summary>
public partial class MediaService
{
  /// <summary>
  /// Updates a media item to be associated with a content item
  /// </summary>
  /// <param name="mediaId">ID of the media item</param>
  /// <param name="contentId">ID of the content</param>
  /// <param name="contentType">Type of content (blog, portfolio, author, etc.)</param>
  /// <returns>True if successful</returns>
  public async Task<bool> UpdateMediaContentReferenceAsync(string mediaId, string contentId, string contentType)
  {
    try
    {
      // Get the media entity
      var media = await GetMediaAsync(mediaId);
      if (media == null)
      {
        _appLogger.LogWarning("Media item with ID {MediaId} not found", mediaId);
        return false;
      }

      // Create a metadata entity to track the relationship
      var metadataEntity = new MediaContentReferenceEntity
      {
        PartitionKey = contentId,
        RowKey = mediaId,
        ContentId = contentId,
        ContentType = contentType,
        MediaId = mediaId,
        MediaType = media.MediaType,
        AssociatedAt = DateTime.UtcNow.EnsureValidStorageDate()
      };

      // Get the appropriate table name for the content type
      string tableName = GetMediaMetadataTableName(contentType);

      // Store the metadata
      await _tableStorageService.UpsertEntityAsync(tableName, metadataEntity);

      _appLogger.LogInformation("Associated media {MediaId} with {ContentType} {ContentId}",
          mediaId, contentType, contentId);

      return true;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Failed to associate media {MediaId} with {ContentType} {ContentId}",
          ex, mediaId, contentType, contentId);
      return false;
    }
  }

  /// <summary>
  /// Gets media items associated with a specific content item
  /// </summary>
  /// <param name="contentId">ID of the content</param>
  /// <param name="contentType">Type of content (blog, portfolio, author, etc.)</param>
  /// <returns>Collection of MediaEntity objects</returns>
  public async Task<IEnumerable<MediaEntity>> GetMediaByContentReferenceInternalAsync(string contentId, string contentType)
  {
    try
    {
      // Get the appropriate table name for the content type
      string tableName = GetMediaMetadataTableName(contentType);

      // Query the metadata table to get references
      var filter = $"PartitionKey eq '{contentId}'";
      var results = await _tableStorageService.GetEntitiesAsync(tableName, filter);

      var mediaItems = new List<MediaEntity>();

      // Get each media item by its ID
      foreach (var entity in results.Entities)
      {
        string mediaId = entity.RowKey;
        var media = await GetMediaAsync(mediaId);
        if (media != null)
        {
          mediaItems.Add(media);
        }
      }

      _appLogger.LogInformation("Retrieved {Count} media items for {ContentType} {ContentId}",
          mediaItems.Count, contentType, contentId);

      return mediaItems;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Failed to retrieve media items for {ContentType} {ContentId}",
          ex, contentType, contentId);
      return new List<MediaEntity>();
    }
  }

  /// <summary>
  /// Helper method to get the appropriate table name for media metadata based on content type
  /// </summary>
  /// <param name="contentType">Type of content (blog, portfolio, author, etc.)</param>
  /// <returns>Table name for the media metadata</returns>
  private string GetMediaMetadataTableName(string contentType)
  {
    return contentType.ToLowerInvariant() switch
    {
      "blog" => "blogmediametadata",
      "portfolio" => "portfoliomediametadata",
      "author" => "authormediametadata",
      "music" => "musicmediametadata",
      "video" => "videomediametadata",
      _ => $"{contentType.ToLowerInvariant()}mediametadata"
    };
  }
}

/// <summary>
/// Entity for tracking media to content associations
/// </summary>
public class MediaContentReferenceEntity : ITableEntity
{
  public string PartitionKey { get; set; } = string.Empty; // ContentId
  public string RowKey { get; set; } = string.Empty;       // MediaId
  public DateTimeOffset? Timestamp { get; set; }
  public ETag ETag { get; set; }

  public string ContentId { get; set; } = string.Empty;
  public string ContentType { get; set; } = string.Empty;
  public string MediaId { get; set; } = string.Empty;
  public string MediaType { get; set; } = string.Empty;
  public DateTime AssociatedAt { get; set; } = DateTime.UtcNow.EnsureValidStorageDate();
  public string Purpose { get; set; } = string.Empty;
}
