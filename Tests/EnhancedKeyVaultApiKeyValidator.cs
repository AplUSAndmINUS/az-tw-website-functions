using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Utils.Services;
using Azure.Core;

namespace Utils.Validation;

/// <summary>
/// Enhanced API Key validator with improved error handling and diagnostics
/// </summary>
public class EnhancedKeyVaultApiKeyValidator : IAPIKeyValidator
{
  private readonly IKeyVaultService _keyVaultService;
  private readonly string _environment;
  private readonly bool _enforceGet;
  private readonly IAppInsightsLogger<EnhancedKeyVaultApiKeyValidator> _appLogger;
  private string? _errorMessage;

  public EnhancedKeyVaultApiKeyValidator(
      IKeyVaultService keyVaultService,
      string environment,
      IAppInsightsLogger<EnhancedKeyVaultApiKeyValidator> appLogger,
      bool? enforceGet = false)
  {
    _keyVaultService = keyVaultService ?? throw new ArgumentNullException(nameof(keyVaultService));
    _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    _appLogger = appLogger ?? throw new ArgumentNullException(nameof(appLogger));
    _enforceGet = enforceGet ?? false;
  }

  public bool IsValid(string? apiKey, HttpRequestData req)
  {
    throw new NotSupportedException("Use ValidateOrThrowAsync or ValidateApiKeyAsync for Key Vault validation");
  }

  public async Task ValidateOrThrowAsync(HttpRequestData req)
  {
    var apiKey = req.Headers.TryGetValues("x-api-key", out var apiKeyValues) ? apiKeyValues.FirstOrDefault() : null;

    _appLogger.LogInformation("Starting API key validation for environment: {Environment}", _environment);

    if (string.IsNullOrWhiteSpace(apiKey))
    {
      _errorMessage = "API key cannot be null or empty.";
      _appLogger.LogWarning("API key validation failed: {ErrorMessage}", _errorMessage);
      throw new UnauthorizedAccessException(_errorMessage);
    }

    // Get the expected API key from Key Vault based on environment
    var secretName = GetSecretNameForEnvironment(_environment);
    _appLogger.LogInformation("Attempting to retrieve secret: {SecretName} for environment: {Environment}", secretName, _environment);

    string validApiKey;
    try
    {
      validApiKey = await _keyVaultService.GetSecretAsync(secretName);
      _appLogger.LogInformation("Successfully retrieved secret from Key Vault. Secret length: {Length}", validApiKey?.Length ?? 0);

      if (string.IsNullOrEmpty(validApiKey))
      {
        _errorMessage = $"Retrieved secret '{secretName}' is null or empty";
        _appLogger.LogWarning("Key Vault secret validation failed: {ErrorMessage}", _errorMessage);
        throw new InvalidOperationException(_errorMessage);
      }
    }
    catch (Exception ex)
    {
      _errorMessage = $"Failed to retrieve secret '{secretName}' from Key Vault: {ex.Message}";
      _appLogger.LogError("Key Vault access failed for secret {SecretName}", ex, secretName);
      throw new InvalidOperationException(_errorMessage, ex);
    }

    // Log key comparison (without exposing actual keys)
    _appLogger.LogInformation("Comparing API keys - Request key length: {RequestLength}, Vault key length: {VaultLength}",
        apiKey.Length, validApiKey.Length);

    // Check if the API key matches the expected valid key
    if (!string.Equals(apiKey, validApiKey, StringComparison.Ordinal))
    {
      _errorMessage = "Invalid API key - key does not match expected value.";
      _appLogger.LogWarning("API key validation failed: {ErrorMessage}", _errorMessage);

      // Additional debugging info (without exposing keys)
      _appLogger.LogWarning("Key comparison failed - Request key starts with: {RequestStart}, Vault key starts with: {VaultStart}",
          apiKey.Length > 4 ? apiKey.Substring(0, 4) + "..." : "short",
          validApiKey.Length > 4 ? validApiKey.Substring(0, 4) + "..." : "short");

      throw new UnauthorizedAccessException(_errorMessage);
    }

    // If _enforceGet is true, only allow GET requests
    if (_enforceGet && !req.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
    {
      _errorMessage = "Only GET requests are allowed.";
      _appLogger.LogWarning("API key validation failed: {ErrorMessage}", _errorMessage);
      throw new UnauthorizedAccessException(_errorMessage);
    }

    // API key is valid and request method is allowed
    _errorMessage = null;
    _appLogger.LogInformation("API key validation successful for environment: {Environment}", _environment);
  }

  public bool TryValidateHeader(HttpRequestData req, out HttpResponseData? unauthorizedResponse)
  {
    throw new NotSupportedException("Use ValidateApiKeyAsync for Key Vault validation");
  }

  public async Task<HttpResponseData?> ValidateApiKeyAsync(HttpRequestData req, object logger, string functionName)
  {
    try
    {
      _appLogger.LogInformation("Starting API key validation for function: {FunctionName}", functionName);
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

      // Create detailed error response for debugging
      var errorResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
      errorResponse.Headers.Add("Content-Type", "application/json; charset=utf-8");

      var errorObject = new
      {
        error = "Unauthorized access due to invalid API key.",
        details = ex.Message,
        functionName = functionName,
        environment = _environment,
        timestamp = DateTime.UtcNow
      };

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
      // Handle Key Vault or other errors with detailed logging
      _appLogger.LogError("Error during API key validation for function {FunctionName}: {ErrorMessage}", ex, functionName, ex.Message);

      var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
      errorResponse.Headers.Add("Content-Type", "application/json; charset=utf-8");

      var errorObject = new
      {
        error = "Internal server error during authentication.",
        details = ex.Message,
        errorType = ex.GetType().Name,
        functionName = functionName,
        environment = _environment,
        timestamp = DateTime.UtcNow,
        innerException = ex.InnerException?.Message
      };

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
