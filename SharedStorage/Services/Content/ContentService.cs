using Azure.Data.Tables;
using SharedStorage.Services.BaseServices;
using Utils;

namespace SharedStorage.Services.ContentServices;

public interface IContentService<TEntity, TModel, TDto>
    where TEntity : class, ITableEntity, new()
    where TModel : class
    where TDto : class
{
  Task<TDto?> GetContentAsync(string slug, bool? isPublished = true)
  {
    // Alias for GetBySlugAsync
    return GetBySlugAsync(slug, isPublished);
  }
  Task<TDto?> GetBySlugAsync(string slug, bool? isPublished = true);
  Task<IEnumerable<TDto>> GetPublishedContentAsync(string? authorSlug = null, string? category = null, int? limit = null);
  Task<TDto?> UpsertAsync(string slug, TModel model);
  Task<bool> DeleteAsync(string slug);
}

public abstract class ContentService<TEntity, TModel, TDto>(
    ITableStorageService tableStorageService,
    IAppInsightsLogger<ContentService<TEntity, TModel, TDto>> appLogger,
    string tableName) : IContentService<TEntity, TModel, TDto>
    where TEntity : class, ITableEntity, new()
    where TModel : class
    where TDto : class
{
  protected readonly ITableStorageService _tableStorageService = tableStorageService;
  protected readonly IAppInsightsLogger<ContentService<TEntity, TModel, TDto>> _appLogger = appLogger;
  protected readonly string _tableName = tableName;

  // Common CRUD operations
  public async Task<TDto?> GetBySlugAsync(string slug, bool? isPublished = true)
  {
    if (string.IsNullOrWhiteSpace(slug))
    {
      _appLogger.LogError("Slug cannot be null or empty.", new Exception("Invalid slug"));
      return null;
    }

    try
    {
      var entity = await _tableStorageService.GetEntityAsync<TEntity>(_tableName, GetPartitionKey(slug), GetRowKey(slug));
      if (entity == null)
      {
        _appLogger.LogWarning("Content with slug {Slug} not found.", slug);
        return null;
      }

      if (isPublished.HasValue && isPublished.Value && !IsPublished(entity))
      {
        _appLogger.LogInformation("Content with slug {Slug} is not published.", slug);
        return null;
      }

      return EntityToDto(entity);
    }
    catch (Exception ex)
    {
      _appLogger.LogError($"Error retrieving content with slug {slug}: {ex.Message}", ex);
      return null;
    }
  }

  public async Task<IEnumerable<TDto>> GetPublishedContentAsync(string? authorSlug = null, string? category = null, int? limit = null)
  {
    try
    {
      var entitiesPageResult = await _tableStorageService.GetEntitiesAsync(_tableName);
      var entities = entitiesPageResult.Entities;

      var typedEntities = new List<TEntity>();

      // Convert TableEntity to TEntity and filter null results
      foreach (var tableEntity in entities)
      {
        try
        {
          var typedEntity = ConvertTableEntityToTEntity(tableEntity);
          if (typedEntity != null)
          {
            typedEntities.Add(typedEntity);
          }
        }
        catch (Exception ex)
        {
          _appLogger.LogWarning("Failed to convert table entity to typed entity: {Error}", ex.Message);
          // Continue processing other entities
        }
      }

      var query = typedEntities.Where(e => IsPublished(e));

      if (!string.IsNullOrWhiteSpace(authorSlug))
        query = query.Where(e => GetAuthorSlug(e) == authorSlug);

      if (!string.IsNullOrWhiteSpace(category))
        query = query.Where(e => GetCategory(e) == category);

      if (limit.HasValue)
        query = query.Take(limit.Value);

      return query.Select(EntityToDto);
    }
    catch (Exception ex)
    {
      _appLogger.LogError($"Error retrieving published content: {ex.Message}", ex);
      return Enumerable.Empty<TDto>();
    }
  }

  public async Task<TDto?> UpsertAsync(string slug, TModel model)
  {
    if (string.IsNullOrWhiteSpace(slug))
    {
      _appLogger.LogError("Slug cannot be null or empty.", new Exception("Missing slug"));
      return null;
    }

    try
    {
      // Validate the model
      ValidateModel(model);

      // Check if exists
      var existingEntity = await _tableStorageService.GetEntityAsync<TEntity>(_tableName, GetPartitionKey(slug), GetRowKey(slug));

      TEntity entity;
      if (existingEntity != null)
      {
        // Update existing
        entity = existingEntity;
        UpdateEntityFromModel(entity, model);
        _appLogger.LogInformation("Updating existing content with slug: {Slug}", slug);
      }
      else
      {
        // Create new
        entity = ModelToEntity(model);
        _appLogger.LogInformation("Creating new content with slug: {Slug}", slug);
      }

      await _tableStorageService.UpsertEntityAsync(_tableName, entity);
      _appLogger.LogInformation("Successfully upserted content with slug: {Slug}", slug);

      return EntityToDto(entity);
    }
    catch (Exception ex)
    {
      _appLogger.LogError($"Error upserting content with slug {slug}: {ex.Message}", ex);
      return null;
    }
  }

  public async Task<bool> DeleteAsync(string slug)
  {
    if (string.IsNullOrWhiteSpace(slug))
    {
      _appLogger.LogError("Slug cannot be null or empty.", new Exception("Missing slug"));
      return false;
    }

    try
    {
      var entity = await _tableStorageService.GetEntityAsync<TEntity>(_tableName, GetPartitionKey(slug), GetRowKey(slug));
      if (entity == null)
      {
        _appLogger.LogWarning("Content with slug {Slug} not found for deletion.", slug);
        return false;
      }

      await _tableStorageService.DeleteEntityAsync(_tableName, entity.PartitionKey, entity.RowKey);
      _appLogger.LogInformation("Successfully deleted content with slug: {Slug}", slug);
      return true;
    }
    catch (Exception ex)
    {
      _appLogger.LogError($"Error deleting content with slug {slug}: {ex.Message}", ex);
      return false;
    }
  }

  // Abstract methods that each service must implement
  protected abstract string GetPartitionKey(string slug);
  protected abstract string GetRowKey(string slug);
  protected abstract bool IsPublished(TEntity entity);
  protected abstract string GetAuthorSlug(TEntity entity);
  protected abstract string GetCategory(TEntity entity);
  protected abstract TDto EntityToDto(TEntity entity);
  protected abstract TEntity ModelToEntity(TModel model);
  protected abstract void UpdateEntityFromModel(TEntity entity, TModel model);
  protected abstract void ValidateModel(TModel model);
  protected abstract TEntity? ConvertTableEntityToTEntity(Azure.Data.Tables.TableEntity tableEntity);
}