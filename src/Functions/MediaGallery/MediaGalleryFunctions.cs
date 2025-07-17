using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SharedStorage.Models;
using SharedStorage.Services.Media;
using System.Net;
using System.Text.Json;
using Utils;
using Utils.Validation;

namespace Functions.MediaGallery;

/// <summary>
/// Azure Functions for Media Gallery operations
/// Provides HTTP endpoints for retrieving media from various platforms
/// </summary>
public class MediaGalleryFunctions
{
  private readonly IAppInsightsLogger<MediaGalleryFunctions> _appLogger;
  private readonly IMediaGalleryService _mediaGalleryService;
  private readonly IAPIKeyValidator _apiKeyValidator;

  public MediaGalleryFunctions(
    IAppInsightsLogger<MediaGalleryFunctions> logger,
    IMediaGalleryService mediaGalleryService,
    IAPIKeyValidator apiKeyValidator)
  {
    _appLogger = logger ?? throw new ArgumentNullException(nameof(logger));
    _mediaGalleryService = mediaGalleryService ?? throw new ArgumentNullException(nameof(mediaGalleryService));
    _apiKeyValidator = apiKeyValidator ?? throw new ArgumentNullException(nameof(apiKeyValidator));
    _appLogger.LogInformation("MediaGalleryFunctions initialized");
  }

