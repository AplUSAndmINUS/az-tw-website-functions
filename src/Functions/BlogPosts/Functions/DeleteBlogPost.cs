using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Functions.BlogPosts.Services;
using Functions.BlogPosts.Models;
using Functions.Shared;
using Utils;
using Utils.Validation;

namespace Functions.BlogPosts.Functions;

public class DeleteBlogPost : BaseContentFunctions<IBlogPostService, BlogPostModel, BlogPostDTO, BlogPostWithMediaDTO>
{
  private readonly IBlogPostService _blogPostService;

  public DeleteBlogPost(
    IAppInsightsLogger<BaseContentFunctions<IBlogPostService, BlogPostModel, BlogPostDTO, BlogPostWithMediaDTO>> logger,
    IBlogPostService blogPostService,
    IAPIKeyValidator apiKeyValidator)
    : base(logger, blogPostService, apiKeyValidator)
  {
    _blogPostService = blogPostService;
  }

  protected override HttpResponseData? ValidateContentModelFields(HttpRequestData req, BlogPostModel model)
  {
    // Not used for delete operations, but required by base class
    return null;
  }

  [Function("DeleteBlogPost")]
  public async Task<HttpResponseData> Run(
    [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "posts/{slug}")] HttpRequestData req)
  {
    return await ProcessDeleteAsync(req, "DeleteBlogPost", _blogPostService.DeletePostAsync);
  }
}