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
    [HttpTrigger(AuthorizationLevel.Function, "post", "put")] HttpRequestData req)
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
      if (string.IsNullOrWhiteSpace(requestBody))
      {
        _appLogger.LogWarning("Request body is empty");
        var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
        await badResponse.WriteStringAsync("Request body is required");
        return badResponse;
      }

      // Deserialize the blog post model
      BlogPostModel? model;
      try
      {
        model = JsonSerializer.Deserialize<BlogPostModel>(requestBody, new JsonSerializerOptions
        {
          PropertyNameCaseInsensitive = true
        });

        if (model == null)
        {
          _appLogger.LogWarning("Failed to deserialize blog post model");
          var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
          await badResponse.WriteStringAsync("Invalid blog post data");
          return badResponse;
        }
      }
      catch (JsonException ex)
      {
        _appLogger.LogError("JSON deserialization error", ex);
        var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
        await badResponse.WriteStringAsync("Invalid JSON format");
        return badResponse;
      }

      // Extract slug from query parameters or use model slug
      var slug = req.Query["slug"] ?? model.Slug;
      if (string.IsNullOrWhiteSpace(slug))
      {
        _appLogger.LogWarning("Slug is missing from request");
        var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
        await badResponse.WriteStringAsync("Slug is required");
        return badResponse;
      }

      // Ensure model slug matches the provided slug
      model.Slug = slug;

      // Validate required fields
      var validationErrors = ValidateModel(model);
      if (validationErrors.Any())
      {
        _appLogger.LogWarning("Model validation failed: {Errors}", string.Join(", ", validationErrors));
        var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
        await badResponse.WriteStringAsync($"Validation errors: {string.Join(", ", validationErrors)}");
        return badResponse;
      }

      // Upsert the blog post
      var result = await _blogPostService.UpsertPostAsync(slug, model);
      if (result == null)
      {
        _appLogger.LogError("Failed to upsert blog post with slug: {Slug}", new InvalidOperationException("Upsert operation failed"), slug);
        var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
        await errorResponse.WriteStringAsync("Failed to create or update blog post");
        return errorResponse;
      }

      // Return success response
      var response = req.CreateResponse(HttpStatusCode.OK);
      response.Headers.Add("Content-Type", "application/json");

      var responseBody = JsonSerializer.Serialize(result, new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
      });

      await response.WriteStringAsync(responseBody);

      _appLogger.LogInformation("Successfully upserted blog post with slug: {Slug}", slug);
      return response;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error processing upsert blog post request", ex);
      var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
      await errorResponse.WriteStringAsync("Internal server error");
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
