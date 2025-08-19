using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.ApplicationInsights;
using SharedStorage.Models;
using SharedStorage.Services.BaseServices;
using Utils;
using System.Net;

namespace SharedStorage.Services;

/// <summary>
/// Implementation of IP-based request throttling using Azure Table Storage
/// Tracks requests by IP address and enforces rate limits to prevent abuse
/// </summary>
public class IPThrottlingService : IIPThrottlingService
{
    private const string TABLE_NAME = "IPTelemetry";
    private const int THROTTLE_WINDOW_MINUTES = 2;
    private const int MAX_REQUESTS_PER_WINDOW = 100;

    private readonly ITableStorageService _tableStorage;
    private readonly IAppInsightsLogger<IPThrottlingService> _logger;
    private readonly TelemetryClient? _telemetryClient;

    public IPThrottlingService(ITableStorageService tableStorage, IAppInsightsLogger<IPThrottlingService> logger, TelemetryClient? telemetryClient = null)
    {
        _tableStorage = tableStorage ?? throw new ArgumentNullException(nameof(tableStorage));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _telemetryClient = telemetryClient; // Allow null for local development
    }

    /// <summary>
    /// Checks if the request should be throttled based on IP address and recent request history
    /// </summary>
    public async Task<ThrottleResult> ShouldThrottleAsync(HttpRequestData request, string endpoint)
    {
        var clientIP = ExtractClientIP(request);
        var cutoffTime = DateTime.UtcNow.AddMinutes(-THROTTLE_WINDOW_MINUTES);

        _logger.LogInformation("Checking throttle status for IP {ClientIP} on endpoint {Endpoint}", clientIP, endpoint);

        try
        {
            // Query recent requests from this IP in the last THROTTLE_WINDOW_MINUTES
            var filter = $"PartitionKey eq '{clientIP}' and RequestTimestamp ge datetime'{cutoffTime:yyyy-MM-ddTHH:mm:ssZ}'";
            var recentRequests = await _tableStorage.GetEntitiesAsync(TABLE_NAME, filter, pageSize: MAX_REQUESTS_PER_WINDOW + 1);

            var requestCount = recentRequests.TotalCount;

            _logger.LogInformation("Found {RequestCount} recent requests for IP {ClientIP} in last {WindowMinutes} minutes", 
                requestCount, clientIP, THROTTLE_WINDOW_MINUTES);

            if (requestCount >= MAX_REQUESTS_PER_WINDOW)
            {
                var reason = $"IP {clientIP} exceeded {MAX_REQUESTS_PER_WINDOW} requests in {THROTTLE_WINDOW_MINUTES} minutes";
                
                // Log throttle breach for monitoring
                _logger.LogWarning("IP throttle breach detected: {Reason}. Endpoint: {Endpoint}, Count: {RequestCount}", 
                    reason, endpoint, requestCount);

                // Track throttle breach event for dashboard monitoring
                _telemetryClient?.TrackEvent("ip_throttle_breach", new Dictionary<string, string>
                {
                    ["ip"] = clientIP,
                    ["endpoint"] = endpoint,
                    ["count"] = requestCount.ToString(),
                    ["timestamp"] = DateTime.UtcNow.ToString("o"),
                    ["window_minutes"] = THROTTLE_WINDOW_MINUTES.ToString(),
                    ["limit"] = MAX_REQUESTS_PER_WINDOW.ToString()
                });

                return new ThrottleResult(true, requestCount, reason);
            }

            return new ThrottleResult(false, requestCount);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error checking throttle status for IP {ClientIP}: {Error}", ex, clientIP, ex.Message);
            
            // In case of error, allow the request through (fail open)
            // but log the incident for investigation
            return new ThrottleResult(false, 0, $"Throttle check failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Logs the current request for throttling analysis
    /// </summary>
    public async Task LogRequestAsync(HttpRequestData request, string endpoint)
    {
        var clientIP = ExtractClientIP(request);
        var requestId = Guid.NewGuid().ToString();
        
        var referer = request.Headers.TryGetValues("Referer", out var refererValues) 
            ? refererValues.FirstOrDefault() 
            : null;
        
        var userAgent = request.Headers.TryGetValues("User-Agent", out var userAgentValues) 
            ? userAgentValues.FirstOrDefault() 
            : null;

        var telemetryEntity = new IPTelemetryEntity(
            clientIP, 
            requestId, 
            endpoint, 
            request.Method, 
            referer, 
            userAgent);

        try
        {
            await _tableStorage.UpsertEntityAsync(TABLE_NAME, telemetryEntity);
            
            _logger.LogInformation("Logged request from IP {ClientIP} to endpoint {Endpoint} with ID {RequestId}", 
                clientIP, endpoint, requestId);

            // Track request for telemetry
            _telemetryClient?.TrackEvent("ip_throttle_request_logged", new Dictionary<string, string>
            {
                ["ip"] = clientIP,
                ["endpoint"] = endpoint,
                ["method"] = request.Method,
                ["request_id"] = requestId,
                ["has_referer"] = (!string.IsNullOrEmpty(referer)).ToString(),
                ["has_user_agent"] = (!string.IsNullOrEmpty(userAgent)).ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to log request from IP {ClientIP} to endpoint {Endpoint}: {Error}", 
                ex, clientIP, endpoint, ex.Message);
            
            // Don't throw - logging failure shouldn't block the request
        }
    }

    /// <summary>
    /// Extracts the client IP address from the HTTP request
    /// Prioritizes x-forwarded-for header, falls back to connection remote address
    /// </summary>
    public string ExtractClientIP(HttpRequestData request)
    {
        // Check x-forwarded-for header first (Azure Functions behind load balancer/proxy)
        if (request.Headers.TryGetValues("x-forwarded-for", out var forwardedValues))
        {
            var forwardedFor = forwardedValues.FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                // x-forwarded-for can contain multiple IPs (client, proxy1, proxy2, etc.)
                // The first IP is typically the original client
                var firstIP = forwardedFor.Split(',')[0].Trim();
                if (IPAddress.TryParse(firstIP, out _))
                {
                    _logger.LogInformation("Extracted IP from x-forwarded-for header: {ClientIP}", firstIP);
                    return firstIP;
                }
            }
        }

        // Fallback to X-Forwarded-For (capital case)
        if (request.Headers.TryGetValues("X-Forwarded-For", out var capitalForwardedValues))
        {
            var forwardedFor = capitalForwardedValues.FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                var firstIP = forwardedFor.Split(',')[0].Trim();
                if (IPAddress.TryParse(firstIP, out _))
                {
                    _logger.LogInformation("Extracted IP from X-Forwarded-For header: {ClientIP}", firstIP);
                    return firstIP;
                }
            }
        }

        // Check for other common proxy headers
        var proxyHeaders = new[] { "X-Real-IP", "X-Client-IP", "CF-Connecting-IP" };
        foreach (var header in proxyHeaders)
        {
            if (request.Headers.TryGetValues(header, out var headerValues))
            {
                var headerValue = headerValues.FirstOrDefault();
                if (!string.IsNullOrEmpty(headerValue) && IPAddress.TryParse(headerValue.Trim(), out _))
                {
                    _logger.LogInformation("Extracted IP from {Header} header: {ClientIP}", header, headerValue.Trim());
                    return headerValue.Trim();
                }
            }
        }

        // Fallback to a default value if we can't determine the IP
        // In Azure Functions, this information might not be directly available
        var fallbackIP = "unknown";
        _logger.LogWarning("Unable to extract client IP from request headers, using fallback: {FallbackIP}", fallbackIP);
        
        return fallbackIP;
    }
}