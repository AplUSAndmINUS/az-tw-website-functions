using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Functions.BlogPosts.Services;
using System.Net;
using Utils;
using Utils.Validation;

namespace Functions.BlogPosts.Functions;

public class DeleteBlogPost
{
  private readonly IAppInsightsLogger<DeleteBlogPost> _appLogger;
  private readonly IBlogPostService _blogPostService;
  private readonly IAPIKeyValidator _apiKeyValidator;

  public DeleteBlogPost(
    IAppInsightsLogger<DeleteBlogPost> logger,
    IBlogPostService blogPostService,
    IAPIKeyValidator apiKeyValidator)
  {
    _appLogger = logger ?? throw new ArgumentNullException(nameof(logger));
    _blogPostService = blogPostService ?? throw new ArgumentNullException(nameof(blogPostService));
    _apiKeyValidator = apiKeyValidator ?? throw new ArgumentNullException(nameof(apiKeyValidator));
  }

  [Function("DeleteBlogPost")]
  public async Task<HttpResponseData> Run(
    [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "posts/{slug}")] HttpRequestData req)
  {
    _appLogger.LogInformation("DeleteBlogPost function triggered");

    // Validate API key using helper method
    var apiValidationResult = await _apiKeyValidator.ValidateApiKeyAsync(req, _appLogger, "DeleteBlogPost");
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

      // Delete the blog post
      var success = await _blogPostService.DeletePostAsync(slug);

      if (!success)
      {
        _appLogger.LogWarning("Failed to delete blog post with slug: {Slug}", slug);
        var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
        await notFoundResponse.WriteStringAsync("Blog post not found or could not be deleted");
        return notFoundResponse;
      }

      // Return success response
      var response = req.CreateResponse(HttpStatusCode.NoContent);
      _appLogger.LogInformation("Successfully deleted blog post with slug: {Slug}", slug);
      return response;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error deleting blog post", ex);
      var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
      await errorResponse.WriteStringAsync("Internal server error");
      return errorResponse;
    }
  }
}