using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text.Json;

using Utils;
using Utils.Validation;
using Functions.Authors.Services;

namespace Functions.Authors.Functions;

// Endpoint: GET /authors/{slug}
// Description: Retrieves an author by their slug. If the author exists, it returns the author's details.
// If the author does not exist, it returns a 404 Not Found error.

public class GetAuthorFunction
{
  private readonly IAppInsightsLogger<GetAuthorFunction> _appLogger;
  private readonly IAPIKeyValidator _apiKeyValidator;
  private readonly IAuthorService _authorService;

  public GetAuthorFunction(IAppInsightsLogger<GetAuthorFunction> logger, IAPIKeyValidator apiKeyValidator, IAuthorService authorService)
  {
    _appLogger = logger;
    _apiKeyValidator = apiKeyValidator;
    _authorService = authorService;
    _appLogger.LogInformation("GetAuthorFunction initialized");
  }

  private static HttpResponseData CreateErrorResponse(HttpRequestData req, string message, HttpStatusCode statusCode)
  {
    var errorResponse = req.CreateResponse(statusCode);
    errorResponse.Headers.Add("Content-Type", "application/json; charset=utf-8");

    var errorObject = new { error = message };
    var jsonOptions = new JsonSerializerOptions
    {
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
      WriteIndented = true
    };

    errorResponse.WriteString(JsonSerializer.Serialize(errorObject, jsonOptions));
    return errorResponse;
  }

  [Function("GetAuthorAsync")]
  public async Task<HttpResponseData> Run(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "authors/{slug}")] HttpRequestData req, string slug, FunctionContext executionContext)
  {
    _appLogger.LogInformation("GetAuthor function triggered for slug: {Slug}", slug);

    // Validate API key using helper method
    var apiValidationResult = await _apiKeyValidator.ValidateApiKeyAsync(req, _appLogger, "GetAuthor");
    if (apiValidationResult != null)
    {
      return apiValidationResult;
    }

    _appLogger.LogInformation("Getting author by slug: {Slug}", slug);

    try
    {
      // Validate the request
      if (req == null)
      {
        throw new ArgumentNullException(nameof(req), "Request cannot be null.");
      }

      _appLogger.LogInformation("Request validation successful.");

      // Create a response with headers
      var response = req.CreateResponse(HttpStatusCode.OK);
      response.Headers.Add("Location", $"/authors/{slug}");
      response.Headers.Add("Content-Type", "application/json; charset=utf-8");
      _appLogger.LogInformation("Response created with Location header: /authors/{Slug}", slug);

      // Perform a table lookup using the author slug
      var author = await _authorService.GetAuthorBySlugAsync(slug);

      if (author == null)
      {
        _appLogger.LogWarning("Author not found for slug: {0}", slug);
        return CreateErrorResponse(req, "Author not found", HttpStatusCode.NotFound);
      }
      else
      {
        _appLogger.LogInformation("Author found for slug: {Slug} with display name: {DisplayName}", slug, author.DisplayName);
      }

      // Serialize the author model to JSON with consistent formatting
      var jsonOptions = new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
      };

      var authorJson = JsonSerializer.Serialize(author, jsonOptions);
      _appLogger.LogInformation("Author serialized to JSON successfully for slug: {Slug}", slug);

      // Write the author JSON to the response
      await response.WriteStringAsync(authorJson);

      return response;
    }
    catch (ArgumentNullException ex)
    {
      _appLogger.LogError("Argument null exception occurred: {Message}", ex);
      return CreateErrorResponse(req, "Invalid request parameters", HttpStatusCode.BadRequest);
    }
    catch (Exception ex)
    {
      _appLogger.LogError("An unexpected error occurred: {Message}", ex);
      return CreateErrorResponse(req, "Internal server error", HttpStatusCode.InternalServerError);
    }
  }
}