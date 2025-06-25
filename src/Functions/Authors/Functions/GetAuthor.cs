using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text.Json;
using Utils;

using Functions.Authors.Services;
using Functions.Authors.Models;
using SharedStorage.Services;

namespace Functions.Authors.Functions;

public class GetAuthorFunction
{
  private readonly IAppInsightsLogger<GetAuthorFunction> _appLogger;
  private readonly ITableStorageService _tableStorageService;
  private readonly IAuthorService _authorService;

  public GetAuthorFunction(IAppInsightsLogger<GetAuthorFunction> logger, ITableStorageService tableStorageService, IAuthorService authorService, string? query)
  {
    _appLogger = logger;
    _tableStorageService = tableStorageService;
    _authorService = authorService;
    _appLogger.LogInformation("GetAuthorFunction initialized with query: {Query}", query ?? "null");
  }

  private static HttpResponseData CreateErrorResponse(HttpRequestData req, string message, HttpStatusCode statusCode)
  {
    var errorResponse = req.CreateResponse(statusCode);
    errorResponse.Headers.Add("Content-Type", "application/json; charset=utf-8");
    errorResponse.WriteString(JsonSerializer.Serialize(new { error = message }));
    return errorResponse;
  }

  [Function("GetAuthorAsync")]
  public HttpResponseData Run(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "authors/{slug}")] HttpRequestData req, string slug, FunctionContext executionContext)
  {
    _appLogger.LogInformation("GetAuthor function triggered with request: {Request}", req);

    try
    {
      // Validate the request
      if (req == null)
      {
        throw new ArgumentNullException(nameof(req), "Request cannot be null.");
      }

      _appLogger.LogInformation("Request validation successful.");


      // Extract the author slug from the request URL
      var authorSlug = req.Url.Segments.LastOrDefault()?.TrimEnd('/') ?? string.Empty;
      _appLogger.LogInformation("Author slug extracted: {AuthorSlug}", authorSlug);

      // Create a response with headers
      var response = req.CreateResponse(HttpStatusCode.OK);
      response.Headers.Add("Location", $"/authors/{slug}");
      response.Headers.Add("Content-Type", "application/json; charset=utf-8");
      _appLogger.LogInformation("Response created with Location header: /authors/{Slug}", slug);

      // Perform a table lookup using the author slug
      var author = _authorService.GetAuthorBySlugAsync(slug).GetAwaiter().GetResult();

      if (author == null)
      {
        _appLogger.LogWarning("Author not found for slug: {Slug}", slug);
        return CreateErrorResponse(req, "Author not found", HttpStatusCode.NotFound);
      }
      else
      {
        _appLogger.LogInformation("Author found: {Author}", author);
      }

      // Serialize the author model to JSON
      var authorJson = JsonSerializer.Serialize(author);
      _appLogger.LogInformation("Author serialized to JSON: {AuthorJson}", authorJson);

      // Write the author JSON to the response
      response.WriteString(authorJson);

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