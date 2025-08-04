using Functions.GitHub.Models;
using SharedStorage.Services.ContentServices;
using SharedStorage.Services.BaseServices;
using Utils;
using Utils.Constants;
using Azure.Data.Tables;
using System.Text.Json;
using Utils.Extensions;

namespace Functions.GitHub.Services;

public interface IGitHubRepoService
{
  // Core operations for GitHub repositories
  Task<GitHubRepoDTO?> GetRepoAsync(string slug, bool? isPublished = true);
  Task<GitHubRepoDTO?> GetRepoByGitHubIdAsync(long gitHubId);
  Task<IEnumerable<GitHubRepoDTO>> GetReposAsync(string? category = null, bool? isPublished = true, int? limit = null);
  Task<GitHubRepoDTO?> UpsertRepoAsync(GitHubRepoModel model);
  Task<bool> DeleteRepoAsync(string slug);

  // Sync operations
  Task<int> SyncRepositoriesFromGitHubAsync(string username);
}

public class GitHubRepoService : ContentService<GitHubRepoEntity, GitHubRepoModel, GitHubRepoDTO>, IGitHubRepoService
{
  private readonly IGitHubApiService _gitHubApiService;

  public GitHubRepoService(
    ITableStorageService tableStorageService,
    IGitHubApiService gitHubApiService,
    IAppInsightsLogger<ContentService<GitHubRepoEntity, GitHubRepoModel, GitHubRepoDTO>> appLogger)
    : base(tableStorageService, appLogger, GetTableName())
  {
    _gitHubApiService = gitHubApiService ?? throw new ArgumentNullException(nameof(gitHubApiService));
  }

  public async Task<GitHubRepoDTO?> GetRepoAsync(string slug, bool? isPublished = true)
  {
    if (string.IsNullOrWhiteSpace(slug))
      return null;

    try
    {
      var currentTableName = GetTableName();
      _appLogger.LogInformation("Getting GitHub repository with slug: {Slug} from table '{TableName}'", slug, currentTableName);

      var entity = await GetEntityBySlugAsync(slug);
      
      // If no entity found, try fallback table (handles environment variable mismatch)
      if (entity == null)
      {
        var fallbackTableName = GetFallbackTableName();
        if (!string.Equals(currentTableName, fallbackTableName, StringComparison.OrdinalIgnoreCase))
        {
          _appLogger.LogInformation("Repository with slug '{Slug}' not found in table '{CurrentTable}', trying fallback table '{FallbackTable}'", 
            slug, currentTableName, fallbackTableName);
          
          entity = await GetEntityBySlugFromTableAsync(fallbackTableName, slug);
          
          if (entity != null)
          {
            _appLogger.LogWarning("Found repository with slug '{Slug}' in fallback table '{FallbackTable}' instead of expected table '{CurrentTable}'. " +
              "This indicates a possible environment variable mismatch between sync and read operations.", 
              slug, fallbackTableName, currentTableName);
          }
        }
      }
      
      if (entity == null || (isPublished == true && !entity.IsPublished))
      {
        _appLogger.LogInformation("GitHub repository with slug {Slug} not found or not published", slug);
        return null;
      }

      var model = entity.ToModel<GitHubRepoModel>();
      var dto = GitHubRepoMapper.ToDTO(model);

      _appLogger.LogInformation("Successfully retrieved GitHub repository with slug: {Slug}", slug);
      return dto;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error getting GitHub repository with slug {Slug}", ex, slug);
      return null;
    }
  }

