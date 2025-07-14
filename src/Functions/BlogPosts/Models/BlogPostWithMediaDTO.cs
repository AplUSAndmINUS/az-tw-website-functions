using SharedStorage.Models;
using System;
using System.Collections.Generic;

namespace Functions.BlogPosts.Models;

public class BlogPostWithMediaDTO
{
  // Original BlogPost DTO properties
  public string Id { get; set; } = Guid.NewGuid().ToString();
  public string PartitionKey { get; set; } = string.Empty;
  public string RowKey { get; set; } = string.Empty;
  public DateTimeOffset? Timestamp { get; set; }
  public string Title { get; set; } = string.Empty;
  public string AuthorSlug { get; set; } = string.Empty;
  public string Description { get; set; } = string.Empty;
  public string Content { get; set; } = string.Empty;
  public string Slug { get; set; } = string.Empty;
  public string Category { get; set; } = string.Empty;
  public string Status { get; set; } = "Draft";

  // Media references (storing IDs that point to media services)
  public string? FeaturedImageId { get; set; }
  public string? FeaturedMediaId { get; set; }
  public string? FeaturedVideoId { get; set; }
  public string MediaReferencesJson { get; set; } = "[]";

  public DateTime PublishDate { get; set; }
  public DateTime LastModified { get; set; }
  public string[] TagsList { get; set; } = [];

  // Added properties with resolved media data
  public MediaEntity? FeaturedImage { get; set; }
  public MediaEntity? FeaturedMedia { get; set; }
  public MediaEntity? FeaturedVideo { get; set; }
  public List<MediaEntity> MediaReferences { get; set; } = new List<MediaEntity>();

  // Factory method to create from a BlogPostDTO
  public static BlogPostWithMediaDTO FromBlogPostDTO(BlogPostDTO dto)
  {
    return new BlogPostWithMediaDTO
    {
      Id = dto.Id,
      PartitionKey = dto.PartitionKey,
      RowKey = dto.RowKey,
      Timestamp = dto.Timestamp,
      Title = dto.Title,
      AuthorSlug = dto.AuthorSlug,
      Description = dto.Description,
      Content = dto.Content,
      Slug = dto.Slug,
      Category = dto.Category,
      Status = dto.Status,
      FeaturedImageId = dto.FeaturedImageId,
      FeaturedMediaId = dto.FeaturedMediaId,
      FeaturedVideoId = dto.FeaturedVideoId,
      MediaReferencesJson = dto.MediaReferencesJson,
      PublishDate = dto.PublishDate,
      LastModified = dto.LastModified,
      TagsList = dto.TagsList
    };
  }
}
