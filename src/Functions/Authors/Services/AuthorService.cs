using Functions.Authors.Models;
using SharedStorage.Services.BaseServices;
using SharedStorage.Services.Media;
using SharedStorage.Models;
using SharedStorage.Validators;
using Utils;
using System.Text.Json;
using Utils.Constants;

namespace Functions.Authors.Services;

public interface IAuthorService
{
  Task<AuthorDTO> UpsertAuthorAsync(AuthorModel model);
  Task<AuthorDTO?> GetAuthorBySlugAsync(string slug);
  Task<AuthorWithMediaDTO?> GetAuthorWithMediaAsync(string slug);
  // Media operations
  Task<AuthorDTO?> SetProfileImageAsync(string slug, string mediaId);
  Task<AuthorDTO?> SetBackgroundImageAsync(string slug, string mediaId);
  Task<AuthorDTO?> AddMediaReferenceAsync(string slug, string mediaId);
  Task<AuthorDTO?> RemoveMediaReferenceAsync(string slug, string mediaId);
  // TODO: Task<AuthorDTO?> GetAuthorByUsernameAsync(string username);
}

public class AuthorService : IAuthorService
{
  private readonly ITableStorageService _tableStorageService;
  private readonly IMediaService _mediaService;
  private readonly IAppInsightsLogger<AuthorService> _appLogger;
  private readonly string _tableName;
  private readonly string _mediaRefsTableName;

  public AuthorService(
    ITableStorageService tableStorageService,
    IMediaService mediaService,
    IAppInsightsLogger<AuthorService> appLogger)
  {
    // Get table name from environment variable with fallback to "authors"
    var useMock = System.Environment.GetEnvironmentVariable("USE_MOCK_STORAGE")?.ToLowerInvariant() == "true";
    var rawTableName = Environment.GetEnvironmentVariable("AUTHORS_TABLE_NAME") ?? "authors";

    // If we're using mock storage and the raw table name doesn't already have the mock prefix
    _tableName = TableNameValidator.ValidateTableName(useMock && !rawTableName.StartsWith("mock") ? $"mock{rawTableName}" : rawTableName);

    // Create the media references table name
    var mediaRefsRawName = Environment.GetEnvironmentVariable("AUTHORS_MEDIA_REFS_TABLE_NAME") ?? $"{rawTableName}mediarefs";
    _mediaRefsTableName = TableNameValidator.ValidateTableName(useMock && !mediaRefsRawName.StartsWith("mock") ? $"mock{mediaRefsRawName}" : mediaRefsRawName);

    _tableStorageService = tableStorageService ?? throw new ArgumentNullException(nameof(tableStorageService));
    _mediaService = mediaService ?? throw new ArgumentNullException(nameof(mediaService));
    _appLogger = appLogger ?? throw new ArgumentNullException(nameof(appLogger));

    _appLogger.LogInformation($"Instantiated AuthorService using table name: {_tableName}, media refs table: {_mediaRefsTableName}");
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

  public async Task<AuthorWithMediaDTO?> GetAuthorWithMediaAsync(string slug)
  {
    _appLogger.LogInformation("Retrieving author with media by slug: {Slug}", slug);

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

      // Convert entity to model
      var authorModel = new AuthorModel
      {
        AuthorSlug = entity.AuthorSlug,
        FirstName = entity.FirstName,
        LastName = entity.LastName,
        DisplayName = entity.DisplayName,
        Email = entity.Email,
        Username = entity.Username,
        Location = entity.Location,
        Bio = entity.Bio,
        Website = entity.Website,
        TwitterHandle = entity.TwitterHandle,
        InstagramHandle = entity.InstagramHandle,
        LinkedInHandle = entity.LinkedInHandle,
        BlueskyHandle = entity.BlueskyHandle,

        // Media properties
        HasValidProfileImage = entity.HasValidProfileImage,
        ProfileImageFileName = entity.ProfileImageFileName,
        ProfileImageId = entity.ProfileImageId,
        ProfileImageBlobContainer = entity.ProfileImageBlobContainer,
        ProfileImageCdnUrl = entity.ProfileImageCdnUrl,
        ThumbnailCdnUrl = entity.ThumbnailCdnUrl,
        MediaReferencesJson = entity.MediaReferencesJson ?? "[]",

        // Image metadata
        ImageContentType = entity.ImageContentType,
        ImageSizeBytes = entity.ImageSizeBytes,
        ImageWidth = entity.ImageWidth,
        ImageHeight = entity.ImageHeight
      };

      // Get all media items for this author from the MediaService
      var mediaItems = await _mediaService.GetMediaByContentIdAsync(slug, "Author");
      var mediaModels = mediaItems.Select(MediaItemMapper.ToModel).ToList();

      // Create the AuthorWithMediaDTO
      var dto = new AuthorWithMediaDTO(authorModel, mediaModels);

      return dto;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Failed to retrieve author with media by slug: {Slug}", ex, slug);
      throw;
    }
  }

