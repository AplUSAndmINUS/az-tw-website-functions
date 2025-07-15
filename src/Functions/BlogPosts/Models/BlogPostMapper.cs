using System.Text.Json;
using Utils.Validation;
using Utils.Extensions;
using Functions.BlogPosts.Models;
using SharedStorage.Models;

namespace Functions.BlogPosts.Mappers;

public class BlogPostMapper : BaseContentMapper<BlogPostModel, BlogPostEntity>
{
  private static BlogPostMapper? _instance;
  public static BlogPostMapper Instance => _instance ??= new BlogPostMapper();

  /// <summary>
  /// Converts a BlogPostModel to BlogPostEntity for storage operations
  /// </summary>
  /// <param name="model">The BlogPostModel to convert</param>
  /// <returns>A BlogPostEntity ready for storage</returns>
  /// <exception cref="ArgumentNullException">Thrown when model or required fields are null</exception>
  /// <exception cref="ArgumentException">Thrown when validation fails</exception>
  public override BlogPostEntity ToEntity(BlogPostModel model)
  {
    // Validate and sanitize common fields
    string status = ValidateAndSanitizeCommonFields(model);

    // Create new entity instance
    var entity = CreateEntityInstance();

    // Set ID if not provided
    entity.Id = model.Id ?? Guid.NewGuid().ToString();

    // Update common fields from base mapper
    UpdateCommonFields(entity, model, status);

    // Set specific fields for this entity type
    entity.PublishDate = EnsureValidPublishDate(model.PublishDate, status);
    entity.LastModified = DateTime.UtcNow; // Always update LastModified on conversion
    entity.PartitionKey = model.PartitionKey;
    entity.RowKey = model.RowKey;
    entity.Timestamp = model.Timestamp;
    entity.ETag = model.ETag;

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
  public override BlogPostModel ToModel(BlogPostEntity entity)
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
  public BlogPostDTO ToDTO(BlogPostModel model)
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
  public BlogPostDTO EntityToDTO(BlogPostEntity entity)
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
  public BlogPostModel FromDTO(BlogPostDTO dto)
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
  public IEnumerable<BlogPostDTO> EntitiesToDTOs(IEnumerable<BlogPostEntity> entities)
  {
    return entities?.Select(EntityToDTO) ?? Enumerable.Empty<BlogPostDTO>();
  }

  /// <summary>
  /// Converts a collection of BlogPostModels to BlogPostDTOs
  /// </summary>
  /// <param name="models">The collection of models to convert</param>
  /// <returns>A collection of DTOs</returns>
  public IEnumerable<BlogPostDTO> ModelsToDTOs(IEnumerable<BlogPostModel> models)
  {
    return models?.Select(ToDTO) ?? Enumerable.Empty<BlogPostDTO>();
  }

  // Static wrapper methods to maintain backward compatibility with existing code
  public static BlogPostEntity ToEntityStatic(BlogPostModel model) => Instance.ToEntity(model);
  public static BlogPostModel ToModelStatic(BlogPostEntity entity) => Instance.ToModel(entity);
  public static BlogPostDTO ToDTOStatic(BlogPostModel model) => Instance.ToDTO(model);
  public static BlogPostDTO EntityToDTOStatic(BlogPostEntity entity) => Instance.EntityToDTO(entity);
  public static BlogPostModel FromDTOStatic(BlogPostDTO dto) => Instance.FromDTO(dto);
  public static IEnumerable<BlogPostDTO> EntitiesToDTOsStatic(IEnumerable<BlogPostEntity> entities) => Instance.EntitiesToDTOs(entities);
  public static IEnumerable<BlogPostDTO> ModelsToDTOsStatic(IEnumerable<BlogPostModel> models) => Instance.ModelsToDTOs(models);
  public static void UpdateEntityFromModelStatic(BlogPostEntity entity, BlogPostModel model) => Instance.UpdateEntityFromModel(entity, model);

  /// <summary>
  /// Updates an existing BlogPostEntity with values from a BlogPostModel
  /// </summary>
  /// <param name="entity">The entity to update</param>
  /// <param name="model">The model containing new values</param>
  /// <exception cref="ArgumentNullException">Thrown when entity or model is null</exception>
  public override void UpdateEntityFromModel(BlogPostEntity entity, BlogPostModel model)
  {
    ArgumentNullException.ThrowIfNull(entity);
    ArgumentNullException.ThrowIfNull(model);

    // Ensure status and isPublished are in sync
    string status = ValidateAndSanitizeCommonFields(model);

    // Update common fields from base mapper
    UpdateCommonFields(entity, model, status);

    // Update specific fields for this entity type
    entity.PublishDate = EnsureValidPublishDate(model.PublishDate, status);
    entity.LastModified = DateTime.UtcNow;

    // Update keys if PublishDate changed
    entity.UpdateKeys();
  }

  /// <summary>
  /// Creates a new entity instance
  /// </summary>
  /// <returns>A new entity instance</returns>
  protected override BlogPostEntity CreateEntityInstance()
  {
    return new BlogPostEntity();
  }

  /// <summary>
  /// Convert a collection of entities to models
  /// </summary>
  /// <param name="entities">The entities to convert</param>
  /// <returns>A collection of models</returns>
  public override IEnumerable<BlogPostModel> EntitiesToModels(IEnumerable<BlogPostEntity> entities)
  {
    return entities?.Select(ToModel) ?? Enumerable.Empty<BlogPostModel>();
  }
}