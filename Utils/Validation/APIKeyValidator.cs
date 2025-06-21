using Utils;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;

namespace Utils.Validation;

public class ApiKeyValidator : IAPIKeyValidator
{
    private readonly string _validApiKey;
    private string? _errorMessage;
    private readonly IAppInsightsLogger<ApiKeyValidator> _appLogger;

    public ApiKeyValidator(string validApiKey, IAppInsightsLogger<ApiKeyValidator> appLogger)
    {
        _validApiKey = validApiKey;
        _appLogger = appLogger;
    }

    public bool IsValid(string? apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _errorMessage = "API key cannot be null or empty.";
            return false;
        }

        if (!string.Equals(apiKey, _validApiKey, StringComparison.Ordinal) || (apiKey != _validApiKey && apiKey.Length < 32))
        {
            // Check if the API key is not null, empty, or too short
            // Assuming a valid API key should be at least 32 characters long
            _errorMessage = "Invalid API key.";
            return false;
        }

        _errorMessage = null;
        return true;
    }

    public bool TryValidateHeader(HttpRequestData req, out HttpResponseData? unauthorizedResponse)
    {
        unauthorizedResponse = null;
        IsValid(req.Headers.TryGetValues("x-api-key", out var apiKeyValues) ? apiKeyValues.FirstOrDefault() : null);

        if (_errorMessage != null)
        {
            unauthorizedResponse = req.CreateResponse(System.Net.HttpStatusCode.Unauthorized);
            unauthorizedResponse.Headers.Add("Content-Type", "application/json");
            unauthorizedResponse.WriteString($"{{\"error\": \"{_errorMessage}\"}}");

            // Log the unauthorized access attempt
            _appLogger.LogError($"API key validation failed: {_errorMessage}", new Exception(_errorMessage));
            return false;
        }

        return true;
    }

    public string? GetErrorMessage()
    {
        return _errorMessage;
    }
}

/* Usage example:
  var apiKeyValidator = new ApiKeyValidator("your-valid-api-key");
  if (!apiKeyValidator.IsValid(requestApiKey))
  {
      return new BadRequestObjectResult(apiKeyValidator.GetErrorMessage());
  }
*/