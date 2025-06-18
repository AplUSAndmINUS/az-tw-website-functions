namespace Utils.Validation;

public class ApiKeyValidator : IAPIKeyValidator
{
    private readonly string _validApiKey;
    private string? _errorMessage;

    public ApiKeyValidator(string validApiKey)
    {
        _validApiKey = validApiKey;
    }

    public bool IsValid(string apiKey)
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