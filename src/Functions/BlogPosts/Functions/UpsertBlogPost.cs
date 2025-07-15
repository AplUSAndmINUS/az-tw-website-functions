using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Functions.BlogPosts.Services;
using Functions.BlogPosts.Models;
using Functions.Shared;
using System.Net;
using Utils;
using Utils.Validation;
using Utils.Extensions;

namespace Functions.BlogPosts.Functions;

public class UpsertBlogPost : BaseContentFunctions<IBlogPostService, BlogPostModel, BlogPostDTO, BlogPostWithMediaDTO>
{
  private readonly IBlogPostService _blogPostService;

  public UpsertBlogPost(
    IAppInsightsLogger<BaseContentFunctions<IBlogPostService, BlogPostModel, BlogPostDTO, BlogPostWithMediaDTO>> logger,
    IBlogPostService blogPostService,
    IAPIKeyValidator apiKeyValidator)
    : base(logger, blogPostService, apiKeyValidator)
  {
    _blogPostService = blogPostService;
  }

  protected override HttpResponseData? ValidateContentModelFields(HttpRequestData req, BlogPostModel model)
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

    // Validate media IDs if provided
    if (!string.IsNullOrEmpty(model.FeaturedImageId))
    {
      try { Utils.Validation.DataValidation.RequireMinLength(model.FeaturedImageId, 1, "FeaturedImageId"); }
      catch (ArgumentException) { errors.Add("FeaturedImageId must be valid"); }
    }

    if (!string.IsNullOrEmpty(model.FeaturedVideoId))
    {
      try { Utils.Validation.DataValidation.RequireMinLength(model.FeaturedVideoId, 1, "FeaturedVideoId"); }
      catch (ArgumentException) { errors.Add("FeaturedVideoId must be valid"); }
    }

    if (!string.IsNullOrEmpty(model.FeaturedMediaId))
    {
      try { Utils.Validation.DataValidation.RequireMinLength(model.FeaturedMediaId, 1, "FeaturedMediaId"); }
      catch (ArgumentException) { errors.Add("FeaturedMediaId must be valid"); }
    }

    if (errors.Any())
    {
      _appLogger.LogWarning("Model validation failed: {Errors}", string.Join(", ", errors));
      return CreateValidationErrorResponse(req, errors);
    }

    return null;
  }

  [Function("UpsertBlogPost")]
  public async Task<HttpResponseData> Run(
    [HttpTrigger(AuthorizationLevel.Function, "post", "put", Route = "posts/{slug?}")] HttpRequestData req)
  {
    _appLogger.LogInformation("UpsertBlogPost function triggered");

    // Validate API key using base class helper method
    var apiValidationResult = await ValidateApiKeyAsync(req, "UpsertBlogPost");
    if (apiValidationResult != null)
    {
      return apiValidationResult;
    }

    try
    {
      // Read and deserialize the request body using base class method
      var (blogPost, errorResponse) = await ReadAndDeserializeBodyAsync<BlogPostModel>(req);
      if (errorResponse != null)
      {
        return errorResponse;
      }

      _appLogger.LogInformation("Deserialized blog post: Title={Title}, Slug={Slug}", blogPost!.Title, blogPost.Slug);

      // Extract slug from route or use the one from the model
      var slug = req.FunctionContext.BindingContext.BindingData.ContainsKey("slug")
          ? req.FunctionContext.BindingContext.BindingData["slug"]?.ToString()
          : blogPost.Slug;

      _appLogger.LogInformation("Using slug: {Slug}", slug ?? "null");

      if (string.IsNullOrWhiteSpace(slug))
      {
        _appLogger.LogWarning("Slug is missing from both route and model");
        return CreateBadRequestResponse(req, "Slug is required");
      }

      blogPost.Slug = slug;

      // Ensure all DateTime fields are properly set to UTC
      EnsureDateTimeFieldsAreUtc(blogPost);

      // Validate the model using base class method
      var validationResult = ValidateContentModel(req, blogPost);
      if (validationResult != null)
      {
        return validationResult;
      }

      _appLogger.LogInformation("About to call UpsertPostAsync for slug: {Slug}", blogPost.Slug);

      // Call the service to upsert the blog post
      var result = await _blogPostService.UpsertPostAsync(blogPost.Slug, blogPost);

      _appLogger.LogInformation("UpsertPostAsync completed. Result: {Result}", result != null ? "Success" : "Failed");

      if (result == null)
      {
        _appLogger.LogError("Failed to upsert blog post", new Exception("UpsertPostAsync returned null"));
        return CreateServerErrorResponse(req, "Failed to upsert blog post");
      }

      // Return success response using base class method
      return await CreateJsonResponseAsync(req, result);
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error in UpsertBlogPost: {Error}", ex);
      _appLogger.LogError("Stack trace: {StackTrace}", ex);
      return CreateServerErrorResponse(req, $"Internal server error: {ex.Message}");
    }
  }

  private static void EnsureDateTimeFieldsAreUtc(BlogPostModel blogPost)
  {
    // Set LastModified to current UTC time if it's the default value
    if (blogPost.LastModified == default || blogPost.LastModified.Year < 2000)
    {
      blogPost.LastModified = DateTime.UtcNow;
    }
    else
    {
      // Convert LastModified to UTC
      blogPost.LastModified = blogPost.LastModified.EnsureUtc();
    }

    // Set PublishDate based on status
    if (blogPost.Status == "Published")
    {
      // For published posts, ensure we have a valid date
      if (blogPost.PublishDate == default || blogPost.PublishDate.Year < 2000)
      {
        blogPost.PublishDate = DateTime.UtcNow;
      }
      else
      {
        blogPost.PublishDate = blogPost.PublishDate.EnsureUtc();
      }
    }
    else
    {
      // For drafts, ensure we have a valid future date to avoid Azure Table Storage errors
      if (blogPost.PublishDate == default || blogPost.PublishDate.Year < 2000)
      {
        // Set to a valid date in the future - Azure Table Storage doesn't accept DateTime.MinValue
        blogPost.PublishDate = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
      }
      else
      {
        blogPost.PublishDate = blogPost.PublishDate.EnsureUtc();
      }
    }

    Console.WriteLine($"DEBUG: EnsureDateTimeFieldsAreUtc - Final PublishDate={blogPost.PublishDate} (Kind={blogPost.PublishDate.Kind})");
    Console.WriteLine($"DEBUG: EnsureDateTimeFieldsAreUtc - Final LastModified={blogPost.LastModified} (Kind={blogPost.LastModified.Kind})");
  }
}
