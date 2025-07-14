using System.Text.Json;
using Utils.Validation;
using Utils.Extensions;

namespace SharedStorage.Models;

/// <summary>
/// Base mapper for content entities and models
/// Contains shared mapping logic for all content types
/// </summary>
public abstract class BaseContentMapper<TModel, TEntity>
    where TModel : BaseContentModel
    where TEntity : BaseContentEntity
{
    /// <summary>
    /// Validates and sanitizes common fields for content models
    /// </summary>
    /// <param name="model">The content model to validate</param>
    /// <returns>A status string that ensures consistency with isPublished flag</returns>
    protected static string ValidateAndSanitizeCommonFields(TModel model)
    {
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
        return DataValidation.EnsureStatusConsistency(model.Status, model.IsPublished);
    }

    /// <summary>
    /// Updates an entity with common fields from a model
    /// </summary>
    /// <param name="entity">The entity to update</param>
    /// <param name="model">The model with updated values</param>
    /// <param name="status">The sanitized status string</param>
    protected static void UpdateCommonFields(TEntity entity, TModel model, string status)
    {
        entity.Title = DataValidation.Required(DataValidation.SafeTrim(model.Title, 200), nameof(model.Title));
        entity.AuthorSlug = DataValidation.Required(DataValidation.SafeTrim(model.AuthorSlug, 100), nameof(model.AuthorSlug));
        entity.Description = DataValidation.SafeTrim(model.Description, 500) ?? string.Empty;
        entity.Content = DataValidation.Required(DataValidation.SafeTrim(model.Content, 50000), nameof(model.Content));
        entity.Slug = DataValidation.Required(DataValidation.SafeTrim(model.Slug, 100), nameof(model.Slug));
        entity.Category = DataValidation.Required(DataValidation.SafeTrim(model.Category, 50), nameof(model.Category));
        entity.Status = status;
        entity.FeaturedImageId = model.FeaturedImageId;
        entity.FeaturedMediaId = model.FeaturedMediaId;
        entity.FeaturedVideoId = model.FeaturedVideoId;
        entity.MediaReferencesJson = model.MediaReferencesJson ?? "[]";
        entity.TagsJson = JsonSerializer.Serialize(model.TagsList);
    }

    /// <summary>
    /// Ensures that the PublishDate is a valid date for Azure Table Storage
    /// </summary>
    /// <param name="publishDate">The original publish date</param>
    /// <param name="status">The content status (Draft or Published)</param>
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

    /// <summary>
    /// Helper method to safely deserialize tags from JSON
    /// </summary>
    /// <param name="tagsJson">The JSON string containing tags</param>
    /// <returns>An array of tag strings</returns>
    public static string[] DeserializeTags(string tagsJson)
    {
        return DataValidation.DeserializeTags(tagsJson);
    }

    /// <summary>
    /// Convert a collection of entities to models
    /// </summary>
    /// <param name="entities">The entities to convert</param>
    /// <returns>A collection of models</returns>
    public abstract IEnumerable<TModel> EntitiesToModels(IEnumerable<TEntity> entities);

    /// <summary>
    /// Creates a new entity instance
    /// </summary>
    /// <returns>A new entity instance</returns>
    protected abstract TEntity CreateEntityInstance();

    /// <summary>
    /// Abstract method for converting model to entity
    /// </summary>
    public abstract TEntity ToEntity(TModel model);

    /// <summary>
    /// Abstract method for converting entity to model
    /// </summary>
    public abstract TModel ToModel(TEntity entity);

    /// <summary>
    /// Abstract method for updating entity from model
    /// </summary>
    public abstract void UpdateEntityFromModel(TEntity entity, TModel model);
}
