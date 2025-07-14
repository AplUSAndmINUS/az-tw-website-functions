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

  // Media-enhanced operations
  Task<BlogPostWithMediaDTO?> GetPostWithMediaAsync(string slug, bool? isPublished = true);
  Task<IEnumerable<BlogPostWithMediaDTO>> GetPostsWithMediaAsync(string? authorSlug = null, string? category = null, bool? isPublished = true, int? limit = null);

  // Media operations
  Task<BlogPostDTO?> SetFeaturedImageAsync(string slug, string mediaId);
  Task<BlogPostDTO?> SetFeaturedMediaAsync(string slug, string mediaId);
  Task<BlogPostDTO?> SetFeaturedVideoAsync(string slug, string mediaId);
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

  public async Task<BlogPostWithMediaDTO?> GetPostWithMediaAsync(string slug, bool? isPublished = true)
  {
    var blogPost = await GetPostAsync(slug, isPublished);
    if (blogPost == null)
    {
      return null;
    }

    return await EnrichWithMediaAsync(blogPost);
  }

  public async Task<IEnumerable<BlogPostWithMediaDTO>> GetPostsWithMediaAsync(string? authorSlug = null, string? category = null, bool? isPublished = true, int? limit = null)
  {
    var blogPosts = await GetPostsAsync(authorSlug, category, isPublished, limit);
    if (blogPosts == null || !blogPosts.Any())
    {
      return Enumerable.Empty<BlogPostWithMediaDTO>();
    }

    var result = new List<BlogPostWithMediaDTO>();
    foreach (var post in blogPosts)
    {
      var enriched = await EnrichWithMediaAsync(post);
      result.Add(enriched);
    }

    return result;
  }

  private async Task<BlogPostWithMediaDTO> EnrichWithMediaAsync(BlogPostDTO blogPost)
  {
    var result = BlogPostWithMediaDTO.FromBlogPostDTO(blogPost);

    // Get featured image if available
    if (!string.IsNullOrEmpty(blogPost.FeaturedImageId))
    {
      result.FeaturedImage = await _mediaService.GetMediaAsync(blogPost.FeaturedImageId);
    }

    // Get featured video if available
    if (!string.IsNullOrEmpty(blogPost.FeaturedVideoId))
    {
      result.FeaturedVideo = await _mediaService.GetMediaAsync(blogPost.FeaturedVideoId);
    }

    // Get featured media if available
    if (!string.IsNullOrEmpty(blogPost.FeaturedMediaId))
    {
      result.FeaturedMedia = await _mediaService.GetMediaAsync(blogPost.FeaturedMediaId);
    }

    // Get all media references if available
    if (!string.IsNullOrEmpty(blogPost.MediaReferencesJson))
    {
      try
      {
        var mediaIds = System.Text.Json.JsonSerializer.Deserialize<List<string>>(blogPost.MediaReferencesJson);
        if (mediaIds != null && mediaIds.Any())
        {
          var mediaEntities = await _mediaService.GetMediaBatchAsync(mediaIds.ToArray());
          result.MediaReferences.AddRange(mediaEntities);
        }
      }
      catch (Exception ex)
      {
        _appLogger.LogError("Error parsing or retrieving media references: {Error}", ex, ex.Message);
      }
    }

    return result;
  }

  private static string GetTableName()
  {
    var useMock = System.Environment.GetEnvironmentVariable("USE_MOCK_STORAGE")?.ToLowerInvariant() == "true";
    var envTableName = System.Environment.GetEnvironmentVariable("BLOGPOSTS_TABLE_NAME");

    if (!string.IsNullOrEmpty(envTableName))
    {
      // If an explicit table name is provided via environment variable, use that
      var resolvedTableName = useMock ? $"mock{envTableName}" : envTableName;
      return TableNameValidator.ValidateTableName(resolvedTableName);
    }
    else
    {
      // Otherwise use ContentNameResolver for consistent naming
      var tableName = ContentNameResolver.GetTableName(ContentSections.Blog, null, useMock);
      return TableNameValidator.ValidateTableName(tableName);
    }
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
      FeaturedVideoId = model.FeaturedVideoId,
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
    entity.FeaturedVideoId = model.FeaturedVideoId;
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
    try
    {
      // First perform the base upsert operation
      var blogPostDto = await UpsertAsync(slug, model);

      if (blogPostDto == null)
      {
        _appLogger.LogWarning("Failed to upsert blog post with slug {Slug}", slug);
        return null;
      }

      // Handle media references
      if (!string.IsNullOrEmpty(model.FeaturedImageId))
      {
        await EnsureMediaReferenceIntegrityAsync(slug, model.FeaturedImageId, "image");
      }

      if (!string.IsNullOrEmpty(model.FeaturedVideoId))
      {
        await EnsureMediaReferenceIntegrityAsync(slug, model.FeaturedVideoId, "video");
      }

      if (!string.IsNullOrEmpty(model.FeaturedMediaId))
      {
        await EnsureMediaReferenceIntegrityAsync(slug, model.FeaturedMediaId, null);
      }

      // Handle media references from the JSON array
      if (!string.IsNullOrEmpty(model.MediaReferencesJson))
      {
        try
        {
          var mediaIds = System.Text.Json.JsonSerializer.Deserialize<List<string>>(model.MediaReferencesJson);
          if (mediaIds != null)
          {
            foreach (var mediaId in mediaIds)
            {
              await EnsureMediaReferenceIntegrityAsync(slug, mediaId, null);
            }
          }
        }
        catch (Exception ex)
        {
          _appLogger.LogError("Error parsing media references JSON: {Error}", ex, ex.Message);
        }
      }

      return blogPostDto;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error in UpsertPostAsync for slug {Slug}: {Error}", ex, slug, ex.Message);
      return null;
    }
  }

  public async Task<bool> DeletePostAsync(string slug)
  {
    try
    {
      // First get the post to retrieve its media references before deletion
      var post = await GetPostAsync(slug, false); // Get regardless of published status

      // If post exists, clean up related media metadata before deleting
      if (post != null)
      {
        // Remove featured image metadata
        if (!string.IsNullOrEmpty(post.FeaturedImageId))
        {
          await RemoveMediaReferenceMetadataAsync(slug, post.FeaturedImageId, "image");
        }

        // Remove featured video metadata
        if (!string.IsNullOrEmpty(post.FeaturedVideoId))
        {
          await RemoveMediaReferenceMetadataAsync(slug, post.FeaturedVideoId, "video");
        }

        // Remove featured media metadata
        if (!string.IsNullOrEmpty(post.FeaturedMediaId))
        {
          await RemoveMediaReferenceMetadataAsync(slug, post.FeaturedMediaId, null);
        }

        // Remove all media references from the JSON array
        if (!string.IsNullOrEmpty(post.MediaReferencesJson))
        {
          try
          {
            var mediaIds = System.Text.Json.JsonSerializer.Deserialize<List<string>>(post.MediaReferencesJson);
            if (mediaIds != null)
            {
              foreach (var mediaId in mediaIds)
              {
                await RemoveMediaReferenceMetadataAsync(slug, mediaId, null);
              }
            }
          }
          catch (Exception ex)
          {
            _appLogger.LogError("Error parsing media references JSON during deletion: {Error}", ex, ex.Message);
          }
        }
      }

      // Now perform the actual deletion of the post
      return await DeleteAsync(slug);
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error in DeletePostAsync for slug {Slug}: {Error}", ex, slug, ex.Message);
      return false;
    }
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

      // If there's an existing image, remove its metadata reference
      if (!string.IsNullOrEmpty(entity.FeaturedImageId) && entity.FeaturedImageId != mediaId)
      {
        await RemoveMediaReferenceMetadataAsync(slug, entity.FeaturedImageId, "image");
      }

      entity.FeaturedImageId = mediaId;
      entity.LastModified = DateTime.UtcNow;

      await _tableStorageService.UpsertEntityAsync(_tableName, entity);

      // Create new metadata reference
      await EnsureMediaReferenceIntegrityAsync(slug, mediaId, "image");

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

      // If there's an existing media, remove its metadata reference
      if (!string.IsNullOrEmpty(entity.FeaturedMediaId) && entity.FeaturedMediaId != mediaId)
      {
        await RemoveMediaReferenceMetadataAsync(slug, entity.FeaturedMediaId);
      }

      entity.FeaturedMediaId = mediaId;
      entity.LastModified = DateTime.UtcNow;

      await _tableStorageService.UpsertEntityAsync(_tableName, entity);

      // Create new metadata reference
      await EnsureMediaReferenceIntegrityAsync(slug, mediaId, media.MediaType);

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

        // Create metadata reference
        await EnsureMediaReferenceIntegrityAsync(slug, mediaId, media.MediaType);

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

      // Try to get the media type for metadata deletion
      string? mediaType = null;
      try
      {
        var media = await _mediaService.GetMediaAsync(mediaId);
        mediaType = media?.MediaType;
      }
      catch
      {
        // Ignore errors, we'll try to delete metadata anyway
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

        // Remove metadata reference
        await RemoveMediaReferenceMetadataAsync(slug, mediaId, mediaType);

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

  public async Task<BlogPostDTO?> SetFeaturedVideoAsync(string slug, string mediaId)
  {
    try
    {
      // Verify media exists
      var media = await _mediaService.GetMediaAsync(mediaId);
      if (media == null || media.MediaType != "video")
      {
        _appLogger.LogWarning("Media {MediaId} not found or is not a video", mediaId);
        return null;
      }

      var entity = await _tableStorageService.GetEntityAsync<BlogPostEntity>(_tableName, GetPartitionKey(slug), GetRowKey(slug));
      if (entity == null)
      {
        _appLogger.LogWarning("Blog post {Slug} not found", slug);
        return null;
      }

      // If there's an existing video, remove its metadata reference
      if (!string.IsNullOrEmpty(entity.FeaturedVideoId) && entity.FeaturedVideoId != mediaId)
      {
        await RemoveMediaReferenceMetadataAsync(slug, entity.FeaturedVideoId, "video");
      }

      entity.FeaturedVideoId = mediaId;
      entity.LastModified = DateTime.UtcNow;

      await _tableStorageService.UpsertEntityAsync(_tableName, entity);

      // Create new metadata reference
      await EnsureMediaReferenceIntegrityAsync(slug, mediaId, "video");

      _appLogger.LogInformation("Set featured video {MediaId} for blog post {Slug}", mediaId, slug);
      return EntityToDto(entity);
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Failed to set featured video for blog post {Slug}: {Error}", ex, slug, ex.Message);
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
        FeaturedVideoId = tableEntity.GetString("FeaturedVideoId") ?? string.Empty,
        MediaReferencesJson = tableEntity.GetString("MediaReferencesJson") ?? string.Empty
      };
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Failed to convert table entity to BlogPostEntity: {Error}", ex, ex.Message);
      return null;
    }
  }

  /// <summary>
  /// Ensures that metadata linking a media item to a blog post is properly maintained.
  /// Creates entries in the appropriate metadata tables (e.g., mockblogimagesmetadata, mockblogvideometadata)
  /// </summary>
  private async Task EnsureMediaReferenceIntegrityAsync(string blogSlug, string mediaId, string? mediaType = null)
  {
    try
    {
      // Skip if either parameter is empty
      if (string.IsNullOrWhiteSpace(blogSlug) || string.IsNullOrWhiteSpace(mediaId))
      {
        return;
      }

      // First verify the media exists
      var media = await _mediaService.GetMediaAsync(mediaId);
      if (media == null)
      {
        _appLogger.LogWarning("Cannot link non-existent media {MediaId} to blog {BlogSlug}", mediaId, blogSlug);
        return;
      }

      // If no specific media type provided, use the one from the media entity
      mediaType ??= media.MediaType;

      // Determine which metadata table to use based on media type
      string metadataTableName = GetMediaMetadataTableName(mediaType);

      // Create a metadata entity to link the blog post with the media
      var metadataEntity = new Azure.Data.Tables.TableEntity
      {
        PartitionKey = blogSlug,
        RowKey = mediaId,
        ["BlogSlug"] = blogSlug,
        ["MediaId"] = mediaId,
        ["MediaType"] = mediaType,
        ["CreatedAt"] = DateTime.UtcNow
      };

      // Add to the metadata table
      await _tableStorageService.UpsertEntityAsync(metadataTableName, metadataEntity);
      _appLogger.LogInformation("Created metadata link between blog {BlogSlug} and {MediaType} {MediaId}",
        blogSlug, mediaType, mediaId);
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Failed to ensure media reference integrity for blog {BlogSlug} and media {MediaId}: {Error}",
        ex, blogSlug, mediaId, ex.Message);
    }
  }

  /// <summary>
  /// Removes metadata entries linking a media item to a blog post when the relationship is removed
  /// </summary>
  private async Task RemoveMediaReferenceMetadataAsync(string blogSlug, string mediaId, string? mediaType = null)
  {
    try
    {
      // Skip if either parameter is empty
      if (string.IsNullOrWhiteSpace(blogSlug) || string.IsNullOrWhiteSpace(mediaId))
      {
        return;
      }

      // If no media type provided, try to fetch it
      if (string.IsNullOrWhiteSpace(mediaType))
      {
        var media = await _mediaService.GetMediaAsync(mediaId);
        mediaType = media?.MediaType ?? "unknown";
      }

      // Determine which metadata table to use based on media type
      string metadataTableName = GetMediaMetadataTableName(mediaType);

      // Remove from the metadata table
      await _tableStorageService.DeleteEntityAsync(metadataTableName, blogSlug, mediaId);
      _appLogger.LogInformation("Removed metadata link between blog {BlogSlug} and {MediaType} {MediaId}",
        blogSlug, mediaType, mediaId);
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Failed to remove media reference metadata for blog {BlogSlug} and media {MediaId}: {Error}",
        ex, blogSlug, mediaId, ex.Message);
    }
  }

  /// <summary>
  /// Gets the appropriate metadata table name based on media type
  /// </summary>
  private string GetMediaMetadataTableName(string mediaType)
  {
    var useMock = System.Environment.GetEnvironmentVariable("USE_MOCK_STORAGE")?.ToLowerInvariant() == "true";
    string baseTableName;

    // Determine base table name based on media type
    switch (mediaType?.ToLowerInvariant())
    {
      case "image":
        baseTableName = ContentNameResolver.GetTableName(ContentSections.Blog, AssetType.Images, useMock) + "metadata";
        break;
      case "video":
        baseTableName = ContentNameResolver.GetTableName(ContentSections.Blog, AssetType.Video, useMock) + "metadata";
        break;
      default:
        baseTableName = ContentNameResolver.GetTableName(ContentSections.Blog, AssetType.Media, useMock) + "metadata";
        break;
    }

    return TableNameValidator.ValidateTableName(baseTableName);
  }

  #endregion
}