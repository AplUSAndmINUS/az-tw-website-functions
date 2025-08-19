using Functions.Books.Models;
using Functions.Books.Mappers;
using SharedStorage.Services.ContentServices;
using SharedStorage.Services.Media;
using SharedStorage.Services.BaseServices;
using SharedStorage.Validators;
using SharedStorage.Models;
using Utils;
using Utils.Constants;
using Utils.Extensions;
using Utils.Validation;
using Azure.Data.Tables;

namespace Functions.Books.Services;

/// <summary>
/// Service interface for book operations
/// </summary>
public interface IBookService
{
  // Core CRUD operations
  Task<BookDTO?> GetBookAsync(string slug, bool? isPublished = true);
  Task<IEnumerable<BookDTO>> GetBooksAsync(string? authorSlug = null, string? category = null, bool? isPublished = true, int? limit = null);
  Task<BookDTO?> UpsertBookAsync(string slug, BookModel model);
  Task<bool> DeleteBookAsync(string slug);

  // Media-enhanced operations
  Task<BookWithMediaDTO?> GetBookWithMediaAsync(string slug, bool? isPublished = true);
  Task<IEnumerable<BookWithMediaDTO>> GetBooksWithMediaAsync(string? authorSlug = null, string? category = null, bool? isPublished = true, int? limit = null);

  // Media operations
  Task<BookDTO?> SetFeaturedImageAsync(string slug, string mediaId);
  Task<BookDTO?> SetFeaturedMediaAsync(string slug, string mediaId);
  Task<BookDTO?> SetFeaturedVideoAsync(string slug, string mediaId);
  Task<BookDTO?> AddMediaReferenceAsync(string slug, string mediaId);
  Task<BookDTO?> RemoveMediaReferenceAsync(string slug, string mediaId);
}

/// <summary>
/// Service implementation for book operations
/// </summary>
public class BookService : ContentService<BookEntity, BookModel, BookDTO>, IBookService
{
  private readonly IMediaService _mediaService;

  public BookService(
    ITableStorageService tableStorageService,
    IMediaService mediaService,
    IAppInsightsLogger<ContentService<BookEntity, BookModel, BookDTO>> appLogger)
    : base(tableStorageService, appLogger, GetTableName())
  {
    _mediaService = mediaService ?? throw new ArgumentNullException(nameof(mediaService));
  }

  public async Task<BookWithMediaDTO?> GetBookWithMediaAsync(string slug, bool? isPublished = true)
  {
    var book = await GetBookAsync(slug, isPublished);
    if (book == null)
    {
      return null;
    }

    var result = BookWithMediaDTO.FromBookDTO(book);

    // Get featured image if available
    if (!string.IsNullOrEmpty(book.FeaturedImageId))
    {
      try
      {
        var featuredImage = await _mediaService.GetMediaAsync(book.FeaturedImageId);
        if (featuredImage != null)
        {
          result.LegacyFeaturedImage = featuredImage;
          result.FeaturedImage = SharedStorage.Models.MediaItemMapper.ToModel(featuredImage);
        }
      }
      catch (Exception ex)
      {
        _appLogger.LogError("Error retrieving featured image: {Error}", ex, ex.Message);
      }
    }

    // Get featured video if available
    if (!string.IsNullOrEmpty(book.FeaturedVideoId))
    {
      try
      {
        var featuredVideo = await _mediaService.GetMediaAsync(book.FeaturedVideoId);
        if (featuredVideo != null)
        {
          result.LegacyFeaturedVideo = featuredVideo;
          result.FeaturedVideo = SharedStorage.Models.MediaItemMapper.ToModel(featuredVideo);
        }
      }
      catch (Exception ex)
      {
        _appLogger.LogError("Error retrieving featured video: {Error}", ex, ex.Message);
      }
    }

    // Get featured media if available
    if (!string.IsNullOrEmpty(book.FeaturedMediaId))
    {
      try
      {
        var featuredMedia = await _mediaService.GetMediaAsync(book.FeaturedMediaId);
        if (featuredMedia != null)
        {
          result.LegacyFeaturedMedia = featuredMedia;
          result.FeaturedMedia = SharedStorage.Models.MediaItemMapper.ToModel(featuredMedia);
        }
      }
      catch (Exception ex)
      {
        _appLogger.LogError("Error retrieving featured media: {Error}", ex, ex.Message);
      }
    }

    // Get all media references if available
    if (!string.IsNullOrEmpty(book.MediaReferencesJson))
    {
      try
      {
        var mediaIds = System.Text.Json.JsonSerializer.Deserialize<List<string>>(book.MediaReferencesJson);
        if (mediaIds != null && mediaIds.Any())
        {
          var mediaEntities = await _mediaService.GetMediaBatchAsync(mediaIds.ToArray());

          // Add to both the legacy references and the new MediaItems collection
          result.LegacyMediaReferences.AddRange(mediaEntities);

          // Convert and add to MediaItems
          foreach (var entity in mediaEntities)
          {
            if (entity != null)
            {
              result.MediaItems.Add(SharedStorage.Models.MediaItemMapper.ToModel(entity));
            }
          }
        }
      }
      catch (Exception ex)
      {
        _appLogger.LogError("Error parsing or retrieving media references: {Error}", ex, ex.Message);
      }
    }

    return result;
  }

