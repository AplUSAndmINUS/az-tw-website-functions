using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Functions.Authors.Services;
using System.Net;
using System.Text.Json;
using Utils;
using Utils.Validation;

namespace Functions.Authors.Functions;

public class AuthorMediaFunctions
{
  private readonly IAppInsightsLogger<AuthorMediaFunctions> _appLogger;
  private readonly IAuthorService _authorService;
  private readonly IAPIKeyValidator _apiKeyValidator;

  public AuthorMediaFunctions(
      IAppInsightsLogger<AuthorMediaFunctions> logger,
      IAuthorService authorService,
      IAPIKeyValidator apiKeyValidator)
  {
    _appLogger = logger ?? throw new ArgumentNullException(nameof(logger));
    _authorService = authorService ?? throw new ArgumentNullException(nameof(authorService));
    _apiKeyValidator = apiKeyValidator ?? throw new ArgumentNullException(nameof(apiKeyValidator));
  }

  [Function("SetAuthorProfileImage")]
  public async Task<HttpResponseData> SetAuthorProfileImage(
      [HttpTrigger(AuthorizationLevel.Function, "post", Route = "authors/{slug}/profile-image")] HttpRequestData req,
      string slug)
  {
    _appLogger.LogInformation("SetAuthorProfileImage function triggered for author {Slug}", slug);

    // Validate API key
    var apiValidationResult = await _apiKeyValidator.ValidateApiKeyAsync(req, _appLogger, "SetAuthorProfileImage");
    if (apiValidationResult != null)
    {
      return apiValidationResult;
    }

    try
    {
      // Get media ID from request body
      string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
      if (string.IsNullOrEmpty(requestBody))
      {
        var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
        await badResponse.WriteStringAsync("Request body is required with mediaId");
        return badResponse;
      }

      var data = JsonSerializer.Deserialize<MediaIdRequest>(requestBody);
      if (data == null || string.IsNullOrWhiteSpace(data.MediaId))
      {
        var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
        await badResponse.WriteStringAsync("MediaId is required");
        return badResponse;
      }

      // Set the profile image
      var result = await _authorService.SetProfileImageAsync(slug, data.MediaId);
      if (result == null)
      {
        var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
        await notFoundResponse.WriteStringAsync($"Author with slug '{slug}' not found or media not found");
        return notFoundResponse;
      }

      // Return updated author
      var response = req.CreateResponse(HttpStatusCode.OK);
      response.Headers.Add("Content-Type", "application/json");
      await response.WriteStringAsync(JsonSerializer.Serialize(result, new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
      }));

      _appLogger.LogInformation("Successfully set profile image {MediaId} for author {Slug}", data.MediaId, slug);
      return response;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error setting profile image for author {Slug}: {Error}", ex, slug, ex.Message);
      var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
      await errorResponse.WriteStringAsync("Internal server error");
      return errorResponse;
    }
  }

  [Function("SetAuthorBackgroundImage")]
  public async Task<HttpResponseData> SetAuthorBackgroundImage(
      [HttpTrigger(AuthorizationLevel.Function, "post", Route = "authors/{slug}/background-image")] HttpRequestData req,
      string slug)
  {
    _appLogger.LogInformation("SetAuthorBackgroundImage function triggered for author {Slug}", slug);

    // Validate API key
    var apiValidationResult = await _apiKeyValidator.ValidateApiKeyAsync(req, _appLogger, "SetAuthorBackgroundImage");
    if (apiValidationResult != null)
    {
      return apiValidationResult;
    }

    try
    {
      // Get media ID from request body
      string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
      if (string.IsNullOrEmpty(requestBody))
      {
        var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
        await badResponse.WriteStringAsync("Request body is required with mediaId");
        return badResponse;
      }

      var data = JsonSerializer.Deserialize<MediaIdRequest>(requestBody);
      if (data == null || string.IsNullOrWhiteSpace(data.MediaId))
      {
        var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
        await badResponse.WriteStringAsync("MediaId is required");
        return badResponse;
      }

      // Set the background image
      var result = await _authorService.SetBackgroundImageAsync(slug, data.MediaId);
      if (result == null)
      {
        var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
        await notFoundResponse.WriteStringAsync($"Author with slug '{slug}' not found or media not found");
        return notFoundResponse;
      }

      // Return updated author
      var response = req.CreateResponse(HttpStatusCode.OK);
      response.Headers.Add("Content-Type", "application/json");
      await response.WriteStringAsync(JsonSerializer.Serialize(result, new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
      }));

      _appLogger.LogInformation("Successfully set background image {MediaId} for author {Slug}", data.MediaId, slug);
      return response;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error setting background image for author {Slug}: {Error}", ex, slug, ex.Message);
      var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
      await errorResponse.WriteStringAsync("Internal server error");
      return errorResponse;
    }
  }

  [Function("AddAuthorMediaReference")]
  public async Task<HttpResponseData> AddAuthorMediaReference(
      [HttpTrigger(AuthorizationLevel.Function, "post", Route = "authors/{slug}/media-references")] HttpRequestData req,
      string slug)
  {
    _appLogger.LogInformation("AddAuthorMediaReference function triggered for author {Slug}", slug);

    // Validate API key
    var apiValidationResult = await _apiKeyValidator.ValidateApiKeyAsync(req, _appLogger, "AddAuthorMediaReference");
    if (apiValidationResult != null)
    {
      return apiValidationResult;
    }

    try
    {
      // Get media ID from request body
      string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
      if (string.IsNullOrEmpty(requestBody))
      {
        var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
        await badResponse.WriteStringAsync("Request body is required with mediaId");
        return badResponse;
      }

      var data = JsonSerializer.Deserialize<MediaIdRequest>(requestBody);
      if (data == null || string.IsNullOrWhiteSpace(data.MediaId))
      {
        var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
        await badResponse.WriteStringAsync("MediaId is required");
        return badResponse;
      }

      // Add media reference
      var result = await _authorService.AddMediaReferenceAsync(slug, data.MediaId);
      if (result == null)
      {
        var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
        await notFoundResponse.WriteStringAsync($"Author with slug '{slug}' not found or media not found");
        return notFoundResponse;
      }

      // Return updated author
      var response = req.CreateResponse(HttpStatusCode.OK);
      response.Headers.Add("Content-Type", "application/json");
      await response.WriteStringAsync(JsonSerializer.Serialize(result, new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
      }));

      _appLogger.LogInformation("Successfully added media reference {MediaId} to author {Slug}", data.MediaId, slug);
      return response;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error adding media reference to author {Slug}: {Error}", ex, slug, ex.Message);
      var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
      await errorResponse.WriteStringAsync("Internal server error");
      return errorResponse;
    }
  }

  [Function("RemoveAuthorMediaReference")]
  public async Task<HttpResponseData> RemoveAuthorMediaReference(
      [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "authors/{slug}/media-references/{mediaId}")] HttpRequestData req,
      string slug,
      string mediaId)
  {
    _appLogger.LogInformation("RemoveAuthorMediaReference function triggered for author {Slug} and media {MediaId}", slug, mediaId);

    // Validate API key
    var apiValidationResult = await _apiKeyValidator.ValidateApiKeyAsync(req, _appLogger, "RemoveAuthorMediaReference");
    if (apiValidationResult != null)
    {
      return apiValidationResult;
    }

    try
    {
      if (string.IsNullOrWhiteSpace(mediaId))
      {
        var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
        await badResponse.WriteStringAsync("MediaId is required");
        return badResponse;
      }

      // Remove media reference
      var result = await _authorService.RemoveMediaReferenceAsync(slug, mediaId);
      if (result == null)
      {
        var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
        await notFoundResponse.WriteStringAsync($"Author with slug '{slug}' not found");
        return notFoundResponse;
      }

      // Return updated author
      var response = req.CreateResponse(HttpStatusCode.OK);
      response.Headers.Add("Content-Type", "application/json");
      await response.WriteStringAsync(JsonSerializer.Serialize(result, new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
      }));

      _appLogger.LogInformation("Successfully removed media reference {MediaId} from author {Slug}", mediaId, slug);
      return response;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error removing media reference from author {Slug}: {Error}", ex, slug, ex.Message);
      var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
      await errorResponse.WriteStringAsync("Internal server error");
      return errorResponse;
    }
  }

  [Function("GetAuthorMedia")]
  public async Task<HttpResponseData> GetAuthorMedia(
      [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "authors/{slug}/media")] HttpRequestData req,
      string slug)
  {
    _appLogger.LogInformation("GetAuthorMedia function triggered for author {Slug}", slug);

    try
    {
      // Get author media
      var result = await _authorService.GetAuthorWithMediaAsync(slug);
      if (result == null)
      {
        var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
        await notFoundResponse.WriteStringAsync($"Author with slug '{slug}' not found");
        return notFoundResponse;
      }

      // Return author with media
      var response = req.CreateResponse(HttpStatusCode.OK);
      response.Headers.Add("Content-Type", "application/json");
      await response.WriteStringAsync(JsonSerializer.Serialize(result, new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
      }));

      _appLogger.LogInformation("Successfully retrieved media for author {Slug}", slug);
      return response;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error retrieving media for author {Slug}: {Error}", ex, slug, ex.Message);
      var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
      await errorResponse.WriteStringAsync("Internal server error");
      return errorResponse;
    }
  }

  // Data class for the request
  private class MediaIdRequest
  {
    public string? MediaId { get; set; }
  }
}
