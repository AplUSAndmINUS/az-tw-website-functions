using Microsoft.Azure.Functions.Worker.Http;

namespace Functions.Proxy.Services;

/// <summary>
/// Service for forwarding HTTP requests with API key injection
/// </summary>
public interface IRequestForwardingService
{
    /// <summary>
    /// Forwards an HTTP request to a target function with API key injection
    /// </summary>
    /// <param name="originalRequest">The original HTTP request from the client</param>
    /// <param name="targetPath">The target function path to forward to</param>
    /// <param name="apiKey">The API key to inject into the request headers</param>
    /// <returns>The HTTP response from the target function</returns>
    Task<HttpResponseData> ForwardRequestAsync(HttpRequestData originalRequest, string targetPath, string apiKey);
}