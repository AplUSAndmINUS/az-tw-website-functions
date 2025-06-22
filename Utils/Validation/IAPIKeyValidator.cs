using Microsoft.Azure.Functions.Worker.Http;

namespace Utils.Validation;

public interface IAPIKeyValidator
{
    /// <summary>
    /// Validates the provided API key.
    /// </summary>
    /// <param name="apiKey">The API key to validate.</param>
    /// <returns>True if the API key is valid; otherwise, false.</returns>
    bool IsValid(string apiKey, HttpRequestData req);

    /// <summary>
    /// Gets the error message if the API key is invalid.
    /// </summary>
    /// <returns>The error message or null if the API key is valid.</returns>

    bool TryValidateHeader(HttpRequestData req, out HttpResponseData? unauthorizedResponse);
    string? GetErrorMessage();
}