  public async Task<AuthorDTO?> SetProfileImageAsync(string slug, string mediaId)
  {
    _appLogger.LogInformation("Setting profile image {MediaId} for author {Slug}", mediaId, slug);

    try
    {
      // Verify media exists and is an image
      var media = await _mediaService.GetMediaAsync(mediaId);
      if (media == null || media.MediaType != "image")
      {
        _appLogger.LogWarning("Media {MediaId} not found or is not an image", mediaId);
        return null;
      }

      // Get the author entity
      var entity = await _tableStorageService.GetEntityAsync<AuthorEntity>(_tableName, slug, "profile");
      if (entity == null)
      {
        _appLogger.LogWarning("Author {Slug} not found", slug);
        return null;
      }

      // Update the entity with profile image info
      entity.HasValidProfileImage = true;
      entity.ProfileImageFileName = media.Filename;
      entity.ProfileImageCdnUrl = media.Url;
      entity.ThumbnailCdnUrl = media.ThumbnailUrl;

      // Update the entity in table storage
      await _tableStorageService.UpsertEntityAsync(_tableName, entity);

      // Ensure media reference integrity
      await EnsureMediaReferenceIntegrityAsync(slug, mediaId, "profile");

      _appLogger.LogInformation("Profile image set for author {Slug}", slug);

      return AuthorDTOMapper.ToDTO(entity, null);
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Failed to set profile image for author {Slug}: {Error}", ex, slug, ex.Message);
      throw;
    }
  }

  public async Task<AuthorDTO?> SetBackgroundImageAsync(string slug, string mediaId)
  {
    _appLogger.LogInformation("Setting background image {MediaId} for author {Slug}", mediaId, slug);

    try
    {
      // Verify media exists and is an image
      var media = await _mediaService.GetMediaAsync(mediaId);
      if (media == null || media.MediaType != "image")
      {
        _appLogger.LogWarning("Media {MediaId} not found or is not an image", mediaId);
        return null;
      }

      // Get the author entity
      var entity = await _tableStorageService.GetEntityAsync<AuthorEntity>(_tableName, slug, "profile");
      if (entity == null)
      {
        _appLogger.LogWarning("Author {Slug} not found", slug);
        return null;
      }

      // Update the entity with background image reference
      if (entity.MediaReferencesJson == null)
      {
        entity.MediaReferencesJson = JsonSerializer.Serialize(new List<string> { mediaId });
      }
      else
      {
        var references = JsonSerializer.Deserialize<List<string>>(entity.MediaReferencesJson) ?? new List<string>();

        // Check if this media is already referenced
        if (!references.Contains(mediaId))
        {
          references.Add(mediaId);
          entity.MediaReferencesJson = JsonSerializer.Serialize(references);
        }
      }

      // Update the entity in table storage
      await _tableStorageService.UpsertEntityAsync(_tableName, entity);

      // Ensure media reference integrity
      await EnsureMediaReferenceIntegrityAsync(slug, mediaId, "background");

      _appLogger.LogInformation("Background image set for author {Slug}", slug);

      return AuthorDTOMapper.ToDTO(entity, null);
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Failed to set background image for author {Slug}: {Error}", ex, slug, ex.Message);
      throw;
    }
  }

  public async Task<AuthorDTO?> AddMediaReferenceAsync(string slug, string mediaId)
  {
    _appLogger.LogInformation("Adding media reference {MediaId} to author {Slug}", mediaId, slug);

    try
    {
      // Verify media exists
      var media = await _mediaService.GetMediaAsync(mediaId);
      if (media == null)
      {
        _appLogger.LogWarning("Media {MediaId} not found", mediaId);
        return null;
      }

      // Get the author entity
      var entity = await _tableStorageService.GetEntityAsync<AuthorEntity>(_tableName, slug, "profile");
      if (entity == null)
      {
        _appLogger.LogWarning("Author {Slug} not found", slug);
        return null;
      }

      // Update the entity with media reference
      if (entity.MediaReferencesJson == null)
      {
        entity.MediaReferencesJson = JsonSerializer.Serialize(new List<string> { mediaId });
      }
      else
      {
        var references = JsonSerializer.Deserialize<List<string>>(entity.MediaReferencesJson) ?? new List<string>();

        // Check if this media is already referenced
        if (!references.Contains(mediaId))
        {
          references.Add(mediaId);
          entity.MediaReferencesJson = JsonSerializer.Serialize(references);
        }
      }

      // Update the entity in table storage
      await _tableStorageService.UpsertEntityAsync(_tableName, entity);

      // Ensure media reference integrity
      await EnsureMediaReferenceIntegrityAsync(slug, mediaId, null);

      _appLogger.LogInformation("Media reference added for author {Slug}", slug);

      return AuthorDTOMapper.ToDTO(entity, null);
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Failed to add media reference for author {Slug}: {Error}", ex, slug, ex.Message);
      throw;
    }
  }

