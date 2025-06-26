using Azure;
using Azure.Data.Tables;
using Utils.Validation;
using System.Text.Json;

namespace Functions.BlogPosts.Models;

public class BlogPostEntity : ITableEntity
{
  public string Id { get; set; } = Guid.NewGuid().ToString();
  public string PartitionKey { get; set; } = string.Empty;
  public string RowKey { get; set; } = string.Empty;
  public DateTimeOffset? Timestamp { get; set; }
  public ETag ETag { get; set; }

  // Core blog properties
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

  public BlogPostEntity()
  {
    var now = DateTime.UtcNow;
    PublishDate = now;
    LastModified = now;
    // Keys will be set when entity is saved or explicitly updated
  }

  public BlogPostEntity(DateTime publishDate)
  {
    PublishDate = publishDate;
    LastModified = DateTime.UtcNow;
    SetKeys(publishDate);
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

  public static BlogPostEntity FromModel(BlogPostModel model)
  {
    // Validate required fields
    ArgumentNullException.ThrowIfNull(model);
    ArgumentNullException.ThrowIfNull(model.Title);
    ArgumentNullException.ThrowIfNull(model.AuthorSlug);
    ArgumentNullException.ThrowIfNull(model.Content);
    ArgumentNullException.ThrowIfNull(model.Slug);
    ArgumentNullException.ThrowIfNull(model.Category);
    ArgumentNullException.ThrowIfNull(model.TagsList);

    // Custom validation: At least one of FeaturedImageId or FeaturedMediaId should be provided for better UX
    // Note: This is optional validation - blog posts can exist without media
    // if (string.IsNullOrWhiteSpace(model.FeaturedImageId) && string.IsNullOrWhiteSpace(model.FeaturedMediaId))
    // {
    //   throw new ArgumentException("Consider providing at least one featured media item for better user experience.", nameof(model));
    // }

    var entity = new BlogPostEntity
    {
      Id = model.Id ?? Guid.NewGuid().ToString(),
      Title = DataValidation.Required(DataValidation.SafeTrim(model.Title), nameof(model.Title)),
      AuthorSlug = DataValidation.Required(DataValidation.SafeTrim(model.AuthorSlug), nameof(model.AuthorSlug)),
      Description = DataValidation.SafeTrim(model.Description) ?? string.Empty,
      Content = DataValidation.Required(DataValidation.SafeTrim(model.Content), nameof(model.Content)),
      Slug = DataValidation.Required(DataValidation.SafeTrim(model.Slug), nameof(model.Slug)),
      Category = DataValidation.Required(DataValidation.SafeTrim(model.Category), nameof(model.Category)),
      Status = DataValidation.SafeTrim(model.Status) ?? "Draft",
      FeaturedImageId = model.FeaturedImageId,
      FeaturedMediaId = model.FeaturedMediaId,
      MediaReferencesJson = model.MediaReferencesJson ?? "[]",
      PublishDate = model.PublishDate,
      LastModified = model.LastModified,
      TagsJson = JsonSerializer.Serialize(model.TagsList)
    };

    // Set keys after all properties are set
    entity.SetKeys(entity.PublishDate);
    return entity;
  }

  public BlogPostModel ToModel()
  {
    return new BlogPostModel
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
      MediaReferencesJson = MediaReferencesJson,
      PublishDate = PublishDate,
      LastModified = LastModified,
      TagsList = string.IsNullOrEmpty(TagsJson) ? [] : JsonSerializer.Deserialize<string[]>(TagsJson) ?? []
    };
  }
}