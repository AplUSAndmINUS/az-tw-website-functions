using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Functions.Books.Services;
using Functions.Books.Models;
using Functions.Shared;
using Utils;
using Utils.Validation;

namespace Functions.Books.Functions;

/// <summary>
/// Book Media Functions using BaseMediaRelationshipFunctions
/// </summary>
public class BookMediaFunctions : BaseMediaRelationshipFunctions<IBookService, BookDTO>
{
  public BookMediaFunctions(
      IAppInsightsLogger<BaseMediaRelationshipFunctions<IBookService, BookDTO>> logger,
      IBookService bookService,
      IAPIKeyValidator apiKeyValidator)
      : base(logger, bookService, apiKeyValidator)
  {
  }

  [Function("SetBookFeaturedImage")]
  public async Task<HttpResponseData> SetFeaturedImage(
      [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "books/{slug}/featured-image")] HttpRequestData req,
      string slug)
  {
    return await ProcessMediaRelationshipAsync(
      req,
      slug,
      "SetBookFeaturedImage",
      async (contentSlug, mediaId) => await _contentService.SetFeaturedImageAsync(contentSlug, mediaId),
      "Successfully set featured image {0} for book {1}");
  }

  [Function("SetBookFeaturedVideo")]
  public async Task<HttpResponseData> SetFeaturedVideo(
      [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "books/{slug}/featured-video")] HttpRequestData req,
      string slug)
  {
    return await ProcessMediaRelationshipAsync(
      req,
      slug,
      "SetBookFeaturedVideo",
      async (contentSlug, mediaId) => await _contentService.SetFeaturedVideoAsync(contentSlug, mediaId),
      "Successfully set featured video {0} for book {1}");
  }

  [Function("SetBookFeaturedMedia")]
  public async Task<HttpResponseData> SetFeaturedMedia(
      [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "books/{slug}/featured-media")] HttpRequestData req,
      string slug)
  {
    return await ProcessMediaRelationshipAsync(
      req,
      slug,
      "SetBookFeaturedMedia",
      async (contentSlug, mediaId) => await _contentService.SetFeaturedMediaAsync(contentSlug, mediaId),
      "Successfully set featured media {0} for book {1}");
  }

  [Function("AddBookMediaReference")]
  public async Task<HttpResponseData> AddBookMediaReference(
      [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "books/{slug}/media")] HttpRequestData req,
      string slug)
  {
    return await ProcessMediaRelationshipAsync(
      req,
      slug,
      "AddBookMediaReference",
      async (contentSlug, mediaId) => await _contentService.AddMediaReferenceAsync(contentSlug, mediaId),
      "Successfully added media reference {0} for book {1}");
  }

  [Function("RemoveBookMediaReference")]
  public async Task<HttpResponseData> RemoveBookMediaReference(
      [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "books/{slug}/media/{mediaId}")] HttpRequestData req,
      string slug,
      string mediaId)
  {
    return await ProcessRemoveMediaAsync(
      req,
      slug,
      mediaId,
      "RemoveBookMediaReference",
      async (contentSlug, mediaIdToRemove) => await _contentService.RemoveMediaReferenceAsync(contentSlug, mediaIdToRemove),
      "Successfully removed media reference {0} from book {1}");
  }
}