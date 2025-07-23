using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using Utils.Services;
using Utils.Configuration;
using Utils.Validation;

namespace Functions.Diagnostics;

/// <summary>
/// Simple test function to verify Key Vault API key validation
/// </summary>
public class TestKeyVaultAuth
{
  private readonly ILogger<TestKeyVaultAuth> _logger;
  private readonly IKeyVaultService _keyVaultService;
  private readonly IAPIKeyValidator _apiKeyValidator;

  public TestKeyVaultAuth(
      ILogger<TestKeyVaultAuth> logger,
      IKeyVaultService keyVaultService,
      IAPIKeyValidator apiKeyValidator)
  {
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    _keyVaultService = keyVaultService ?? throw new ArgumentNullException(nameof(keyVaultService));
    _apiKeyValidator = apiKeyValidator ?? throw new ArgumentNullException(nameof(apiKeyValidator));
  }

  [Function("TestKeyVaultAuth")]
  public async Task<HttpResponseData> Run(
      [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "test/auth")] HttpRequestData req)
  {
    _logger.LogInformation("TestKeyVaultAuth function triggered");

    try
    {
      // Get current environment and expected secret name
      var environment = EnvironmentHelper.GetCurrentEnvironment();
      var secretName = GetSecretNameForEnvironment(environment);

      _logger.LogInformation("Testing Key Vault auth for environment: {Environment}, secret: {SecretName}", environment, secretName);

      // Step 1: Test direct Key Vault access
      string? keyVaultValue = null;
      try
      {
        keyVaultValue = await _keyVaultService.GetSecretAsync(secretName);
        _logger.LogInformation("Successfully retrieved secret from Key Vault. Length: {Length}", keyVaultValue?.Length ?? 0);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Failed to retrieve secret from Key Vault");
        throw new InvalidOperationException($"Key Vault access failed: {ex.Message}", ex);
      }

      // Step 2: Get the API key from the request header
      var requestApiKey = req.Headers.TryGetValues("x-api-key", out var apiKeyValues) ? apiKeyValues.FirstOrDefault() : null;

      _logger.LogInformation("Request API key present: {HasKey}, Length: {Length}",
          !string.IsNullOrEmpty(requestApiKey), requestApiKey?.Length ?? 0);

      // Step 3: Test API key validation
      var validationResult = await _apiKeyValidator.ValidateApiKeyAsync(req, _logger, "TestKeyVaultAuth");

      var response = req.CreateResponse(HttpStatusCode.OK);
      response.Headers.Add("Content-Type", "application/json");

      var result = new
      {
        success = true,
        environment = environment,
        secretName = secretName,
        keyVaultConnected = true,
        secretRetrieved = !string.IsNullOrEmpty(keyVaultValue),
        secretLength = keyVaultValue?.Length ?? 0,
        requestHasApiKey = !string.IsNullOrEmpty(requestApiKey),
        requestApiKeyLength = requestApiKey?.Length ?? 0,
        apiKeysMatch = string.Equals(requestApiKey, keyVaultValue, StringComparison.Ordinal),
        validationPassed = validationResult == null,
        timestamp = DateTime.UtcNow,
        message = validationResult == null ? "Authentication successful!" : "Authentication failed"
      };

      var jsonOptions = new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
      };

      await response.WriteStringAsync(JsonSerializer.Serialize(result, jsonOptions));
      return response;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error during Key Vault auth test");

      var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
      errorResponse.Headers.Add("Content-Type", "application/json");

      var errorResult = new
      {
        success = false,
        error = ex.Message,
        errorType = ex.GetType().Name,
        innerException = ex.InnerException?.Message,
        stackTrace = ex.StackTrace,
        timestamp = DateTime.UtcNow
      };

      var jsonOptions = new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
      };

      await errorResponse.WriteStringAsync(JsonSerializer.Serialize(errorResult, jsonOptions));
      return errorResponse;
    }
  }

  private static string GetSecretNameForEnvironment(string environment)
  {
    return environment.ToLowerInvariant() switch
    {
      "develop" or "localhost" => "DEV-X-API-ENVIRONMENT-KEY",
      "test" => "STAGING-X-API-ENVIRONMENT-KEY",
      "production" => "PROD-X-API-ENVIRONMENT-KEY",
      _ => throw new InvalidOperationException($"Unknown environment: {environment}")
    };
  }
}
