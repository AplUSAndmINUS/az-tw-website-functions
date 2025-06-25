using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Functions.Authors.Models;
using Functions.Authors.Validators;
using Functions.Authors.Helpers;
using Functions.Authors.Services;
using System.Net;
using SharedStorage.Services;
using Utils;
using Utils.Validation;
using System.Text.Json;

namespace Functions.Authors.Functions;

public class UpsertAuthorAsync
{
  private readonly IAppInsightsLogger<UpsertAuthorAsync> _appLogger;
  private readonly ITableStorageService _tableStorageService;
  private readonly IAPIKeyValidator _apiKeyValidator;

  private readonly IAuthorService _authorService;

  // Constructor to inject the logger
  public UpsertAuthorAsync(IAppInsightsLogger<UpsertAuthorAsync> logger, ITableStorageService tableStorageService,
    IAPIKeyValidator apiKeyValidator, IAuthorService authorService)
  {
    _appLogger = logger;
    _tableStorageService = tableStorageService;
    _apiKeyValidator = apiKeyValidator;
    _authorService = authorService;
    _appLogger.LogInformation("UpsertAuthorAsync function initialized.");
  }

  private static HttpResponseData CreateValidationErrorResponse(HttpRequestData req, IEnumerable<string> errors)
  {
    var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
    errorResponse.Headers.Add("Content-Type", "application/json; charset=utf-8");
    errorResponse.WriteString(JsonSerializer.Serialize(new { errors }));
    return errorResponse;
  }

  [Function("UpsertAuthorAsync")]
  public async Task<HttpResponseData> Run(
    [HttpTrigger(AuthorizationLevel.Function, "put", Route = "authors/{slug}")] HttpRequestData req, string slug,
    FunctionContext executionContext)

  {
    // Log the function execution context
    // You can use this logger to log information, warnings, errors, etc.
    _appLogger.LogInformation("Validating API key for UpsertAuthorAsync function.");
    // Validate the API key
    try
    {
      await _apiKeyValidator.ValidateOrThrowAsync(req);
      _appLogger.LogInformation("API key validation successful.");
    }
    catch (UnauthorizedAccessException ex)
    {
      _appLogger.LogError($"Unauthorized access attempt: {ex.Message}", ex);
      return req.CreateResponse(HttpStatusCode.Unauthorized);
    }

    _appLogger.LogInformation("Creating a new author.");
    AuthorModel? model = null;

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
    _appLogger.LogInformation("Author model validated successfully. Proceeding to create the author.");

    // Create the author using the AuthorService
    var result = await _authorService.UpsertAuthorAsync(model);
    var response = req.CreateResponse(HttpStatusCode.Created);

    // Set the response headers and body
    response.Headers.Add("Location", $"/authors/{result.AuthorSlug}");
    response.Headers.Add("Content-Type", "application/json; charset=utf-8");
    response.WriteString(JsonSerializer.Serialize(result));

    // Log the successful creation of the author
    _appLogger.LogInformation("Author created successfully.");
    return response;
  }
}