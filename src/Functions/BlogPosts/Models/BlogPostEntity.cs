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

  // Media properties (nullable)
  public string? MediaUrl { get; set; }
  public string? MediaDescription { get; set; }
  public string? ImageUrl { get; set; }
  public string? ImageDescription { get; set; }

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

    // Custom validation: At least one of ImageUrl or MediaUrl must be provided
    if (string.IsNullOrWhiteSpace(model.ImageUrl) && string.IsNullOrWhiteSpace(model.MediaUrl))
    {
      throw new ArgumentException("At least one of ImageUrl or MediaUrl must be provided.", nameof(model));
    }

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
      MediaUrl = model.MediaUrl,
      MediaDescription = model.MediaDescription,
      ImageUrl = model.ImageUrl,
      ImageDescription = model.ImageDescription,
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
      MediaUrl = MediaUrl,
      MediaDescription = MediaDescription,
      ImageUrl = ImageUrl,
      ImageDescription = ImageDescription,
      PublishDate = PublishDate,
      LastModified = LastModified,
      TagsList = string.IsNullOrEmpty(TagsJson) ? [] : JsonSerializer.Deserialize<string[]>(TagsJson) ?? []
    };
  }
}