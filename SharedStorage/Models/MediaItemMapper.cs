using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using SharedStorage.Extensions;
using Utils.Extensions;

namespace SharedStorage.Models;

/// <summary>
/// Provides mapping functions between MediaEntity, MediaItemModel, and MediaItemDTO objects
/// </summary>
public static class MediaItemMapper
{
  /// <summary>
  /// Maps a MediaEntity to a MediaItemModel
  /// </summary>
  public static MediaItemModel ToModel(MediaEntity entity)
  {
    ArgumentNullException.ThrowIfNull(entity);

    var model = new MediaItemModel
    {
      Id = entity.Id,
      AuthorId = entity.AuthorId,
      Filename = entity.Filename,
      MediaType = entity.MediaType,
      Purpose = entity.Purpose,
      ContentType = entity.ContentType,
      Url = entity.Url,
      ThumbnailUrl = entity.ThumbnailUrl,
      Description = entity.Description,
      AltText = entity.AltText,
      Width = entity.Width,
      Height = entity.Height,
      UploadedAt = entity.UploadedAt.EnsureUtc(),
      LastModified = DateTime.UtcNow
    };

    // Ensure valid CDN URLs
    model.EnsureValidCdnUrls();

    // Add specialized properties based on entity type
    if (entity is ImageEntity imageEntity)
    {
      model.Resolution = imageEntity.Resolution;
      model.ImagePurpose = imageEntity.ImgPurpose;
    }
    else if (entity is VideoEntity videoEntity)
    {
      model.Resolution = videoEntity.Resolution;
      model.VideoQuality = DetermineVideoQuality(videoEntity.Resolution);
    }

    return model;
  }

  /// <summary>
  /// Maps a MediaItemModel to the appropriate MediaEntity type
  /// </summary>
  public static MediaEntity ToEntity(MediaItemModel model)
  {
    ArgumentNullException.ThrowIfNull(model);

    MediaEntity entity;

    // Create the appropriate entity type based on the media type
    switch (model.MediaType.ToLowerInvariant())
    {
      case "image":
        entity = new ImageEntity
        {
          ImageType = "image",
          ImgPurpose = model.ImagePurpose ?? model.Purpose,
          Resolution = model.Resolution
        };
        break;

      case "video":
        entity = new VideoEntity
        {
          VideoType = "video",
          VidPurpose = model.Purpose,
          Resolution = model.Resolution
        };
        break;

      default:
        entity = new MediaEntity();
        break;
    }

    // Set common properties
    entity.Id = string.IsNullOrEmpty(model.Id) ? Guid.NewGuid().ToString() : model.Id;
    entity.AuthorId = model.AuthorId;
    entity.Filename = model.Filename;
    entity.MediaType = model.MediaType;
    entity.Purpose = model.Purpose;
    entity.ContentType = model.ContentType;
    entity.Url = model.Url;
    entity.ThumbnailUrl = model.ThumbnailUrl;
    entity.Description = model.Description;
    entity.AltText = model.AltText;
    entity.Width = model.Width;
    entity.Height = model.Height;
    entity.UploadedAt = model.UploadedAt.EnsureUtc();

    // Set partition and row keys
    entity.PartitionKey = model.AuthorId;
    entity.RowKey = entity.Id;

    return entity;
  }

  /// <summary>
  /// Converts a collection of MediaEntity objects to MediaItemModel objects
  /// </summary>
  public static IEnumerable<MediaItemModel> ToModels(IEnumerable<MediaEntity> entities)
  {
    return entities?.Select(ToModel) ?? Enumerable.Empty<MediaItemModel>();
  }

  /// <summary>
  /// Converts a collection of MediaItemModel objects to MediaEntity objects
  /// </summary>
  public static IEnumerable<MediaEntity> ToEntities(IEnumerable<MediaItemModel> models)
  {
    return models?.Select(ToEntity) ?? Enumerable.Empty<MediaEntity>();
  }

  /// <summary>
  /// Converts a MediaItemModel to a MediaItemDTO
  /// </summary>
  public static MediaItemDTO ToDTO(MediaItemModel model)
  {
    ArgumentNullException.ThrowIfNull(model);

    // Ensure the model has valid CDN URLs before mapping to DTO
    model.EnsureValidCdnUrls();

    var dto = new MediaItemDTO
    {
      Id = model.Id,
      AuthorId = model.AuthorId,
      Filename = model.Filename,
      MediaType = model.MediaType,
      Purpose = model.Purpose,
      ContentType = model.ContentType,
      Url = model.Url,
      ThumbnailUrl = model.ThumbnailUrl,
      Description = model.Description,
      AltText = model.AltText,
      Width = model.Width,
      Height = model.Height,
      SizeBytes = model.SizeBytes,
      Resolution = model.Resolution,
      UploadedAt = model.UploadedAt,
      LastModified = model.LastModified,
      ContentId = model.ContentId,
      RelatedContentType = model.RelatedContentType,
      ImagePurpose = model.ImagePurpose,
      Duration = model.Duration,
      VideoQuality = model.VideoQuality,
      AudioDuration = model.AudioDuration,
      AudioBitrate = model.AudioBitrate
    };

    // Double check that DTO has valid CDN URLs
    return dto.EnsureValidCdnUrls();
  }

  /// <summary>
  /// Converts a MediaItemDTO to a MediaItemModel
  /// </summary>
  public static MediaItemModel FromDTO(MediaItemDTO dto)
  {
    ArgumentNullException.ThrowIfNull(dto);

    // Ensure the DTO has valid CDN URLs before mapping to model
    dto.EnsureValidCdnUrls();

    var model = new MediaItemModel
    {
      Id = dto.Id,
      AuthorId = dto.AuthorId,
      Filename = dto.Filename,
      MediaType = dto.MediaType,
      Purpose = dto.Purpose,
      ContentType = dto.ContentType,
      Url = dto.Url,
      ThumbnailUrl = dto.ThumbnailUrl,
      Description = dto.Description,
      AltText = dto.AltText,
      Width = dto.Width,
      Height = dto.Height,
      SizeBytes = dto.SizeBytes,
      Resolution = dto.Resolution,
      UploadedAt = dto.UploadedAt,
      LastModified = dto.LastModified,
      ContentId = dto.ContentId,
      RelatedContentType = dto.RelatedContentType,
      ImagePurpose = dto.ImagePurpose,
      Duration = dto.Duration,
      VideoQuality = dto.VideoQuality,
      AudioDuration = dto.AudioDuration,
      AudioBitrate = dto.AudioBitrate
    };

    // Double check that model has valid CDN URLs
    return model.EnsureValidCdnUrls();
  }

  /// <summary>
  /// Converts a collection of MediaItemModels to MediaItemDTOs
  /// </summary>
  public static IEnumerable<MediaItemDTO> ToDTOs(IEnumerable<MediaItemModel> models)
  {
    return models?.Select(ToDTO) ?? Enumerable.Empty<MediaItemDTO>();
  }

  /// <summary>
  /// Helper method to determine video quality from resolution
  /// </summary>
  private static string DetermineVideoQuality(string resolution)
  {
    return resolution switch
    {
      "480p" => "SD",
      "720p" => "HD",
      "1080p" => "Full HD",
      "1440p" => "QHD",
      "2160p" => "4K",
      "4320p" => "8K",
      _ => "Unknown"
    };
  }
}
