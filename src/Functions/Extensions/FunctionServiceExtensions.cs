using Microsoft.Extensions.DependencyInjection;
using Functions.BlogPosts.Services;
using Functions.Authors.Services;
using Functions.PortfolioPiece.Services;
using Functions.Books.Services;
using Functions.GitHub.Services;
using Functions.ContactMe.Services;
using Functions.Proxy.Services;
using Functions.Proxy.Models;
using SharedStorage.Extensions;
using Utils.Configuration;

namespace Functions.Extensions;

public static class FunctionServiceExtensions
{
  /// <summary>
  /// Registers all Function-specific services (content services that are specific to this Functions app)
  /// </summary>
  public static IServiceCollection AddFunctionServices(this IServiceCollection services)
  {
    // Register Function-specific content services
    services.AddScoped<IBlogPostService, BlogPostService>();
    services.AddScoped<IAuthorService, AuthorService>();
    services.AddScoped<IPortfolioPieceService, PortfolioPieceService>();
    services.AddScoped<IBookService, BookService>();

    // Register GitHub services
    services.AddHttpClient<IGitHubApiService, GitHubApiService>();
    services.AddScoped<IGitHubRepoService, GitHubRepoService>();

    // Register ContactMe service
    services.AddScoped<IContactMeService, ContactMeService>();

    // Register Proxy services
    services.AddProxyServices();

    // Add other Function-specific services here

    return services;
  }

  /// <summary>
  /// Registers proxy-related services for secure Key Vault integration
  /// </summary>
  public static IServiceCollection AddProxyServices(this IServiceCollection services)
  {
    // Register proxy configuration
    services.AddSingleton<ProxyConfiguration>(provider =>
    {
      var config = new ProxyConfiguration
      {
        BaseUrl = Environment.GetEnvironmentVariable("PROXY_BASE_URL") ?? "http://localhost:7071",
        ApiKeySecretName = Environment.GetEnvironmentVariable("PROXY_API_KEY_SECRET_NAME") ?? "X-API-ENVIRONMENT-KEY",
        RequestTimeoutSeconds = int.TryParse(Environment.GetEnvironmentVariable("PROXY_REQUEST_TIMEOUT"), out var timeout) ? timeout : 30,
        ApiKeyCacheDurationMinutes = int.TryParse(Environment.GetEnvironmentVariable("PROXY_API_KEY_CACHE_DURATION"), out var cacheDuration) ? cacheDuration : 5,
        EnableDetailedLogging = bool.TryParse(Environment.GetEnvironmentVariable("PROXY_ENABLE_DETAILED_LOGGING"), out var enableLogging) ? enableLogging : true
      };

      // Configure allowed origins from environment variable
      var allowedOrigins = Environment.GetEnvironmentVariable("PROXY_ALLOWED_ORIGINS");
      if (!string.IsNullOrEmpty(allowedOrigins))
      {
        config.AllowedOrigins = allowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries)
          .Select(origin => origin.Trim())
          .ToList();
      }
      else
      {
        // Default to allowing all origins in development
        config.AllowedOrigins = new List<string> { "*" };
      }

      // Configure allowed IP ranges from environment variable
      var allowedIpRanges = Environment.GetEnvironmentVariable("PROXY_ALLOWED_IP_RANGES");
      if (!string.IsNullOrEmpty(allowedIpRanges))
      {
        config.AllowedIpRanges = allowedIpRanges.Split(',', StringSplitOptions.RemoveEmptyEntries)
          .Select(ip => ip.Trim())
          .ToList();
      }

      return config;
    });

    // Register HttpClient for request forwarding
    services.AddHttpClient<IRequestForwardingService, RequestForwardingService>();

    // Register proxy services
    services.AddScoped<ICorsValidationService, CorsValidationService>();
    services.AddScoped<IRequestForwardingService, RequestForwardingService>();

    return services;
  }
}