  public async Task<GitHubRepoDTO?> GetRepoByGitHubIdAsync(long gitHubId)
  {
    try
    {
      var currentTableName = GetTableName();
      _appLogger.LogInformation("Getting GitHub repository with GitHub ID: {GitHubId} from table '{TableName}'", gitHubId, currentTableName);

      // Query by GitHubId (this requires scanning the table since GitHubId is not a key)
      var entities = await GetAllEntitiesAsync();
      var entity = entities.FirstOrDefault(e => e.GitHubId == gitHubId);

      // If no entity found, try fallback table (handles environment variable mismatch)
      if (entity == null)
      {
        var fallbackTableName = GetFallbackTableName();
        if (!string.Equals(currentTableName, fallbackTableName, StringComparison.OrdinalIgnoreCase))
        {
          _appLogger.LogInformation("Repository with GitHub ID '{GitHubId}' not found in table '{CurrentTable}', trying fallback table '{FallbackTable}'", 
            gitHubId, currentTableName, fallbackTableName);
          
          var fallbackEntities = await GetAllEntitiesFromTableAsync(fallbackTableName);
          entity = fallbackEntities.FirstOrDefault(e => e.GitHubId == gitHubId);
          
          if (entity != null)
          {
            _appLogger.LogWarning("Found repository with GitHub ID '{GitHubId}' in fallback table '{FallbackTable}' instead of expected table '{CurrentTable}'. " +
              "This indicates a possible environment variable mismatch between sync and read operations.", 
              gitHubId, fallbackTableName, currentTableName);
          }
        }
      }

      if (entity == null)
      {
        _appLogger.LogInformation("GitHub repository with GitHub ID {GitHubId} not found", gitHubId);
        return null;
      }

      var model = entity.ToModel<GitHubRepoModel>();
      var dto = GitHubRepoMapper.ToDTO(model);

      _appLogger.LogInformation("Successfully retrieved GitHub repository with GitHub ID: {GitHubId}", gitHubId);
      return dto;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error getting GitHub repository with GitHub ID {GitHubId}", ex, gitHubId);
      return null;
    }
  }

  public async Task<IEnumerable<GitHubRepoDTO>> GetReposAsync(string? category = null, bool? isPublished = true, int? limit = null)
  {
    try
    {
      var currentTableName = GetTableName();
      _appLogger.LogInformation("Getting GitHub repositories from table '{TableName}' with category: {Category}, isPublished: {IsPublished}, limit: {Limit}",
        currentTableName, category ?? "all", isPublished ?? false, limit ?? 0);

      var entities = await GetEntitiesAsync(category, isPublished, limit);
      
      // If no entities found, try fallback table (handles environment variable mismatch)
      if (!entities.Any())
      {
        var fallbackTableName = GetFallbackTableName();
        if (!string.Equals(currentTableName, fallbackTableName, StringComparison.OrdinalIgnoreCase))
        {
          _appLogger.LogInformation("No repositories found in table '{CurrentTable}', trying fallback table '{FallbackTable}'", 
            currentTableName, fallbackTableName);
          
          entities = await GetEntitiesFromTableAsync(fallbackTableName, category, isPublished, limit);
          
          if (entities.Any())
          {
            _appLogger.LogWarning("Found repositories in fallback table '{FallbackTable}' instead of expected table '{CurrentTable}'. " +
              "This indicates a possible environment variable mismatch between sync and read operations.", 
              fallbackTableName, currentTableName);
          }
        }
      }
      
      var models = entities.Select(e => e.ToModel<GitHubRepoModel>());
      var dtos = GitHubRepoMapper.ToDTOs(models);

      _appLogger.LogInformation("Successfully retrieved {Count} GitHub repositories", dtos.Count());
      return dtos;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error getting GitHub repositories", ex);
      return [];
    }
  }

  public async Task<GitHubRepoDTO?> UpsertRepoAsync(GitHubRepoModel model)
  {
    ArgumentNullException.ThrowIfNull(model);

    try
    {
      _appLogger.LogInformation("Upserting GitHub repository: {Name}", model.Name);

      var entity = GitHubRepoEntity.FromModel(model);
      var upsertedEntity = await UpsertEntityAsync(entity);

      if (upsertedEntity == null)
      {
        _appLogger.LogWarning("Failed to upsert GitHub repository: {Name}", model.Name);
        return null;
      }

      var upsertedModel = upsertedEntity.ToModel<GitHubRepoModel>();
      var dto = GitHubRepoMapper.ToDTO(upsertedModel);

      _appLogger.LogInformation("Successfully upserted GitHub repository: {Name}", model.Name);
      return dto;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error upserting GitHub repository {Name}", ex, model.Name);
      return null;
    }
  }

  public async Task<bool> DeleteRepoAsync(string slug)
  {
    if (string.IsNullOrWhiteSpace(slug))
      return false;

    try
    {
      _appLogger.LogInformation("Deleting GitHub repository with slug: {Slug}", slug);

      var success = await DeleteEntityBySlugAsync(slug);

      if (success)
      {
        _appLogger.LogInformation("Successfully deleted GitHub repository with slug: {Slug}", slug);
      }
      else
      {
        _appLogger.LogWarning("Failed to delete GitHub repository with slug: {Slug}", slug);
      }

      return success;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error deleting GitHub repository with slug {Slug}", ex, slug);
      return false;
    }
  }

