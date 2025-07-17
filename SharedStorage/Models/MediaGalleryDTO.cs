namespace SharedStorage.Models;

/// <summary>
/// DTO for Media Gallery to be used in API responses
/// This represents media from both blob storage and external platforms
/// </summary>
public class MediaGalleryDTO
{
  // Core identification
  public string Id { get; set; } = string.Empty;
  public string AuthorId { get; set; } = string.Empty;
  
  // Content properties
  public string Title { get; set; } = string.Empty; // Display title for the media
  public string Description { get; set; } = string.Empty;
  public string MediaType { get; set; } = string.Empty; // "image", "video", "audio"
  public string ContentType { get; set; } = string.Empty; // MIME type
  
  // Display URLs
  public string Url { get; set; } = string.Empty; // CDN URL for blob storage, external URL for platforms
  public string ThumbnailUrl { get; set; } = string.Empty;
  public string AltText { get; set; } = string.Empty;
  
  // Dimensions
  public int Width { get; set; }
  public int Height { get; set; }
  
  // Platform information
  public string Platform { get; set; } = string.Empty; // "blob", "tiktok", "instagram", etc.
  public string PlatformDisplayName { get; set; } = string.Empty; // "Blob Storage", "TikTok", "Instagram", etc.
  public string ExternalUrl { get; set; } = string.Empty; // Direct link to external platform
  public string EmbedCode { get; set; } = string.Empty; // HTML embed code
  
  // Engagement metrics
  public int LikeCount { get; set; }
  public int ShareCount { get; set; }
  public int ViewCount { get; set; }
  
  // Metadata
  public string[] Tags { get; set; } = Array.Empty<string>();
  public DateTime CreatedAt { get; set; } // When content was created (external platform date or upload date)
  public DateTime LastUpdated { get; set; } // When metadata was last updated
  
  // Video-specific properties
  public int Duration { get; set; } // Duration in seconds for videos
  public string VideoQuality { get; set; } = string.Empty;
  
  // Audio-specific properties  
  public int AudioDuration { get; set; } // Duration in seconds for audio
  public string AudioBitrate { get; set; } = string.Empty;
  
  // Gallery-specific properties
  public string Purpose { get; set; } = string.Empty; // "gallery", "featured", etc.
  public bool IsExternal { get; set; } // True if from external platform, false if from blob storage
  public bool IsAvailable { get; set; } = true; // False if external content is no longer available
  
  // Sorting and filtering helpers
  public string SortKey { get; set; } = string.Empty; // For custom sorting
  public string Category { get; set; } = string.Empty; // For categorization
}

/// <summary>
/// Response wrapper for media gallery API calls
/// </summary>
public class MediaGalleryResponse
{
  public IEnumerable<MediaGalleryDTO> Media { get; set; } = Enumerable.Empty<MediaGalleryDTO>();
  public int TotalCount { get; set; }
  public int PageSize { get; set; }
  public string? NextPageToken { get; set; }
  public DateTime LastSyncTime { get; set; }
  public string[] AvailablePlatforms { get; set; } = Array.Empty<string>();
  public string[] AvailableMediaTypes { get; set; } = Array.Empty<string>();
}