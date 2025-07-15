using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Functions.BlogPosts.Services;
using Functions.BlogPosts.Models;
using Functions.Shared;
using Utils;
using Utils.Validation;

namespace Functions.BlogPosts.Functions;

/// <summary>
/// BlogPost Media Functions using BaseMediaRelationshipFunctions
/// </summary>
public class BlogPostMediaFunctions : BaseMediaRelationshipFunctions<IBlogPostService, BlogPostDTO>
{
  public BlogPostMediaFunctions(
      IAppInsightsLogger<BaseMediaRelationshipFunctions<IBlogPostService, BlogPostDTO>> logger,
      IBlogPostService blogPostService,
      IAPIKeyValidator apiKeyValidator)
      : base(logger, blogPostService, apiKeyValidator)
  {
  }

  [Function("SetBlogPostFeaturedImage")]
  public async Task<HttpResponseData> SetFeaturedImage(
      [HttpTrigger(AuthorizationLevel.Function, "post", Route = "posts/{slug}/featured-image")] HttpRequestData req,
      string slug)
  {
    return await ProcessMediaRelationshipAsync(
      req,
      slug,
      "SetBlogPostFeaturedImage",
      async (contentSlug, mediaId) => await _contentService.SetFeaturedImageAsync(contentSlug, mediaId),
      "Successfully set featured image {0} for blog post {1}");
  }

  [Function("SetBlogPostFeaturedVideo")]
  public async Task<HttpResponseData> SetFeaturedVideo(
      [HttpTrigger(AuthorizationLevel.Function, "post", Route = "posts/{slug}/featured-video")] HttpRequestData req,
      string slug)
  {
    return await ProcessMediaRelationshipAsync(
      req,
      slug,
      "SetBlogPostFeaturedVideo",
      async (contentSlug, mediaId) => await _contentService.SetFeaturedVideoAsync(contentSlug, mediaId),
      "Successfully set featured video {0} for blog post {1}");
  }

  [Function("AddBlogPostMediaReference")]
  public async Task<HttpResponseData> AddBlogPostMediaReference(
      [HttpTrigger(AuthorizationLevel.Function, "post", Route = "posts/{slug}/media")] HttpRequestData req,
      string slug)
  {
    return await ProcessMediaRelationshipAsync(
      req,
      slug,
      "AddBlogPostMediaReference",
      async (contentSlug, mediaId) => await _contentService.AddMediaReferenceAsync(contentSlug, mediaId),
      "Successfully added media reference {0} for blog post {1}");
  }

  [Function("RemoveBlogPostMediaReference")]
  public async Task<HttpResponseData> RemoveBlogPostMediaReference(
      [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "posts/{slug}/media/{mediaId}")] HttpRequestData req,
      string slug,
      string mediaId)
  {
    return await ProcessRemoveMediaAsync(
      req,
      slug,
      mediaId,
      "RemoveBlogPostMediaReference",
      async (contentSlug, mediaIdToRemove) => await _contentService.RemoveMediaReferenceAsync(contentSlug, mediaIdToRemove),
      "Successfully removed media reference {0} from blog post {1}");
  }
}
