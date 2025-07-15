using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Functions.BlogPosts.Services;
using Functions.BlogPosts.Models;
using Functions.Shared;
using System.Net;
using Utils;
using Utils.Validation;

namespace Functions.BlogPosts.Functions;

public class GetPostsFunction : BaseContentFunctions<IBlogPostService, BlogPostModel, BlogPostDTO, BlogPostWithMediaDTO>
{
  private readonly IBlogPostService _blogPostService;

  public GetPostsFunction(
    IAppInsightsLogger<BaseContentFunctions<IBlogPostService, BlogPostModel, BlogPostDTO, BlogPostWithMediaDTO>> logger,
    IBlogPostService blogPostService,
    IAPIKeyValidator apiKeyValidator)
    : base(logger, blogPostService, apiKeyValidator)
  {
    _blogPostService = blogPostService;
  }

  protected override HttpResponseData? ValidateContentModelFields(HttpRequestData req, BlogPostModel model)
  {
    if (string.IsNullOrWhiteSpace(model.Slug))
    {
      _appLogger.LogWarning("BlogPost model slug is required");
      return CreateBadRequestResponse(req, "Slug is required");
    }

    if (string.IsNullOrWhiteSpace(model.Title))
    {
      _appLogger.LogWarning("BlogPost model title is required");
      return CreateBadRequestResponse(req, "Title is required");
    }

    if (string.IsNullOrWhiteSpace(model.Content))
    {
      _appLogger.LogWarning("BlogPost model content is required");
      return CreateBadRequestResponse(req, "Content is required");
    }

    if (string.IsNullOrWhiteSpace(model.AuthorSlug))
    {
      _appLogger.LogWarning("BlogPost model author slug is required");
      return CreateBadRequestResponse(req, "Author slug is required");
    }

    return null;
  }

  [Function("GetBlogPosts")]
  public async Task<HttpResponseData> GetPosts([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "posts")] HttpRequestData req)
  {
    _appLogger.LogInformation("GetBlogPosts function triggered");

    // Validate API key using base class helper method
    var apiValidationResult = await ValidateApiKeyAsync(req, "GetBlogPosts");
    if (apiValidationResult != null)
    {
      return apiValidationResult;
    }

    try
    {
      // Parse query parameters using base class method
      var (authorSlug, category, isPublished, limit, includeMedia) = ParseGetQueryParameters(req);

      // Get blog posts with or without media
      object result;
      if (includeMedia)
      {
        result = await _blogPostService.GetPostsWithMediaAsync(authorSlug, category, isPublished, limit);
      }
      else
      {
        result = await _blogPostService.GetPostsAsync(authorSlug, category, isPublished, limit);
      }

      // Create response using base class method
      var response = await CreateJsonResponseAsync(req, result);

      _appLogger.LogInformation("Successfully retrieved blog posts");
      return response;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error retrieving blog posts", ex);
      return CreateServerErrorResponse(req);
    }
  }

  [Function("GetBlogPost")]
  public async Task<HttpResponseData> GetPost([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "posts/{slug}")] HttpRequestData req)
  {
    _appLogger.LogInformation("GetBlogPost function triggered");

    // Validate API key using base class helper method
    var apiValidationResult = await ValidateApiKeyAsync(req, "GetBlogPost");
    if (apiValidationResult != null)
    {
      return apiValidationResult;
    }

    try
    {
      // Extract and validate slug using base class methods
      var slug = ExtractSlugFromRoute(req);
      var slugValidationResult = ValidateSlug(req, slug);
      if (slugValidationResult != null)
      {
        return slugValidationResult;
      }

      // Parse query parameters using base class method
      var (isPublished, includeMedia) = ParseGetSingleQueryParameters(req);

      // Get the blog post with or without media
      object? result = null;
      if (includeMedia)
      {
        result = await _blogPostService.GetPostWithMediaAsync(slug!, isPublished);
      }
      else
      {
        result = await _blogPostService.GetPostAsync(slug!, isPublished);
      }

      if (result == null)
      {
        _appLogger.LogInformation("Blog post with slug {Slug} not found", slug ?? "unknown");
        return CreateNotFoundResponse(req, "Blog post not found");
      }

      // Create response using base class method
      var response = await CreateJsonResponseAsync(req, result);

      _appLogger.LogInformation("Successfully retrieved blog post with slug: {Slug}", slug ?? "unknown");
      return response;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error retrieving blog post", ex);
      return CreateServerErrorResponse(req);
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
