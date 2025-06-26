using Functions.BlogPosts.Models;
using Functions.BlogPosts.Mappers;
using SharedStorage.Services.ContentServices;
using SharedStorage.Services.MediaServices;
using SharedStorage.Services.BaseServices;
using SharedStorage.Validators;
using Utils;
using Utils.Constants;

namespace Functions.BlogPosts.Services;

public interface IBlogPostService
{
  // Core CRUD operations
  Task<BlogPostDTO?> GetPostAsync(string slug, bool? isPublished = true);
  Task<IEnumerable<BlogPostDTO>> GetPostsAsync(string? authorSlug = null, string? category = null, bool? isPublished = true, int? limit = null);
  Task<BlogPostDTO?> UpsertPostAsync(string slug, BlogPostModel model);
  Task<bool> DeletePostAsync(string slug);

  // Media operations
  Task<BlogPostDTO?> SetFeaturedImageAsync(string slug, string mediaId);
  Task<BlogPostDTO?> SetFeaturedMediaAsync(string slug, string mediaId);
  Task<BlogPostDTO?> AddMediaReferenceAsync(string slug, string mediaId);
  Task<BlogPostDTO?> RemoveMediaReferenceAsync(string slug, string mediaId);
}

public class BlogPostService : ContentService<BlogPostEntity, BlogPostModel, BlogPostDTO>, IBlogPostService
{
  private readonly IMediaService _mediaService;

  public BlogPostService(
    ITableStorageService tableStorageService,
    IMediaService mediaService,
    IAppInsightsLogger<ContentService<BlogPostEntity, BlogPostModel, BlogPostDTO>> appLogger)
    : base(tableStorageService, appLogger, GetTableName())
  {
    _mediaService = mediaService ?? throw new ArgumentNullException(nameof(mediaService));
  }

  private static string GetTableName()
  {
    var rawTableName = System.Environment.GetEnvironmentVariable("BLOGPOSTS_TABLE_NAME") ?? "blog";
    return TableNameValidator.ValidateTableName(rawTableName);
  }

  #region ContentService Implementation

  protected override string GetPartitionKey(string slug) => slug;
  protected override string GetRowKey(string slug) => "post";

  protected override bool IsPublished(BlogPostEntity entity) => entity.IsPublished;
  protected override string GetAuthorSlug(BlogPostEntity entity) => entity.AuthorSlug;
  protected override string GetCategory(BlogPostEntity entity) => entity.Category;

  protected override BlogPostDTO EntityToDto(BlogPostEntity entity) => BlogPostMapper.EntityToDTO(entity);

  protected override BlogPostEntity ModelToEntity(BlogPostModel model)
  {
    var entity = new BlogPostEntity
    {
      Id = model.Id ?? Guid.NewGuid().ToString(),
      Title = model.Title,
      Content = model.Content,
      AuthorSlug = model.AuthorSlug,
      Category = model.Category,
      Status = model.Status,
      PublishDate = model.PublishDate,
      LastModified = DateTime.UtcNow,
      TagsJson = System.Text.Json.JsonSerializer.Serialize(model.TagsList),
      FeaturedImageId = model.FeaturedImageId,
      FeaturedMediaId = model.FeaturedMediaId,
      MediaReferencesJson = model.MediaReferencesJson,
      Description = model.Description,
      Slug = model.Slug
    };

    // Use consistent slug-based partitioning
    entity.PartitionKey = GetPartitionKey(model.Slug);
    entity.RowKey = GetRowKey(model.Slug);

    return entity;
  }

  protected override BlogPostEntity? ConvertTableEntityToTEntity(Azure.Data.Tables.TableEntity tableEntity)
  {
    return ConvertToEntity(tableEntity);
  }

  protected override void UpdateEntityFromModel(BlogPostEntity entity, BlogPostModel model)
  {
    entity.Title = model.Title;
    entity.Content = model.Content;
    entity.AuthorSlug = model.AuthorSlug;
    entity.Category = model.Category;
    entity.Status = model.Status;
    entity.PublishDate = model.PublishDate;
    entity.LastModified = DateTime.UtcNow;
    entity.TagsJson = System.Text.Json.JsonSerializer.Serialize(model.TagsList);
    entity.FeaturedImageId = model.FeaturedImageId;
    entity.FeaturedMediaId = model.FeaturedMediaId;
    entity.MediaReferencesJson = model.MediaReferencesJson;
  }

