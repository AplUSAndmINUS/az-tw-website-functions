namespace SharedStorage.Models;

/// <summary>
/// DTO for MediaItem to be used in API responses
/// </summary>
public class MediaItemDTO
{
  // Core identification properties
  public string Id { get; set; } = string.Empty;
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
  public DateTime UploadedAt { get; set; }
  public DateTime LastModified { get; set; }

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

  // External platform support
  public bool IsExternal { get; set; } = false; // True if this media is from an external platform
  public string Platform { get; set; } = string.Empty; // Platform name (TikTok, Instagram, YouTube, Facebook, LinkedIn, Pinterest, BlobStorage)
  public string ExternalId { get; set; } = string.Empty; // Platform-specific ID for the media
  public string ExternalUrl { get; set; } = string.Empty; // Original URL on the external platform
}
