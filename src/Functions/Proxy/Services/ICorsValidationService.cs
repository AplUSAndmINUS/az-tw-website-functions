using Microsoft.Azure.Functions.Worker.Http;

namespace Functions.Proxy.Services;

/// <summary>
/// Service for validating CORS and request origins
/// </summary>
public interface ICorsValidationService
{
    /// <summary>
    /// Validates if the request is allowed based on CORS policy and origin
    /// </summary>
    /// <param name="request">The HTTP request to validate</param>
    /// <returns>True if the request is allowed; otherwise, false</returns>
    Task<bool> IsRequestAllowedAsync(HttpRequestData request);

    /// <summary>
    /// Gets the error message for a rejected request
    /// </summary>
    /// <returns>The error message for logging purposes</returns>
    string GetRejectionReason();

    /// <summary>
    /// Applies appropriate CORS headers to the response
    /// </summary>
    /// <param name="response">The HTTP response to modify</param>
    /// <param name="request">The original HTTP request</param>
    void ApplyCorsHeaders(HttpResponseData response, HttpRequestData request);
}