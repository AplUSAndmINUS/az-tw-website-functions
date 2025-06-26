using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text.Json;

using Functions.Authors.Models;
using Functions.Authors.Validators;
using Functions.Authors.Helpers;
using Functions.Authors.Services;
using Utils;
using Utils.Validation;

namespace Functions.Authors.Functions;

// Endpoint: PUT /authors/{slug}
// Description: Upserts an author by slug. If the author exists, it updates the properties.
// If the author does not exist, it creates a new author with the provided data.
// TODO: Create a scaffolding PATCH function to update an author's properties

public class UpsertAuthorAsync
{
  private readonly IAppInsightsLogger<UpsertAuthorAsync> _appLogger;
  private readonly IAPIKeyValidator _apiKeyValidator;

  private readonly IAuthorService _authorService;

  // Constructor to inject the logger
  public UpsertAuthorAsync(IAppInsightsLogger<UpsertAuthorAsync> logger,
    IAPIKeyValidator apiKeyValidator, IAuthorService authorService)
  {
    _appLogger = logger;
    _apiKeyValidator = apiKeyValidator;
    _authorService = authorService;
    _appLogger.LogInformation("UpsertAuthorAsync function initialized.");
  }

  private static HttpResponseData CreateValidationErrorResponse(HttpRequestData req, IEnumerable<string> errors)
  {
    var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
    errorResponse.Headers.Add("Content-Type", "application/json; charset=utf-8");

    var errorObject = new { errors = errors.ToArray() };
    var jsonOptions = new JsonSerializerOptions
    {
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
      WriteIndented = true
    };

    errorResponse.WriteString(JsonSerializer.Serialize(errorObject, jsonOptions));
    return errorResponse;
  }

  [Function("UpsertAuthorAsync")]
  public async Task<HttpResponseData> Run(
    [HttpTrigger(AuthorizationLevel.Function, "put", Route = "authors/{slug}")] HttpRequestData req, string slug,
    FunctionContext executionContext)

  {
    _appLogger.LogInformation("UpsertAuthorAsync function triggered for slug: {Slug}", slug);
    
    // Validate API key using helper method
    var apiValidationResult = await _apiKeyValidator.ValidateApiKeyAsync(req, _appLogger, "UpsertAuthorAsync");
    if (apiValidationResult != null)
    {
      return apiValidationResult;
    }

    _appLogger.LogInformation("Creating a new author.");
    AuthorModel? model;

    try
    {
      var body = await new StreamReader(req.Body).ReadToEndAsync();
      // Deserialize the data payload to create a new author
      model = JsonSerializer.Deserialize<AuthorModel>(body, new JsonSerializerOptions
      {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
      });

      // Check if the model is null before validation
      if (model == null)
      {
        var modelNullErrors = new[] { "Invalid or missing author data." };
        _appLogger.LogError("Author model is null.", new ArgumentNullException(nameof(model)));
        return CreateValidationErrorResponse(req, modelNullErrors);
      }

      // Validate the model using the AuthorModelDataValidator
      _appLogger.LogInformation("Validating author model data.");
      if (!AuthorModelDataValidator.TryValidate(model, out var errors))
      {
        _appLogger.LogError("Author model validation failed.", new Exception(string.Join(" | ", errors)));
        return CreateValidationErrorResponse(req, errors);
      }
    }
    catch (JsonException ex)
    {
      _appLogger.LogError("Failed to deserialize author data.", ex);
      return CreateValidationErrorResponse(req, new[] { "Invalid JSON format." });
    }
    catch (Exception ex)
    {
      _appLogger.LogError("An unexpected error occurred while processing the request.", ex);
      return req.CreateResponse(HttpStatusCode.InternalServerError);
    }

    // check to see if the slug exists
    // TODO: implement a check to see if the slug already exists to generate -2, -3, etc.
    if (string.IsNullOrEmpty(model.AuthorSlug))
    {
      model.AuthorSlug = slug ??
      SlugGenerator.FromName(model.FirstName, model.LastName)
      ?? SlugGenerator.FromString(model.Username)
      ?? SlugGenerator.FromString(model.DisplayName)
      ?? SlugGenerator.FromAnonymous();
      _appLogger.LogInformation($"Generated slug for author: {model.AuthorSlug}");
    }
    else if (!string.Equals(model.AuthorSlug, slug, StringComparison.OrdinalIgnoreCase))
    {
      _appLogger.LogWarning($"Slug mismatch: provided slug '{slug}' does not match model slug '{model.AuthorSlug}'.");
      return CreateValidationErrorResponse(req, new[] { "Slug mismatch." });
    }

    // Now, do stuff with the validated model
    _appLogger.LogInformation("Author model validated successfully. Proceeding to upsert the author.");

    try
    {
      // Create/Update the author using the AuthorService
      var result = await _authorService.UpsertAuthorAsync(model);

      // Create response with appropriate status code
      var response = req.CreateResponse(HttpStatusCode.Created);

      // Set the response headers
      response.Headers.Add("Location", $"/authors/{result.AuthorSlug}");
      response.Headers.Add("Content-Type", "application/json; charset=utf-8");

      // Serialize the AuthorDTO response
      var jsonOptions = new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
      };

      await response.WriteStringAsync(JsonSerializer.Serialize(result, jsonOptions));

      // Log the successful creation/update of the author
      _appLogger.LogInformation("Author upserted successfully with slug: {AuthorSlug}", result.AuthorSlug);
      return response;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Failed to upsert author: {ErrorMessage}", ex, ex.Message);
      return req.CreateResponse(HttpStatusCode.InternalServerError);
    }
  }
}