  public async Task<AuthorDTO?> RemoveMediaReferenceAsync(string slug, string mediaId)
  {
    _appLogger.LogInformation("Removing media reference {MediaId} from author {Slug}", mediaId, slug);

    try
    {
      // Get the author entity
      var entity = await _tableStorageService.GetEntityAsync<AuthorEntity>(_tableName, slug, "profile");
      if (entity == null)
      {
        _appLogger.LogWarning("Author {Slug} not found", slug);
        return null;
      }

      // Check if this is the profile image
      if (entity.HasValidProfileImage && mediaId == entity.ProfileImageId)
      {
        entity.HasValidProfileImage = false;
        entity.ProfileImageFileName = null;
        entity.ProfileImageCdnUrl = null;
        entity.ThumbnailCdnUrl = null;
        entity.ProfileImageId = null;
      }

      // Update the media references list
      if (entity.MediaReferencesJson != null)
      {
        var references = JsonSerializer.Deserialize<List<string>>(entity.MediaReferencesJson) ?? new List<string>();

        // Remove the media reference if it exists
        if (references.Remove(mediaId))
        {
          entity.MediaReferencesJson = JsonSerializer.Serialize(references);
        }
      }

      // Update the entity in table storage
      await _tableStorageService.UpsertEntityAsync(_tableName, entity);

      // Remove media reference metadata
      await RemoveMediaReferenceMetadataAsync(slug, mediaId);

      _appLogger.LogInformation("Media reference removed for author {Slug}", slug);

      return AuthorDTOMapper.ToDTO(entity, null);
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Failed to remove media reference for author {Slug}: {Error}", ex, slug, ex.Message);
      throw;
    }
  }

