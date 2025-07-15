using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Functions.PortfolioPiece.Services;
using Functions.PortfolioPiece.Models;
using Functions.Shared;
using System.Net;
using Utils;
using Utils.Validation;

namespace Functions.PortfolioPiece.Functions;

public class UpsertPortfolioPieceFunction : BaseContentFunctions<IPortfolioPieceService, PortfolioPieceModel, PortfolioPieceDTO, PortfolioPieceWithMediaDTO>
{
  private readonly IPortfolioPieceService _portfolioService;

  public UpsertPortfolioPieceFunction(
    IAppInsightsLogger<BaseContentFunctions<IPortfolioPieceService, PortfolioPieceModel, PortfolioPieceDTO, PortfolioPieceWithMediaDTO>> logger,
    IPortfolioPieceService portfolioService,
    IAPIKeyValidator apiKeyValidator)
    : base(logger, portfolioService, apiKeyValidator)
  {
    _portfolioService = portfolioService;
  }

  protected override HttpResponseData? ValidateContentModelFields(HttpRequestData req, PortfolioPieceModel model)
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

    if (errors.Any())
    {
      _appLogger.LogWarning("Model validation failed: {Errors}", string.Join(", ", errors));
      return CreateValidationErrorResponse(req, errors);
    }

    return null;
  }

  [Function("UpsertPortfolioPiece")]
  public async Task<HttpResponseData> UpsertPiece([HttpTrigger(AuthorizationLevel.Anonymous, "post", "put", Route = "portfolio/{slug}")] HttpRequestData req)
  {
    _appLogger.LogInformation("UpsertPortfolioPiece function triggered");

    // Validate API key using base class helper method
    var apiValidationResult = await ValidateApiKeyAsync(req, "UpsertPortfolioPiece");
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

      // Read and deserialize the request body using base class method
      var (portfolioModel, errorResponse) = await ReadAndDeserializeBodyAsync<PortfolioPieceModel>(req);
      if (errorResponse != null)
      {
        return errorResponse;
      }

      // Ensure slug consistency
      portfolioModel!.Slug = slug!;

      // Validate the model using base class method
      var validationResult = ValidateContentModel(req, portfolioModel);
      if (validationResult != null)
      {
        return validationResult;
      }

      // Upsert portfolio piece
      var result = await _portfolioService.UpsertPieceAsync(slug!, portfolioModel);

      if (result == null)
      {
        _appLogger.LogWarning("Failed to upsert portfolio piece with slug: {Slug}", slug ?? "unknown");
        return CreateServerErrorResponse(req, "Failed to save portfolio piece");
      }

      // Create response using base class method
      var response = await CreateJsonResponseAsync(req, result);

      _appLogger.LogInformation("Successfully upserted portfolio piece with slug: {Slug}", slug ?? "unknown");
      return response;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error upserting portfolio piece", ex);
      return CreateServerErrorResponse(req);
    }
  }
}
