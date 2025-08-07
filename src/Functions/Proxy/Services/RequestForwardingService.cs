using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Functions.Proxy.Models;
using Utils;
using System.Net;
using System.Text;

namespace Functions.Proxy.Services;

/// <summary>
/// Service for forwarding HTTP requests with API key injection
/// </summary>
public class RequestForwardingService : IRequestForwardingService
{
    private readonly ProxyConfiguration _config;
    private readonly IAppInsightsLogger<RequestForwardingService> _logger;
    private readonly HttpClient _httpClient;

    public RequestForwardingService(
        ProxyConfiguration config, 
        IAppInsightsLogger<RequestForwardingService> logger,
        HttpClient httpClient)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        
        // Configure HTTP client timeout
        _httpClient.Timeout = TimeSpan.FromSeconds(_config.RequestTimeoutSeconds);
    }

    public async Task<HttpResponseData> ForwardRequestAsync(HttpRequestData originalRequest, string targetPath, string apiKey)
    {
        try
        {
            _logger.LogInformation("Forwarding request to target path: {TargetPath}", targetPath);

            // Build the target URL
            var targetUrl = BuildTargetUrl(targetPath, originalRequest.Query.ToString() ?? string.Empty);
            
            _logger.LogInformation("Target URL: {TargetUrl}", targetUrl);

            // Create the forwarded request
            using var forwardedRequest = await CreateForwardedRequestAsync(originalRequest, targetUrl, apiKey);

            // Send the request and get response
            using var httpResponse = await _httpClient.SendAsync(forwardedRequest);

            // Create Azure Functions response from HTTP response
            var response = await CreateFunctionResponseAsync(originalRequest, httpResponse);

            _logger.LogInformation("Request forwarded successfully. Status: {StatusCode}", httpResponse.StatusCode);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error forwarding request to {TargetPath}: {Message}", ex, targetPath, ex.Message);
            return CreateErrorResponse(originalRequest, "Internal proxy error", HttpStatusCode.InternalServerError);
        }
    }

    private string BuildTargetUrl(string targetPath, string queryString)
    {
        var baseUrl = _config.BaseUrl.TrimEnd('/');
        var path = targetPath.TrimStart('/');
        
        var url = $"{baseUrl}/{path}";
        
        if (!string.IsNullOrEmpty(queryString))
        {
            var separator = queryString.StartsWith("?") ? "" : "?";
            url += $"{separator}{queryString}";
        }

        return url;
    }

    private async Task<HttpRequestMessage> CreateForwardedRequestAsync(HttpRequestData originalRequest, string targetUrl, string apiKey)
    {
        var forwardedRequest = new HttpRequestMessage
        {
            Method = new HttpMethod(originalRequest.Method),
            RequestUri = new Uri(targetUrl)
        };

        // Copy headers from original request, excluding some headers that shouldn't be forwarded
        var excludedHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Host",
            "Content-Length",
            "x-api-key" // Remove any existing API key to prevent override
        };

        foreach (var header in originalRequest.Headers)
        {
            if (!excludedHeaders.Contains(header.Key))
            {
                forwardedRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        // Inject the API key header
        forwardedRequest.Headers.TryAddWithoutValidation("x-api-key", apiKey);

        // Add proxy identification headers
        forwardedRequest.Headers.TryAddWithoutValidation("X-Forwarded-By", "AzureFunctionProxy");
        forwardedRequest.Headers.TryAddWithoutValidation("X-Proxy-Timestamp", DateTimeOffset.UtcNow.ToString("O"));

        // Copy request body if present
        if (originalRequest.Body != null && originalRequest.Body.CanRead)
        {
            var bodyContent = await ReadRequestBodyAsync(originalRequest);
            if (bodyContent.Length > 0)
            {
                forwardedRequest.Content = new ByteArrayContent(bodyContent);
                
                // Set content type if present in original request
                if (originalRequest.Headers.TryGetValues("Content-Type", out var contentTypeValues))
                {
                    var contentType = contentTypeValues.FirstOrDefault();
                    if (!string.IsNullOrEmpty(contentType))
                    {
                        forwardedRequest.Content.Headers.TryAddWithoutValidation("Content-Type", contentType);
                    }
                }
            }
        }

        return forwardedRequest;
    }

    private async Task<byte[]> ReadRequestBodyAsync(HttpRequestData request)
    {
        try
        {
            using var memoryStream = new MemoryStream();
            await request.Body.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Error reading request body: {Message}", ex, ex.Message);
            return Array.Empty<byte>();
        }
    }

    private async Task<HttpResponseData> CreateFunctionResponseAsync(HttpRequestData originalRequest, HttpResponseMessage httpResponse)
    {
        var response = originalRequest.CreateResponse(httpResponse.StatusCode);

        // Copy headers from HTTP response to Function response
        foreach (var header in httpResponse.Headers)
        {
            try
            {
                response.Headers.Add(header.Key, header.Value);
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Could not copy header {HeaderKey}: {Message}", header.Key, ex.Message);
            }
        }

        // Copy content headers
        if (httpResponse.Content?.Headers != null)
        {
            foreach (var header in httpResponse.Content.Headers)
            {
                try
                {
                    response.Headers.Add(header.Key, header.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogInformation("Could not copy content header {HeaderKey}: {Message}", header.Key, ex.Message);
                }
            }
        }

        // Copy response body
        if (httpResponse.Content != null)
        {
            var content = await httpResponse.Content.ReadAsByteArrayAsync();
            if (content.Length > 0)
            {
                await response.Body.WriteAsync(content);
            }
        }

        return response;
    }

    private HttpResponseData CreateErrorResponse(HttpRequestData request, string message, HttpStatusCode statusCode)
    {
        var response = request.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");

        var errorJson = $"{{\"error\": \"{message}\", \"timestamp\": \"{DateTimeOffset.UtcNow:O}\"}}";
        response.WriteString(errorJson);

        return response;
    }
}