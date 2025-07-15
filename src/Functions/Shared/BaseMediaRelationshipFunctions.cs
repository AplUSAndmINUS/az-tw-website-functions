using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text.Json;
using Utils;
using Utils.Validation;

namespace Functions.Shared;

/// <summary>
/// Base class for media relationship operations to reduce code duplication
/// Common operations: Set profile/featured images, add/remove media references
/// </summary>
public abstract class BaseMediaRelationshipFunctions<TService, TDto>
  where TService : class
  where TDto : class
{
  protected readonly IAppInsightsLogger<BaseMediaRelationshipFunctions<TService, TDto>> _appLogger;
  protected readonly TService _contentService;
  protected readonly IAPIKeyValidator _apiKeyValidator;

  protected BaseMediaRelationshipFunctions(
    IAppInsightsLogger<BaseMediaRelationshipFunctions<TService, TDto>> logger,
    TService contentService,
    IAPIKeyValidator apiKeyValidator)
  {
    _appLogger = logger ?? throw new ArgumentNullException(nameof(logger));
    _contentService = contentService ?? throw new ArgumentNullException(nameof(contentService));
    _apiKeyValidator = apiKeyValidator ?? throw new ArgumentNullException(nameof(apiKeyValidator));
  }

  /// <summary>
  /// Common logic for media relationship operations that require a mediaId in the request body
  /// </summary>
  protected async Task<HttpResponseData> ProcessMediaRelationshipAsync(
    HttpRequestData req,
    string slug,
    string functionName,
    Func<string, string, Task<TDto?>> operation,
    string successMessage)
  {
    _appLogger.LogInformation("{FunctionName} function triggered for slug: {Slug}", functionName, slug);

    // Validate API key
    var apiValidationResult = await _apiKeyValidator.ValidateApiKeyAsync(req, _appLogger, functionName);
    if (apiValidationResult != null)
    {
      return apiValidationResult;
    }

    try
    {
      // Extract and validate mediaId from request body
      var (mediaId, errorResponse) = await ExtractMediaIdFromBodyAsync(req);
      if (errorResponse != null)
      {
        return errorResponse;
      }

      // Perform the media relationship operation
      var result = await operation(slug, mediaId!);
      if (result == null)
      {
        _appLogger.LogWarning("Content not found for slug: {Slug} or media not found: {MediaId}", slug ?? "null", mediaId ?? "null");
        return CreateNotFoundResponse(req, $"Content with slug '{slug}' not found or media not found");
      }

      _appLogger.LogInformation(successMessage, mediaId ?? "null", slug ?? "null");
      return await CreateJsonResponseAsync(req, result);
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error in {FunctionName} for slug {Slug}: {Error}", ex, functionName, slug, ex.Message);
      return CreateServerErrorResponse(req, "Internal server error");
    }
  }

  /// <summary>
  /// Common logic for media removal operations that require a mediaId in the route
  /// </summary>
  protected async Task<HttpResponseData> ProcessRemoveMediaAsync(
    HttpRequestData req,
    string slug,
    string mediaId,
    string functionName,
    Func<string, string, Task<TDto?>> operation,
    string successMessage)
  {
    _appLogger.LogInformation("{FunctionName} function triggered for slug: {Slug}, mediaId: {MediaId}", functionName, slug, mediaId);

    // Validate API key
    var apiValidationResult = await _apiKeyValidator.ValidateApiKeyAsync(req, _appLogger, functionName);
    if (apiValidationResult != null)
    {
      return apiValidationResult;
    }

    try
    {
      // Validate parameters
      if (string.IsNullOrWhiteSpace(slug))
      {
        return CreateBadRequestResponse(req, "Slug is required");
      }

      if (string.IsNullOrWhiteSpace(mediaId))
      {
        return CreateBadRequestResponse(req, "MediaId is required");
      }

      // Perform the media removal operation
      var result = await operation(slug, mediaId);
      if (result == null)
      {
        _appLogger.LogWarning("Content not found for slug: {Slug} or media not found: {MediaId}", slug, mediaId);
        return CreateNotFoundResponse(req, $"Content with slug '{slug}' not found or media reference not found");
      }

      _appLogger.LogInformation(successMessage, mediaId, slug);
      return await CreateJsonResponseAsync(req, result);
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error in {FunctionName} for slug {Slug}, mediaId {MediaId}: {Error}", ex, functionName, slug, mediaId, ex.Message);
      return CreateServerErrorResponse(req, "Internal server error");
    }
  }

  /// <summary>
  /// Extract mediaId from request body
  /// </summary>
  protected async Task<(string? mediaId, HttpResponseData? errorResponse)> ExtractMediaIdFromBodyAsync(HttpRequestData req)
  {
    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
    if (string.IsNullOrEmpty(requestBody))
    {
      return (null, CreateBadRequestResponse(req, "Request body is required with mediaId"));
    }

    try
    {
      var data = JsonSerializer.Deserialize<MediaIdRequest>(requestBody);
      if (data == null || string.IsNullOrWhiteSpace(data.MediaId))
      {
        return (null, CreateBadRequestResponse(req, "MediaId is required"));
      }

      return (data.MediaId, null);
    }
    catch (JsonException)
    {
      return (null, CreateBadRequestResponse(req, "Invalid JSON format"));
    }
  }

  /// <summary>
  /// Common logic for creating JSON response
  /// </summary>
  protected async Task<HttpResponseData> CreateJsonResponseAsync(HttpRequestData req, object data)
  {
    var response = req.CreateResponse(HttpStatusCode.OK);
    response.Headers.Add("Content-Type", "application/json");
    await response.WriteStringAsync(JsonSerializer.Serialize(data, new JsonSerializerOptions
    {
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    }));
    return response;
  }

  /// <summary>
  /// Common logic for creating not found response
  /// </summary>
  protected HttpResponseData CreateNotFoundResponse(HttpRequestData req, string message)
  {
    var response = req.CreateResponse(HttpStatusCode.NotFound);
    response.WriteString(message);
    return response;
  }

  /// <summary>
  /// Common logic for creating bad request response
  /// </summary>
  protected HttpResponseData CreateBadRequestResponse(HttpRequestData req, string message)
  {
    var response = req.CreateResponse(HttpStatusCode.BadRequest);
    response.WriteString(message);
    return response;
  }

  /// <summary>
  /// Common logic for creating internal server error response
  /// </summary>
  protected HttpResponseData CreateServerErrorResponse(HttpRequestData req, string message = "Internal server error")
  {
    var response = req.CreateResponse(HttpStatusCode.InternalServerError);
    response.WriteString(message);
    return response;
  }
}

/// <summary>
/// Request model for media ID operations
/// </summary>
public class MediaIdRequest
{
  public string? MediaId { get; set; }
}
