using Functions.Authors.Models;
using SharedStorage.Services;
using SharedStorage.Validators;
using Utils;

namespace Functions.Authors.Services;

public interface IAuthorService
{
  Task<AuthorDTO> UpsertAuthorAsync(AuthorModel model);
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
    _appLogger.LogInformation($"Instantiated table using table name: {_tableName}");
  }

  public async Task<AuthorDTO> UpsertAuthorAsync(AuthorModel model)
  {
    _appLogger.LogInformation("Upserting or creating an author entity.");

    try
    {
      // Calculate the slug once and use it consistently
      var authorSlug = model.AuthorSlug ?? model.Username;

      // Pass the slug as the partitionKey
      var entity = AuthorEntity.FromModel(model, authorSlug, "profile");
      await _tableStorageService.UpsertEntityAsync(_tableName, entity);

      return new AuthorDTO
      {
        AuthorSlug = entity.AuthorSlug, // This now correctly uses the computed property
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
      _appLogger.LogError("Failed to upsert the author entity.", ex);
      throw;
    }
  }
}

