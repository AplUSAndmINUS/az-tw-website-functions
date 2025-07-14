using System;

namespace SharedStorage.Models;

/// <summary>
/// A shared model for media items that can be used across different content types
/// (Authors, Blog Posts, Portfolio Pieces, etc.)
/// </summary>
public class MediaItemModel
{
  // Core identification properties
  public string Id { get; set; } = Guid.NewGuid().ToString();
  public string AuthorId { get; set; } = string.Empty;

  // Media content properties
  public string Filename { get; set; } = string.Empty;
  public string MediaType { get; set; } = string.Empty; // "image", "video", "audio", etc.
  public string Purpose { get; set; } = string.Empty; // "profile", "cover", "gallery", "featured", "content", etc.
  public string ContentType { get; set; } = string.Empty; // MIME type (e.g., "image/jpeg", "video/mp4")

  // URLs and presentation
  public string Url { get; set; } = string.Empty; // Full-size media URL
  public string ThumbnailUrl { get; set; } = string.Empty; // Thumbnail URL

  // Metadata
  public string Description { get; set; } = string.Empty;
  public string AltText { get; set; } = string.Empty;
  public int Width { get; set; }
  public int Height { get; set; }
  public long SizeBytes { get; set; }
  public string Resolution { get; set; } = string.Empty; // For images: "96dpi", for videos: "1080p", etc.

  // Timestamps
  public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
  public DateTime LastModified { get; set; } = DateTime.UtcNow;

  // Relationship tracking
  public string ContentId { get; set; } = string.Empty; // ID of the related content (blog post, portfolio piece, etc.)
  public string RelatedContentType { get; set; } = string.Empty; // Type of content this media is related to

  // Type-specific properties
  // For images
  public string ImagePurpose { get; set; } = string.Empty; // More specific image purposes

  // For videos
  public int Duration { get; set; } // In seconds
  public string VideoQuality { get; set; } = string.Empty; // "SD", "HD", "4K", etc.

  // For audio
  public int AudioDuration { get; set; } // In seconds
  public string AudioBitrate { get; set; } = string.Empty;

  // Future extensibility
  public string MetadataJson { get; set; } = "{}"; // Additional metadata as JSON
}
