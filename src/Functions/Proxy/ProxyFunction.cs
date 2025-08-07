using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using Functions.Proxy.Services;
using Functions.Proxy.Models;
using Utils;
using Utils.Services;

namespace Functions.Proxy;

/// <summary>
/// Azure Function Proxy for secure Key Vault integration
/// Acts as a reverse proxy that retrieves API keys from Key Vault and forwards requests to target functions
/// </summary>
public class ProxyFunction
{
    private readonly IAppInsightsLogger<ProxyFunction> _logger;
    private readonly IKeyVaultService _keyVaultService;
    private readonly IRequestForwardingService _forwardingService;
    private readonly ICorsValidationService _corsValidationService;
    private readonly ProxyConfiguration _config;
    
    // Simple in-memory cache for API keys (in production, consider using IMemoryCache)
    private static readonly Dictionary<string, (string apiKey, DateTime expiry)> _apiKeyCache = new();
    private static readonly object _cacheLock = new();

    public ProxyFunction(
        IAppInsightsLogger<ProxyFunction> logger,
        IKeyVaultService keyVaultService,
        IRequestForwardingService forwardingService,
        ICorsValidationService corsValidationService,
        ProxyConfiguration config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _keyVaultService = keyVaultService ?? throw new ArgumentNullException(nameof(keyVaultService));
        _forwardingService = forwardingService ?? throw new ArgumentNullException(nameof(forwardingService));
        _corsValidationService = corsValidationService ?? throw new ArgumentNullException(nameof(corsValidationService));
        _config = config ?? throw new ArgumentNullException(nameof(config));

        _logger.LogInformation("ProxyFunction initialized");
    }

