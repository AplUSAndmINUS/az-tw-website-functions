using Microsoft.Azure.Functions.Worker.Http;

namespace SharedStorage.Services;

/// <summary>
/// Result of IP throttling check
/// </summary>
public record ThrottleResult(bool IsThrottled, int RequestCount, string? Reason = null);

/// <summary>
/// Service for IP-based request throttling using Azure Table Storage
/// </summary>
public interface IIPThrottlingService
{
    /// <summary>
    /// Checks if the request should be throttled based on IP address and recent request history
    /// </summary>
    /// <param name="request">The HTTP request to analyze</param>
    /// <param name="endpoint">The endpoint being accessed</param>
    /// <returns>Throttle result indicating if request should be blocked</returns>
    Task<ThrottleResult> ShouldThrottleAsync(HttpRequestData request, string endpoint);

    /// <summary>
    /// Logs the current request for throttling analysis
    /// </summary>
    /// <param name="request">The HTTP request to log</param>
    /// <param name="endpoint">The endpoint being accessed</param>
    /// <returns>Task representing the async operation</returns>
    Task LogRequestAsync(HttpRequestData request, string endpoint);

    /// <summary>
    /// Extracts the client IP address from the HTTP request
    /// </summary>
    /// <param name="request">The HTTP request</param>
    /// <returns>The client IP address</returns>
    string ExtractClientIP(HttpRequestData request);
}