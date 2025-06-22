using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Functions.Authors.Models;
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

  private readonly string _authorTable = (Environment.GetEnvironmentVariable("AUTHORS_TABLE_NAME")?.ToLowerInvariant() ?? "defaultauthortable";
  private readonly string _validApiKey = Environment.GetEnvironmentVariable("X_API_ENVIRONMENT_KEY") ?? "default_key";
  private readonly string _tableName;

  // Constructor to inject the logger
  public CreateAuthor(IAppInsightsLogger<CreateAuthor> logger, ITableStorageService tableStorageService,
    IAPIKeyValidator apiKeyValidator)
  {
    _appLogger = logger;
    _tableStorageService = tableStorageService;
    _apiKeyValidator = apiKeyValidator;
    _tableName = Environment.GetEnvironmentVariable("USE_MOCK_STORAGE") == "true" ? "mock" + _authorTable : _authorTable;
    _appLogger.LogInformation("CreateAuthor function initialized.");
    _appLogger.LogInformation($"Using table: {_tableName}");
  }

  [Function("CreateAuthor")]
  public async Task<HttpResponseData> Run(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = "authors")] HttpRequestData req,
    FunctionContext executionContext)

  {
    // Log the function execution context
    // You can use this logger to log information, warnings, errors, etc.
    _appLogger.LogInformation("Creating a new author.");

    // Deserialize the data payload to create a new author
    var body = await new StreamReader(req.Body).ReadToEndAsync();

    // apply it to the AuthorModel
    var model = JsonSerializer.Deserialize<AuthorModel>(body);

    // Data validation to ensure no null or empty values from the AuthorEntity and ModelMapper
    var authorEntity = AuthorEntity.FromModel(model, model.AuthorSlug, "profile");

    // Request processing logic goes here
    // For example, you might read the request body, validate input,
    // and create a new author entity in your data store.

    // For now, we will return a simple response indicating success. (No logic has happened yet)
    var response = req.CreateResponse(HttpStatusCode.Created);
    response.Headers.Add("Content-Type", "application/json; charset=utf-8");
    response.WriteString("{\"message\":\"Author created successfully.\"}");
    return Task.FromResult(response);
  }
}