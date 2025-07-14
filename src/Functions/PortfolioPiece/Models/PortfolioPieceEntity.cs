using Azure;
using Azure.Data.Tables;
using System.Text.Json;

using Utils.Validation;
using Utils.Extensions;
using Functions.PortfolioPiece.Models;

namespace Functions.PortfolioPieces.Models;

public class PortfolioPieceEntity : ITableEntity
{
  public string Id { get; set; } = Guid.NewGuid().ToString();
  public string PartitionKey { get; set; } = string.Empty;
  public string RowKey { get; set; } = string.Empty;
  public DateTimeOffset? Timestamp { get; set; }
  public ETag ETag { get; set; }

  // Core portfolio piece properties
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

  // Computed properties
  public bool IsPublished => Status == "Published";

  public PortfolioPieceEntity()
  {
    var now = DateTime.UtcNow;
    PublishDate = now;
    LastModified = now;
    // Keys will be set by the service layer for consistency
  }

  public PortfolioPieceEntity(DateTime publishDate)
  {
    PublishDate = publishDate.EnsureUtc();
    LastModified = DateTime.UtcNow;
    SetKeys(publishDate.EnsureUtc());
  }

  private void SetKeys(DateTime publishDate)
  {
    PartitionKey = publishDate.ToString("yyyy-MM");
    RowKey = $"{publishDate:yyyyMMddHHmmss}_{Id}";
  }

  // Method to update keys when PublishDate changes
  public void UpdateKeys()
  {
    SetKeys(PublishDate);
  }

  public static PortfolioPieceEntity FromModel(PortfolioPieceModel model)
  {
    // Validate required fields
    ArgumentNullException.ThrowIfNull(model);
    DataValidation.ValidateContentRequiredFields(
      model.Title,
      model.AuthorSlug,
      model.Content,
      model.Slug,
      model.Category
    );
    ArgumentNullException.ThrowIfNull(model.TagsList);

    // Ensure status and isPublished are in sync
    string status = DataValidation.EnsureStatusConsistency(model.Status, model.IsPublished);

    var entity = new PortfolioPieceEntity
    {
      Id = model.Id ?? Guid.NewGuid().ToString(),
      Title = DataValidation.Required(DataValidation.SafeTrim(model.Title, 200), nameof(model.Title)),
      Description = DataValidation.SafeTrim(model.Description, 500) ?? string.Empty,
      AuthorSlug = DataValidation.Required(DataValidation.SafeTrim(model.AuthorSlug, 100), nameof(model.AuthorSlug)),
      Slug = DataValidation.Required(DataValidation.SafeTrim(model.Slug, 100), nameof(model.Slug)),
      Category = DataValidation.Required(DataValidation.SafeTrim(model.Category, 50), nameof(model.Category)),
      Status = status,
      FeaturedImageId = model.FeaturedImageId,
      FeaturedMediaId = model.FeaturedMediaId,
      FeaturedVideoId = model.FeaturedVideoId,
      MediaReferencesJson = model.MediaReferencesJson ?? "[]",
      PublishDate = PortfolioPieceMapper.EnsureValidPublishDate(model.PublishDate, status),
      LastModified = DateTime.UtcNow, // Always set to current time on creation
      TagsJson = JsonSerializer.Serialize(model.TagsList)
    };

    // Set keys for consistency
    entity.UpdateKeys();

    return entity;
  }

  public PortfolioPieceModel ToModel()
  {
    return new PortfolioPieceModel
    {
      Id = Id,
      PartitionKey = PartitionKey,
      RowKey = RowKey,
      Timestamp = Timestamp,
      ETag = ETag,
      Title = Title,
      AuthorSlug = AuthorSlug,
      Description = Description,
      Content = Content,
      Slug = Slug,
      Category = Category,
      Status = Status,
      FeaturedImageId = FeaturedImageId,
      FeaturedMediaId = FeaturedMediaId,
      FeaturedVideoId = FeaturedVideoId,
      MediaReferencesJson = MediaReferencesJson ?? "[]",
      PublishDate = PublishDate.EnsureUtc(),
      LastModified = LastModified.EnsureUtc(),
      TagsList = string.IsNullOrEmpty(TagsJson) ? Array.Empty<string>() : JsonSerializer.Deserialize<string[]>(TagsJson) ?? Array.Empty<string>()
    };
  }
}