  /// <summary>
  /// Ensures that metadata linking a media item to an author is properly maintained.
  /// Updates the MediaEntity with ContentId and RelatedContentType.
  /// </summary>
  private async Task EnsureMediaReferenceIntegrityAsync(string authorSlug, string mediaId, string? purpose = null)
  {
    try
    {
      // Skip if either parameter is empty
      if (string.IsNullOrWhiteSpace(authorSlug) || string.IsNullOrWhiteSpace(mediaId))
      {
        return;
      }

      // First verify the media exists
      var media = await _mediaService.GetMediaAsync(mediaId);
      if (media == null)
      {
        _appLogger.LogWarning("Cannot link non-existent media {MediaId} to author {AuthorSlug}", mediaId, authorSlug);
        return;
      }

      // Create a metadata entity to link the author with the media
      var metadataEntity = new Azure.Data.Tables.TableEntity
      {
        PartitionKey = authorSlug,
        RowKey = mediaId,
        ["AuthorSlug"] = authorSlug,
        ["MediaId"] = mediaId,
        ["MediaType"] = media.MediaType,
        ["Purpose"] = purpose ?? "general",
        ["CreatedAt"] = DateTime.UtcNow
      };

      // Add to the metadata table
      await _tableStorageService.UpsertEntityAsync(_mediaRefsTableName, metadataEntity);
      _appLogger.LogInformation("Created metadata link between author {AuthorSlug} and {MediaType} {MediaId}",
        authorSlug, media.MediaType, mediaId);

      // Update the MediaEntity with ContentId and RelatedContentType
      if (media != null && (string.IsNullOrEmpty(media.ContentId) || media.ContentId != authorSlug))
      {
        // Shallow copy the entity to avoid modifying the cached object
        var updatedMedia = new MediaEntity
        {
          Id = media.Id,
          PartitionKey = media.PartitionKey,
          RowKey = media.RowKey,
          ETag = media.ETag,
          Timestamp = media.Timestamp,
          MediaType = media.MediaType,
          Filename = media.Filename,
          Url = media.Url,
          ThumbnailUrl = media.ThumbnailUrl,
          Description = media.Description,
          AltText = media.AltText,
          ContentType = media.ContentType,
          AuthorId = media.AuthorId,
          Width = media.Width,
          Height = media.Height,
          Purpose = purpose ?? media.Purpose,
          UploadedAt = media.UploadedAt,
          ContentId = authorSlug,
          RelatedContentType = "Author"
        };

        // Get table name for media entities
        var useMock = System.Environment.GetEnvironmentVariable("USE_MOCK_STORAGE")?.ToLowerInvariant() == "true";
        var envTableName = System.Environment.GetEnvironmentVariable("MEDIA_TABLE_NAME");

        string mediaTableName;
        if (!string.IsNullOrEmpty(envTableName))
        {
          // If an explicit table name is provided via environment variable, use that
          mediaTableName = useMock ? $"mock{envTableName}" : envTableName;
        }
        else
        {
          // Otherwise use ContentNameResolver for consistent naming
          mediaTableName = Utils.ContentNameResolver.GetTableName(ContentSections.Authors, AssetType.Media, useMock);
        }

        var validatedMediaTableName = SharedStorage.Validators.TableNameValidator.ValidateTableName(mediaTableName);

        // Update the media entity with the author reference
        await _tableStorageService.UpsertEntityAsync(validatedMediaTableName, updatedMedia);
        _appLogger.LogInformation("Updated media {MediaId} with ContentId={AuthorSlug} and RelatedContentType=Author",
          mediaId, authorSlug);
      }
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Failed to ensure media reference integrity for author {AuthorSlug} and media {MediaId}: {Error}",
        ex, authorSlug, mediaId, ex.Message);
    }
  }

  /// <summary>
  /// Removes metadata entries linking a media item to an author when the relationship is removed
  /// </summary>
  private async Task RemoveMediaReferenceMetadataAsync(string authorSlug, string mediaId)
  {
    try
    {
      // Skip if either parameter is empty
      if (string.IsNullOrWhiteSpace(authorSlug) || string.IsNullOrWhiteSpace(mediaId))
      {
        return;
      }

      // Remove from the metadata table
      await _tableStorageService.DeleteEntityAsync(_mediaRefsTableName, authorSlug, mediaId);
      _appLogger.LogInformation("Removed metadata link between author {AuthorSlug} and media {MediaId}",
        authorSlug, mediaId);

      // Try to get the media to reset its ContentId if it points to this author
      try
      {
        var media = await _mediaService.GetMediaAsync(mediaId);
        if (media != null && media.ContentId == authorSlug && media.RelatedContentType == "Author")
        {
          // Shallow copy the entity
          var updatedMedia = new MediaEntity
          {
            Id = media.Id,
            PartitionKey = media.PartitionKey,
            RowKey = media.RowKey,
            ETag = media.ETag,
            Timestamp = media.Timestamp,
            MediaType = media.MediaType,
            Filename = media.Filename,
            Url = media.Url,
            ThumbnailUrl = media.ThumbnailUrl,
            Description = media.Description,
            AltText = media.AltText,
            ContentType = media.ContentType,
            AuthorId = media.AuthorId,
            Width = media.Width,
            Height = media.Height,
            Purpose = media.Purpose,
            UploadedAt = media.UploadedAt,
            ContentId = null,  // Reset the ContentId
            RelatedContentType = null  // Reset the RelatedContentType
          };

          // Get table name for media entities
          var useMock = System.Environment.GetEnvironmentVariable("USE_MOCK_STORAGE")?.ToLowerInvariant() == "true";
          var envTableName = System.Environment.GetEnvironmentVariable("MEDIA_TABLE_NAME");

          string mediaTableName;
          if (!string.IsNullOrEmpty(envTableName))
          {
            // If an explicit table name is provided via environment variable, use that
            mediaTableName = useMock ? $"mock{envTableName}" : envTableName;
          }
          else
          {
            // Otherwise use ContentNameResolver for consistent naming
            mediaTableName = Utils.ContentNameResolver.GetTableName(ContentSections.Authors, AssetType.Media, useMock);
          }

          var validatedMediaTableName = SharedStorage.Validators.TableNameValidator.ValidateTableName(mediaTableName);

          // Update the media entity to remove the author reference
          await _tableStorageService.UpsertEntityAsync(validatedMediaTableName, updatedMedia);
          _appLogger.LogInformation("Reset ContentId and RelatedContentType for media {MediaId}", mediaId);
        }
      }
      catch (Exception)
      {
        // Ignore errors here - it's just a cleanup operation
      }
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Failed to remove media reference metadata for author {AuthorSlug} and media {MediaId}: {Error}",
        ex, authorSlug, mediaId, ex.Message);
    }
  }
}

