using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Functions.BlogPosts.Services;
using Functions.BlogPosts.Models;
using System.Net;
using System.Text.Json;
using Utils;
using Utils.Validation;

namespace Functions.BlogPosts.Functions;

public class UpsertBlogPost
{
  private readonly IAppInsightsLogger<UpsertBlogPost> _appLogger;
  private readonly IBlogPostService _blogPostService;
  private readonly IAPIKeyValidator _apiKeyValidator;

  public UpsertBlogPost(
    IAppInsightsLogger<UpsertBlogPost> logger,
    IBlogPostService blogPostService,
    IAPIKeyValidator apiKeyValidator)
  {
    _appLogger = logger ?? throw new ArgumentNullException(nameof(logger));
    _blogPostService = blogPostService ?? throw new ArgumentNullException(nameof(blogPostService));
    _apiKeyValidator = apiKeyValidator ?? throw new ArgumentNullException(nameof(apiKeyValidator));
  }

  [Function("UpsertBlogPost")]
  public async Task<HttpResponseData> Run(
    [HttpTrigger(AuthorizationLevel.Function, "post", "put", Route = "posts/{slug?}")] HttpRequestData req)
  {
    _appLogger.LogInformation("UpsertBlogPost function triggered");

    // Validate API key using helper method
    var apiValidationResult = await _apiKeyValidator.ValidateApiKeyAsync(req, _appLogger, "UpsertBlogPost");
    if (apiValidationResult != null)
    {
      return apiValidationResult;
    }

    try
    {
      // Read the request body
      var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
      _appLogger.LogInformation("Request body received: {RequestBody}", requestBody);

      if (string.IsNullOrWhiteSpace(requestBody))
      {
        _appLogger.LogWarning("Request body is empty");
        var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
        await badResponse.WriteStringAsync("Request body is required");
        return badResponse;
      }

      // Use the simplified JsonHelper that always returns a valid object
      var blogPost = JsonHelper.Deserialize<BlogPostModel>(requestBody);

      // Basic validation to ensure we got meaningful data
      if (string.IsNullOrWhiteSpace(blogPost.Title) && string.IsNullOrWhiteSpace(blogPost.Content))
      {
        _appLogger.LogWarning("Failed to deserialize blog post model or received empty model");
        var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
        await badResponse.WriteStringAsync("Invalid blog post data provided");
        return badResponse;
      }

      _appLogger.LogInformation("Deserialized blog post: Title={Title}, Slug={Slug}", blogPost.Title, blogPost.Slug);

      // Extract slug from route or use the one from the model
      var slug = req.FunctionContext.BindingContext.BindingData.ContainsKey("slug")
          ? req.FunctionContext.BindingContext.BindingData["slug"]?.ToString()
          : blogPost.Slug;

      _appLogger.LogInformation("Using slug: {Slug}", slug ?? "null");

      if (string.IsNullOrWhiteSpace(slug))
      {
        _appLogger.LogWarning("Slug is missing from both route and model");
        var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
        await badResponse.WriteStringAsync("Slug is required");
        return badResponse;
      }

      blogPost.Slug = slug;

      // Validate the model
      var validationErrors = ValidateModel(blogPost);
      if (validationErrors.Any())
      {
        _appLogger.LogWarning("Model validation failed: {Errors}", string.Join(", ", validationErrors));
        var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
        await badResponse.WriteStringAsync($"Validation errors: {string.Join(", ", validationErrors)}");
        return badResponse;
      }

      _appLogger.LogInformation("About to call UpsertPostAsync for slug: {Slug}", blogPost.Slug);

      // Call the service to upsert the blog post
      var result = await _blogPostService.UpsertPostAsync(blogPost.Slug, blogPost);

      _appLogger.LogInformation("UpsertPostAsync completed. Result: {Result}", result != null ? "Success" : "Failed");

      if (result == null)
      {
        _appLogger.LogError("Failed to upsert blog post", new Exception("UpsertPostAsync returned null"));
        var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
        await errorResponse.WriteStringAsync("Failed to upsert blog post");
        return errorResponse;
      }

      // Return success response using JsonHelper
      var response = req.CreateResponse(HttpStatusCode.OK);
      response.Headers.Add("Content-Type", "application/json");
      await response.WriteStringAsync(JsonHelper.Serialize(result));
      return response;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error in UpsertBlogPost: {Error}", ex);
      _appLogger.LogError("Stack trace: {StackTrace}", ex);
      var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
      await errorResponse.WriteStringAsync($"Internal server error: {ex.Message}");
      return errorResponse;
    }
  }

  private static List<string> ValidateModel(BlogPostModel model)
  {
    var errors = new List<string>();

    if (string.IsNullOrWhiteSpace(model.Title))
      errors.Add("Title is required");

    if (string.IsNullOrWhiteSpace(model.Slug))
      errors.Add("Slug is required");

    if (string.IsNullOrWhiteSpace(model.AuthorSlug))
      errors.Add("Author slug is required");

    if (string.IsNullOrWhiteSpace(model.Content))
      errors.Add("Content is required");

    if (string.IsNullOrWhiteSpace(model.Category))
      errors.Add("Category is required");

    if (model.TagsList == null)
      errors.Add("Tags list is required (can be empty array)");

    return errors;
  }
}
