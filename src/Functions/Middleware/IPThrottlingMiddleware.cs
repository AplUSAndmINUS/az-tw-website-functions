using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using SharedStorage.Services;
using System.Net;
using System.Text.Json;

namespace Functions.Middleware;

/// <summary>
/// Middleware for IP-based request throttling to prevent abuse and DDoS-style attacks
/// Runs before other middleware and function execution
/// </summary>
public class IPThrottlingMiddleware : IFunctionsWorkerMiddleware
{
    private readonly IIPThrottlingService _throttlingService;
    private readonly ILogger<IPThrottlingMiddleware> _logger;

    public IPThrottlingMiddleware(IIPThrottlingService throttlingService, ILogger<IPThrottlingMiddleware> logger)
    {
        _throttlingService = throttlingService ?? throw new ArgumentNullException(nameof(throttlingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var functionName = context.FunctionDefinition.Name;
        _logger.LogInformation("IPThrottlingMiddleware: Processing function {FunctionName}", functionName);

        // Only process HTTP trigger functions
        var httpRequest = await GetHttpRequestDataAsync(context);
        if (httpRequest == null)
        {
            _logger.LogInformation("IPThrottlingMiddleware: Not an HTTP function, skipping throttling for {FunctionName}", functionName);
            // Not an HTTP function, skip throttling
            await next(context);
            return;
        }

        var endpoint = httpRequest.Url.PathAndQuery;
        var clientIP = _throttlingService.ExtractClientIP(httpRequest);

        _logger.LogInformation("IPThrottlingMiddleware: Processing IP throttling for function {FunctionName}, endpoint {Endpoint}, IP {ClientIP}", 
            functionName, endpoint, clientIP);

        try
        {
            // Check if request should be throttled
            var throttleResult = await _throttlingService.ShouldThrottleAsync(httpRequest, endpoint);

            _logger.LogInformation("IPThrottlingMiddleware: Throttle check result - IsThrottled: {IsThrottled}, RequestCount: {RequestCount}", 
                throttleResult.IsThrottled, throttleResult.RequestCount);

            if (throttleResult.IsThrottled)
            {
                _logger.LogWarning("Request throttled for IP {ClientIP} on endpoint {Endpoint}: {Reason}", 
                    clientIP, endpoint, throttleResult.Reason);

                // Create throttled response
                var response = httpRequest.CreateResponse(HttpStatusCode.TooManyRequests);
                response.Headers.Add("Content-Type", "application/json");
                
                var responseBody = new
                {
                    status = 429,
                    body = "Too Many Requests",
                    message = $"Rate limit exceeded. Try again later.",
                    details = new
                    {
                        ip = clientIP,
                        requestCount = throttleResult.RequestCount,
                        windowMinutes = 2,
                        maxRequests = 100
                    }
                };

                await response.WriteStringAsync(JsonSerializer.Serialize(responseBody));

                // Set the response in the context to short-circuit the pipeline
                var httpContextFeature = context.Features.FirstOrDefault(f => f.Key.Name == "IHttpContextFeature");
                if (httpContextFeature.Value != null)
                {
                    var httpContext = httpContextFeature.Value.GetType().GetProperty("HttpContext")?.GetValue(httpContextFeature.Value);
                    if (httpContext != null)
                    {
                        var responseProperty = httpContext.GetType().GetProperty("Response");
                        responseProperty?.SetValue(httpContext, response);
                    }
                }

                _logger.LogInformation("IPThrottlingMiddleware: Request throttled, returning 429 response");
                // Don't call next - request is throttled
                return;
            }

            // Log the request for throttling analysis (async, don't await to avoid blocking)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _throttlingService.LogRequestAsync(httpRequest, endpoint);
                    _logger.LogInformation("IPThrottlingMiddleware: Request logged for throttling analysis");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error logging request for throttling analysis");
                }
            });

            _logger.LogInformation("IPThrottlingMiddleware: Request allowed, continuing to next middleware");
            // Request is not throttled, continue with normal processing
            await next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in IP throttling middleware for IP {ClientIP} on endpoint {Endpoint}", 
                clientIP, endpoint);
            
            // On error, allow request through (fail open) but log the incident
            await next(context);
        }
    }

    /// <summary>
    /// Extracts HttpRequestData from FunctionContext for HTTP trigger functions
    /// </summary>
    private Task<HttpRequestData?> GetHttpRequestDataAsync(FunctionContext context)
    {
        try
        {
            // Look for HTTP trigger binding
            var httpTriggerBinding = context.BindingContext.BindingData.FirstOrDefault(b => 
                b.Key.Equals("req", StringComparison.OrdinalIgnoreCase) || 
                b.Key.Equals("request", StringComparison.OrdinalIgnoreCase));

            if (httpTriggerBinding.Value is HttpRequestData httpRequest)
            {
                return Task.FromResult<HttpRequestData?>(httpRequest);
            }

            // Try to get the request from the invocation
            foreach (var binding in context.BindingContext.BindingData)
            {
                if (binding.Value is HttpRequestData request)
                {
                    return Task.FromResult<HttpRequestData?>(request);
                }
            }

            return Task.FromResult<HttpRequestData?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not extract HttpRequestData from FunctionContext");
            return Task.FromResult<HttpRequestData?>(null);
        }
    }
}