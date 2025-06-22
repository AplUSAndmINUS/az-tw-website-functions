using Functions.Authors.Models;
using SharedStorage.Services;
using SharedStorage.Validators;
using Utils;

namespace Functions.Authors.Services;

public interface IAuthorService
{
  Task<AuthorDTO> CreateAuthorAsync(AuthorModel model);
}

public class AuthorService : IAuthorService
{
  private readonly ITableStorageService _tableStorageService;
  private readonly IAppInsightsLogger<AuthorService> _appLogger;
  private readonly string _tableName;

  public AuthorService(ITableStorageService tableStorageService, IAppInsightsLogger<AuthorService> appLogger, string tableName)
  {
    // Validate the table name
    var rawTableName = Environment.GetEnvironmentVariable("AUTHORS_TABLE_NAME") ?? tableName;

    // Then instantiate the AuthorService with the other services and table name
    _tableName = TableNameValidator.ValidateTableName(rawTableName);
    _tableStorageService = tableStorageService;
    _appLogger = appLogger;
    _appLogger.LogInformation($"Instantiated table using table name: {_tableName}");
  }

  public async Task<AuthorDTO> CreateAuthorAsync(AuthorModel model)
  {
    _appLogger.LogInformation("Creating author entity.");

    try
    {
      var entity = AuthorEntity.FromModel(model, model.AuthorSlug ?? model.Username, "profile");
      await _tableStorageService.UpsertEntityAsync(_tableName, entity);

      return new AuthorDTO
      {
        AuthorSlug = entity.AuthorSlug,
        DisplayName = entity.DisplayName,
        FirstName = entity.FirstName,
        LastName = entity.LastName,
        Username = entity.Username,

        // Email is not included in the DTO for privacy reasons
        // Email = entity.Email,
        Location = entity.Location,
        Bio = entity.Bio,
        Website = entity.Website,
        TwitterHandle = entity.TwitterHandle,
        InstagramHandle = entity.InstagramHandle,
        LinkedInHandle = entity.LinkedInHandle,
        BlueskyHandle = entity.BlueskyHandle,

        // Profile image properties
        HasValidProfileImage = entity.HasValidProfileImage,
        ProfileImageFileName = entity.ProfileImageFileName,
        ProfileImageCdnUrl = entity.ProfileImageCdnUrl,
        ThumbnailCdnUrl = entity.ThumbnailCdnUrl
      };
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Failed to create author entity.", ex);
      throw;
    }
  }
}

