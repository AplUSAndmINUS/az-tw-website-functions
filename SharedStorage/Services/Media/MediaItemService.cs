using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using Utils.Extensions;
using SharedStorage.Models;
using SharedStorage.Services;
using Azure.Data.Tables;
using System.Linq;
using Utils;

namespace SharedStorage.Services.Media;

/// <summary>
/// Service for handling the unified media model across all content types
/// </summary>
public class MediaItemService : IMediaItemService
{
  private readonly IMediaService _mediaService;
  private readonly IAppInsightsLogger<MediaItemService> _appLogger;

  public MediaItemService(
      IMediaService mediaService,
      IAppInsightsLogger<MediaItemService> appLogger)
  {
    _mediaService = mediaService ?? throw new ArgumentNullException(nameof(mediaService));
    _appLogger = appLogger ?? throw new ArgumentNullException(nameof(appLogger));
  }

  /// <summary>
  /// Retrieves all media items associated with specific content
  /// </summary>
  /// <param name="contentId">The ID of the content (blog post, portfolio piece, etc.)</param>
  /// <param name="contentType">The type of content (blog, portfolio, author, etc.)</param>
  /// <returns>A collection of MediaItemModel objects</returns>
  public async Task<IEnumerable<MediaItemModel>> GetMediaForContentAsync(string contentId, string contentType)
  {
    _appLogger.LogInformation("Getting media items for {ContentType} with ID {ContentId}", contentType, contentId);

    try
    {
      // Retrieve all media entities from underlying storage
      var mediaEntities = await _mediaService.GetMediaByContentReferenceAsync(contentId, contentType);

      // Convert to MediaItemModel objects
      var mediaItems = MediaItemMapper.ToModels(mediaEntities).ToList();

      _appLogger.LogInformation("Retrieved {Count} media items for {ContentType} with ID {ContentId}",
          mediaItems.Count, contentType, contentId);

      return mediaItems;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error retrieving media items for {ContentType} with ID {ContentId}",
          ex, contentType, contentId);
      throw;
    }
  }

  /// <summary>
  /// Retrieves media items by their IDs
  /// </summary>
  /// <param name="mediaIds">Array of media IDs</param>
  /// <returns>A collection of MediaItemModel objects</returns>
  public async Task<IEnumerable<MediaItemModel>> GetMediaByIdsAsync(string[] mediaIds)
  {
    if (mediaIds == null || !mediaIds.Any())
    {
      return Enumerable.Empty<MediaItemModel>();
    }

    _appLogger.LogInformation("Getting {Count} media items by IDs", mediaIds.Length);

    try
    {
      var mediaItems = new List<MediaItemModel>();

      foreach (var id in mediaIds)
      {
        if (string.IsNullOrEmpty(id)) continue;

        var entity = await _mediaService.GetMediaAsync(id);
        if (entity != null)
        {
          mediaItems.Add(MediaItemMapper.ToModel(entity));
        }
      }

      _appLogger.LogInformation("Retrieved {Count} media items by IDs", mediaItems.Count);
      return mediaItems;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error retrieving media items by IDs", ex);
      throw;
    }
  }

  /// <summary>
  /// Uploads a new media item and associates it with content
  /// </summary>
  /// <param name="stream">The media content stream</param>
  /// <param name="filename">The filename of the media</param>
  /// <param name="contentType">MIME type of the media</param>
  /// <param name="mediaType">Type of media (image, video, audio, etc.)</param>
  /// <param name="purpose">Purpose of the media (profile, cover, gallery, etc.)</param>
  /// <param name="authorId">ID of the author who uploaded the media</param>
  /// <param name="contentId">ID of the content this media is associated with</param>
  /// <param name="description">Description of the media</param>
  /// <param name="altText">Alt text for accessibility</param>
  /// <returns>A MediaItemModel representing the uploaded media</returns>
  public async Task<MediaItemModel> UploadMediaAsync(
      System.IO.Stream stream,
      string filename,
      string contentType,
      string mediaType,
      string purpose,
      string authorId,
      string contentId,
      string description = "",
      string altText = "")
  {
    _appLogger.LogInformation("Uploading {MediaType} with filename {Filename} for {Purpose}",
        mediaType, filename, purpose);

    try
    {
      // Use the appropriate upload method based on media type
      MediaEntity entity;

      switch (mediaType.ToLowerInvariant())
      {
        case "image":
          entity = await _mediaService.UploadImageAsync(
              stream,
              filename,
              authorId,
              description,
              altText,
              purpose);
          break;

        case "video":
          entity = await _mediaService.UploadVideoAsync(
              stream,
              filename,
              authorId,
              description,
              purpose);
          break;

        default:
          entity = await _mediaService.UploadMediaAsync(
              mediaType,
              stream,
              filename,
              authorId,
              description,
              altText,
              purpose);
          break;
      }

      // Associate the media with the content
      if (!string.IsNullOrEmpty(contentId))
      {
        await _mediaService.AssociateMediaWithContentAsync(entity.Id, contentId, mediaType);
      }

      // Convert to MediaItemModel
      var mediaItem = MediaItemMapper.ToModel(entity);
      mediaItem.ContentId = contentId;

      _appLogger.LogInformation("Successfully uploaded {MediaType} with ID {MediaId} for {ContentId}",
          mediaType, mediaItem.Id, contentId);

      return mediaItem;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error uploading {MediaType} with filename {Filename}", ex, mediaType, filename);
      throw;
    }
  }

  /// <summary>
  /// Deletes a media item and removes its associations
  /// </summary>
  /// <param name="mediaId">ID of the media to delete</param>
  /// <returns>True if successful</returns>
  public async Task<bool> DeleteMediaAsync(string mediaId)
  {
    _appLogger.LogInformation("Deleting media with ID {MediaId}", mediaId);

    try
    {
      var result = await _mediaService.DeleteMediaAsync(mediaId);
      _appLogger.LogInformation("Media deletion result for ID {MediaId}: {Result}", mediaId, result);
      return result;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error deleting media with ID {MediaId}", ex, mediaId);
      throw;
    }
  }
}

/// <summary>
/// Interface for the MediaItemService
/// </summary>
public interface IMediaItemService
{
  Task<IEnumerable<MediaItemModel>> GetMediaForContentAsync(string contentId, string contentType);
  Task<IEnumerable<MediaItemModel>> GetMediaByIdsAsync(string[] mediaIds);
  Task<MediaItemModel> UploadMediaAsync(
      System.IO.Stream stream,
      string filename,
      string contentType,
      string mediaType,
      string purpose,
      string authorId,
      string contentId,
      string description = "",
      string altText = "");
  Task<bool> DeleteMediaAsync(string mediaId);
}