  /// <summary>
  /// Gets all media from all platforms and blob storage
  /// </summary>
  [Function("GetAllMedia")]
  public async Task<HttpResponseData> GetAllMedia(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "media-gallery")] HttpRequestData req)
  {
    _appLogger.LogInformation("MediaGalleryFunctions.GetAllMedia function triggered");

    // Validate API key
    var apiValidationResult = await _apiKeyValidator.ValidateApiKeyAsync(req, _appLogger, "GetAllMedia");
    if (apiValidationResult != null)
    {
      return apiValidationResult;
    }

    try
    {
      // Get query parameters
      var authorId = req.Query["authorId"];
      var limitStr = req.Query["limit"];
      int? limit = null;

      if (!string.IsNullOrWhiteSpace(limitStr) && int.TryParse(limitStr, out var parsedLimit))
      {
        limit = Math.Min(parsedLimit, 500); // Cap at 500 for performance
      }

      // Get all media
      var mediaEntities = await _mediaGalleryService.GetAllMediaAsync(authorId, limit);
      var mediaGalleryDTOs = mediaEntities.ToGalleryDTOs().OrderByDescending(m => m.CreatedAt);

      // Create response
      var supportedPlatforms = await _mediaGalleryService.GetSupportedPlatformsAsync();
      var response = new MediaGalleryResponse
      {
        Media = mediaGalleryDTOs,
        TotalCount = mediaGalleryDTOs.Count(),
        PageSize = limit ?? 100,
        LastSyncTime = DateTime.UtcNow,
        AvailablePlatforms = supportedPlatforms.ToArray(),
        AvailableMediaTypes = new[] { "image", "video", "audio" }
      };

      // Return success response
      var httpResponse = req.CreateResponse(HttpStatusCode.OK);
      httpResponse.Headers.Add("Content-Type", "application/json");

      var responseBody = JsonSerializer.Serialize(response, new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
      });

      await httpResponse.WriteStringAsync(responseBody);

      _appLogger.LogInformation("Successfully retrieved {Count} media items", response.TotalCount);
      return httpResponse;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error retrieving all media", ex);
      var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
      await errorResponse.WriteStringAsync("Internal server error");
      return errorResponse;
    }
  }

  /// <summary>
  /// Gets media filtered by medium type (image, video, audio)
  /// </summary>
  [Function("GetMediaByMedium")]
  public async Task<HttpResponseData> GetMediaByMedium(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "media-gallery/medium/{mediaType}")] HttpRequestData req)
  {
    _appLogger.LogInformation("MediaGalleryFunctions.GetMediaByMedium function triggered");

    // Validate API key
    var apiValidationResult = await _apiKeyValidator.ValidateApiKeyAsync(req, _appLogger, "GetMediaByMedium");
    if (apiValidationResult != null)
    {
      return apiValidationResult;
    }

    try
    {
      // Extract media type from route
      var mediaType = req.FunctionContext.BindingContext.BindingData["mediaType"]?.ToString();

      if (string.IsNullOrWhiteSpace(mediaType))
      {
        var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
        await badResponse.WriteStringAsync("Media type parameter is required");
        return badResponse;
      }

      // Validate media type
      var validMediaTypes = new[] { "image", "video", "audio" };
      if (!validMediaTypes.Contains(mediaType.ToLowerInvariant()))
      {
        var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
        await badResponse.WriteStringAsync($"Invalid media type. Valid types: {string.Join(", ", validMediaTypes)}");
        return badResponse;
      }

      // Get query parameters
      var authorId = req.Query["authorId"];
      var limitStr = req.Query["limit"];
      int? limit = null;

      if (!string.IsNullOrWhiteSpace(limitStr) && int.TryParse(limitStr, out var parsedLimit))
      {
        limit = Math.Min(parsedLimit, 500);
      }

      // Get media by medium
      var mediaEntities = await _mediaGalleryService.GetMediaByMediumAsync(mediaType, authorId, limit);
      var mediaGalleryDTOs = mediaEntities.ToGalleryDTOs().OrderByDescending(m => m.CreatedAt);

      // Create response
      var supportedPlatforms = await _mediaGalleryService.GetSupportedPlatformsAsync();
      var response = new MediaGalleryResponse
      {
        Media = mediaGalleryDTOs,
        TotalCount = mediaGalleryDTOs.Count(),
        PageSize = limit ?? 100,
        LastSyncTime = DateTime.UtcNow,
        AvailablePlatforms = supportedPlatforms.ToArray(),
        AvailableMediaTypes = new[] { mediaType }
      };

      // Return success response
      var httpResponse = req.CreateResponse(HttpStatusCode.OK);
      httpResponse.Headers.Add("Content-Type", "application/json");

      var responseBody = JsonSerializer.Serialize(response, new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
      });

      await httpResponse.WriteStringAsync(responseBody);

      _appLogger.LogInformation("Successfully retrieved {Count} media items for medium {MediaType}", 
        response.TotalCount, mediaType);
      return httpResponse;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error retrieving media by medium", ex);
      var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
      await errorResponse.WriteStringAsync("Internal server error");
      return errorResponse;
    }
  }

  /// <summary>
  /// Gets media filtered by platform (tiktok, instagram, youtube, etc.)
  /// </summary>
  [Function("GetMediaByPlatform")]
  public async Task<HttpResponseData> GetMediaByPlatform(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "media-gallery/platform/{platform}")] HttpRequestData req)
  {
    _appLogger.LogInformation("MediaGalleryFunctions.GetMediaByPlatform function triggered");

    // Validate API key
    var apiValidationResult = await _apiKeyValidator.ValidateApiKeyAsync(req, _appLogger, "GetMediaByPlatform");
    if (apiValidationResult != null)
    {
      return apiValidationResult;
    }

    try
    {
      // Extract platform from route
      var platform = req.FunctionContext.BindingContext.BindingData["platform"]?.ToString();

      if (string.IsNullOrWhiteSpace(platform))
      {
        var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
        await badResponse.WriteStringAsync("Platform parameter is required");
        return badResponse;
      }

      // Validate platform
      var supportedPlatforms = await _mediaGalleryService.GetSupportedPlatformsAsync();
      if (!supportedPlatforms.Contains(platform.ToLowerInvariant()))
      {
        var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
        await badResponse.WriteStringAsync($"Invalid platform. Supported platforms: {string.Join(", ", supportedPlatforms)}");
        return badResponse;
      }

      // Get query parameters
      var authorId = req.Query["authorId"];
      var limitStr = req.Query["limit"];
      int? limit = null;

      if (!string.IsNullOrWhiteSpace(limitStr) && int.TryParse(limitStr, out var parsedLimit))
      {
        limit = Math.Min(parsedLimit, 500);
      }

      // Get media by platform
      var mediaEntities = await _mediaGalleryService.GetMediaByPlatformAsync(platform, authorId, limit);
      var mediaGalleryDTOs = mediaEntities.ToGalleryDTOs().OrderByDescending(m => m.CreatedAt);

      // Create response
      var response = new MediaGalleryResponse
      {
        Media = mediaGalleryDTOs,
        TotalCount = mediaGalleryDTOs.Count(),
        PageSize = limit ?? 100,
        LastSyncTime = DateTime.UtcNow,
        AvailablePlatforms = new[] { platform },
        AvailableMediaTypes = new[] { "image", "video", "audio" }
      };

      // Return success response
      var httpResponse = req.CreateResponse(HttpStatusCode.OK);
      httpResponse.Headers.Add("Content-Type", "application/json");

      var responseBody = JsonSerializer.Serialize(response, new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
      });

      await httpResponse.WriteStringAsync(responseBody);

      _appLogger.LogInformation("Successfully retrieved {Count} media items for platform {Platform}", 
        response.TotalCount, platform);
      return httpResponse;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error retrieving media by platform", ex);
      var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
      await errorResponse.WriteStringAsync("Internal server error");
      return errorResponse;
    }
  }
}