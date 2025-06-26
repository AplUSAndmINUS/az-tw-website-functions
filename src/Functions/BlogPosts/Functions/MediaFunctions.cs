using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SharedStorage.Services.MediaServices;
using System.Net;
using System.Text.Json;
using Utils;

namespace Functions.BlogPosts.Functions;

public class MediaFunctions
{
  private readonly IAppInsightsLogger<MediaFunctions> _appLogger;
  private readonly IMediaService _mediaService;

  public MediaFunctions(
    IAppInsightsLogger<MediaFunctions> logger,
    IMediaService mediaService)
  {
    _appLogger = logger ?? throw new ArgumentNullException(nameof(logger));
    _mediaService = mediaService ?? throw new ArgumentNullException(nameof(mediaService));
  }

  [Function("UploadImage")]
  public async Task<HttpResponseData> UploadImage(
    [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
  {
    _appLogger.LogInformation("UploadImage function triggered");

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

      // Upload the image
      var mediaEntity = await _mediaService.UploadImageAsync(
        req.Body,
        fileName,
        authorId,
        description,
        altText,
        purpose);

      // Return success response
      var response = req.CreateResponse(HttpStatusCode.Created);
      response.Headers.Add("Content-Type", "application/json");

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
    [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
  {
    _appLogger.LogInformation("UploadVideo function triggered");

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

      // Upload the video
      var mediaEntity = await _mediaService.UploadVideoAsync(
        req.Body,
        fileName,
        authorId,
        description,
        purpose);

      // Return success response
      var response = req.CreateResponse(HttpStatusCode.Created);
      response.Headers.Add("Content-Type", "application/json");

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
    _appLogger.LogInformation("GetMedia function triggered");

    try
    {
      // Extract media ID from route
      var mediaId = req.Query["mediaId"] ?? req.FunctionContext.BindingContext.BindingData["mediaId"]?.ToString();

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

  [Function("DeleteMedia")]
  public async Task<HttpResponseData> DeleteMedia(
    [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "media/{mediaId}")] HttpRequestData req)
  {
    _appLogger.LogInformation("DeleteMedia function triggered");

    try
    {
      // Extract media ID from route
      var mediaId = req.Query["mediaId"] ?? req.FunctionContext.BindingContext.BindingData["mediaId"]?.ToString();

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
}
