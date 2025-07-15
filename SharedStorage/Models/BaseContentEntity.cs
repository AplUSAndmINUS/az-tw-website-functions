using Azure;
using Azure.Data.Tables;
using System.Text.Json;
using Utils.Extensions;

namespace SharedStorage.Models;

/// <summary>
/// Base entity for all content types stored in Azure Table Storage
/// </summary>
public abstract class BaseContentEntity : ITableEntity
{
  // Storage identifiers (required by ITableEntity)
  public string PartitionKey { get; set; } = string.Empty;
  public string RowKey { get; set; } = string.Empty;
  public DateTimeOffset? Timestamp { get; set; }
  public ETag ETag { get; set; }

  // Unique identifier
  public string Id { get; set; } = Guid.NewGuid().ToString();

  // Core content properties
  public string Title { get; set; } = string.Empty;
  public string AuthorSlug { get; set; } = string.Empty;
  public string Description { get; set; } = string.Empty;
  public string Content { get; set; } = string.Empty;
  public string Slug { get; set; } = string.Empty;
  public string Category { get; set; } = string.Empty;
  public string Status { get; set; } = "Draft";

  // Media references (storing IDs that point to media services)
  public string? FeaturedImageId { get; set; }        // Reference to primary image in ImageService
  public string? FeaturedMediaId { get; set; }        // Reference to primary media in MediaService
  public string? FeaturedVideoId { get; set; }        // Reference to primary video in VideoService
  public string MediaReferencesJson { get; set; } = "[]"; // Array of media IDs for additional attachments

  // Date properties
  public DateTime PublishDate { get; set; }
  public DateTime LastModified { get; set; }

  // Tags (stored as JSON string in Table Storage)
  public string TagsJson { get; set; } = "[]";

  // Computed property
  public bool IsPublished => Status == "Published";

  protected BaseContentEntity()
  {
    var now = DateTime.UtcNow;
    PublishDate = now;
    LastModified = now;
    // Keys will be set by the service layer for consistency
  }

  protected BaseContentEntity(DateTime publishDate)
  {
    PublishDate = publishDate.EnsureUtc();
    LastModified = DateTime.UtcNow;
    SetKeys(publishDate.EnsureUtc());
  }

  protected virtual void SetKeys(DateTime publishDate)
  {
    PartitionKey = publishDate.ToString("yyyy-MM");
    RowKey = $"{publishDate:yyyyMMddHHmmss}_{Id}";
  }

  // Method to update keys when PublishDate changes
  public virtual void UpdateKeys()
  {
    SetKeys(PublishDate);
  }

  // Abstract method that each content type will implement
  public abstract T ToModel<T>() where T : BaseContentModel;
}
