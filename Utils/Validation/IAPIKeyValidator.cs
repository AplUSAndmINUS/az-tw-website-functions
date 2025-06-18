namespace Utils.Validation;

public interface IAPIKeyValidator
{
    /// <summary>
    /// Validates the provided API key.
    /// </summary>
    /// <param name="apiKey">The API key to validate.</param>
    /// <returns>True if the API key is valid; otherwise, false.</returns>
    bool IsValid(string apiKey);

    /// <summary>
    /// Gets the error message if the API key is invalid.
    /// </summary>
    /// <returns>The error message or null if the API key is valid.</returns>
    string? GetErrorMessage();
}