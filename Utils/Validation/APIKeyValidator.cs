using Utils;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using System.Text.Json;

namespace Utils.Validation;

public class ApiKeyValidator : IAPIKeyValidator
{
    private readonly string _validApiKey;
    private readonly bool _enforceGet;
    private string? _errorMessage;
    private readonly IAppInsightsLogger<ApiKeyValidator> _appLogger;

    public ApiKeyValidator(string validApiKey, IAppInsightsLogger<ApiKeyValidator> appLogger, bool? enforceGet = false)
    {
        _enforceGet = enforceGet ?? false;
        _validApiKey = validApiKey;
        _appLogger = appLogger;
    }

    public bool IsValid(string? apiKey, HttpRequestData req)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _errorMessage = "API key cannot be null or empty.";
            return false;
        }

        // Check if the API key matches the expected valid key
        if (!string.Equals(apiKey, _validApiKey, StringComparison.Ordinal))
        {
            _errorMessage = "Invalid API key.";
            return false;
        }

        // If _enforceGet is true, only allow GET requests
        if (_enforceGet && !req.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            _errorMessage = "Only GET requests are allowed.";
            return false;
        }

        // API key is valid and request method is allowed
        _errorMessage = null;
        return true;
    }

    public Task ValidateOrThrowAsync(HttpRequestData req)
    {
        var apiKey = req.Headers.TryGetValues("x-api-key", out var apiKeyValues) ? apiKeyValues.FirstOrDefault() : null;
        if (!IsValid(apiKey, req))
        {
            _appLogger.LogError($"API key validation failed: {_errorMessage}", new Exception(_errorMessage));
            throw new UnauthorizedAccessException(_errorMessage ?? "Unauthorized access due to invalid API key.");
        }
        return Task.CompletedTask;
    }

    public bool TryValidateHeader(HttpRequestData req, out HttpResponseData? unauthorizedResponse)
    {
        unauthorizedResponse = null;
        var apiKey = req.Headers.TryGetValues("x-api-key", out var apiKeyValues) ? apiKeyValues.FirstOrDefault() : null;

        if (!IsValid(apiKey, req))
        {
            unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
            unauthorizedResponse.Headers.Add("Content-Type", "application/json");
            unauthorizedResponse.WriteString($"{{\"error\": \"{_errorMessage}\"}}");

            // Log the unauthorized access attempt
            _appLogger.LogError($"API key validation failed: {_errorMessage}", new Exception(_errorMessage));
            return false;
        }

        return true;
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

            // Create standardized error response matching GetAuthor pattern
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
    }

    public string? GetErrorMessage()
    {
        return _errorMessage;
    }
}