  protected override void ValidateModel(BlogPostModel model)
  {
    if (string.IsNullOrWhiteSpace(model.Title))
      throw new ArgumentException("Title is required", nameof(model));

    if (string.IsNullOrWhiteSpace(model.Slug))
      throw new ArgumentException("Slug is required", nameof(model));

    if (string.IsNullOrWhiteSpace(model.AuthorSlug))
      throw new ArgumentException("Author slug is required", nameof(model));
  }

  #endregion

  #region IBlogPostService Implementation

  public async Task<BlogPostDTO?> GetPostAsync(string slug, bool? isPublished = true)
  {
    return await GetBySlugAsync(slug, isPublished);
  }

  public async Task<IEnumerable<BlogPostDTO>> GetPostsAsync(string? authorSlug = null, string? category = null, bool? isPublished = true, int? limit = null)
  {
    if (isPublished == true)
    {
      return await GetPublishedContentAsync(authorSlug, category, limit);
    }

    try
    {
      var filters = new List<string>();

      if (!string.IsNullOrWhiteSpace(authorSlug))
        filters.Add($"AuthorSlug eq '{authorSlug}'");

      if (!string.IsNullOrWhiteSpace(category))
        filters.Add($"Category eq '{category}'");

      var filter = filters.Any() ? string.Join(" and ", filters) : null;
      var pageSize = Math.Min(limit ?? 50, 100);

      var result = await _tableStorageService.GetEntitiesAsync(_tableName, filter, pageSize);
      var entities = result.Entities.Select(e => ConvertToEntity(e))
                          .Where(e => e != null)
                          .Select(e => EntityToDto(e!))
                          .ToList();

      _appLogger.LogInformation("Retrieved {Count} blog posts", entities.Count);
      return entities;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Failed to get blog posts: {Error}", ex, ex.Message);
      return Enumerable.Empty<BlogPostDTO>();
    }
  }

  public async Task<BlogPostDTO?> UpsertPostAsync(string slug, BlogPostModel model)
  {
    return await UpsertAsync(slug, model);
  }

  public async Task<bool> DeletePostAsync(string slug)
  {
    return await DeleteAsync(slug);
  }

  #endregion

  #region Media Operations

  public async Task<BlogPostDTO?> SetFeaturedImageAsync(string slug, string mediaId)
  {
    try
    {
      // Verify media exists
      var media = await _mediaService.GetMediaAsync(mediaId);
      if (media == null || media.MediaType != "image")
      {
        _appLogger.LogWarning("Media {MediaId} not found or is not an image", mediaId);
        return null;
      }

      var entity = await _tableStorageService.GetEntityAsync<BlogPostEntity>(_tableName, GetPartitionKey(slug), GetRowKey(slug));
      if (entity == null)
      {
        _appLogger.LogWarning("Blog post {Slug} not found", slug);
        return null;
      }

      entity.FeaturedImageId = mediaId;
      entity.LastModified = DateTime.UtcNow;

      await _tableStorageService.UpsertEntityAsync(_tableName, entity);

      _appLogger.LogInformation("Set featured image {MediaId} for blog post {Slug}", mediaId, slug);
      return EntityToDto(entity);
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Failed to set featured image for blog post {Slug}: {Error}", ex, slug, ex.Message);
      return null;
    }
  }

  public async Task<BlogPostDTO?> SetFeaturedMediaAsync(string slug, string mediaId)
  {
    try
    {
      // Verify media exists
      var media = await _mediaService.GetMediaAsync(mediaId);
      if (media == null)
      {
        _appLogger.LogWarning("Media {MediaId} not found", mediaId);
        return null;
      }

      var entity = await _tableStorageService.GetEntityAsync<BlogPostEntity>(_tableName, GetPartitionKey(slug), GetRowKey(slug));
      if (entity == null)
      {
        _appLogger.LogWarning("Blog post {Slug} not found", slug);
        return null;
      }

      entity.FeaturedMediaId = mediaId;
      entity.LastModified = DateTime.UtcNow;

      await _tableStorageService.UpsertEntityAsync(_tableName, entity);

      _appLogger.LogInformation("Set featured media {MediaId} for blog post {Slug}", mediaId, slug);
      return EntityToDto(entity);
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Failed to set featured media for blog post {Slug}: {Error}", ex, slug, ex.Message);
      return null;
    }
  }

