using Azure;

namespace Functions.PortfolioPiece.Models;

public class PortfolioPieceModel
{
  public string Id { get; set; } = Guid.NewGuid().ToString();
  public string PartitionKey { get; set; } = string.Empty;
  public string RowKey { get; set; } = string.Empty;
  public DateTimeOffset? Timestamp { get; set; }
  public ETag ETag { get; set; }

  // Core portfolio properties
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

  // Tags as array (for API/business logic)
  public string[] TagsList { get; set; } = [];

  // Computed properties
  public bool IsPublished => Status == "Published";
}