  public async Task<IEnumerable<BookWithMediaDTO>> GetBooksWithMediaAsync(string? authorSlug = null, string? category = null, bool? isPublished = true, int? limit = null)
  {
    var books = await GetBooksAsync(authorSlug, category, isPublished, limit);
    var result = new List<BookWithMediaDTO>();

    foreach (var book in books)
    {
      var bookWithMedia = await GetBookWithMediaAsync(book.Slug, isPublished);
      if (bookWithMedia != null)
      {
        result.Add(bookWithMedia);
      }
    }

    return result;
  }

  private static string GetTableName()
  {
    var useMock = System.Environment.GetEnvironmentVariable("USE_MOCK_STORAGE")?.ToLowerInvariant() == "true";
    var envTableName = System.Environment.GetEnvironmentVariable("BOOKS_TABLE_NAME");

    Console.WriteLine($"DEBUG: USE_MOCK_STORAGE={useMock}, BOOKS_TABLE_NAME={envTableName}");

    string tableName;
    if (!string.IsNullOrEmpty(envTableName))
    {
      // If an explicit table name is provided via environment variable, use that
      var resolvedTableName = useMock ? $"mock{envTableName}" : envTableName;
      tableName = TableNameValidator.ValidateTableName(resolvedTableName);
      Console.WriteLine($"DEBUG: Using environment variable table name. Raw={envTableName}, Resolved={resolvedTableName}, Validated={tableName}");
    }
    else
    {
      // Otherwise use ContentNameResolver for consistent naming
      var resolvedTableName = ContentNameResolver.GetTableName(ContentSections.Books, null, useMock);
      tableName = TableNameValidator.ValidateTableName(resolvedTableName);
      Console.WriteLine($"DEBUG: Using ContentNameResolver table name. Resolved={resolvedTableName}, Validated={tableName}");
    }

    return tableName;
  }

  #region ContentService Implementation

  protected override string GetPartitionKey(string slug) => slug;
  protected override string GetRowKey(string slug) => "book";

  protected override bool IsPublished(BookEntity entity) => entity.IsPublished;
  protected override string GetAuthorSlug(BookEntity entity) => entity.AuthorSlug;
  protected override string GetCategory(BookEntity entity) => entity.Category;
  protected override DateTime GetPublishDate(BookEntity entity) => entity.PublishDate;

  protected override BookDTO EntityToDto(BookEntity entity) => BookMapper.Instance.EntityToDTO(entity);

