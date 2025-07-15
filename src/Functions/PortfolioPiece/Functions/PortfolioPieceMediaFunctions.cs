using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Functions.PortfolioPiece.Services;
using System.Net;
using System.Text.Json;
using Utils;
using Utils.Validation;

namespace Functions.PortfolioPiece.Functions;

public class PortfolioPieceMediaFunctions
{
  private readonly IAppInsightsLogger<PortfolioPieceMediaFunctions> _appLogger;
  private readonly IPortfolioPieceService _portfolioService;
  private readonly IAPIKeyValidator _apiKeyValidator;

  public PortfolioPieceMediaFunctions(
    IAppInsightsLogger<PortfolioPieceMediaFunctions> logger,
    IPortfolioPieceService portfolioService,
    IAPIKeyValidator apiKeyValidator)
  {
    _appLogger = logger ?? throw new ArgumentNullException(nameof(logger));
    _portfolioService = portfolioService ?? throw new ArgumentNullException(nameof(portfolioService));
    _apiKeyValidator = apiKeyValidator ?? throw new ArgumentNullException(nameof(apiKeyValidator));
  }

  [Function("SetPortfolioPieceFeaturedImage")]
  public async Task<HttpResponseData> SetFeaturedImage([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "portfolio/{slug}/featured-image/{mediaId}")] HttpRequestData req)
  {
    _appLogger.LogInformation("SetPortfolioPieceFeaturedImage function triggered");

    // Validate API key using helper method
    var apiValidationResult = await _apiKeyValidator.ValidateApiKeyAsync(req, _appLogger, "SetPortfolioPieceFeaturedImage");
    if (apiValidationResult != null)
    {
      return apiValidationResult;
    }

    try
    {
      // Extract parameters from route
      var slug = req.FunctionContext.BindingContext.BindingData["slug"]?.ToString();
      var mediaId = req.FunctionContext.BindingContext.BindingData["mediaId"]?.ToString();

      if (string.IsNullOrWhiteSpace(slug))
      {
        _appLogger.LogWarning("Slug parameter is missing");
        var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
        await badResponse.WriteStringAsync("Slug parameter is required");
        return badResponse;
      }

      if (string.IsNullOrWhiteSpace(mediaId))
      {
        _appLogger.LogWarning("MediaId parameter is missing");
        var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
        await badResponse.WriteStringAsync("MediaId parameter is required");
        return badResponse;
      }

      // Set featured image
      var result = await _portfolioService.SetFeaturedImageAsync(slug, mediaId);

      if (result == null)
      {
        _appLogger.LogInformation("Portfolio piece with slug {Slug} not found", slug);
        var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
        await notFoundResponse.WriteStringAsync("Portfolio piece not found");
        return notFoundResponse;
      }

      // Create response
      var response = req.CreateResponse(HttpStatusCode.OK);
      response.Headers.Add("Content-Type", "application/json");

      await response.WriteStringAsync(JsonHelper.Serialize(result));

      _appLogger.LogInformation("Successfully set featured image for portfolio piece with slug: {Slug}", slug);
      return response;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error setting featured image for portfolio piece", ex);
      var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
      await errorResponse.WriteStringAsync("Internal server error");
      return errorResponse;
    }
  }

  [Function("SetPortfolioPieceFeaturedVideo")]
  public async Task<HttpResponseData> SetFeaturedVideo([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "portfolio/{slug}/featured-video/{mediaId}")] HttpRequestData req)
  {
    _appLogger.LogInformation("SetPortfolioPieceFeaturedVideo function triggered");

    // Validate API key using helper method
    var apiValidationResult = await _apiKeyValidator.ValidateApiKeyAsync(req, _appLogger, "SetPortfolioPieceFeaturedVideo");
    if (apiValidationResult != null)
    {
      return apiValidationResult;
    }

    try
    {
      // Extract parameters from route
      var slug = req.FunctionContext.BindingContext.BindingData["slug"]?.ToString();
      var mediaId = req.FunctionContext.BindingContext.BindingData["mediaId"]?.ToString();

      if (string.IsNullOrWhiteSpace(slug))
      {
        _appLogger.LogWarning("Slug parameter is missing");
        var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
        await badResponse.WriteStringAsync("Slug parameter is required");
        return badResponse;
      }

      if (string.IsNullOrWhiteSpace(mediaId))
      {
        _appLogger.LogWarning("MediaId parameter is missing");
        var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
        await badResponse.WriteStringAsync("MediaId parameter is required");
        return badResponse;
      }

      // Set featured video
      var result = await _portfolioService.SetFeaturedVideoAsync(slug, mediaId);

      if (result == null)
      {
        _appLogger.LogInformation("Portfolio piece with slug {Slug} not found", slug);
        var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
        await notFoundResponse.WriteStringAsync("Portfolio piece not found");
        return notFoundResponse;
      }

      // Create response
      var response = req.CreateResponse(HttpStatusCode.OK);
      response.Headers.Add("Content-Type", "application/json");

      await response.WriteStringAsync(JsonHelper.Serialize(result));

      _appLogger.LogInformation("Successfully set featured video for portfolio piece with slug: {Slug}", slug);
      return response;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error setting featured video for portfolio piece", ex);
      var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
      await errorResponse.WriteStringAsync("Internal server error");
      return errorResponse;
    }
  }

  [Function("SetPortfolioPieceFeaturedMedia")]
  public async Task<HttpResponseData> SetFeaturedMedia([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "portfolio/{slug}/featured-media/{mediaId}")] HttpRequestData req)
  {
    _appLogger.LogInformation("SetPortfolioPieceFeaturedMedia function triggered");

    // Validate API key using helper method
    var apiValidationResult = await _apiKeyValidator.ValidateApiKeyAsync(req, _appLogger, "SetPortfolioPieceFeaturedMedia");
    if (apiValidationResult != null)
    {
      return apiValidationResult;
    }

    try
    {
      // Extract parameters from route
      var slug = req.FunctionContext.BindingContext.BindingData["slug"]?.ToString();
      var mediaId = req.FunctionContext.BindingContext.BindingData["mediaId"]?.ToString();

      if (string.IsNullOrWhiteSpace(slug))
      {
        _appLogger.LogWarning("Slug parameter is missing");
        var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
        await badResponse.WriteStringAsync("Slug parameter is required");
        return badResponse;
      }

      if (string.IsNullOrWhiteSpace(mediaId))
      {
        _appLogger.LogWarning("MediaId parameter is missing");
        var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
        await badResponse.WriteStringAsync("MediaId parameter is required");
        return badResponse;
      }

      // Set featured media
      var result = await _portfolioService.SetFeaturedMediaAsync(slug, mediaId);

      if (result == null)
      {
        _appLogger.LogInformation("Portfolio piece with slug {Slug} not found", slug);
        var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
        await notFoundResponse.WriteStringAsync("Portfolio piece not found");
        return notFoundResponse;
      }

      // Create response
      var response = req.CreateResponse(HttpStatusCode.OK);
      response.Headers.Add("Content-Type", "application/json");

      await response.WriteStringAsync(JsonHelper.Serialize(result));

      _appLogger.LogInformation("Successfully set featured media for portfolio piece with slug: {Slug}", slug);
      return response;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error setting featured media for portfolio piece", ex);
      var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
      await errorResponse.WriteStringAsync("Internal server error");
      return errorResponse;
    }
  }

  [Function("AddPortfolioPieceMediaReference")]
  public async Task<HttpResponseData> AddMediaReference([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "portfolio/{slug}/media/{mediaId}")] HttpRequestData req)
  {
    _appLogger.LogInformation("AddPortfolioPieceMediaReference function triggered");

    // Validate API key using helper method
    var apiValidationResult = await _apiKeyValidator.ValidateApiKeyAsync(req, _appLogger, "AddPortfolioPieceMediaReference");
    if (apiValidationResult != null)
    {
      return apiValidationResult;
    }

    try
    {
      // Extract parameters from route
      var slug = req.FunctionContext.BindingContext.BindingData["slug"]?.ToString();
      var mediaId = req.FunctionContext.BindingContext.BindingData["mediaId"]?.ToString();

      if (string.IsNullOrWhiteSpace(slug))
      {
        _appLogger.LogWarning("Slug parameter is missing");
        var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
        await badResponse.WriteStringAsync("Slug parameter is required");
        return badResponse;
      }

      if (string.IsNullOrWhiteSpace(mediaId))
      {
        _appLogger.LogWarning("MediaId parameter is missing");
        var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
        await badResponse.WriteStringAsync("MediaId parameter is required");
        return badResponse;
      }

      // Add media reference
      var result = await _portfolioService.AddMediaReferenceAsync(slug, mediaId);

      if (result == null)
      {
        _appLogger.LogInformation("Portfolio piece with slug {Slug} not found", slug);
        var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
        await notFoundResponse.WriteStringAsync("Portfolio piece not found");
        return notFoundResponse;
      }

      // Create response
      var response = req.CreateResponse(HttpStatusCode.OK);
      response.Headers.Add("Content-Type", "application/json");

      await response.WriteStringAsync(JsonHelper.Serialize(result));

      _appLogger.LogInformation("Successfully added media reference to portfolio piece with slug: {Slug}", slug);
      return response;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error adding media reference to portfolio piece", ex);
      var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
      await errorResponse.WriteStringAsync("Internal server error");
      return errorResponse;
    }
  }

  [Function("RemovePortfolioPieceMediaReference")]
  public async Task<HttpResponseData> RemoveMediaReference([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "portfolio/{slug}/media/{mediaId}")] HttpRequestData req)
  {
    _appLogger.LogInformation("RemovePortfolioPieceMediaReference function triggered");

    // Validate API key using helper method
    var apiValidationResult = await _apiKeyValidator.ValidateApiKeyAsync(req, _appLogger, "RemovePortfolioPieceMediaReference");
    if (apiValidationResult != null)
    {
      return apiValidationResult;
    }

    try
    {
      // Extract parameters from route
      var slug = req.FunctionContext.BindingContext.BindingData["slug"]?.ToString();
      var mediaId = req.FunctionContext.BindingContext.BindingData["mediaId"]?.ToString();

      if (string.IsNullOrWhiteSpace(slug))
      {
        _appLogger.LogWarning("Slug parameter is missing");
        var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
        await badResponse.WriteStringAsync("Slug parameter is required");
        return badResponse;
      }

      if (string.IsNullOrWhiteSpace(mediaId))
      {
        _appLogger.LogWarning("MediaId parameter is missing");
        var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
        await badResponse.WriteStringAsync("MediaId parameter is required");
        return badResponse;
      }

      // Remove media reference
      var result = await _portfolioService.RemoveMediaReferenceAsync(slug, mediaId);

      if (result == null)
      {
        _appLogger.LogInformation("Portfolio piece with slug {Slug} not found", slug);
        var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
        await notFoundResponse.WriteStringAsync("Portfolio piece not found");
        return notFoundResponse;
      }

      // Create response
      var response = req.CreateResponse(HttpStatusCode.OK);
      response.Headers.Add("Content-Type", "application/json");

      await response.WriteStringAsync(JsonHelper.Serialize(result));

      _appLogger.LogInformation("Successfully removed media reference from portfolio piece with slug: {Slug}", slug);
      return response;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error removing media reference from portfolio piece", ex);
      var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
      await errorResponse.WriteStringAsync("Internal server error");
      return errorResponse;
    }
  }
}
