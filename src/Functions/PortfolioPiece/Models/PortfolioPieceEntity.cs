using System.Text.Json;
using SharedStorage.Models;
using Utils.Extensions;
using Utils.Validation;
using Functions.PortfolioPiece.Models;

namespace Functions.PortfolioPieces.Models;

/// <summary>
/// Entity class for portfolio pieces stored in Azure Table Storage
/// </summary>
public class PortfolioPieceEntity : BaseContentEntity
{
  public PortfolioPieceEntity() : base()
  {
  }

  public PortfolioPieceEntity(DateTime publishDate) : base(publishDate)
  {
  }

  /// <summary>
  /// Converts the entity to a model
  /// </summary>
  /// <typeparam name="T">The type of model to convert to</typeparam>
  /// <returns>The converted model</returns>
  public override T ToModel<T>()
  {
    if (typeof(T) != typeof(PortfolioPieceModel))
      throw new ArgumentException($"Cannot convert PortfolioPieceEntity to {typeof(T).Name}");

    var model = new PortfolioPieceModel
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
      PublishDate = BaseContentMapper<PortfolioPieceModel, PortfolioPieceEntity>.EnsureValidPublishDate(model.PublishDate, status),
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