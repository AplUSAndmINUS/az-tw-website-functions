using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Functions.Proxy.Models;
using Utils;
using System.Net;

namespace Functions.Proxy.Services;

/// <summary>
/// Service for validating CORS and request origins
/// </summary>
public class CorsValidationService : ICorsValidationService
{
    private readonly ProxyConfiguration _config;
    private readonly IAppInsightsLogger<CorsValidationService> _logger;
    private string _rejectionReason = string.Empty;

    public CorsValidationService(ProxyConfiguration config, IAppInsightsLogger<CorsValidationService> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> IsRequestAllowedAsync(HttpRequestData request)
    {
        try
        {
            // Validate Origin header for CORS
            if (!await ValidateOriginAsync(request))
            {
                return false;
            }

            // Validate IP address if IP filtering is configured
            if (!await ValidateIpAddressAsync(request))
            {
                return false;
            }

            _rejectionReason = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error during CORS validation: {Message}", ex, ex.Message);
            _rejectionReason = "Internal validation error";
            return false;
        }
    }

    public string GetRejectionReason()
    {
        return _rejectionReason;
    }

    public void ApplyCorsHeaders(HttpResponseData response, HttpRequestData request)
    {
        try
        {
            // Get origin from request
            var origin = GetOriginFromRequest(request);

            // Set CORS headers
            if (!string.IsNullOrEmpty(origin) && IsOriginAllowed(origin))
            {
                response.Headers.Add("Access-Control-Allow-Origin", origin);
            }
            else if (_config.AllowedOrigins.Contains("*"))
            {
                response.Headers.Add("Access-Control-Allow-Origin", "*");
            }

            response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization, X-Forwarded-For, X-Real-IP");
            response.Headers.Add("Access-Control-Max-Age", "3600");

            _logger.LogInformation("Applied CORS headers for origin: {Origin}", origin);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Error applying CORS headers: {Message}", ex, ex.Message);
        }
    }

    private async Task<bool> ValidateOriginAsync(HttpRequestData request)
    {
        await Task.CompletedTask; // Make method async for future expansion

        var origin = GetOriginFromRequest(request);

        // If no allowed origins configured, allow all
        if (_config.AllowedOrigins.Count == 0)
        {
            _logger.LogInformation("No CORS origins configured, allowing all origins");
            return true;
        }

        // Check if origin is in allowed list
        if (IsOriginAllowed(origin))
        {
            _logger.LogInformation("Origin allowed: {Origin}", origin);
            return true;
        }

        _rejectionReason = $"Origin not allowed: {origin}";
        _logger.LogWarning("CORS validation failed for origin: {Origin}", origin);
        return false;
    }

    private async Task<bool> ValidateIpAddressAsync(HttpRequestData request)
    {
        await Task.CompletedTask; // Make method async for future expansion

        // If no IP filtering configured, allow all
        if (_config.AllowedIpRanges.Count == 0)
        {
            _logger.LogInformation("No IP filtering configured, allowing all IPs");
            return true;
        }

        var clientIp = GetClientIpAddress(request);
        if (string.IsNullOrEmpty(clientIp))
        {
            _rejectionReason = "Unable to determine client IP address";
            _logger.LogWarning("Unable to determine client IP address for request");
            return false;
        }

        // Check if IP is in allowed ranges
        if (IsIpAddressAllowed(clientIp))
        {
            _logger.LogInformation("IP address allowed: {ClientIp}", clientIp);
            return true;
        }

        _rejectionReason = $"IP address not allowed: {clientIp}";
        _logger.LogWarning("IP validation failed for address: {ClientIp}", clientIp);
        return false;
    }

    private string GetOriginFromRequest(HttpRequestData request)
    {
        if (request.Headers.TryGetValues("Origin", out var originValues))
        {
            return originValues.FirstOrDefault() ?? string.Empty;
        }

        if (request.Headers.TryGetValues("Referer", out var refererValues))
        {
            var referer = refererValues.FirstOrDefault();
            if (!string.IsNullOrEmpty(referer) && Uri.TryCreate(referer, UriKind.Absolute, out var uri))
            {
                return $"{uri.Scheme}://{uri.Host}{(uri.IsDefaultPort ? "" : $":{uri.Port}")}";
            }
        }

        return string.Empty;
    }

    private bool IsOriginAllowed(string origin)
    {
        if (string.IsNullOrEmpty(origin))
        {
            return _config.AllowedOrigins.Contains("*");
        }

        return _config.AllowedOrigins.Contains("*") ||
               _config.AllowedOrigins.Contains(origin) ||
               _config.AllowedOrigins.Any(allowed => 
                   allowed.EndsWith("*") && origin.StartsWith(allowed.TrimEnd('*')));
    }

    private string GetClientIpAddress(HttpRequestData request)
    {
        // Check X-Forwarded-For header first (for requests through proxy/load balancer)
        if (request.Headers.TryGetValues("X-Forwarded-For", out var forwardedValues))
        {
            var forwarded = forwardedValues.FirstOrDefault();
            if (!string.IsNullOrEmpty(forwarded))
            {
                // X-Forwarded-For can contain multiple IPs, take the first one
                return forwarded.Split(',')[0].Trim();
            }
        }

        // Check X-Real-IP header
        if (request.Headers.TryGetValues("X-Real-IP", out var realIpValues))
        {
            var realIp = realIpValues.FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp.Trim();
            }
        }

        // Fallback to request's remote IP if available
        // Note: In Azure Functions, this might not be available depending on hosting model
        return string.Empty;
    }

    private bool IsIpAddressAllowed(string ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress))
        {
            return false;
        }

        // Simple implementation - in production, this should support CIDR notation
        return _config.AllowedIpRanges.Contains("*") ||
               _config.AllowedIpRanges.Contains(ipAddress);
    }
}