  public async Task<int> SyncRepositoriesFromGitHubAsync(string username)
  {
    if (string.IsNullOrWhiteSpace(username))
      throw new ArgumentException("Username cannot be null or empty", nameof(username));

    try
    {
      _appLogger.LogInformation("Starting sync of GitHub repositories for user: {Username}", username);

      var repositories = await _gitHubApiService.GetRepositoriesAsync(username);
      var syncedCount = 0;

      foreach (var repo in repositories)
      {
        var result = await UpsertRepoAsync(repo);
        if (result != null)
        {
          syncedCount++;
        }
      }

      _appLogger.LogInformation("Successfully synced {SyncedCount} out of {TotalCount} repositories for user: {Username}",
        syncedCount, repositories.Count(), username);

      return syncedCount;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error syncing repositories for user {Username}", ex, username);
      return 0;
    }
  }

  private static string GetTableName()
  {
    var useMockStorage = Environment.GetEnvironmentVariable("USE_MOCK_STORAGE");
    var prefix = useMockStorage?.ToLowerInvariant() == "true" ? "mock" : string.Empty;
    return $"{prefix}github";
  }

  private static string GetFallbackTableName()
  {
    // Return the opposite table name for fallback scenarios
    var useMockStorage = Environment.GetEnvironmentVariable("USE_MOCK_STORAGE");
    var currentlyUsingMock = useMockStorage?.ToLowerInvariant() == "true";
    var fallbackPrefix = currentlyUsingMock ? string.Empty : "mock";
    return $"{fallbackPrefix}github";
  }

  protected override GitHubRepoDTO EntityToDto(GitHubRepoEntity entity)
  {
    var model = entity.ToModel<GitHubRepoModel>();
    return GitHubRepoMapper.ToDTO(model);
  }

  protected override GitHubRepoEntity ModelToEntity(GitHubRepoModel model)
  {
    return GitHubRepoEntity.FromModel(model);
  }

  protected override void UpdateEntityFromModel(GitHubRepoEntity entity, GitHubRepoModel model)
  {
    entity.Title = model.Name;
    entity.Description = model.Description ?? string.Empty;
    entity.Content = model.Content ?? string.Empty;
    entity.Category = model.Category ?? "repository";
    entity.Status = "Published";
    entity.LastModified = DateTime.UtcNow.EnsureValidStorageDate();
    entity.GitHubId = model.GitHubId;
    entity.Name = model.Name;
    entity.FullName = model.FullName;
    entity.HtmlUrl = model.HtmlUrl;
    entity.Language = model.Language;
    entity.StargazersCount = model.StargazersCount;
    entity.ForksCount = model.ForksCount;
    entity.WatchersCount = model.WatchersCount;
    entity.OpenIssuesCount = model.OpenIssuesCount;
    entity.IsPrivate = model.IsPrivate;
    entity.IsFork = model.IsFork;
    entity.IsArchived = model.IsArchived;
    entity.GitHubCreatedAt = model.GitHubCreatedAt;
    entity.GitHubUpdatedAt = model.GitHubUpdatedAt;
    entity.GitHubPushedAt = model.GitHubPushedAt;
    entity.DefaultBranch = model.DefaultBranch;
    entity.TopicsJson = JsonSerializer.Serialize(model.TopicsList ?? []);
  }

  protected override void ValidateModel(GitHubRepoModel model)
  {
    ArgumentNullException.ThrowIfNull(model);

    if (string.IsNullOrWhiteSpace(model.Name))
      throw new ArgumentException("Repository name is required", nameof(model.Name));

    if (string.IsNullOrWhiteSpace(model.FullName))
      throw new ArgumentException("Repository full name is required", nameof(model.FullName));

    if (model.GitHubId <= 0)
      throw new ArgumentException("GitHub ID must be positive", nameof(model.GitHubId));
  }

