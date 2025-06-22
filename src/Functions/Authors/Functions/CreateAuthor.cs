using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Functions.Authors.Models;
using Functions.Authors.Validators;
using Functions.Authors.Services;
using System.Net;
using SharedStorage.Services;
using Utils;
using Utils.Validation;
using System.Text.Json;

namespace Functions.Authors.Functions;

public class CreateAuthor
{
  // This function is a placeholder for creating an author.
  // The actual implementation will depend on your specific requirements.
  // You can use this function to handle HTTP requests to create a new author.
  private readonly IAppInsightsLogger<CreateAuthor> _appLogger;
  private readonly ITableStorageService _tableStorageService;
  private readonly IAPIKeyValidator _apiKeyValidator;

  private readonly IAuthorService _authorService;
  private readonly string _validApiKey = Environment.GetEnvironmentVariable("X_API_ENVIRONMENT_KEY") ?? "default_key";

  // Constructor to inject the logger
  public CreateAuthor(IAppInsightsLogger<CreateAuthor> logger, ITableStorageService tableStorageService,
    IAPIKeyValidator apiKeyValidator, IAuthorService authorService)
  {
    _appLogger = logger;
    _tableStorageService = tableStorageService;
    _apiKeyValidator = apiKeyValidator;
    _authorService = authorService;
    _appLogger.LogInformation("CreateAuthor function initialized.");
  }

  private static HttpResponseData CreateValidationErrorResponse(HttpRequestData req, IEnumerable<string> errors)
  {
    var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
    errorResponse.Headers.Add("Content-Type", "application/json; charset=utf-8");
    errorResponse.WriteString(JsonSerializer.Serialize(new { errors }));
    return errorResponse;
  }

  [Function("CreateAuthorAsync")]
  public async Task<HttpResponseData> Run(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = "authors")] HttpRequestData req,
    FunctionContext executionContext)

  {
    // Log the function execution context
    // You can use this logger to log information, warnings, errors, etc.
    _appLogger.LogInformation("Validating API key for CreateAuthor function.");
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

    // Now, do stuff with the validated model
    _appLogger.LogInformation("Author model validated successfully. Proceeding to create the author.");

    // Create the author using the AuthorService
    var result = await _authorService.CreateAuthorAsync(model);
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