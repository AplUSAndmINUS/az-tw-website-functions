using System.Text.Json;
using Utils.Validation;
using Functions.BlogPosts.Models;

namespace Functions.BlogPosts.Mappers;

public static class BlogPostMapper
{
  /// <summary>
  /// Converts a BlogPostModel to BlogPostEntity for storage operations
  /// </summary>
  /// <param name="model">The BlogPostModel to convert</param>
  /// <returns>A BlogPostEntity ready for storage</returns>
  /// <exception cref="ArgumentNullException">Thrown when model or required fields are null</exception>
  /// <exception cref="ArgumentException">Thrown when validation fails</exception>
  public static BlogPostEntity ToEntity(BlogPostModel model)
  {
    // Validate required fields
    ArgumentNullException.ThrowIfNull(model);
    ArgumentNullException.ThrowIfNull(model.Title);
    ArgumentNullException.ThrowIfNull(model.AuthorSlug);
    ArgumentNullException.ThrowIfNull(model.Content);
    ArgumentNullException.ThrowIfNull(model.Slug);
    ArgumentNullException.ThrowIfNull(model.Category);
    ArgumentNullException.ThrowIfNull(model.TagsList);

    // Media validation is now optional since media references can be added later
    // Business logic can enforce media requirements at the service level if needed

    var entity = new BlogPostEntity
    {
      Id = model.Id ?? Guid.NewGuid().ToString(),
      Title = DataValidation.Required(DataValidation.SafeTrim(model.Title, 200), nameof(model.Title)),
      AuthorSlug = DataValidation.Required(DataValidation.SafeTrim(model.AuthorSlug, 100), nameof(model.AuthorSlug)),
      Description = DataValidation.SafeTrim(model.Description, 500) ?? string.Empty,
      Content = DataValidation.Required(DataValidation.SafeTrim(model.Content, 50000), nameof(model.Content)),
      Slug = DataValidation.Required(DataValidation.SafeTrim(model.Slug, 100), nameof(model.Slug)),
      Category = DataValidation.Required(DataValidation.SafeTrim(model.Category, 50), nameof(model.Category)),
      Status = DataValidation.SafeTrim(model.Status, 20) ?? "Draft",
      FeaturedImageId = model.FeaturedImageId,
      FeaturedMediaId = model.FeaturedMediaId,
      FeaturedVideoId = model.FeaturedVideoId,
      MediaReferencesJson = model.MediaReferencesJson ?? "[]",
      PublishDate = model.PublishDate,
      LastModified = DateTime.UtcNow, // Always update LastModified on conversion
      TagsJson = JsonSerializer.Serialize(model.TagsList),
      PartitionKey = model.PartitionKey,
      RowKey = model.RowKey,
      Timestamp = model.Timestamp,
      ETag = model.ETag
    };

    // Set keys if not already provided
    if (string.IsNullOrEmpty(entity.PartitionKey) || string.IsNullOrEmpty(entity.RowKey))
    {
      entity.UpdateKeys();
    }

    return entity;
  }

  /// <summary>
  /// Converts a BlogPostEntity to BlogPostModel for business logic operations
  /// </summary>
  /// <param name="entity">The BlogPostEntity to convert</param>
  /// <returns>A BlogPostModel for business operations</returns>
  /// <exception cref="ArgumentNullException">Thrown when entity is null</exception>
  public static BlogPostModel ToModel(BlogPostEntity entity)
  {
    ArgumentNullException.ThrowIfNull(entity);

    return new BlogPostModel
    {
      Id = entity.Id,
      PartitionKey = entity.PartitionKey,
      RowKey = entity.RowKey,
      Timestamp = entity.Timestamp,
      ETag = entity.ETag,
      Title = entity.Title,
      AuthorSlug = entity.AuthorSlug,
      Description = entity.Description,
      Content = entity.Content,
      Slug = entity.Slug,
      Category = entity.Category,
      Status = entity.Status,
      FeaturedImageId = entity.FeaturedImageId,
      FeaturedMediaId = entity.FeaturedMediaId,
      FeaturedVideoId = entity.FeaturedVideoId,
      MediaReferencesJson = entity.MediaReferencesJson,
      PublishDate = entity.PublishDate,
      LastModified = entity.LastModified,
      TagsList = DeserializeTags(entity.TagsJson)
    };
  }

  /// <summary>
  /// Converts a BlogPostModel to BlogPostDTO for API responses
  /// </summary>
  /// <param name="model">The BlogPostModel to convert</param>
  /// <returns>A BlogPostDTO for API responses</returns>
  /// <exception cref="ArgumentNullException">Thrown when model is null</exception>
  public static BlogPostDTO ToDTO(BlogPostModel model)
  {
    ArgumentNullException.ThrowIfNull(model);

    return new BlogPostDTO
    {
      Id = model.Id,
      PartitionKey = model.PartitionKey,
      RowKey = model.RowKey,
      Timestamp = model.Timestamp,
      Title = model.Title,
      AuthorSlug = model.AuthorSlug,
      Description = model.Description,
      Content = model.Content,
      Slug = model.Slug,
      Category = model.Category,
      Status = model.Status,
      FeaturedImageId = model.FeaturedImageId,
      FeaturedMediaId = model.FeaturedMediaId,
      FeaturedVideoId = model.FeaturedVideoId,
      MediaReferencesJson = model.MediaReferencesJson,
      PublishDate = model.PublishDate,
      LastModified = model.LastModified,
      TagsList = model.TagsList
    };
  }

