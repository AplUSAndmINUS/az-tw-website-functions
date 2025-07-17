using SharedStorage.Models;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Functions.Books.Models;

/// <summary>
/// DTO for combining a book with its associated media items
/// Enhanced to use shared MediaItemModel across all content types
/// </summary>
public class BookWithMediaDTO
{
  // Original Book DTO properties
  public BookDTO Book { get; set; } = new BookDTO();

  // Media items collection
  public List<MediaItemModel> MediaItems { get; set; } = new List<MediaItemModel>();

  // Convenience properties for featured media
  public MediaItemModel? FeaturedImage { get; set; }
  public MediaItemModel? FeaturedVideo { get; set; }

  // Legacy media entity references (will be deprecated in future versions)
  public MediaEntity? LegacyFeaturedImage { get; set; }
  public MediaEntity? LegacyFeaturedMedia { get; set; }
  public MediaEntity? LegacyFeaturedVideo { get; set; }
  public MediaItemModel? FeaturedMedia { get; set; } // New property for featured media
  public List<MediaEntity> LegacyMediaReferences { get; set; } = new List<MediaEntity>();

  // Constructor with minimal setup
  public BookWithMediaDTO()
  {
    Book = new BookDTO();
    MediaItems = new List<MediaItemModel>();
  }

  // Constructor with book and media items
  public BookWithMediaDTO(BookDTO book, IEnumerable<MediaItemModel> mediaItems)
  {
    Book = book;
    MediaItems = new List<MediaItemModel>(mediaItems);
    InitializeFeaturedMedia();
  }

  // Compatibility properties for backward compatibility with the original DTO
  // These properties delegate to the Book object

  public string Id
  {
    get => Book.Id;
    set => Book.Id = value;
  }

  public string PartitionKey
  {
    get => Book.PartitionKey;
    set => Book.PartitionKey = value;
  }

  public string RowKey
  {
    get => Book.RowKey;
    set => Book.RowKey = value;
  }

  public DateTimeOffset? Timestamp
  {
    get => Book.Timestamp;
    set => Book.Timestamp = value;
  }

  public string Title
  {
    get => Book.Title;
    set => Book.Title = value;
  }

  public string AuthorSlug
  {
    get => Book.AuthorSlug;
    set => Book.AuthorSlug = value;
  }

  public string Description
  {
    get => Book.Description;
    set => Book.Description = value;
  }

  public string Content
  {
    get => Book.Content;
    set => Book.Content = value;
  }

  public string Slug
  {
    get => Book.Slug;
    set => Book.Slug = value;
  }

  public string Category
  {
    get => Book.Category;
    set => Book.Category = value;
  }

  public string Status
  {
    get => Book.Status;
    set => Book.Status = value;
  }

  // Media reference IDs
  public string? FeaturedImageId
  {
    get => Book.FeaturedImageId;
    set => Book.FeaturedImageId = value;
  }

  public string? FeaturedMediaId
  {
    get => Book.FeaturedMediaId;
    set => Book.FeaturedMediaId = value;
  }

  public string? FeaturedVideoId
  {
    get => Book.FeaturedVideoId;
    set => Book.FeaturedVideoId = value;
  }

  public string MediaReferencesJson
  {
    get => Book.MediaReferencesJson;
    set => Book.MediaReferencesJson = value;
  }

  public DateTime PublishDate
  {
    get => Book.PublishDate;
    set => Book.PublishDate = value;
  }

  public DateTime LastModified
  {
    get => Book.LastModified;
    set => Book.LastModified = value;
  }

  public string[] TagsList
  {
    get => Book.TagsList;
    set => Book.TagsList = value;
  }

  // Factory method to create from a BookDTO
  public static BookWithMediaDTO FromBookDTO(BookDTO dto)
  {
    return new BookWithMediaDTO
    {
      Book = dto
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