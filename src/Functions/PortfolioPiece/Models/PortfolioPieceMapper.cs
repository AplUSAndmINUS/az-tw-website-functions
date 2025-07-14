using System.Text.Json;
using Utils.Validation;
using Utils.Extensions;
using Functions.PortfolioPieces.Models;
using Functions.PortfolioPiece.Models;

namespace Functions.PortfolioPieces.Models;

public static class PortfolioPieceMapper
{
  /// <summary>
  /// Maps a PortfolioPieceEntity to a PortfolioPieceModel
  /// </summary>
  /// <param model="model">The PortfolioPieceEntity to map</param>
  /// <returns>A PortfolioPieceModel with mapped properties</returns>
  /// <exception cref="ArgumentNullException">Thrown if the model is null</exception>
  /// <exception cref="ArgumentException">Thrown when validation fails</exception>

  /// <summary>
  /// Converts a PortfolioPieceModel to PortfolioPieceEntity for storage operations
  /// </summary>
  /// <param name="model">The PortfolioPieceModel to convert</param>
  /// <returns>A PortfolioPieceEntity ready for storage</returns>
  /// <exception cref="ArgumentNullException">Thrown when model or required fields are null</exception>
  /// <exception cref="ArgumentException">Thrown when validation fails</exception>
  public static PortfolioPieceEntity ToEntity(PortfolioPieceModel model)
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
      AuthorSlug = DataValidation.Required(DataValidation.SafeTrim(model.AuthorSlug, 100), nameof(model.AuthorSlug)),
      Description = DataValidation.SafeTrim(model.Description, 500) ?? string.Empty,
      Content = DataValidation.Required(DataValidation.SafeTrim(model.Content, 50000), nameof(model.Content)),
      Slug = DataValidation.Required(DataValidation.SafeTrim(model.Slug, 100), nameof(model.Slug)),
      Category = DataValidation.Required(DataValidation.SafeTrim(model.Category, 50), nameof(model.Category)),
      Status = status,
      FeaturedImageId = model.FeaturedImageId,
      FeaturedMediaId = model.FeaturedMediaId,
      FeaturedVideoId = model.FeaturedVideoId,
      MediaReferencesJson = model.MediaReferencesJson ?? "[]",
      PublishDate = EnsureValidPublishDate(model.PublishDate, status),
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

  public static PortfolioPieceModel ToModel(PortfolioPieceEntity entity)
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
      TagsList = string.IsNullOrEmpty(entity.TagsJson) ? Array.Empty<string>() : JsonSerializer.Deserialize<string[]>(entity.TagsJson) ?? Array.Empty<string>()
    };

    return model;
  }

  /// <summary>
  /// Validates the PortfolioPieceModel for required fields and formats
  /// </summary>
  /// <param name="model">The PortfolioPieceModel to validate</param>
  /// <returns>A list of validation error messages</returns>
  public static PortfolioPieceDTO ToDTO(PortfolioPieceModel model)
  {
    ArgumentNullException.ThrowIfNull(model);

    var dto = new PortfolioPieceDTO
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

    return dto;
  }

  /// <summary>
  /// Maps a PortfolioPieceDTO to a PortfolioPieceModel
  /// </summary>
  /// <param name="dto">The PortfolioPieceDTO to map</param>
  /// <returns>A PortfolioPieceModel with mapped properties</returns>

  public static PortfolioPieceDTO EntityToDTO(PortfolioPieceEntity entity)
  {
    ArgumentNullException.ThrowIfNull(entity);

    var dto = new PortfolioPieceDTO
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
      TagsList = string.IsNullOrEmpty(entity.TagsJson) ? Array.Empty<string>() : JsonSerializer.Deserialize<string[]>(entity.TagsJson) ?? Array.Empty<string>()
    };

    return dto;
  }

  /// <summary>
  /// Maps a PortfolioPieceModel to a PortfolioPieceEntity
  /// </summary>
  /// <param name="model">The PortfolioPieceModel to map</param>
  /// <returns>A PortfolioPieceEntity with mapped properties</returns>
  /// <exception cref="ArgumentNullException">Thrown if the model is null</exception>
  /// <exception cref="ArgumentException">Thrown when validation fails</exception>
  public static PortfolioPieceModel FromDTO(PortfolioPieceDTO dto)
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

  /// <summary>
  /// Updates a PortfolioPieceEntity with values from a PortfolioPieceModel
  /// </summary>
  /// <param name="entity">The PortfolioPieceEntity to update</param>
  /// <param name="model">The PortfolioPieceModel containing the updated values</param>
  /// <returns>The updated PortfolioPieceEntity</returns>
  /// <exception cref="ArgumentNullException">Thrown if the entity or model is null</exception>
  public static PortfolioPieceEntity UpdateEntityFromModel(PortfolioPieceEntity entity, PortfolioPieceModel model)
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

    // Ensure status and isPublished are in sync
    string status = DataValidation.EnsureStatusConsistency(model.Status, model.IsPublished);

    entity.Status = status;
    entity.FeaturedImageId = model.FeaturedImageId;
    entity.FeaturedMediaId = model.FeaturedMediaId;
    entity.FeaturedVideoId = model.FeaturedVideoId;
    entity.MediaReferencesJson = model.MediaReferencesJson ?? "[]";
    entity.PublishDate = EnsureValidPublishDate(model.PublishDate, status);
    entity.LastModified = DateTime.UtcNow; // Update last modified to now
    entity.TagsJson = JsonSerializer.Serialize(model.TagsList);

    // Update keys if PublishDate changed
    entity.UpdateKeys();

    return entity;
  }

  private static string[] DeserializeTags(string tagsJson)
  {
    return DataValidation.DeserializeTags(tagsJson);
  }

  /// <summary>
  /// Ensures that the PublishDate is a valid date for Azure Table Storage
  /// </summary>
  /// <param name="publishDate">The original publish date</param>
  /// <param name="status">The post status (Draft or Published)</param>
  /// <returns>A valid DateTime value for Azure Table Storage</returns>
  public static DateTime EnsureValidPublishDate(DateTime publishDate, string status)
  {
    // First ensure it's UTC
    var utcDate = publishDate.EnsureUtc();

    // Check if it's a valid date for Azure Table Storage
    if (utcDate == default || utcDate.Year < 2000)
    {
      // Use current date for published posts, future date for drafts
      if (status == "Published")
      {
        return DateTime.UtcNow;
      }
      else
      {
        // Use a future date for drafts
        return new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
      }
    }

    return utcDate;
  }
}