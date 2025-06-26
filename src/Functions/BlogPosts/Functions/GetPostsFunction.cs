using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Functions.BlogPosts.Services;
using System.Net;
using System.Text.Json;
using Utils;
using Utils.Validation;

namespace Functions.BlogPosts.Functions;

public class GetPostsFunction
{
  private readonly IAppInsightsLogger<GetPostsFunction> _appLogger;
  private readonly IBlogPostService _blogPostService;
  private readonly IAPIKeyValidator _apiKeyValidator;

  public GetPostsFunction(
    IAppInsightsLogger<GetPostsFunction> logger,
    IBlogPostService blogPostService,
    IAPIKeyValidator apiKeyValidator)
  {
    _appLogger = logger ?? throw new ArgumentNullException(nameof(logger));
    _blogPostService = blogPostService ?? throw new ArgumentNullException(nameof(blogPostService));
    _apiKeyValidator = apiKeyValidator ?? throw new ArgumentNullException(nameof(apiKeyValidator));
  }

  [Function("GetBlogPosts")]
  public async Task<HttpResponseData> GetPosts([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
  {
    _appLogger.LogInformation("GetBlogPosts function triggered");

    // Validate API key using helper method
    var apiValidationResult = await _apiKeyValidator.ValidateApiKeyAsync(req, _appLogger, "GetBlogPosts");
    if (apiValidationResult != null)
    {
      return apiValidationResult;
    }

    try
    {
      // Parse query parameters
      var authorSlug = req.Query["authorSlug"];
      var category = req.Query["category"];
      var isPublishedParam = req.Query["isPublished"];
      var limitParam = req.Query["limit"];

      // Parse boolean and integer parameters
      bool? isPublished = string.IsNullOrEmpty(isPublishedParam) ? true : bool.Parse(isPublishedParam);
      int? limit = string.IsNullOrEmpty(limitParam) ? null : int.Parse(limitParam);

      // Get blog posts
      var posts = await _blogPostService.GetPostsAsync(authorSlug, category, isPublished, limit);

      // Create response
      var response = req.CreateResponse(HttpStatusCode.OK);
      response.Headers.Add("Content-Type", "application/json");

      var responseBody = JsonSerializer.Serialize(posts, new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
      });

      await response.WriteStringAsync(responseBody);

      _appLogger.LogInformation("Successfully retrieved {Count} blog posts", posts.Count());
      return response;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error retrieving blog posts", ex);
      var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
      await errorResponse.WriteStringAsync("Internal server error");
      return errorResponse;
    }
  }

  [Function("GetBlogPost")]
  public async Task<HttpResponseData> GetPost([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "posts/{slug}")] HttpRequestData req)
  {
    _appLogger.LogInformation("GetBlogPost function triggered");

    // Validate API key using helper method
    var apiValidationResult = await _apiKeyValidator.ValidateApiKeyAsync(req, _appLogger, "GetBlogPost");
    if (apiValidationResult != null)
    {
      return apiValidationResult;
    }

    try
    {
      // Extract slug from route
      var slug = req.Query["slug"] ?? req.FunctionContext.BindingContext.BindingData["slug"]?.ToString();

      if (string.IsNullOrWhiteSpace(slug))
      {
        _appLogger.LogWarning("Slug parameter is missing");
        var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
        await badResponse.WriteStringAsync("Slug parameter is required");
        return badResponse;
      }

      // Parse query parameters
      var isPublishedParam = req.Query["isPublished"];
      bool? isPublished = string.IsNullOrEmpty(isPublishedParam) ? true : bool.Parse(isPublishedParam);

      // Get the blog post
      var post = await _blogPostService.GetPostAsync(slug, isPublished);

      if (post == null)
      {
        _appLogger.LogInformation("Blog post with slug {Slug} not found", slug);
        var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
        await notFoundResponse.WriteStringAsync("Blog post not found");
        return notFoundResponse;
      }

      // Create response
      var response = req.CreateResponse(HttpStatusCode.OK);
      response.Headers.Add("Content-Type", "application/json");

      var responseBody = JsonSerializer.Serialize(post, new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
      });

      await response.WriteStringAsync(responseBody);

      _appLogger.LogInformation("Successfully retrieved blog post with slug: {Slug}", slug);
      return response;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error retrieving blog post", ex);
      var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
      await errorResponse.WriteStringAsync("Internal server error");
      return errorResponse;
    }
  }

  [Function("Ping")]
  public HttpResponseData Ping([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
  {
    _appLogger.LogInformation("Ping function triggered.");

    var response = req.CreateResponse(HttpStatusCode.OK);
    response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
    response.WriteString("OK");

    return response;
  }
}
