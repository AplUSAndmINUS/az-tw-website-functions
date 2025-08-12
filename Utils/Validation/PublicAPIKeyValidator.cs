using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using Utils;

namespace Utils.Validation;

/// <summary>
/// API Key validator for public endpoints that allows GET requests without API keys
/// while still protecting write operations (POST, PUT, DELETE) with API key validation
/// </summary>
public class PublicAPIKeyValidator : IAPIKeyValidator
{
    private readonly IAPIKeyValidator _baseValidator;
    private readonly IAppInsightsLogger<PublicAPIKeyValidator> _appLogger;
    private string? _errorMessage;

    public PublicAPIKeyValidator(
        IAPIKeyValidator baseValidator,
        IAppInsightsLogger<PublicAPIKeyValidator> appLogger)
    {
        _baseValidator = baseValidator ?? throw new ArgumentNullException(nameof(baseValidator));
        _appLogger = appLogger ?? throw new ArgumentNullException(nameof(appLogger));
    }

    public bool IsValid(string? apiKey, HttpRequestData req)
    {
        // Allow GET requests without API key validation for public read access
        if (req.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            _appLogger.LogInformation("Allowing GET request without API key validation for public endpoint");
            _errorMessage = null;
            return true;
        }

        // For non-GET requests, delegate to the base validator
        return _baseValidator.IsValid(apiKey, req);
    }

    public async Task ValidateOrThrowAsync(HttpRequestData req)
    {
        // Allow GET requests without API key validation for public read access
        if (req.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            _appLogger.LogInformation("Allowing GET request without API key validation for public endpoint");
            _errorMessage = null;
            return;
        }

        // For non-GET requests, delegate to the base validator
        await _baseValidator.ValidateOrThrowAsync(req);
    }

    public bool TryValidateHeader(HttpRequestData req, out HttpResponseData? unauthorizedResponse)
    {
        unauthorizedResponse = null;

        // Allow GET requests without API key validation for public read access
        if (req.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            _appLogger.LogInformation("Allowing GET request without API key validation for public endpoint");
            _errorMessage = null;
            return true;
        }

        // For non-GET requests, delegate to the base validator
        return _baseValidator.TryValidateHeader(req, out unauthorizedResponse);
    }

    public async Task<HttpResponseData?> ValidateApiKeyAsync(HttpRequestData req, object logger, string functionName)
    {
        // Allow GET requests without API key validation for public read access
        if (req.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            _appLogger.LogInformation("Allowing GET request without API key validation for public endpoint: {FunctionName}", functionName);
            
            // Use reflection to log success if logger has LogInformation method
            var loggerType = logger.GetType();
            var logMethod = loggerType.GetMethod("LogInformation", new[] { typeof(string), typeof(object[]) });
            logMethod?.Invoke(logger, new object[] { "API key validation bypassed for public GET request in {FunctionName}", new object[] { functionName } });

            _errorMessage = null;
            return null; // Validation successful (bypassed)
        }

        // For non-GET requests, delegate to the base validator
        return await _baseValidator.ValidateApiKeyAsync(req, logger, functionName);
    }

    public string? GetErrorMessage()
    {
        return _errorMessage ?? _baseValidator.GetErrorMessage();
    }
}