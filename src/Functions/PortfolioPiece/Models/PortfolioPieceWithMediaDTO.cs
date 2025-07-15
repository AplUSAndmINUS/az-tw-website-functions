using SharedStorage.Models;
using Functions.Shared.Models;
using System.Collections.Generic;

namespace Functions.PortfolioPiece.Models;

/// <summary>
/// DTO for combining a portfolio piece with its associated media items
/// Enhanced to match BlogPostWithMediaDTO structure for consistency
/// </summary>
public class PortfolioPieceWithMediaDTO
{
  // Original PortfolioPiece DTO properties
  public PortfolioPieceDTO PortfolioPiece { get; set; } = new PortfolioPieceDTO();

  // Media items collection
  public List<MediaItemModel> MediaItems { get; set; } = new List<MediaItemModel>();

  // Convenience properties for featured media
  public MediaItemModel? FeaturedImage { get; set; }
  public MediaItemModel? FeaturedVideo { get; set; }
  public MediaItemModel? FeaturedMedia { get; set; }

  // Legacy media entity references (will be deprecated in future versions)
  public MediaEntity? LegacyFeaturedImage { get; set; }
  public MediaEntity? LegacyFeaturedMedia { get; set; }
  public MediaEntity? LegacyFeaturedVideo { get; set; }
  public List<MediaEntity> LegacyMediaReferences { get; set; } = new List<MediaEntity>();

  // Constructor with minimal setup
  public PortfolioPieceWithMediaDTO()
  {
    PortfolioPiece = new PortfolioPieceDTO();
    MediaItems = new List<MediaItemModel>();
  }

  // Constructor with portfolio piece and media items
  public PortfolioPieceWithMediaDTO(PortfolioPieceDTO piece, IEnumerable<MediaItemModel> mediaItems)
  {
    PortfolioPiece = piece;
    MediaItems = new List<MediaItemModel>(mediaItems);
    InitializeFeaturedMedia();
  }

  // Compatibility properties for backward compatibility with the original DTO
  // These properties delegate to the PortfolioPiece object

  public string Id
  {
    get => PortfolioPiece.Id;
    set => PortfolioPiece.Id = value;
  }

  public string PartitionKey
  {
    get => PortfolioPiece.PartitionKey;
    set => PortfolioPiece.PartitionKey = value;
  }

  public string RowKey
  {
    get => PortfolioPiece.RowKey;
    set => PortfolioPiece.RowKey = value;
  }

  public DateTimeOffset? Timestamp
  {
    get => PortfolioPiece.Timestamp;
    set => PortfolioPiece.Timestamp = value;
  }

  public string Title
  {
    get => PortfolioPiece.Title;
    set => PortfolioPiece.Title = value;
  }

  public string AuthorSlug
  {
    get => PortfolioPiece.AuthorSlug;
    set => PortfolioPiece.AuthorSlug = value;
  }

  public string Description
  {
    get => PortfolioPiece.Description;
    set => PortfolioPiece.Description = value;
  }

  public string Content
  {
    get => PortfolioPiece.Content;
    set => PortfolioPiece.Content = value;
  }

  public string Slug
  {
    get => PortfolioPiece.Slug;
    set => PortfolioPiece.Slug = value;
  }

  public string Category
  {
    get => PortfolioPiece.Category;
    set => PortfolioPiece.Category = value;
  }

  public string Status
  {
    get => PortfolioPiece.Status;
    set => PortfolioPiece.Status = value;
  }

  // Media reference IDs
  public string? FeaturedImageId
  {
    get => PortfolioPiece.FeaturedImageId;
    set => PortfolioPiece.FeaturedImageId = value;
  }

  public string? FeaturedMediaId
  {
    get => PortfolioPiece.FeaturedMediaId;
    set => PortfolioPiece.FeaturedMediaId = value;
  }

  public string? FeaturedVideoId
  {
    get => PortfolioPiece.FeaturedVideoId;
    set => PortfolioPiece.FeaturedVideoId = value;
  }

  public string MediaReferencesJson
  {
    get => PortfolioPiece.MediaReferencesJson;
    set => PortfolioPiece.MediaReferencesJson = value;
  }

  public DateTime PublishDate
  {
    get => PortfolioPiece.PublishDate;
    set => PortfolioPiece.PublishDate = value;
  }

