using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Functions.Authors.Services;
using Functions.Authors.Models;
using Functions.Shared;
using Utils;
using Utils.Validation;

namespace Functions.Authors.Functions;

/// <summary>
/// Author Media Functions using BaseMediaRelationshipFunctions
/// </summary>
public class AuthorMediaFunctions : BaseMediaRelationshipFunctions<IAuthorService, AuthorDTO>
{
  public AuthorMediaFunctions(
      IAppInsightsLogger<BaseMediaRelationshipFunctions<IAuthorService, AuthorDTO>> logger,
      IAuthorService authorService,
      IAPIKeyValidator apiKeyValidator)
      : base(logger, authorService, apiKeyValidator)
  {
  }

  [Function("SetAuthorProfileImage")]
  public async Task<HttpResponseData> SetProfileImage(
      [HttpTrigger(AuthorizationLevel.Function, "post", Route = "authors/{slug}/profile-image")] HttpRequestData req,
      string slug)
  {
    return await ProcessMediaRelationshipAsync(
      req,
      slug,
      "SetAuthorProfileImage",
      async (contentSlug, mediaId) => await _contentService.SetProfileImageAsync(contentSlug, mediaId),
      "Successfully set profile image {0} for author {1}");
  }

  [Function("SetAuthorBackgroundImage")]
  public async Task<HttpResponseData> SetBackgroundImage(
      [HttpTrigger(AuthorizationLevel.Function, "post", Route = "authors/{slug}/background-image")] HttpRequestData req,
      string slug)
  {
    return await ProcessMediaRelationshipAsync(
      req,
      slug,
      "SetAuthorBackgroundImage",
      async (contentSlug, mediaId) => await _contentService.SetBackgroundImageAsync(contentSlug, mediaId),
      "Successfully set background image {0} for author {1}");
  }

  [Function("AddAuthorMediaReference")]
  public async Task<HttpResponseData> AddMediaReference(
      [HttpTrigger(AuthorizationLevel.Function, "post", Route = "authors/{slug}/media")] HttpRequestData req,
      string slug)
  {
    return await ProcessMediaRelationshipAsync(
      req,
      slug,
      "AddAuthorMediaReference",
      async (contentSlug, mediaId) => await _contentService.AddMediaReferenceAsync(contentSlug, mediaId),
      "Successfully added media reference {0} for author {1}");
  }

  [Function("RemoveAuthorMediaReference")]
  public async Task<HttpResponseData> RemoveMediaReference(
      [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "authors/{slug}/media/{mediaId}")] HttpRequestData req,
      string slug,
      string mediaId)
  {
    return await ProcessRemoveMediaAsync(
      req,
      slug,
      mediaId,
      "RemoveAuthorMediaReference",
      async (contentSlug, mediaIdToRemove) => await _contentService.RemoveMediaReferenceAsync(contentSlug, mediaIdToRemove),
      "Successfully removed media reference {0} from author {1}");
  }
}
