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

    Task ValidateOrThrowAsync(HttpRequestData req);

    /// <summary>
    /// Validates the API key and returns an appropriate HTTP response if validation fails.
    /// This method provides a standardized way to handle API key validation across all functions.
    /// </summary>
    /// <param name="req">The HTTP request data</param>
    /// <param name="logger">The logger instance for the calling function</param>
    /// <param name="functionName">The name of the function for logging purposes</param>
    /// <returns>An HTTP response with 401 Unauthorized if validation fails, or null if validation succeeds</returns>
    Task<HttpResponseData?> ValidateApiKeyAsync(HttpRequestData req, object logger, string functionName);

    string? GetErrorMessage();
}

/// <summary>
/// Result of async API key validation
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public HttpResponseData? ErrorResponse { get; set; }

    public static ValidationResult Success() => new() { IsValid = true };
    public static ValidationResult Failure(HttpResponseData errorResponse) => new() { IsValid = false, ErrorResponse = errorResponse };
}