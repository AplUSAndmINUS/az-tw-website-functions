using Azure.Data.Tables;
using Azure;

namespace SharedStorage.Models;

// Base class shared by all media types
public class MediaEntity : ITableEntity
{
  public string Id { get; set; } = Guid.NewGuid().ToString();
  public string PartitionKey { get; set; }
  public string RowKey { get; set; }
  public DateTimeOffset? Timestamp { get; set; }
  public ETag ETag { get; set; }

  public string AuthorId { get; set; } = string.Empty;
  public string Filename { get; set; } = string.Empty;
  public string MediaType { get; set; } = string.Empty; // e.g. "image", "video"
  public string Purpose { get; set; } = string.Empty; // "coverImage", "introVideo"
  public string Url { get; set; } = string.Empty;
  public string Description { get; set; } = string.Empty;
  public string AltText { get; set; } = string.Empty;

  public string ThumbnailUrl { get; set; } = string.Empty; // URL for a smaller version of the media
  public string ContentType { get; set; } = string.Empty; // MIME type of the media (e.g., "image/jpeg", "video/mp4")

  public string? ContentId { get; set; } // ID of the content this media is associated with (blog post, portfolio piece, etc.)
  public string? RelatedContentType { get; set; } // Type of the related content (blog, portfolio, etc.)

  public int Width { get; set; }
  public int Height { get; set; }

  public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

  // External platform properties
  public string Platform { get; set; } = string.Empty; // "blob", "tiktok", "instagram", "youtube", "facebook", "linkedin", "pinterest"
  public string ExternalId { get; set; } = string.Empty; // External platform's unique identifier
  public string ExternalUrl { get; set; } = string.Empty; // Direct link to external platform content
  public string EmbedCode { get; set; } = string.Empty; // HTML embed code for external content
  public DateTime? ExternalCreatedAt { get; set; } // When content was created on external platform
  public DateTime? LastSyncedAt { get; set; } // When metadata was last synced from external platform

  // Platform-specific metadata
  public string PlatformMetadata { get; set; } = string.Empty; // JSON string with platform-specific data
  public int LikeCount { get; set; } = 0; // External platform likes/reactions
  public int ShareCount { get; set; } = 0; // External platform shares
  public int ViewCount { get; set; } = 0; // External platform views
  public string Tags { get; set; } = string.Empty; // Comma-separated tags from external platform

  public MediaEntity()
  {
    PartitionKey = AuthorId;
    RowKey = Id;
  }
}