  protected override GitHubRepoEntity? ConvertTableEntityToTEntity(TableEntity tableEntity)
  {
    try
    {
      var entity = new GitHubRepoEntity
      {
        PartitionKey = tableEntity.PartitionKey,
        RowKey = tableEntity.RowKey,
        Timestamp = tableEntity.Timestamp,
        ETag = tableEntity.ETag
      };

      // Map common properties
      if (tableEntity.TryGetValue("Id", out var id)) entity.Id = id?.ToString() ?? string.Empty;
      if (tableEntity.TryGetValue("Title", out var title)) entity.Title = title?.ToString() ?? string.Empty;
      if (tableEntity.TryGetValue("AuthorSlug", out var authorSlug)) entity.AuthorSlug = authorSlug?.ToString() ?? string.Empty;
      if (tableEntity.TryGetValue("Description", out var description)) entity.Description = description?.ToString() ?? string.Empty;
      if (tableEntity.TryGetValue("Content", out var content)) entity.Content = content?.ToString() ?? string.Empty;
      if (tableEntity.TryGetValue("Slug", out var slug)) entity.Slug = slug?.ToString() ?? string.Empty;
      if (tableEntity.TryGetValue("Category", out var category)) entity.Category = category?.ToString() ?? string.Empty;
      if (tableEntity.TryGetValue("Status", out var status)) entity.Status = status?.ToString() ?? string.Empty;
      if (tableEntity.TryGetValue("PublishDate", out var publishDate) && publishDate is DateTime dt1) entity.PublishDate = dt1;
      if (tableEntity.TryGetValue("LastModified", out var lastModified) && lastModified is DateTime dt2) entity.LastModified = dt2;
      if (tableEntity.TryGetValue("TagsJson", out var tagsJson)) entity.TagsJson = tagsJson?.ToString() ?? "[]";

      // Map GitHub-specific properties
      if (tableEntity.TryGetValue("GitHubId", out var githubId) && long.TryParse(githubId?.ToString(), out var gid)) entity.GitHubId = gid;
      if (tableEntity.TryGetValue("Name", out var name)) entity.Name = name?.ToString() ?? string.Empty;
      if (tableEntity.TryGetValue("FullName", out var fullName)) entity.FullName = fullName?.ToString() ?? string.Empty;
      if (tableEntity.TryGetValue("HtmlUrl", out var htmlUrl)) entity.HtmlUrl = htmlUrl?.ToString() ?? string.Empty;
      if (tableEntity.TryGetValue("Language", out var language)) entity.Language = language?.ToString();
      if (tableEntity.TryGetValue("StargazersCount", out var stargazers) && int.TryParse(stargazers?.ToString(), out var sc)) entity.StargazersCount = sc;
      if (tableEntity.TryGetValue("ForksCount", out var forks) && int.TryParse(forks?.ToString(), out var fc)) entity.ForksCount = fc;
      if (tableEntity.TryGetValue("WatchersCount", out var watchers) && int.TryParse(watchers?.ToString(), out var wc)) entity.WatchersCount = wc;
      if (tableEntity.TryGetValue("OpenIssuesCount", out var openIssues) && int.TryParse(openIssues?.ToString(), out var oic)) entity.OpenIssuesCount = oic;
      if (tableEntity.TryGetValue("IsPrivate", out var isPrivate) && bool.TryParse(isPrivate?.ToString(), out var ip)) entity.IsPrivate = ip;
      if (tableEntity.TryGetValue("IsFork", out var isFork) && bool.TryParse(isFork?.ToString(), out var ifo)) entity.IsFork = ifo;
      if (tableEntity.TryGetValue("IsArchived", out var isArchived) && bool.TryParse(isArchived?.ToString(), out var ia)) entity.IsArchived = ia;
      if (tableEntity.TryGetValue("GitHubCreatedAt", out var createdAt) && createdAt is DateTime dt3) entity.GitHubCreatedAt = dt3;
      if (tableEntity.TryGetValue("GitHubUpdatedAt", out var updatedAt) && updatedAt is DateTime dt4) entity.GitHubUpdatedAt = dt4;
      if (tableEntity.TryGetValue("GitHubPushedAt", out var pushedAt) && pushedAt is DateTime dt5) entity.GitHubPushedAt = dt5;
      if (tableEntity.TryGetValue("DefaultBranch", out var defaultBranch)) entity.DefaultBranch = defaultBranch?.ToString() ?? "main";
      if (tableEntity.TryGetValue("TopicsJson", out var topicsJson)) entity.TopicsJson = topicsJson?.ToString() ?? "[]";

      return entity;
    }
    catch (Exception ex)
    {
      _appLogger.LogWarning("Failed to convert TableEntity to GitHubRepoEntity: {Error}", ex.Message);
      return null;
    }
  }

