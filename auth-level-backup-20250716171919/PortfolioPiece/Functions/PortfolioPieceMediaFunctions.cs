using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Functions.PortfolioPiece.Services;
using Functions.PortfolioPiece.Models;
using Functions.Shared;
using Utils;
using Utils.Validation;

namespace Functions.PortfolioPiece.Functions;

/// <summary>
/// Portfolio Media Functions using BaseMediaRelationshipFunctions
/// </summary>
public class PortfolioPieceMediaFunctions : BaseMediaRelationshipFunctions<IPortfolioPieceService, PortfolioPieceDTO>
{
  public PortfolioPieceMediaFunctions(
      IAppInsightsLogger<BaseMediaRelationshipFunctions<IPortfolioPieceService, PortfolioPieceDTO>> logger,
      IPortfolioPieceService portfolioService,
      IAPIKeyValidator apiKeyValidator)
      : base(logger, portfolioService, apiKeyValidator)
  {
  }

  [Function("SetPortfolioPieceFeaturedImage")]
  public async Task<HttpResponseData> SetFeaturedImage(
      [HttpTrigger(AuthorizationLevel.Function, "post", Route = "portfolio/{slug}/featured-image")] HttpRequestData req,
      string slug)
  {
    return await ProcessMediaRelationshipAsync(
      req,
      slug,
      "SetPortfolioPieceFeaturedImage",
      async (contentSlug, mediaId) => await _contentService.SetFeaturedImageAsync(contentSlug, mediaId),
      "Successfully set featured image {0} for portfolio piece {1}");
  }

  [Function("SetPortfolioPieceFeaturedVideo")]
  public async Task<HttpResponseData> SetFeaturedVideo(
      [HttpTrigger(AuthorizationLevel.Function, "post", Route = "portfolio/{slug}/featured-video")] HttpRequestData req,
      string slug)
  {
    return await ProcessMediaRelationshipAsync(
      req,
      slug,
      "SetPortfolioPieceFeaturedVideo",
      async (contentSlug, mediaId) => await _contentService.SetFeaturedVideoAsync(contentSlug, mediaId),
      "Successfully set featured video {0} for portfolio piece {1}");
  }

  [Function("AddPortfolioPieceMediaReference")]
  public async Task<HttpResponseData> AddMediaReference(
      [HttpTrigger(AuthorizationLevel.Function, "post", Route = "portfolio/{slug}/media")] HttpRequestData req,
      string slug)
  {
    return await ProcessMediaRelationshipAsync(
      req,
      slug,
      "AddPortfolioPieceMediaReference",
      async (contentSlug, mediaId) => await _contentService.AddMediaReferenceAsync(contentSlug, mediaId),
      "Successfully added media reference {0} for portfolio piece {1}");
  }

  [Function("RemovePortfolioPieceMediaReference")]
  public async Task<HttpResponseData> RemoveMediaReference(
      [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "portfolio/{slug}/media/{mediaId}")] HttpRequestData req,
      string slug,
      string mediaId)
  {
    return await ProcessRemoveMediaAsync(
      req,
      slug,
      mediaId,
      "RemovePortfolioPieceMediaReference",
      async (contentSlug, mediaIdToRemove) => await _contentService.RemoveMediaReferenceAsync(contentSlug, mediaIdToRemove),
      "Successfully removed media reference {0} from portfolio piece {1}");
  }
}
