using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using Utils;
using Utils.Validation;

namespace Functions.Shared;

/// <summary>
/// Base class for content-related Azure Functions to reduce code duplication
/// </summary>
public abstract class BaseContentFunctions<TService, TModel, TDto, TWithMediaDto>
  where TService : class
  where TModel : class
  where TDto : class
  where TWithMediaDto : class
{
  protected readonly IAppInsightsLogger<BaseContentFunctions<TService, TModel, TDto, TWithMediaDto>> _appLogger;
  protected readonly TService _contentService;
  protected readonly IAPIKeyValidator _apiKeyValidator;

  protected BaseContentFunctions(
    IAppInsightsLogger<BaseContentFunctions<TService, TModel, TDto, TWithMediaDto>> logger,
    TService contentService,
    IAPIKeyValidator apiKeyValidator)
  {
    _appLogger = logger ?? throw new ArgumentNullException(nameof(logger));
    _contentService = contentService ?? throw new ArgumentNullException(nameof(contentService));
    _apiKeyValidator = apiKeyValidator ?? throw new ArgumentNullException(nameof(apiKeyValidator));
  }

  /// <summary>
  /// Common logic for validating API keys
  /// </summary>
  protected async Task<HttpResponseData?> ValidateApiKeyAsync(HttpRequestData req, string functionName)
  {
    return await _apiKeyValidator.ValidateApiKeyAsync(req, _appLogger, functionName);
  }

  /// <summary>
  /// Common logic for extracting slug from route
  /// </summary>
  protected string? ExtractSlugFromRoute(HttpRequestData req)
  {
    return req.Query["slug"] ?? req.FunctionContext.BindingContext.BindingData["slug"]?.ToString();
  }

  /// <summary>
  /// Common logic for validating slug parameter
  /// </summary>
  protected HttpResponseData? ValidateSlug(HttpRequestData req, string? slug)
  {
    if (string.IsNullOrWhiteSpace(slug))
    {
      _appLogger.LogWarning("Slug parameter is missing");
      var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
      badResponse.WriteString("Slug parameter is required");
      return badResponse;
    }
    return null;
  }

  /// <summary>
  /// Common logic for parsing query parameters
  /// </summary>
  protected (string? authorSlug, string? category, bool? isPublished, int? limit, bool includeMedia) ParseGetQueryParameters(HttpRequestData req)
  {
    var authorSlug = req.Query["authorSlug"];
    var category = req.Query["category"];
    var isPublishedParam = req.Query["isPublished"];
    var limitParam = req.Query["limit"];
    var includeMediaParam = req.Query["includeMedia"];

    bool? isPublished = string.IsNullOrEmpty(isPublishedParam) ? true : bool.Parse(isPublishedParam);
    int? limit = string.IsNullOrEmpty(limitParam) ? null : int.Parse(limitParam);
    bool includeMedia = !string.IsNullOrEmpty(includeMediaParam) && bool.Parse(includeMediaParam);

    return (authorSlug, category, isPublished, limit, includeMedia);
  }

  /// <summary>
  /// Common logic for parsing single item query parameters
  /// </summary>
  protected (bool? isPublished, bool includeMedia) ParseGetSingleQueryParameters(HttpRequestData req)
  {
    var isPublishedParam = req.Query["isPublished"];
    var includeMediaParam = req.Query["includeMedia"];

    bool? isPublished = string.IsNullOrEmpty(isPublishedParam) ? true : bool.Parse(isPublishedParam);
    bool includeMedia = !string.IsNullOrEmpty(includeMediaParam) && bool.Parse(includeMediaParam);

    return (isPublished, includeMedia);
  }

  /// <summary>
  /// Common logic for creating success response with JSON
  /// </summary>
  protected async Task<HttpResponseData> CreateJsonResponseAsync(HttpRequestData req, object data)
  {
    var response = req.CreateResponse(HttpStatusCode.OK);
    response.Headers.Add("Content-Type", "application/json");
    await response.WriteStringAsync(JsonHelper.Serialize(data));
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

  /// <summary>
  /// Common logic for reading and deserializing request body
  /// </summary>
  protected async Task<(T? model, HttpResponseData? errorResponse)> ReadAndDeserializeBodyAsync<T>(HttpRequestData req) where T : class, new()
  {
    var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
    _appLogger.LogInformation("Request body received: {RequestBody}", requestBody);

    if (string.IsNullOrWhiteSpace(requestBody))
    {
      _appLogger.LogWarning("Request body is empty");
      return (null, CreateBadRequestResponse(req, "Request body is required"));
    }

    try
    {
      var model = JsonHelper.Deserialize<T>(requestBody);

      // Basic validation to ensure we got meaningful data
      if (model == null)
      {
        _appLogger.LogWarning("Failed to deserialize model or received null model");
        return (null, CreateBadRequestResponse(req, "Invalid data format"));
      }

      return (model, null);
    }
    catch (JsonException ex)
    {
      _appLogger.LogWarning("Invalid JSON in request body: {Error}", ex.Message);
      return (null, CreateBadRequestResponse(req, "Invalid JSON format"));
    }
  }

  /// <summary>
  /// Common validation for required content fields
  /// </summary>
  protected HttpResponseData? ValidateContentModel(HttpRequestData req, TModel model)
  {
    return ValidateContentModelFields(req, model);
  }

  /// <summary>
  /// Override this method to provide model-specific field validation
  /// </summary>
  protected abstract HttpResponseData? ValidateContentModelFields(HttpRequestData req, TModel model);

  /// <summary>
  /// Common logic for extracting media ID from route
  /// </summary>
  protected string? ExtractMediaIdFromRoute(HttpRequestData req)
  {
    return req.FunctionContext.BindingContext.BindingData["mediaId"]?.ToString();
  }

  /// <summary>
  /// Common logic for delete operations
  /// </summary>
  protected async Task<HttpResponseData> ProcessDeleteAsync(HttpRequestData req, string functionName, Func<string, Task<bool>> deleteOperation)
  {
    _appLogger.LogInformation("{FunctionName} function triggered", functionName);

    // Validate API key
    var apiValidationResult = await ValidateApiKeyAsync(req, functionName);
    if (apiValidationResult != null)
    {
      return apiValidationResult;
    }

    try
    {
      // Extract and validate slug
      var slug = ExtractSlugFromRoute(req);
      var slugValidationResult = ValidateSlug(req, slug);
      if (slugValidationResult != null)
      {
        return slugValidationResult;
      }

      // Perform delete operation
      var success = await deleteOperation(slug!);

      if (!success)
      {
        _appLogger.LogWarning("Failed to delete content with slug: {Slug}", slug ?? "unknown");
        return CreateNotFoundResponse(req, "Content not found or could not be deleted");
      }

      // Return success response
      var response = req.CreateResponse(HttpStatusCode.NoContent);
      _appLogger.LogInformation("Successfully deleted content with slug: {Slug}", slug ?? "unknown");
      return response;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error deleting content", ex);
      return CreateServerErrorResponse(req);
    }
  }

  /// <summary>
  /// Common logic for validating media ID parameter
  /// </summary>
  protected HttpResponseData? ValidateMediaId(HttpRequestData req, string? mediaId)
  {
    if (string.IsNullOrWhiteSpace(mediaId))
    {
      _appLogger.LogWarning("MediaId parameter is missing");
      return CreateBadRequestResponse(req, "MediaId parameter is required");
    }
    return null;
  }
}