  protected override string GetPartitionKey(string slug)
  {
    // Use a constant partition key for GitHub repos to keep them grouped
    return "github";
  }

  protected override string GetRowKey(string slug)
  {
    // Use slug as row key for easy lookup
    return slug;
  }

  protected override bool IsPublished(GitHubRepoEntity entity)
  {
    return entity.Status == "Published";
  }

  protected override string GetAuthorSlug(GitHubRepoEntity entity)
  {
    return entity.AuthorSlug;
  }

  protected override string GetCategory(GitHubRepoEntity entity)
  {
    return entity.Category;
  }

  // Helper methods for additional functionality
  protected async Task<GitHubRepoEntity?> GetEntityBySlugAsync(string slug)
  {
    return await _tableStorageService.GetEntityAsync<GitHubRepoEntity>(_tableName, GetPartitionKey(slug), GetRowKey(slug));
  }

  protected async Task<GitHubRepoEntity?> GetEntityBySlugFromTableAsync(string tableName, string slug)
  {
    return await _tableStorageService.GetEntityAsync<GitHubRepoEntity>(tableName, GetPartitionKey(slug), GetRowKey(slug));
  }

  protected async Task<IEnumerable<GitHubRepoEntity>> GetAllEntitiesAsync()
  {
    var result = await _tableStorageService.GetEntitiesAsync(_tableName);
    var entities = new List<GitHubRepoEntity>();

    foreach (var tableEntity in result.Entities)
    {
      var entity = ConvertTableEntityToTEntity(tableEntity);
      if (entity != null)
      {
        entities.Add(entity);
      }
    }

    return entities;
  }

  protected async Task<IEnumerable<GitHubRepoEntity>> GetAllEntitiesFromTableAsync(string tableName)
  {
    var result = await _tableStorageService.GetEntitiesAsync(tableName);
    var entities = new List<GitHubRepoEntity>();

    foreach (var tableEntity in result.Entities)
    {
      var entity = ConvertTableEntityToTEntity(tableEntity);
      if (entity != null)
      {
        entities.Add(entity);
      }
    }

    return entities;
  }

  protected async Task<IEnumerable<GitHubRepoEntity>> GetEntitiesFromTableAsync(string tableName, string? category = null, bool? isPublished = true, int? limit = null)
  {
    var result = await _tableStorageService.GetEntitiesAsync(tableName);
    var entities = new List<GitHubRepoEntity>();

    foreach (var tableEntity in result.Entities)
    {
      var entity = ConvertTableEntityToTEntity(tableEntity);
      if (entity != null)
      {
        entities.Add(entity);
      }
    }

    var query = entities.AsQueryable();

    if (isPublished.HasValue)
    {
      query = query.Where(e => IsPublished(e) == isPublished.Value);
    }

    if (!string.IsNullOrWhiteSpace(category))
    {
      query = query.Where(e => GetCategory(e) == category);
    }

    if (limit.HasValue)
    {
      query = query.Take(limit.Value);
    }

    return query.ToList();
  }

  protected async Task<IEnumerable<GitHubRepoEntity>> GetEntitiesAsync(string? category = null, bool? isPublished = true, int? limit = null)
  {
    var allEntities = await GetAllEntitiesAsync();
    var query = allEntities.AsQueryable();

    if (isPublished.HasValue)
    {
      query = query.Where(e => IsPublished(e) == isPublished.Value);
    }

    if (!string.IsNullOrWhiteSpace(category))
    {
      query = query.Where(e => GetCategory(e) == category);
    }

    if (limit.HasValue)
    {
      query = query.Take(limit.Value);
    }

    return query.ToList();
  }

  protected async Task<GitHubRepoEntity?> UpsertEntityAsync(GitHubRepoEntity entity)
  {
    // Set partition and row keys
    entity.PartitionKey = GetPartitionKey(entity.Slug);
    entity.RowKey = GetRowKey(entity.Slug);

    await _tableStorageService.UpsertEntityAsync(_tableName, entity);
    return entity;
  }

  protected async Task<bool> DeleteEntityBySlugAsync(string slug)
  {
    var entity = await GetEntityBySlugAsync(slug);
    if (entity == null) return false;

    await _tableStorageService.DeleteEntityAsync(_tableName, entity.PartitionKey, entity.RowKey);
    return true;
  }
}