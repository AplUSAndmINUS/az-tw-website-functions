using SharedStorage.Models;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Functions.BlogPosts.Models;

/// <summary>
/// DTO for combining a blog post with its associated media items
/// Enhanced to use shared MediaItemModel across all content types
/// </summary>
public class BlogPostWithMediaDTO
{
  // Original BlogPost DTO properties
  public BlogPostDTO BlogPost { get; set; } = new BlogPostDTO();

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
  public BlogPostWithMediaDTO()
  {
    BlogPost = new BlogPostDTO();
    MediaItems = new List<MediaItemModel>();
  }

  // Constructor with post and media items
  public BlogPostWithMediaDTO(BlogPostDTO post, IEnumerable<MediaItemModel> mediaItems)
  {
    BlogPost = post;
    MediaItems = new List<MediaItemModel>(mediaItems);
    InitializeFeaturedMedia();
  }

  // Compatibility properties for backward compatibility with the original DTO
  // These properties delegate to the BlogPost object

  public string Id
  {
    get => BlogPost.Id;
    set => BlogPost.Id = value;
  }

  public string PartitionKey
  {
    get => BlogPost.PartitionKey;
    set => BlogPost.PartitionKey = value;
  }

  public string RowKey
  {
    get => BlogPost.RowKey;
    set => BlogPost.RowKey = value;
  }

  public DateTimeOffset? Timestamp
  {
    get => BlogPost.Timestamp;
    set => BlogPost.Timestamp = value;
  }

  public string Title
  {
    get => BlogPost.Title;
    set => BlogPost.Title = value;
  }

  public string AuthorSlug
  {
    get => BlogPost.AuthorSlug;
    set => BlogPost.AuthorSlug = value;
  }

  public string Description
  {
    get => BlogPost.Description;
    set => BlogPost.Description = value;
  }

  public string Content
  {
    get => BlogPost.Content;
    set => BlogPost.Content = value;
  }

  public string Slug
  {
    get => BlogPost.Slug;
    set => BlogPost.Slug = value;
  }

  public string Category
  {
    get => BlogPost.Category;
    set => BlogPost.Category = value;
  }

  public string Status
  {
    get => BlogPost.Status;
    set => BlogPost.Status = value;
  }

  // Media reference IDs
  public string? FeaturedImageId
  {
    get => BlogPost.FeaturedImageId;
    set => BlogPost.FeaturedImageId = value;
  }

  public string? FeaturedMediaId
  {
    get => BlogPost.FeaturedMediaId;
    set => BlogPost.FeaturedMediaId = value;
  }

  public string? FeaturedVideoId
  {
    get => BlogPost.FeaturedVideoId;
    set => BlogPost.FeaturedVideoId = value;
  }

  public string MediaReferencesJson
  {
    get => BlogPost.MediaReferencesJson;
    set => BlogPost.MediaReferencesJson = value;
  }

  public DateTime PublishDate
  {
    get => BlogPost.PublishDate;
    set => BlogPost.PublishDate = value;
  }

  public DateTime LastModified
  {
    get => BlogPost.LastModified;
    set => BlogPost.LastModified = value;
  }

  public string[] TagsList
  {
    get => BlogPost.TagsList;
    set => BlogPost.TagsList = value;
  }

  // Factory method to create from a BlogPostDTO
  public static BlogPostWithMediaDTO FromBlogPostDTO(BlogPostDTO dto)
  {
    return new BlogPostWithMediaDTO
    {
      BlogPost = dto
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