    /// <summary>
    /// Main proxy endpoint that handles all requests and forwards them to target functions
    /// Route: /proxy/{*route} - captures all requests under /proxy/
    /// </summary>
    [Function("ProxyFunction")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "delete", "options", Route = "proxy/{*route}")] 
        HttpRequestData req,
        string route,
        FunctionContext executionContext)
    {
        var requestId = Guid.NewGuid().ToString("N")[..8];
        _logger.LogInformation("[{RequestId}] Proxy request received: {Method} /proxy/{Route}", requestId, req.Method, route);

        try
        {
            // Handle CORS preflight requests
            if (req.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                return await HandleOptionsRequestAsync(req, requestId);
            }

            // Log request metadata for auditing
            await LogRequestMetadataAsync(req, route, requestId);

            // Validate CORS and request origin
            var corsValidationResult = await ValidateRequestAsync(req, requestId);
            if (!corsValidationResult.IsValid)
            {
                return CreateErrorResponse(req, corsValidationResult.ErrorMessage, HttpStatusCode.Forbidden, requestId);
            }

            // Get API key from Key Vault (with caching)
            var apiKey = await GetApiKeyAsync(requestId);
            if (string.IsNullOrEmpty(apiKey))
            {
                return CreateErrorResponse(req, "Unable to retrieve API key", HttpStatusCode.InternalServerError, requestId);
            }

            // Parse target path from route
            var targetPath = ParseTargetPath(route);
            if (string.IsNullOrEmpty(targetPath))
            {
                return CreateErrorResponse(req, "Invalid target path", HttpStatusCode.BadRequest, requestId);
            }

            // Forward request to target function with injected API key
            var response = await _forwardingService.ForwardRequestAsync(req, targetPath, apiKey);

            // Apply CORS headers to response
            _corsValidationService.ApplyCorsHeaders(response, req);

            _logger.LogInformation("[{RequestId}] Proxy request completed successfully", requestId);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError("[{RequestId}] Proxy request failed: {Message}", ex, requestId, ex.Message);
            return CreateErrorResponse(req, "Internal proxy error", HttpStatusCode.InternalServerError, requestId);
        }
    }

    private async Task<HttpResponseData> HandleOptionsRequestAsync(HttpRequestData req, string requestId)
    {
        _logger.LogInformation("[{RequestId}] Handling CORS preflight request", requestId);

        var response = req.CreateResponse(HttpStatusCode.OK);
        _corsValidationService.ApplyCorsHeaders(response, req);

        return await Task.FromResult(response);
    }

    private async Task LogRequestMetadataAsync(HttpRequestData req, string route, string requestId)
    {
        try
        {
            var clientIp = GetClientIpAddress(req);
            var userAgent = req.Headers.TryGetValues("User-Agent", out var userAgentValues) 
                ? userAgentValues.FirstOrDefault() 
                : "Unknown";
            var origin = req.Headers.TryGetValues("Origin", out var originValues) 
                ? originValues.FirstOrDefault() 
                : "Unknown";

            _logger.LogInformation(
                "[{RequestId}] Request metadata - IP: {ClientIp}, Origin: {Origin}, UserAgent: {UserAgent}, Route: {Route}",
                requestId, clientIp ?? "Unknown", origin ?? "Unknown", userAgent ?? "Unknown", route ?? "Unknown");

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("[{RequestId}] Error logging request metadata: {Message}", requestId, ex.Message);
        }
    }

    private async Task<ProxyValidationResult> ValidateRequestAsync(HttpRequestData req, string requestId)
    {
        try
        {
            var isAllowed = await _corsValidationService.IsRequestAllowedAsync(req);
            if (!isAllowed)
            {
                var reason = _corsValidationService.GetRejectionReason();
                _logger.LogWarning("[{RequestId}] Request validation failed: {Reason}", requestId, reason);
                return ProxyValidationResult.Failure(reason, "CORS_VALIDATION_FAILED");
            }

            return ProxyValidationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError("[{RequestId}] Error during request validation: {Message}", ex, requestId, ex.Message);
            return ProxyValidationResult.Failure("Validation error", "VALIDATION_ERROR");
        }
    }

    private async Task<string> GetApiKeyAsync(string requestId)
    {
        try
        {
            // Check cache first
            lock (_cacheLock)
            {
                if (_apiKeyCache.TryGetValue(_config.ApiKeySecretName, out var cachedEntry))
                {
                    if (DateTime.UtcNow < cachedEntry.expiry)
                    {
                        _logger.LogInformation("[{RequestId}] Using cached API key", requestId);
                        return cachedEntry.apiKey;
                    }
                    else
                    {
                        _apiKeyCache.Remove(_config.ApiKeySecretName);
                    }
                }
            }

            // Retrieve from Key Vault
            _logger.LogInformation("[{RequestId}] Retrieving API key from Key Vault: {SecretName}", requestId, _config.ApiKeySecretName);
            var apiKey = await _keyVaultService.GetSecretAsync(_config.ApiKeySecretName);

            // Cache the API key
            lock (_cacheLock)
            {
                var expiry = DateTime.UtcNow.AddMinutes(_config.ApiKeyCacheDurationMinutes);
                _apiKeyCache[_config.ApiKeySecretName] = (apiKey, expiry);
            }

            _logger.LogInformation("[{RequestId}] API key retrieved and cached successfully", requestId);
            return apiKey;
        }
        catch (Exception ex)
        {
            _logger.LogError("[{RequestId}] Error retrieving API key: {Message}", ex, requestId, ex.Message);
            return string.Empty;
        }
    }

    private string ParseTargetPath(string route)
    {
        if (string.IsNullOrWhiteSpace(route))
        {
            return string.Empty;
        }

        // Remove leading/trailing slashes and return the path
        // Input: "authors/some-slug" -> Output: "authors/some-slug"
        // Input: "posts" -> Output: "posts"
        return route.Trim('/');
    }

    private string GetClientIpAddress(HttpRequestData req)
    {
        // Check X-Forwarded-For header first (for requests through proxy/load balancer)
        if (req.Headers.TryGetValues("X-Forwarded-For", out var forwardedValues))
        {
            var forwarded = forwardedValues.FirstOrDefault();
            if (!string.IsNullOrEmpty(forwarded))
            {
                return forwarded.Split(',')[0].Trim();
            }
        }

        // Check X-Real-IP header
        if (req.Headers.TryGetValues("X-Real-IP", out var realIpValues))
        {
            var realIp = realIpValues.FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp.Trim();
            }
        }

        return "Unknown";
    }

    private HttpResponseData CreateErrorResponse(HttpRequestData req, string message, HttpStatusCode statusCode, string requestId)
    {
        var response = req.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");

        // Apply CORS headers even for error responses
        _corsValidationService.ApplyCorsHeaders(response, req);

        var errorJson = $$"""
            {
                "error": "{{message}}",
                "requestId": "{{requestId}}",
                "timestamp": "{{DateTimeOffset.UtcNow:O}}"
            }
            """;

        response.WriteString(errorJson);
        return response;
    }
}