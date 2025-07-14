using System.Text.Json;
using Utils.Validation;
using Utils.Extensions;
using Functions.PortfolioPieces.Models;
using Functions.PortfolioPiece.Models;
using SharedStorage.Models;

namespace Functions.PortfolioPieces.Models;

public class PortfolioPieceMapper : BaseContentMapper<PortfolioPieceModel, PortfolioPieceEntity>
{
  private static PortfolioPieceMapper? _instance;
  public static PortfolioPieceMapper Instance => _instance ??= new PortfolioPieceMapper();
  /// <summary>
  /// Converts a PortfolioPieceModel to PortfolioPieceEntity for storage operations
  /// </summary>
  /// <param name="model">The PortfolioPieceModel to convert</param>
  /// <returns>A PortfolioPieceEntity ready for storage</returns>
  /// <exception cref="ArgumentNullException">Thrown when model or required fields are null</exception>
  /// <exception cref="ArgumentException">Thrown when validation fails</exception>
  public override PortfolioPieceEntity ToEntity(PortfolioPieceModel model)
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

  public override PortfolioPieceModel ToModel(PortfolioPieceEntity entity)
  {
    ArgumentNullException.ThrowIfNull(entity);

    var model = new PortfolioPieceModel
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
      Status = entity.Status ?? "Draft",
      FeaturedImageId = entity.FeaturedImageId,
      FeaturedMediaId = entity.FeaturedMediaId,
      FeaturedVideoId = entity.FeaturedVideoId,
      MediaReferencesJson = entity.MediaReferencesJson ?? "[]",
      PublishDate = entity.PublishDate.EnsureUtc(),
      LastModified = entity.LastModified.EnsureUtc(),
      TagsList = DeserializeTags(entity.TagsJson)
    };

    return model;
  }
  
  /// <summary>
  /// Creates a new entity instance
  /// </summary>
  /// <returns>A new entity instance</returns>
  protected override PortfolioPieceEntity CreateEntityInstance()
  {
    return new PortfolioPieceEntity();
  }
  
  /// <summary>
  /// Convert a collection of entities to models
  /// </summary>
  /// <param name="entities">The entities to convert</param>
  /// <returns>A collection of models</returns>
  public override IEnumerable<PortfolioPieceModel> EntitiesToModels(IEnumerable<PortfolioPieceEntity> entities)
  {
    return entities?.Select(ToModel) ?? Enumerable.Empty<PortfolioPieceModel>();
  }

  // The static DTO methods will be replaced by their instance counterparts below

  /// <summary>
  /// Updates a PortfolioPieceEntity with values from a PortfolioPieceModel
  /// </summary>
  /// <param name="entity">The PortfolioPieceEntity to update</param>
  /// <param name="model">The PortfolioPieceModel containing the updated values</param>
  /// <exception cref="ArgumentNullException">Thrown if the entity or model is null</exception>
  public override void UpdateEntityFromModel(PortfolioPieceEntity entity, PortfolioPieceModel model)
  {
    ArgumentNullException.ThrowIfNull(entity);
    ArgumentNullException.ThrowIfNull(model);

    // Ensure status and isPublished are in sync
    string status = ValidateAndSanitizeCommonFields(model);

    // Update common fields from base mapper
    UpdateCommonFields(entity, model, status);
    
    // Update specific fields for this entity type
    entity.PublishDate = EnsureValidPublishDate(model.PublishDate, status);
    entity.LastModified = DateTime.UtcNow; // Update last modified to now

    // Update keys if PublishDate changed
    entity.UpdateKeys();
  }
  
  // DTO conversion methods
  public PortfolioPieceDTO ToDTO(PortfolioPieceModel model)
  {
    ArgumentNullException.ThrowIfNull(model);

    return new PortfolioPieceDTO
    {
      Id = model.Id,
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
      MediaReferencesJson = model.MediaReferencesJson ?? "[]",
      PublishDate = model.PublishDate.EnsureUtc(),
      LastModified = model.LastModified.EnsureUtc(),
      TagsList = model.TagsList
    };
  }

  public PortfolioPieceDTO EntityToDTO(PortfolioPieceEntity entity)
  {
    ArgumentNullException.ThrowIfNull(entity);

    return new PortfolioPieceDTO
    {
      Id = entity.Id,
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
      MediaReferencesJson = entity.MediaReferencesJson ?? "[]",
      PublishDate = entity.PublishDate.EnsureUtc(),
      LastModified = entity.LastModified.EnsureUtc(),
      TagsList = DeserializeTags(entity.TagsJson)
    };
  }

  public PortfolioPieceModel FromDTO(PortfolioPieceDTO dto)
  {
    ArgumentNullException.ThrowIfNull(dto);

    return new PortfolioPieceModel
    {
      Id = dto.Id,
      Title = dto.Title,
      AuthorSlug = dto.AuthorSlug,
      Description = dto.Description,
      Content = dto.Content,
      Slug = dto.Slug,
      Category = dto.Category,
      Status = dto.Status ?? "Draft",
      FeaturedImageId = dto.FeaturedImageId,
      FeaturedMediaId = dto.FeaturedMediaId,
      FeaturedVideoId = dto.FeaturedVideoId,
      MediaReferencesJson = dto.MediaReferencesJson ?? "[]",
      PublishDate = dto.PublishDate.EnsureUtc(),
      LastModified = dto.LastModified.EnsureUtc(),
      TagsList = dto.TagsList
    };
  }

  public IEnumerable<PortfolioPieceDTO> EntitiesToDTOs(IEnumerable<PortfolioPieceEntity> entities)
  {
    return entities?.Select(e => EntityToDTO(e)) ?? Enumerable.Empty<PortfolioPieceDTO>();
  }

  public IEnumerable<PortfolioPieceDTO> ModelsToDTOs(IEnumerable<PortfolioPieceModel> models)
  {
    return models?.Select(m => ToDTO(m)) ?? Enumerable.Empty<PortfolioPieceDTO>();
  }
  
  // Static wrapper methods to maintain backward compatibility with existing code
  public static PortfolioPieceEntity ToEntityStatic(PortfolioPieceModel model) => Instance.ToEntity(model);
  public static PortfolioPieceModel ToModelStatic(PortfolioPieceEntity entity) => Instance.ToModel(entity);
  public static PortfolioPieceDTO ToDTOStatic(PortfolioPieceModel model) => Instance.ToDTO(model);
  public static PortfolioPieceDTO EntityToDTOStatic(PortfolioPieceEntity entity) => Instance.EntityToDTO(entity);
  public static PortfolioPieceModel FromDTOStatic(PortfolioPieceDTO dto) => Instance.FromDTO(dto);
  public static IEnumerable<PortfolioPieceDTO> EntitiesToDTOsStatic(IEnumerable<PortfolioPieceEntity> entities) => Instance.EntitiesToDTOs(entities);
  public static IEnumerable<PortfolioPieceDTO> ModelsToDTOsStatic(IEnumerable<PortfolioPieceModel> models) => Instance.ModelsToDTOs(models);
  public static void UpdateEntityFromModelStatic(PortfolioPieceEntity entity, PortfolioPieceModel model) => Instance.UpdateEntityFromModel(entity, model);
}