  /// <summary>
  /// Converts a BlogPostEntity directly to BlogPostDTO for API responses
  /// </summary>
  /// <param name="entity">The BlogPostEntity to convert</param>
  /// <returns>A BlogPostDTO for API responses</returns>
  /// <exception cref="ArgumentNullException">Thrown when entity is null</exception>
  public static BlogPostDTO EntityToDTO(BlogPostEntity entity)
  {
    ArgumentNullException.ThrowIfNull(entity);

    return new BlogPostDTO
    {
      Id = entity.Id,
      PartitionKey = entity.PartitionKey,
      RowKey = entity.RowKey,
      Timestamp = entity.Timestamp,
      Title = entity.Title,
      AuthorSlug = entity.AuthorSlug,
      Description = entity.Description,
      Content = entity.Content,
      Slug = entity.Slug,
      Category = entity.Category,
      Status = entity.Status,
      FeaturedImageId = entity.FeaturedImageId,
      FeaturedMediaId = entity.FeaturedMediaId,
      FeaturedVideoId = entity.FeaturedVideoId,
      MediaReferencesJson = entity.MediaReferencesJson,
      PublishDate = entity.PublishDate,
      LastModified = entity.LastModified,
      TagsList = DeserializeTags(entity.TagsJson)
    };
  }

  /// <summary>
  /// Converts a BlogPostDTO to BlogPostModel for business logic operations
  /// </summary>
  /// <param name="dto">The BlogPostDTO to convert</param>
  /// <returns>A BlogPostModel for business operations</returns>
  /// <exception cref="ArgumentNullException">Thrown when dto is null</exception>
  public static BlogPostModel FromDTO(BlogPostDTO dto)
  {
    ArgumentNullException.ThrowIfNull(dto);

    return new BlogPostModel
    {
      Id = dto.Id,
      PartitionKey = dto.PartitionKey,
      RowKey = dto.RowKey,
      Timestamp = dto.Timestamp,
      ETag = default, // DTOs don't carry ETags
      Title = dto.Title,
      AuthorSlug = dto.AuthorSlug,
      Description = dto.Description,
      Content = dto.Content,
      Slug = dto.Slug,
      Category = dto.Category,
      Status = dto.Status,
      FeaturedImageId = dto.FeaturedImageId,
      FeaturedMediaId = dto.FeaturedMediaId,
      FeaturedVideoId = dto.FeaturedVideoId,
      MediaReferencesJson = dto.MediaReferencesJson,
      PublishDate = dto.PublishDate,
      LastModified = dto.LastModified,
      TagsList = dto.TagsList
    };
  }

  /// <summary>
  /// Converts a collection of BlogPostEntities to BlogPostDTOs
  /// </summary>
  /// <param name="entities">The collection of entities to convert</param>
  /// <returns>A collection of DTOs</returns>
  public static IEnumerable<BlogPostDTO> EntitiesToDTOs(IEnumerable<BlogPostEntity> entities)
  {
    return entities?.Select(EntityToDTO) ?? Enumerable.Empty<BlogPostDTO>();
  }

  /// <summary>
  /// Converts a collection of BlogPostModels to BlogPostDTOs
  /// </summary>
  /// <param name="models">The collection of models to convert</param>
  /// <returns>A collection of DTOs</returns>
  public static IEnumerable<BlogPostDTO> ModelsToDTOs(IEnumerable<BlogPostModel> models)
  {
    return models?.Select(ToDTO) ?? Enumerable.Empty<BlogPostDTO>();
  }

  /// <summary>
  /// Updates an existing BlogPostEntity with values from a BlogPostModel
  /// </summary>
  /// <param name="entity">The entity to update</param>
  /// <param name="model">The model containing new values</param>
  /// <exception cref="ArgumentNullException">Thrown when entity or model is null</exception>
  public static void UpdateEntityFromModel(BlogPostEntity entity, BlogPostModel model)
  {
    ArgumentNullException.ThrowIfNull(entity);
    ArgumentNullException.ThrowIfNull(model);

    // Update all non-key properties
    entity.Title = DataValidation.Required(DataValidation.SafeTrim(model.Title, 200), nameof(model.Title));
    entity.AuthorSlug = DataValidation.Required(DataValidation.SafeTrim(model.AuthorSlug, 100), nameof(model.AuthorSlug));
    entity.Description = DataValidation.SafeTrim(model.Description, 500) ?? string.Empty;
    entity.Content = DataValidation.Required(DataValidation.SafeTrim(model.Content, 50000), nameof(model.Content));
    entity.Slug = DataValidation.Required(DataValidation.SafeTrim(model.Slug, 100), nameof(model.Slug));
    entity.Category = DataValidation.Required(DataValidation.SafeTrim(model.Category, 50), nameof(model.Category));
    entity.Status = DataValidation.SafeTrim(model.Status, 20) ?? "Draft";
    entity.FeaturedImageId = model.FeaturedImageId;
    entity.FeaturedMediaId = model.FeaturedMediaId;
    entity.FeaturedVideoId = model.FeaturedVideoId;
    entity.MediaReferencesJson = model.MediaReferencesJson ?? "[]";
    entity.PublishDate = model.PublishDate;
    entity.LastModified = DateTime.UtcNow;
    entity.TagsJson = JsonSerializer.Serialize(model.TagsList);

    // Update keys if PublishDate changed
    entity.UpdateKeys();
  }

  /// <summary>
  /// Helper method to safely deserialize tags from JSON
  /// </summary>
  /// <param name="tagsJson">The JSON string containing tags</param>
  /// <returns>An array of tag strings</returns>
  private static string[] DeserializeTags(string tagsJson)
  {
    if (string.IsNullOrEmpty(tagsJson))
      return [];

    try
    {
      return JsonSerializer.Deserialize<string[]>(tagsJson) ?? [];
    }
    catch (JsonException)
    {
      // If deserialization fails, return empty array
      return [];
    }
  }
}