using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Functions.BlogPosts.Services;
using System.Net;
using System.Text.Json;
using Utils;
using Utils.Validation;

namespace Functions.BlogPosts.Functions;

public class BlogPostMediaFunctions
{
  private readonly IAppInsightsLogger<BlogPostMediaFunctions> _appLogger;
  private readonly IBlogPostService _blogPostService;
  private readonly IAPIKeyValidator _apiKeyValidator;

  public BlogPostMediaFunctions(
      IAppInsightsLogger<BlogPostMediaFunctions> logger,
      IBlogPostService blogPostService,
      IAPIKeyValidator apiKeyValidator)
  {
    _appLogger = logger ?? throw new ArgumentNullException(nameof(logger));
    _blogPostService = blogPostService ?? throw new ArgumentNullException(nameof(blogPostService));
    _apiKeyValidator = apiKeyValidator ?? throw new ArgumentNullException(nameof(apiKeyValidator));
  }

  [Function("SetFeaturedImage")]
  public async Task<HttpResponseData> SetFeaturedImage(
      [HttpTrigger(AuthorizationLevel.Function, "post", Route = "posts/{slug}/featured-image")] HttpRequestData req,
      string slug)
  {
    _appLogger.LogInformation("SetFeaturedImage function triggered for blog {Slug}", slug);

    // Validate API key
    var apiValidationResult = await _apiKeyValidator.ValidateApiKeyAsync(req, _appLogger, "SetFeaturedImage");
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

      // Set the featured image
      var result = await _blogPostService.SetFeaturedImageAsync(slug, data.MediaId);
      if (result == null)
      {
        var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
        await notFoundResponse.WriteStringAsync($"Blog post with slug '{slug}' not found or media not found");
        return notFoundResponse;
      }

      // Return updated blog post
      var response = req.CreateResponse(HttpStatusCode.OK);
      response.Headers.Add("Content-Type", "application/json");
      await response.WriteStringAsync(JsonSerializer.Serialize(result, new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
      }));

      _appLogger.LogInformation("Successfully set featured image {MediaId} for blog {Slug}", data.MediaId, slug);
      return response;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error setting featured image for blog {Slug}: {Error}", ex, slug, ex.Message);
      var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
      await errorResponse.WriteStringAsync("Internal server error");
      return errorResponse;
    }
  }

  [Function("SetFeaturedVideo")]
  public async Task<HttpResponseData> SetFeaturedVideo(
      [HttpTrigger(AuthorizationLevel.Function, "post", Route = "posts/{slug}/featured-video")] HttpRequestData req,
      string slug)
  {
    _appLogger.LogInformation("SetFeaturedVideo function triggered for blog {Slug}", slug);

    // Validate API key
    var apiValidationResult = await _apiKeyValidator.ValidateApiKeyAsync(req, _appLogger, "SetFeaturedVideo");
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

      // Set the featured video
      var result = await _blogPostService.SetFeaturedVideoAsync(slug, data.MediaId);
      if (result == null)
      {
        var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
        await notFoundResponse.WriteStringAsync($"Blog post with slug '{slug}' not found or media not found or not a video");
        return notFoundResponse;
      }

      // Return updated blog post
      var response = req.CreateResponse(HttpStatusCode.OK);
      response.Headers.Add("Content-Type", "application/json");
      await response.WriteStringAsync(JsonSerializer.Serialize(result, new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
      }));

      _appLogger.LogInformation("Successfully set featured video {MediaId} for blog {Slug}", data.MediaId, slug);
      return response;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error setting featured video for blog {Slug}: {Error}", ex, slug, ex.Message);
      var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
      await errorResponse.WriteStringAsync("Internal server error");
      return errorResponse;
    }
  }

  [Function("SetFeaturedMedia")]
  public async Task<HttpResponseData> SetFeaturedMedia(
      [HttpTrigger(AuthorizationLevel.Function, "post", Route = "posts/{slug}/featured-media")] HttpRequestData req,
      string slug)
  {
    _appLogger.LogInformation("SetFeaturedMedia function triggered for blog {Slug}", slug);

    // Validate API key
    var apiValidationResult = await _apiKeyValidator.ValidateApiKeyAsync(req, _appLogger, "SetFeaturedMedia");
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

      // Set the featured media
      var result = await _blogPostService.SetFeaturedMediaAsync(slug, data.MediaId);
      if (result == null)
      {
        var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
        await notFoundResponse.WriteStringAsync($"Blog post with slug '{slug}' not found or media not found");
        return notFoundResponse;
      }

      // Return updated blog post
      var response = req.CreateResponse(HttpStatusCode.OK);
      response.Headers.Add("Content-Type", "application/json");
      await response.WriteStringAsync(JsonSerializer.Serialize(result, new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
      }));

      _appLogger.LogInformation("Successfully set featured media {MediaId} for blog {Slug}", data.MediaId, slug);
      return response;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error setting featured media for blog {Slug}: {Error}", ex, slug, ex.Message);
      var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
      await errorResponse.WriteStringAsync("Internal server error");
      return errorResponse;
    }
  }

  [Function("AddMediaReference")]
  public async Task<HttpResponseData> AddMediaReference(
      [HttpTrigger(AuthorizationLevel.Function, "post", Route = "posts/{slug}/media-references")] HttpRequestData req,
      string slug)
  {
    _appLogger.LogInformation("AddMediaReference function triggered for blog {Slug}", slug);

    // Validate API key
    var apiValidationResult = await _apiKeyValidator.ValidateApiKeyAsync(req, _appLogger, "AddMediaReference");
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
      var result = await _blogPostService.AddMediaReferenceAsync(slug, data.MediaId);
      if (result == null)
      {
        var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
        await notFoundResponse.WriteStringAsync($"Blog post with slug '{slug}' not found or media not found");
        return notFoundResponse;
      }

      // Return updated blog post
      var response = req.CreateResponse(HttpStatusCode.OK);
      response.Headers.Add("Content-Type", "application/json");
      await response.WriteStringAsync(JsonSerializer.Serialize(result, new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
      }));

      _appLogger.LogInformation("Successfully added media reference {MediaId} to blog {Slug}", data.MediaId, slug);
      return response;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error adding media reference to blog {Slug}: {Error}", ex, slug, ex.Message);
      var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
      await errorResponse.WriteStringAsync("Internal server error");
      return errorResponse;
    }
  }

  [Function("RemoveMediaReference")]
  public async Task<HttpResponseData> RemoveMediaReference(
      [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "posts/{slug}/media-references/{mediaId}")] HttpRequestData req,
      string slug,
      string mediaId)
  {
    _appLogger.LogInformation("RemoveMediaReference function triggered for blog {Slug} and media {MediaId}", slug, mediaId);

    // Validate API key
    var apiValidationResult = await _apiKeyValidator.ValidateApiKeyAsync(req, _appLogger, "RemoveMediaReference");
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
      var result = await _blogPostService.RemoveMediaReferenceAsync(slug, mediaId);
      if (result == null)
      {
        var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
        await notFoundResponse.WriteStringAsync($"Blog post with slug '{slug}' not found");
        return notFoundResponse;
      }

      // Return updated blog post
      var response = req.CreateResponse(HttpStatusCode.OK);
      response.Headers.Add("Content-Type", "application/json");
      await response.WriteStringAsync(JsonSerializer.Serialize(result, new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
      }));

      _appLogger.LogInformation("Successfully removed media reference {MediaId} from blog {Slug}", mediaId, slug);
      return response;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error removing media reference from blog {Slug}: {Error}", ex, slug, ex.Message);
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