  public DateTime LastModified
  {
    get => PortfolioPiece.LastModified;
    set => PortfolioPiece.LastModified = value;
  }

  public string[] TagsList
  {
    get => PortfolioPiece.TagsList;
    set => PortfolioPiece.TagsList = value;
  }

  // Compatibility property for backward compatibility (will be removed in future versions)
  public PortfolioPieceModel Post
  {
    get => new PortfolioPieceModel
    {
      Id = PortfolioPiece.Id,
      Title = PortfolioPiece.Title,
      AuthorSlug = PortfolioPiece.AuthorSlug,
      Description = PortfolioPiece.Description,
      Content = PortfolioPiece.Content,
      Slug = PortfolioPiece.Slug,
      Category = PortfolioPiece.Category,
      Status = PortfolioPiece.Status,
      FeaturedImageId = PortfolioPiece.FeaturedImageId,
      FeaturedMediaId = PortfolioPiece.FeaturedMediaId,
      FeaturedVideoId = PortfolioPiece.FeaturedVideoId,
      MediaReferencesJson = PortfolioPiece.MediaReferencesJson,
      PublishDate = PortfolioPiece.PublishDate,
      LastModified = PortfolioPiece.LastModified,
      TagsList = PortfolioPiece.TagsList
    };
    set
    {
      PortfolioPiece.Id = value.Id;
      PortfolioPiece.Title = value.Title;
      PortfolioPiece.AuthorSlug = value.AuthorSlug;
      PortfolioPiece.Description = value.Description;
      PortfolioPiece.Content = value.Content;
      PortfolioPiece.Slug = value.Slug;
      PortfolioPiece.Category = value.Category;
      PortfolioPiece.Status = value.Status;
      PortfolioPiece.FeaturedImageId = value.FeaturedImageId;
      PortfolioPiece.FeaturedMediaId = value.FeaturedMediaId;
      PortfolioPiece.FeaturedVideoId = value.FeaturedVideoId;
      PortfolioPiece.MediaReferencesJson = value.MediaReferencesJson;
      PortfolioPiece.PublishDate = value.PublishDate;
      PortfolioPiece.LastModified = value.LastModified;
      PortfolioPiece.TagsList = value.TagsList;
    }
  }

  // Factory method to create from a PortfolioPieceDTO
  public static PortfolioPieceWithMediaDTO FromPortfolioPieceDTO(PortfolioPieceDTO dto)
  {
    return new PortfolioPieceWithMediaDTO
    {
      PortfolioPiece = dto
    };
  }

  // Helper method to initialize featured media from the full list
  private void InitializeFeaturedMedia()
  {
    foreach (var media in MediaItems)
    {
      if (media.MediaType?.ToLowerInvariant() == "image" &&
          (media.Purpose?.Contains("featured") == true || media.Purpose?.Contains("cover") == true))
      {
        FeaturedImage = media;
      }
      else if (media.MediaType?.ToLowerInvariant() == "video" &&
               (media.Purpose?.Contains("featured") == true || media.Purpose?.Contains("intro") == true))
      {
        FeaturedVideo = media;
      }
      else if (media.Purpose?.Contains("featured") == true)
      {
        FeaturedMedia = media;
      }
    }
  }

  // Method to convert legacy MediaEntity objects to MediaItemModel objects
  public void MigrateMediaEntities()
  {
    // Convert FeaturedImage
    if (LegacyFeaturedImage != null)
    {
      var model = MediaItemMapper.ToModel(LegacyFeaturedImage);
      model.Purpose = "featured";
      MediaItems.Add(model);
      FeaturedImage = model;
    }

    // Convert FeaturedVideo
    if (LegacyFeaturedVideo != null)
    {
      var model = MediaItemMapper.ToModel(LegacyFeaturedVideo);
      model.Purpose = "featured";
      MediaItems.Add(model);
      FeaturedVideo = model;
    }

    // Convert FeaturedMedia
    if (LegacyFeaturedMedia != null)
    {
      var model = MediaItemMapper.ToModel(LegacyFeaturedMedia);
      model.Purpose = "featured";
      MediaItems.Add(model);
      FeaturedMedia = model;
    }

    // Convert MediaReferences
    foreach (var entity in LegacyMediaReferences)
    {
      var model = MediaItemMapper.ToModel(entity);
      model.Purpose = "content";
      MediaItems.Add(model);
    }
  }
}