  protected override BookEntity ModelToEntity(BookModel model)
  {
    var entity = new BookEntity
    {
      Id = model.Id ?? Guid.NewGuid().ToString(),
      Title = model.Title,
      Content = model.Content,
      AuthorSlug = model.AuthorSlug,
      Category = model.Category,
      Status = model.Status,
      PublishDate = model.PublishDate.EnsureValidStorageDate(),
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

  protected override BookEntity? ConvertTableEntityToTEntity(Azure.Data.Tables.TableEntity tableEntity)
  {
    return ConvertToEntity(tableEntity);
  }

  protected override void UpdateEntityFromModel(BookEntity entity, BookModel model)
  {
    entity.Title = model.Title;
    entity.Content = model.Content;
    entity.AuthorSlug = model.AuthorSlug;
    entity.Category = model.Category;
    entity.Status = model.Status;

    entity.PublishDate = model.PublishDate.EnsureValidStorageDate();
    entity.LastModified = DateTime.UtcNow;
    entity.TagsJson = System.Text.Json.JsonSerializer.Serialize(model.TagsList);
    entity.FeaturedImageId = model.FeaturedImageId;
    entity.FeaturedMediaId = model.FeaturedMediaId;
    entity.FeaturedVideoId = model.FeaturedVideoId;
    entity.MediaReferencesJson = model.MediaReferencesJson;
  }

  protected override void ValidateModel(BookModel model)
  {
    if (string.IsNullOrWhiteSpace(model.Title))
      throw new ArgumentException("Title is required", nameof(model));

    if (string.IsNullOrWhiteSpace(model.Slug))
      throw new ArgumentException("Slug is required", nameof(model));

    if (string.IsNullOrWhiteSpace(model.AuthorSlug))
      throw new ArgumentException("Author slug is required", nameof(model));
  }

  #endregion

  private BookEntity? ConvertToEntity(Azure.Data.Tables.TableEntity tableEntity)
  {
    try
    {
      // Get status first since we need it for the PublishDate validation
      string status = tableEntity.GetString("Status") ?? "Draft";

      // Get the PublishDate and ensure it's valid
      DateTime publishDate = tableEntity.GetDateTime("PublishDate") ?? DateTime.UtcNow;
      publishDate = BaseContentMapper<BookModel, BookEntity>.EnsureValidPublishDate(publishDate, status);

      Console.WriteLine($"DEBUG: Converting TableEntity to BookEntity - PublishDate after validation: {publishDate}");

      return new BookEntity
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
        Status = status,
        PublishDate = publishDate,
        LastModified = (tableEntity.GetDateTime("LastModified") ?? DateTime.UtcNow).EnsureUtc(),
        TagsJson = tableEntity.GetString("TagsJson") ?? "[]",
        FeaturedImageId = tableEntity.GetString("FeaturedImageId") ?? string.Empty,
        FeaturedMediaId = tableEntity.GetString("FeaturedMediaId") ?? string.Empty,
        FeaturedVideoId = tableEntity.GetString("FeaturedVideoId") ?? string.Empty,
        MediaReferencesJson = tableEntity.GetString("MediaReferencesJson") ?? string.Empty
      };
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Failed to convert table entity to BookEntity: {Error}", ex, ex.Message);
      return null;
    }
  }

  // Delegate to base class implementations
  public async Task<BookDTO?> GetBookAsync(string slug, bool? isPublished = true)
  {
    return await GetBySlugAsync(slug, isPublished);
  }

  public async Task<IEnumerable<BookDTO>> GetBooksAsync(string? authorSlug = null, string? category = null, bool? isPublished = true, int? limit = null)
  {
    if (isPublished == true)
    {
      return await GetPublishedContentAsync(authorSlug, category, limit);
    }

    // Fix: If isPublished is false, get all content and filter for unpublished only
    // If isPublished is null, get all content without filtering
    try
    {
      var filters = new List<string>();

      if (!string.IsNullOrWhiteSpace(authorSlug))
        filters.Add($"AuthorSlug eq '{authorSlug}'");

      if (!string.IsNullOrWhiteSpace(category))
        filters.Add($"Category eq '{category}'");

      // Add published status filter when isPublished is explicitly false
      if (isPublished == false)
        filters.Add("IsPublished eq false");

      var filter = filters.Any() ? string.Join(" and ", filters) : null;
      var pageSize = Math.Min(limit ?? 50, 100);

      var result = await _tableStorageService.GetEntitiesAsync(_tableName, filter, pageSize);
      var entities = result.Entities.Select(e => ConvertToEntity(e))
                          .Where(e => e != null)
                          .OrderByDescending(e => GetPublishDate(e!))
                          .Select(e => EntityToDto(e!))
                          .ToList();

      _appLogger.LogInformation("Retrieved {Count} books", entities.Count);
      return entities;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error retrieving books: {Error}", ex, ex.Message);
      return Enumerable.Empty<BookDTO>();
    }
  }

  public async Task<BookDTO?> UpsertBookAsync(string slug, BookModel model)
  {
    try
    {
      // Ensure status and isPublished are consistent before upserting
      string originalStatus = model.Status;
      model.Status = DataValidation.EnsureStatusConsistency(model.Status, model.IsPublished);

      if (originalStatus != model.Status)
      {
        _appLogger.LogInformation("Updated status from '{OldStatus}' to '{NewStatus}' based on IsPublished={IsPublished} for book with slug {Slug}",
          originalStatus, model.Status, model.IsPublished, slug);
      }

      // First perform the base upsert operation
      var bookDto = await UpsertAsync(slug, model);

      if (bookDto == null)
      {
        _appLogger.LogWarning("Failed to upsert book with slug {Slug}", slug);
        return null;
      }

      return bookDto;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error upserting book with slug {Slug}: {Error}", ex, slug, ex.Message);
      return null;
    }
  }

  public async Task<bool> DeleteBookAsync(string slug)
  {
    return await DeleteAsync(slug);
  }

  public async Task<BookDTO?> SetFeaturedImageAsync(string slug, string mediaId)
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

      var entity = await _tableStorageService.GetEntityAsync<BookEntity>(_tableName, GetPartitionKey(slug), GetRowKey(slug));
      if (entity == null)
      {
        _appLogger.LogWarning("Book {Slug} not found", slug);
        return null;
      }

      entity.FeaturedImageId = mediaId;
      entity.LastModified = DateTime.UtcNow;

      await _tableStorageService.UpsertEntityAsync(_tableName, entity);
      return EntityToDto(entity);
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error setting featured image for book {Slug}: {Error}", ex, slug, ex.Message);
      return null;
    }
  }

  public async Task<BookDTO?> SetFeaturedMediaAsync(string slug, string mediaId)
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

      var entity = await _tableStorageService.GetEntityAsync<BookEntity>(_tableName, GetPartitionKey(slug), GetRowKey(slug));
      if (entity == null)
      {
        _appLogger.LogWarning("Book {Slug} not found", slug);
        return null;
      }

      entity.FeaturedMediaId = mediaId;
      entity.LastModified = DateTime.UtcNow;

      await _tableStorageService.UpsertEntityAsync(_tableName, entity);
      return EntityToDto(entity);
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error setting featured media for book {Slug}: {Error}", ex, slug, ex.Message);
      return null;
    }
  }

  public async Task<BookDTO?> SetFeaturedVideoAsync(string slug, string mediaId)
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

      var entity = await _tableStorageService.GetEntityAsync<BookEntity>(_tableName, GetPartitionKey(slug), GetRowKey(slug));
      if (entity == null)
      {
        _appLogger.LogWarning("Book {Slug} not found", slug);
        return null;
      }

      entity.FeaturedVideoId = mediaId;
      entity.LastModified = DateTime.UtcNow;

      await _tableStorageService.UpsertEntityAsync(_tableName, entity);
      return EntityToDto(entity);
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error setting featured video for book {Slug}: {Error}", ex, slug, ex.Message);
      return null;
    }
  }

  public async Task<BookDTO?> AddMediaReferenceAsync(string slug, string mediaId)
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

      var entity = await _tableStorageService.GetEntityAsync<BookEntity>(_tableName, GetPartitionKey(slug), GetRowKey(slug));
      if (entity == null)
      {
        _appLogger.LogWarning("Book {Slug} not found", slug);
        return null;
      }

      // Get current media references
      var mediaReferences = new List<string>();
      if (!string.IsNullOrEmpty(entity.MediaReferencesJson) && entity.MediaReferencesJson != "[]")
      {
        try
        {
          mediaReferences = System.Text.Json.JsonSerializer.Deserialize<List<string>>(entity.MediaReferencesJson) ?? new List<string>();
        }
        catch (System.Text.Json.JsonException)
        {
          _appLogger.LogWarning("Invalid MediaReferencesJson format for book {Slug}, initializing empty list", slug);
          mediaReferences = new List<string>();
        }
      }

      // Add media reference if not already present
      if (!mediaReferences.Contains(mediaId))
      {
        mediaReferences.Add(mediaId);
        entity.MediaReferencesJson = System.Text.Json.JsonSerializer.Serialize(mediaReferences);
        entity.LastModified = DateTime.UtcNow;

        await _tableStorageService.UpsertEntityAsync(_tableName, entity);
        return EntityToDto(entity);
      }

      // Media reference already exists, return current state
      return EntityToDto(entity);
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error adding media reference to book {Slug}: {Error}", ex, slug, ex.Message);
      return null;
    }
  }

  public async Task<BookDTO?> RemoveMediaReferenceAsync(string slug, string mediaId)
  {
    try
    {
      var entity = await _tableStorageService.GetEntityAsync<BookEntity>(_tableName, GetPartitionKey(slug), GetRowKey(slug));
      if (entity == null)
      {
        _appLogger.LogWarning("Book {Slug} not found", slug);
        return null;
      }

      // Get current media references
      var mediaReferences = new List<string>();
      if (!string.IsNullOrEmpty(entity.MediaReferencesJson) && entity.MediaReferencesJson != "[]")
      {
        try
        {
          mediaReferences = System.Text.Json.JsonSerializer.Deserialize<List<string>>(entity.MediaReferencesJson) ?? new List<string>();
        }
        catch (System.Text.Json.JsonException)
        {
          _appLogger.LogWarning("Invalid MediaReferencesJson format for book {Slug}, treating as empty", slug);
          mediaReferences = new List<string>();
        }
      }

      // Remove media reference if present
      if (mediaReferences.Remove(mediaId))
      {
        entity.MediaReferencesJson = System.Text.Json.JsonSerializer.Serialize(mediaReferences);
        entity.LastModified = DateTime.UtcNow;

        await _tableStorageService.UpsertEntityAsync(_tableName, entity);
        return EntityToDto(entity);
      }

      // Media reference didn't exist, return current state
      return EntityToDto(entity);
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error removing media reference from book {Slug}: {Error}", ex, slug, ex.Message);
      return null;
    }
  }
}