  public async Task<BlogPostDTO?> AddMediaReferenceAsync(string slug, string mediaId)
  {
    try
    {
      // Verify media exists
      var media = await _mediaService.GetMediaAsync(mediaId);
      if (media == null)
      {
        _appLogger.LogWarning("Media {MediaId} not found", mediaId);
        return null;
      }

      var entity = await _tableStorageService.GetEntityAsync<BlogPostEntity>(_tableName, GetPartitionKey(slug), GetRowKey(slug));
      if (entity == null)
      {
        _appLogger.LogWarning("Blog post {Slug} not found", slug);
        return null;
      }

      // Parse existing media references
      var mediaReferences = string.IsNullOrWhiteSpace(entity.MediaReferencesJson)
        ? new List<string>()
        : System.Text.Json.JsonSerializer.Deserialize<List<string>>(entity.MediaReferencesJson) ?? new List<string>();

      // Add new reference if not already present
      if (!mediaReferences.Contains(mediaId))
      {
        mediaReferences.Add(mediaId);
        entity.MediaReferencesJson = System.Text.Json.JsonSerializer.Serialize(mediaReferences);
        entity.LastModified = DateTime.UtcNow;

        await _tableStorageService.UpsertEntityAsync(_tableName, entity);
        _appLogger.LogInformation("Added media reference {MediaId} to blog post {Slug}", mediaId, slug);
      }
      else
      {
        _appLogger.LogInformation("Media reference {MediaId} already exists for blog post {Slug}", mediaId, slug);
      }

      return EntityToDto(entity);
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Failed to add media reference to blog post {Slug}: {Error}", ex, slug, ex.Message);
      return null;
    }
  }

  public async Task<BlogPostDTO?> RemoveMediaReferenceAsync(string slug, string mediaId)
  {
    try
    {
      var entity = await _tableStorageService.GetEntityAsync<BlogPostEntity>(_tableName, GetPartitionKey(slug), GetRowKey(slug));
      if (entity == null)
      {
        _appLogger.LogWarning("Blog post {Slug} not found", slug);
        return null;
      }

      // Parse existing media references
      var mediaReferences = string.IsNullOrWhiteSpace(entity.MediaReferencesJson)
        ? new List<string>()
        : System.Text.Json.JsonSerializer.Deserialize<List<string>>(entity.MediaReferencesJson) ?? new List<string>();

      // Remove reference if present
      if (mediaReferences.Remove(mediaId))
      {
        entity.MediaReferencesJson = System.Text.Json.JsonSerializer.Serialize(mediaReferences);
        entity.LastModified = DateTime.UtcNow;

        await _tableStorageService.UpsertEntityAsync(_tableName, entity);
        _appLogger.LogInformation("Removed media reference {MediaId} from blog post {Slug}", mediaId, slug);
      }
      else
      {
        _appLogger.LogInformation("Media reference {MediaId} not found for blog post {Slug}", mediaId, slug);
      }

      return EntityToDto(entity);
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Failed to remove media reference from blog post {Slug}: {Error}", ex, slug, ex.Message);
      return null;
    }
  }

  #endregion

  #region Helper Methods

  private BlogPostEntity? ConvertToEntity(Azure.Data.Tables.TableEntity tableEntity)
  {
    try
    {
      return new BlogPostEntity
      {
        PartitionKey = tableEntity.PartitionKey,
        RowKey = tableEntity.RowKey,
        Timestamp = tableEntity.Timestamp,
        ETag = tableEntity.ETag,
        Slug = tableEntity.GetString("Slug") ?? string.Empty,
        Title = tableEntity.GetString("Title") ?? string.Empty,
        Content = tableEntity.GetString("Content") ?? string.Empty,
        AuthorSlug = tableEntity.GetString("AuthorSlug") ?? string.Empty,
        Category = tableEntity.GetString("Category") ?? string.Empty,
        Status = tableEntity.GetString("Status") ?? "Draft",
        PublishDate = tableEntity.GetDateTime("PublishDate") ?? DateTime.UtcNow,
        LastModified = tableEntity.GetDateTime("LastModified") ?? DateTime.UtcNow,
        TagsJson = tableEntity.GetString("TagsJson") ?? "[]",
        FeaturedImageId = tableEntity.GetString("FeaturedImageId") ?? string.Empty,
        FeaturedMediaId = tableEntity.GetString("FeaturedMediaId") ?? string.Empty,
        MediaReferencesJson = tableEntity.GetString("MediaReferencesJson") ?? string.Empty
      };
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Failed to convert table entity to BlogPostEntity: {Error}", ex, ex.Message);
      return null;
    }
  }

  #endregion
}