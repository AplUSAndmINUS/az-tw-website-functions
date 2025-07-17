using System.Text.Json;
using Utils.Validation;
using Utils.Extensions;
using Functions.Books.Models;
using SharedStorage.Models;

namespace Functions.Books.Mappers;

/// <summary>
/// Mapper class for converting between BookModel, BookEntity, and BookDTO
/// </summary>
public class BookMapper : BaseContentMapper<BookModel, BookEntity>
{
  private static BookMapper? _instance;
  public static BookMapper Instance => _instance ??= new BookMapper();

  /// <summary>
  /// Converts a BookModel to BookEntity for storage operations
  /// </summary>
  /// <param name="model">The BookModel to convert</param>
  /// <returns>A BookEntity ready for storage</returns>
  /// <exception cref="ArgumentNullException">Thrown when model or required fields are null</exception>
  /// <exception cref="ArgumentException">Thrown when validation fails</exception>
  public override BookEntity ToEntity(BookModel model)
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
  /// Converts a BookEntity to BookModel for business logic operations
  /// </summary>
  /// <param name="entity">The BookEntity to convert</param>
  /// <returns>A BookModel for business operations</returns>
  /// <exception cref="ArgumentNullException">Thrown when entity is null</exception>
  public override BookModel ToModel(BookEntity entity)
  {
    ArgumentNullException.ThrowIfNull(entity);

    return new BookModel
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
  /// Converts a BookModel to BookDTO for API responses
  /// </summary>
  /// <param name="model">The BookModel to convert</param>
  /// <returns>A BookDTO for API responses</returns>
  /// <exception cref="ArgumentNullException">Thrown when model is null</exception>
  public BookDTO ToDTO(BookModel model)
  {
    ArgumentNullException.ThrowIfNull(model);

    return new BookDTO
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
  /// Converts a BookEntity directly to BookDTO for API responses
  /// </summary>
  /// <param name="entity">The BookEntity to convert</param>
  /// <returns>A BookDTO for API responses</returns>
  /// <exception cref="ArgumentNullException">Thrown when entity is null</exception>
  public BookDTO EntityToDTO(BookEntity entity)
  {
    ArgumentNullException.ThrowIfNull(entity);

    return new BookDTO
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
  /// Converts a BookDTO to BookModel for business logic operations
  /// </summary>
  /// <param name="dto">The BookDTO to convert</param>
  /// <returns>A BookModel for business operations</returns>
  /// <exception cref="ArgumentNullException">Thrown when dto is null</exception>
  public BookModel FromDTO(BookDTO dto)
  {
    ArgumentNullException.ThrowIfNull(dto);

    return new BookModel
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
  /// Converts a collection of BookEntities to BookDTOs
  /// </summary>
  /// <param name="entities">The collection of entities to convert</param>
  /// <returns>A collection of DTOs</returns>
  public IEnumerable<BookDTO> EntitiesToDTOs(IEnumerable<BookEntity> entities)
  {
    return entities?.Select(EntityToDTO) ?? Enumerable.Empty<BookDTO>();
  }

  /// <summary>
  /// Converts a collection of BookModels to BookDTOs
  /// </summary>
  /// <param name="models">The collection of models to convert</param>
  /// <returns>A collection of DTOs</returns>
  public IEnumerable<BookDTO> ModelsToDTOs(IEnumerable<BookModel> models)
  {
    return models?.Select(ToDTO) ?? Enumerable.Empty<BookDTO>();
  }

  // Static wrapper methods to maintain backward compatibility with existing code
  public static BookEntity ToEntityStatic(BookModel model) => Instance.ToEntity(model);
  public static BookModel ToModelStatic(BookEntity entity) => Instance.ToModel(entity);
  public static BookDTO ToDTOStatic(BookModel model) => Instance.ToDTO(model);
  public static BookDTO EntityToDTOStatic(BookEntity entity) => Instance.EntityToDTO(entity);
  public static BookModel FromDTOStatic(BookDTO dto) => Instance.FromDTO(dto);
  public static IEnumerable<BookDTO> EntitiesToDTOsStatic(IEnumerable<BookEntity> entities) => Instance.EntitiesToDTOs(entities);
  public static IEnumerable<BookDTO> ModelsToDTOsStatic(IEnumerable<BookModel> models) => Instance.ModelsToDTOs(models);
  public static void UpdateEntityFromModelStatic(BookEntity entity, BookModel model) => Instance.UpdateEntityFromModel(entity, model);

  /// <summary>
  /// Updates an existing BookEntity with values from a BookModel
  /// </summary>
  /// <param name="entity">The entity to update</param>
  /// <param name="model">The model containing new values</param>
  /// <exception cref="ArgumentNullException">Thrown when entity or model is null</exception>
  public override void UpdateEntityFromModel(BookEntity entity, BookModel model)
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
  protected override BookEntity CreateEntityInstance()
  {
    return new BookEntity();
  }

  /// <summary>
  /// Convert a collection of entities to models
  /// </summary>
  /// <param name="entities">The entities to convert</param>
  /// <returns>A collection of models</returns>
  public override IEnumerable<BookModel> EntitiesToModels(IEnumerable<BookEntity> entities)
  {
    return entities?.Select(ToModel) ?? Enumerable.Empty<BookModel>();
  }
}