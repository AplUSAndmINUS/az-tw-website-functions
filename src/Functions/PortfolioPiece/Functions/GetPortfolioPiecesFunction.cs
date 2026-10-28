using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Functions.PortfolioPiece.Services;
using Functions.PortfolioPiece.Models;
using Functions.Shared;
using System.Net;
using Utils;
using Utils.Validation;

namespace Functions.PortfolioPiece.Functions;

public class GetPortfolioPiecesFunction : BaseContentFunctions<IPortfolioPieceService, PortfolioPieceModel, PortfolioPieceDTO, PortfolioPieceWithMediaDTO>
{
  private readonly IPortfolioPieceService _portfolioService;

  public GetPortfolioPiecesFunction(
    IAppInsightsLogger<BaseContentFunctions<IPortfolioPieceService, PortfolioPieceModel, PortfolioPieceDTO, PortfolioPieceWithMediaDTO>> logger,
    IPortfolioPieceService portfolioService,
    PublicAPIKeyValidator apiKeyValidator)
    : base(logger, portfolioService, apiKeyValidator)
  {
    _portfolioService = portfolioService;
  }

  protected override HttpResponseData? ValidateContentModelFields(HttpRequestData req, PortfolioPieceModel model)
  {
    if (string.IsNullOrWhiteSpace(model.Slug))
    {
      _appLogger.LogWarning("PortfolioPiece model slug is required");
      return CreateBadRequestResponse(req, "Slug is required");
    }

    if (string.IsNullOrWhiteSpace(model.Title))
    {
      _appLogger.LogWarning("PortfolioPiece model title is required");
      return CreateBadRequestResponse(req, "Title is required");
    }

    if (string.IsNullOrWhiteSpace(model.Content))
    {
      _appLogger.LogWarning("PortfolioPiece model content is required");
      return CreateBadRequestResponse(req, "Content is required");
    }

    if (string.IsNullOrWhiteSpace(model.AuthorSlug))
    {
      _appLogger.LogWarning("PortfolioPiece model author slug is required");
      return CreateBadRequestResponse(req, "Author slug is required");
    }

    return null;
  }

  [Function("GetPortfolioPieces")]
  public async Task<HttpResponseData> GetPieces([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "portfolio")] HttpRequestData req)
  {
    _appLogger.LogInformation("GetPortfolioPieces function triggered");

    // Validate API key using base class helper method
    var apiValidationResult = await ValidateApiKeyAsync(req, "GetPortfolioPieces");
    if (apiValidationResult != null)
    {
      return apiValidationResult;
    }

    try
    {
      // Parse query parameters using base class method
      var (authorSlug, category, isPublished, limit, includeMedia) = ParseGetQueryParameters(req);

      // Get portfolio pieces with or without media
      object result;
      if (includeMedia)
      {
        result = await _portfolioService.GetPiecesWithMediaAsync(authorSlug, category, isPublished, limit);
      }
      else
      {
        result = await _portfolioService.GetPiecesAsync(authorSlug, category, isPublished, limit);
      }

      // Create response using base class method
      var response = await CreateJsonResponseAsync(req, result);

      _appLogger.LogInformation("Successfully retrieved portfolio pieces");
      return response;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error retrieving portfolio pieces", ex);
      return CreateServerErrorResponse(req);
    }
  }

  [Function("GetPortfolioPiece")]
  public async Task<HttpResponseData> GetPiece([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "portfolio/{slug}")] HttpRequestData req)
  {
    _appLogger.LogInformation("GetPortfolioPiece function triggered");

    // Validate API key using base class helper method
    var apiValidationResult = await ValidateApiKeyAsync(req, "GetPortfolioPiece");
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

      // Get the portfolio piece with or without media
      object? result = null;
      if (includeMedia)
      {
        result = await _portfolioService.GetPieceWithMediaAsync(slug!, isPublished);
      }
      else
      {
        result = await _portfolioService.GetPieceAsync(slug!, isPublished);
      }

      if (result == null)
      {
        _appLogger.LogInformation("Portfolio piece with slug {Slug} not found", slug ?? "unknown");
        return CreateNotFoundResponse(req, "Portfolio piece not found");
      }

      // Create response using base class method
      var response = await CreateJsonResponseAsync(req, result);

      _appLogger.LogInformation("Successfully retrieved portfolio piece with slug: {Slug}", slug ?? "unknown");
      return response;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error retrieving portfolio piece", ex);
      return CreateServerErrorResponse(req);
    }
  }
}
