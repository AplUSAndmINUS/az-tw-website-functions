using System.Text.Json;
using SharedStorage.Models;
using Utils.Extensions;
using Utils.Validation;

namespace Functions.BlogPosts.Models;

/// <summary>
/// Entity class for blog posts stored in Azure Table Storage
/// </summary>
public class BlogPostEntity : BaseContentEntity
{
  public BlogPostEntity() : base()
  {
  }

  public BlogPostEntity(DateTime publishDate) : base(publishDate)
  {
  }

  /// <summary>
  /// Converts the entity to a model
  /// </summary>
  /// <typeparam name="T">The type of model to convert to</typeparam>
  /// <returns>The converted model</returns>
  public override T ToModel<T>()
  {
    if (typeof(T) != typeof(BlogPostModel))
      throw new ArgumentException($"Cannot convert BlogPostEntity to {typeof(T).Name}");

    var model = new BlogPostModel
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
      TagsList = DeserializeTags(TagsJson)
    };

    return (T)(object)model;
  }

  private string[] DeserializeTags(string tagsJson)
  {
    return DataValidation.DeserializeTags(tagsJson);
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
      PublishDate = model.PublishDate.EnsureUtc(),
      LastModified = model.LastModified.EnsureUtc(),
      TagsJson = JsonSerializer.Serialize(model.TagsList)
    };

    // NOTE: Keys should be set by the service layer for consistency
    // Do not set PartitionKey/RowKey here to avoid conflicts
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