using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Utils.Services;

namespace Utils.Validation;

/// <summary>
/// API Key validator that retrieves keys from Azure Key Vault
/// Uses managed identity for secure access to Key Vault
/// </summary>
public class KeyVaultApiKeyValidator : IAPIKeyValidator
{
  private readonly IKeyVaultService _keyVaultService;
  private readonly string _environment;
  private readonly bool _enforceGet;
  private readonly IAppInsightsLogger<KeyVaultApiKeyValidator> _appLogger;
  private string? _errorMessage;

  public KeyVaultApiKeyValidator(
      IKeyVaultService keyVaultService,
      string environment,
      IAppInsightsLogger<KeyVaultApiKeyValidator> appLogger,
      bool? enforceGet = false)
  {
    _keyVaultService = keyVaultService ?? throw new ArgumentNullException(nameof(keyVaultService));
    _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    _appLogger = appLogger ?? throw new ArgumentNullException(nameof(appLogger));
    _enforceGet = enforceGet ?? false;
  }

  public bool IsValid(string? apiKey, HttpRequestData req)
  {
    // This method is not async, so we can't use it for Key Vault validation
    // We'll need to use ValidateOrThrowAsync or ValidateApiKeyAsync instead
    throw new NotSupportedException("Use ValidateOrThrowAsync or ValidateApiKeyAsync for Key Vault validation");
  }

  public async Task ValidateOrThrowAsync(HttpRequestData req)
  {
    var apiKey = req.Headers.TryGetValues("x-api-key", out var apiKeyValues) ? apiKeyValues.FirstOrDefault() : null;

    if (string.IsNullOrWhiteSpace(apiKey))
    {
      _errorMessage = "API key cannot be null or empty.";
      _appLogger.LogError($"API key validation failed: {_errorMessage}", new Exception(_errorMessage));
      throw new UnauthorizedAccessException(_errorMessage);
    }

    // Get the expected API key from Key Vault based on environment
    var secretName = GetSecretNameForEnvironment(_environment);
    var validApiKey = await _keyVaultService.GetSecretAsync(secretName);

    // Check if the API key matches the expected valid key
    if (!string.Equals(apiKey, validApiKey, StringComparison.Ordinal))
    {
      _errorMessage = "Invalid API key.";
      _appLogger.LogError($"API key validation failed: {_errorMessage}", new Exception(_errorMessage));
      throw new UnauthorizedAccessException(_errorMessage);
    }

    // If _enforceGet is true, only allow GET requests
    if (_enforceGet && !req.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
    {
      _errorMessage = "Only GET requests are allowed.";
      _appLogger.LogError($"API key validation failed: {_errorMessage}", new Exception(_errorMessage));
      throw new UnauthorizedAccessException(_errorMessage);
    }

    // API key is valid and request method is allowed
    _errorMessage = null;
    _appLogger.LogInformation("API key validation successful for environment: {Environment}", _environment);
  }

  public bool TryValidateHeader(HttpRequestData req, out HttpResponseData? unauthorizedResponse)
  {
    // This is the synchronous version - we need to make it async for Key Vault
    throw new NotSupportedException("Use ValidateApiKeyAsync for Key Vault validation");
  }

  public async Task<HttpResponseData?> ValidateApiKeyAsync(HttpRequestData req, object logger, string functionName)
  {
    try
    {
      await ValidateOrThrowAsync(req);

      // Use reflection to log success if logger has LogInformation method
      var loggerType = logger.GetType();
      var logMethod = loggerType.GetMethod("LogInformation", new[] { typeof(string), typeof(object[]) });
      logMethod?.Invoke(logger, new object[] { "API key validation successful for {FunctionName}", new object[] { functionName } });

      return null; // Validation successful
    }
    catch (UnauthorizedAccessException ex)
    {
      // Use reflection to log error if logger has LogError method
      var loggerType = logger.GetType();
      var logMethod = loggerType.GetMethod("LogError", new[] { typeof(string), typeof(Exception), typeof(object[]) });
      logMethod?.Invoke(logger, new object[] { "Unauthorized access attempt in {FunctionName}: {Message}", ex, new object[] { functionName, ex.Message } });

      // Create standardized error response
      var errorResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
      errorResponse.Headers.Add("Content-Type", "application/json; charset=utf-8");

      var errorObject = new { error = "Unauthorized access due to invalid API key." };
      var jsonOptions = new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
      };

      await errorResponse.WriteStringAsync(JsonSerializer.Serialize(errorObject, jsonOptions));
      return errorResponse;
    }
    catch (Exception ex)
    {
      // Handle Key Vault or other errors
      _appLogger.LogError($"Error during API key validation: {ex.Message}", ex);

      var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
      errorResponse.Headers.Add("Content-Type", "application/json; charset=utf-8");

      var errorObject = new { error = "Internal server error during authentication." };
      var jsonOptions = new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
      };

      await errorResponse.WriteStringAsync(JsonSerializer.Serialize(errorObject, jsonOptions));
      return errorResponse;
    }
  }

  public string? GetErrorMessage()
  {
    return _errorMessage;
  }

  /// <summary>
  /// Maps environment to the corresponding Key Vault secret name
  /// </summary>
  /// <param name="environment">The environment (develop, test, production)</param>
  /// <returns>The secret name in Key Vault</returns>
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
