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

  public int Width { get; set; }
  public int Height { get; set; }

  public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

  public MediaEntity()
  {
    PartitionKey = AuthorId;
    RowKey = Id;
  }
} 