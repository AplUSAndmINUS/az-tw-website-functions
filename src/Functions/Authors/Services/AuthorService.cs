using Functions.Authors.Models;
using SharedStorage.Services.BaseServices;
using SharedStorage.Validators;
using Utils;

namespace Functions.Authors.Services;

public interface IAuthorService
{
  Task<AuthorDTO> UpsertAuthorAsync(AuthorModel model);
  Task<AuthorDTO?> GetAuthorBySlugAsync(string slug);
  // TODO: Task<AuthorDTO?> GetAuthorByUsernameAsync(string username);
}

public class AuthorService : IAuthorService
{
  private readonly ITableStorageService _tableStorageService;
  private readonly IAppInsightsLogger<AuthorService> _appLogger;
  private readonly string _tableName;

  public AuthorService(ITableStorageService tableStorageService, IAppInsightsLogger<AuthorService> appLogger)
  {
    // Get table name from environment variable with fallback to "authors"
    var rawTableName = Environment.GetEnvironmentVariable("AUTHORS_TABLE_NAME") ?? "authors";

    _tableName = TableNameValidator.ValidateTableName(rawTableName);
    _tableStorageService = tableStorageService;
    _appLogger = appLogger;
    _appLogger.LogInformation($"Instantiated table in AuthorService using table name: {_tableName} -- will be updated if mock storage later.");
  }

  public async Task<AuthorDTO> UpsertAuthorAsync(AuthorModel model)
  {
    _appLogger.LogInformation("Upserting or creating an author entity.");

    try
    {
      // Validate required fields
      if (string.IsNullOrWhiteSpace(model.AuthorSlug))
        throw new ArgumentException("AuthorSlug is required", nameof(model.AuthorSlug));
      if (string.IsNullOrWhiteSpace(model.FirstName))
        throw new ArgumentException("FirstName is required", nameof(model.FirstName));
      if (string.IsNullOrWhiteSpace(model.LastName))
        throw new ArgumentException("LastName is required", nameof(model.LastName));
      if (string.IsNullOrWhiteSpace(model.Email))
        throw new ArgumentException("Email is required", nameof(model.Email));
      if (string.IsNullOrWhiteSpace(model.Username))
        throw new ArgumentException("Username is required", nameof(model.Username));
      if (string.IsNullOrWhiteSpace(model.DisplayName))
        throw new ArgumentException("DisplayName is required", nameof(model.DisplayName));
            
      // Calculate the slug once and use it consistently
      var authorSlug = model.AuthorSlug ?? model.Username;

      // Pass the slug as the partitionKey - use the proper mapper for consistency
      var entity = AuthorModelToEntityMapper.Map(model, authorSlug, "profile");
      await _tableStorageService.UpsertEntityAsync(_tableName, entity);

      // Use the consistent mapper to create DTO
      return AuthorDTOMapper.ToDTO(entity, null);
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Failed to upsert the author entity.", ex);
      throw;
    }
  }

  public async Task<AuthorDTO?> GetAuthorBySlugAsync(string slug)
  {
    _appLogger.LogInformation("Retrieving author by slug: {Slug}", slug);

    if (string.IsNullOrWhiteSpace(slug))
    {
      _appLogger.LogWarning("Provided slug is null or empty.");
      return null;
    }

    try
    {
      // Retrieve the author entity using the slug as the partition key
      var entity = await _tableStorageService.GetEntityAsync<AuthorEntity>(_tableName, slug, "profile");

      if (entity == null)
      {
        _appLogger.LogWarning("No author found for slug: {Slug}", slug);
        return null;
      }

      var dto = AuthorDTOMapper.ToDTO(entity, null);

      return dto;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Failed to retrieve author by slug: {Slug}", ex, slug);
      throw;
    }
  }
}

