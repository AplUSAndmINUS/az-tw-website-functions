using Microsoft.Extensions.DependencyInjection;
using Functions.BlogPosts.Services;
using Functions.Authors.Services;
using Functions.PortfolioPiece.Services;
using Functions.GitHub.Services;
using SharedStorage.Extensions;

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

    // Register GitHub services
    services.AddHttpClient<IGitHubApiService, GitHubApiService>();
    services.AddScoped<IGitHubRepoService, GitHubRepoService>();

    // Add other Function-specific services here

    return services;
  }
}
