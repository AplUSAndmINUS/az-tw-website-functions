using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SharedStorage.Services.Media;
using System.Net;
using System.Text.Json;
using Utils;
using Utils.Validation;

namespace Functions.Shared;

/// <summary>
/// Shared media functions for handling global media operations across all content types.
/// These functions provide centralized upload, retrieval, and deletion of media assets
/// that can be used by blog posts, portfolio pieces, authors, and future content types.
/// </summary>
public class SharedMediaFunctions
{
  private readonly IAppInsightsLogger<SharedMediaFunctions> _appLogger;
  private readonly IMediaService _mediaService;
  private readonly IAPIKeyValidator _apiKeyValidator;

  public SharedMediaFunctions(
    IAppInsightsLogger<SharedMediaFunctions> logger,
    IMediaService mediaService,
    IAPIKeyValidator apiKeyValidator)
  {
    _appLogger = logger ?? throw new ArgumentNullException(nameof(logger));
    _mediaService = mediaService ?? throw new ArgumentNullException(nameof(mediaService));
    _apiKeyValidator = apiKeyValidator ?? throw new ArgumentNullException(nameof(apiKeyValidator));
    _appLogger.LogInformation("SharedMediaFunctions initialized");
  }

  [Function("UploadImage")]
  public async Task<HttpResponseData> UploadImage(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = "media/images")] HttpRequestData req)
  {
    _appLogger.LogInformation("SharedMediaFunctions.UploadImage function triggered");

    // Validate API key using helper method
    var apiValidationResult = await _apiKeyValidator.ValidateApiKeyAsync(req, _appLogger, "UploadImage");
    if (apiValidationResult != null)
    {
      return apiValidationResult;
    }

    try
    {
      // Check if request has a body
      if (req.Body == null || req.Body.Length == 0)
      {
        var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
        await badResponse.WriteStringAsync("Image file is required");
        return badResponse;
      }

      // Get parameters from query string
      var fileName = req.Query["fileName"] ?? "uploaded-image.jpg";
      var authorId = req.Query["authorId"];
      var description = req.Query["description"];
      var altText = req.Query["altText"];
      var purpose = req.Query["purpose"] ?? "coverImage";
      var contentId = req.Query["contentId"];
      var relatedContentType = req.Query["relatedContentType"];

      // Upload the image
      var mediaEntity = await _mediaService.UploadImageAsync(
        req.Body,
        fileName,
        authorId,
        description,
        altText,
        purpose,
        contentId,
        relatedContentType);

      // Return success response
      var response = req.CreateResponse(HttpStatusCode.Created);
      response.Headers.Add("Content-Type", "application/json");
      response.Headers.Add("Location", $"/media/{mediaEntity.Id}");

      var responseBody = JsonSerializer.Serialize(mediaEntity, new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
      });

      await response.WriteStringAsync(responseBody);

      _appLogger.LogInformation("Successfully uploaded image with ID: {MediaId}", mediaEntity.Id);
      return response;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error uploading image", ex);
      var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
      await errorResponse.WriteStringAsync("Failed to upload image");
      return errorResponse;
    }
  }

  [Function("UploadVideo")]
  public async Task<HttpResponseData> UploadVideo(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = "media/videos")] HttpRequestData req)
  {
    _appLogger.LogInformation("SharedMediaFunctions.UploadVideo function triggered");

    // Validate API key using helper method
    var apiValidationResult = await _apiKeyValidator.ValidateApiKeyAsync(req, _appLogger, "UploadVideo");
    if (apiValidationResult != null)
    {
      return apiValidationResult;
    }

    try
    {
      // Check if request has a body
      if (req.Body == null || req.Body.Length == 0)
      {
        var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
        await badResponse.WriteStringAsync("Video file is required");
        return badResponse;
      }

      // Get parameters from query string
      var fileName = req.Query["fileName"] ?? "uploaded-video.mp4";
      var authorId = req.Query["authorId"];
      var description = req.Query["description"];
      var purpose = req.Query["purpose"] ?? "introVideo";
      var contentId = req.Query["contentId"];
      var relatedContentType = req.Query["relatedContentType"];

      // Upload the video
      var mediaEntity = await _mediaService.UploadVideoAsync(
        req.Body,
        fileName,
        authorId,
        description,
        purpose,
        contentId,
        relatedContentType);

      // Return success response
      var response = req.CreateResponse(HttpStatusCode.Created);
      response.Headers.Add("Content-Type", "application/json");
      response.Headers.Add("Location", $"/media/{mediaEntity.Id}");

      var responseBody = JsonSerializer.Serialize(mediaEntity, new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
      });

      await response.WriteStringAsync(responseBody);

      _appLogger.LogInformation("Successfully uploaded video with ID: {MediaId}", mediaEntity.Id);
      return response;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error uploading video", ex);
      var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
      await errorResponse.WriteStringAsync("Failed to upload video");
      return errorResponse;
    }
  }

  [Function("GetMedia")]
  public async Task<HttpResponseData> GetMedia(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "media/{mediaId}")] HttpRequestData req)
  {
    _appLogger.LogInformation("SharedMediaFunctions.GetMedia function triggered");

    // Validate API key using helper method
    var apiValidationResult = await _apiKeyValidator.ValidateApiKeyAsync(req, _appLogger, "GetMedia");
    if (apiValidationResult != null)
    {
      return apiValidationResult;
    }

    try
    {
      // Extract media ID from route
      var mediaId = req.FunctionContext.BindingContext.BindingData["mediaId"]?.ToString();

      if (string.IsNullOrWhiteSpace(mediaId))
      {
        var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
        await badResponse.WriteStringAsync("Media ID parameter is required");
        return badResponse;
      }

      // Get the media
      var media = await _mediaService.GetMediaAsync(mediaId);

      if (media == null)
      {
        var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
        await notFoundResponse.WriteStringAsync("Media not found");
        return notFoundResponse;
      }

      // Return success response
      var response = req.CreateResponse(HttpStatusCode.OK);
      response.Headers.Add("Content-Type", "application/json");

      var responseBody = JsonSerializer.Serialize(media, new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
      });

      await response.WriteStringAsync(responseBody);

      _appLogger.LogInformation("Successfully retrieved media with ID: {MediaId}", mediaId);
      return response;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error retrieving media", ex);
      var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
      await errorResponse.WriteStringAsync("Internal server error");
      return errorResponse;
    }
  }

  [Function("GetAllMedia")]
  public async Task<HttpResponseData> GetAllMedia(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "media")] HttpRequestData req)
  {
    _appLogger.LogInformation("SharedMediaFunctions.GetAllMedia function triggered");

    // Validate API key using helper method
    var apiValidationResult = await _apiKeyValidator.ValidateApiKeyAsync(req, _appLogger, "GetAllMedia");
    if (apiValidationResult != null)
    {
      return apiValidationResult;
    }

    try
    {
      // Get optional query parameters
      var mediaType = req.Query["mediaType"];
      var authorId = req.Query["authorId"];
      var limitStr = req.Query["limit"];
      int? limit = null;

      if (!string.IsNullOrWhiteSpace(limitStr) && int.TryParse(limitStr, out var parsedLimit))
      {
        limit = parsedLimit;
      }

      IEnumerable<SharedStorage.Models.MediaEntity> mediaItems;

      // Get media based on provided filters
      if (!string.IsNullOrWhiteSpace(authorId))
      {
        mediaItems = await _mediaService.GetMediaByAuthorAsync(authorId, mediaType, limit);
      }
      else if (!string.IsNullOrWhiteSpace(mediaType))
      {
        mediaItems = await _mediaService.GetMediaByTypeAsync(mediaType, limit);
      }
      else
      {
        // For now, return by type to avoid getting all media (performance)
        // In a real implementation, you might want to implement pagination
        mediaItems = await _mediaService.GetMediaByTypeAsync("image", limit ?? 50);
        var videos = await _mediaService.GetMediaByTypeAsync("video", limit ?? 50);
        mediaItems = mediaItems.Concat(videos);
      }

      // Return success response
      var response = req.CreateResponse(HttpStatusCode.OK);
      response.Headers.Add("Content-Type", "application/json");

      var responseBody = JsonSerializer.Serialize(mediaItems, new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
      });

      await response.WriteStringAsync(responseBody);

      _appLogger.LogInformation("Successfully retrieved {Count} media items", mediaItems.Count());
      return response;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error retrieving media list", ex);
      var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
      await errorResponse.WriteStringAsync("Internal server error");
      return errorResponse;
    }
  }

  [Function("GetMediaByContentId")]
  public async Task<HttpResponseData> GetMediaByContentId(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "media/content/{contentId}")] HttpRequestData req)
  {
    _appLogger.LogInformation("SharedMediaFunctions.GetMediaByContentId function triggered");

    // Validate API key using helper method
    var apiValidationResult = await _apiKeyValidator.ValidateApiKeyAsync(req, _appLogger, "GetMediaByContentId");
    if (apiValidationResult != null)
    {
      return apiValidationResult;
    }

    try
    {
      // Extract content ID from route
      var contentId = req.FunctionContext.BindingContext.BindingData["contentId"]?.ToString();

      if (string.IsNullOrWhiteSpace(contentId))
      {
        var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
        await badResponse.WriteStringAsync("Content ID parameter is required");
        return badResponse;
      }

      // Get optional relatedContentType parameter
      var relatedContentType = req.Query["relatedContentType"];

      // Get media items associated with the content
      var mediaItems = await _mediaService.GetMediaByContentIdAsync(contentId, relatedContentType);

      // Return success response
      var response = req.CreateResponse(HttpStatusCode.OK);
      response.Headers.Add("Content-Type", "application/json");

      var responseBody = JsonSerializer.Serialize(mediaItems, new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
      });

      await response.WriteStringAsync(responseBody);

      _appLogger.LogInformation("Successfully retrieved {Count} media items for content ID: {ContentId}",
        mediaItems.Count(), contentId);
      return response;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error retrieving media by content ID", ex);
      var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
      await errorResponse.WriteStringAsync("Internal server error");
      return errorResponse;
    }
  }

  [Function("DeleteMedia")]
  public async Task<HttpResponseData> DeleteMedia(
    [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "media/{mediaId}")] HttpRequestData req)
  {
    _appLogger.LogInformation("SharedMediaFunctions.DeleteMedia function triggered");

    // Validate API key using helper method
    var apiValidationResult = await _apiKeyValidator.ValidateApiKeyAsync(req, _appLogger, "DeleteMedia");
    if (apiValidationResult != null)
    {
      return apiValidationResult;
    }

    try
    {
      // Extract media ID from route
      var mediaId = req.FunctionContext.BindingContext.BindingData["mediaId"]?.ToString();

      if (string.IsNullOrWhiteSpace(mediaId))
      {
        var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
        await badResponse.WriteStringAsync("Media ID parameter is required");
        return badResponse;
      }

      // Delete the media
      var success = await _mediaService.DeleteMediaAsync(mediaId);

      if (!success)
      {
        var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
        await notFoundResponse.WriteStringAsync("Media not found or could not be deleted");
        return notFoundResponse;
      }

      // Return success response
      var response = req.CreateResponse(HttpStatusCode.NoContent);
      _appLogger.LogInformation("Successfully deleted media with ID: {MediaId}", mediaId);
      return response;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error deleting media", ex);
      var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
      await errorResponse.WriteStringAsync("Internal server error");
      return errorResponse;
    }
  }

  [Function("Ping")]
  public async Task<HttpResponseData> Ping(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "media/ping")] HttpRequestData req)
  {
    _appLogger.LogInformation("SharedMediaFunctions.Ping function triggered");

    var response = req.CreateResponse(HttpStatusCode.OK);
    response.Headers.Add("Content-Type", "application/json");

    var pingResponse = new
    {
      status = "healthy",
      timestamp = DateTime.UtcNow,
      service = "SharedMediaFunctions",
      version = "1.0.0"
    };

    await response.WriteStringAsync(JsonSerializer.Serialize(pingResponse, new JsonSerializerOptions
    {
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    }));

    return response;
  }
}
