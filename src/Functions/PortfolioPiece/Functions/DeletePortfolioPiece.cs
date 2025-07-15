using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Functions.PortfolioPiece.Services;
using Functions.PortfolioPiece.Models;
using Functions.Shared;
using Utils;
using Utils.Validation;

namespace Functions.PortfolioPiece.Functions;

public class DeletePortfolioPieceFunction : BaseContentFunctions<IPortfolioPieceService, PortfolioPieceModel, PortfolioPieceDTO, PortfolioPieceWithMediaDTO>
{
  private readonly IPortfolioPieceService _portfolioService;

  public DeletePortfolioPieceFunction(
    IAppInsightsLogger<BaseContentFunctions<IPortfolioPieceService, PortfolioPieceModel, PortfolioPieceDTO, PortfolioPieceWithMediaDTO>> logger,
    IPortfolioPieceService portfolioService,
    IAPIKeyValidator apiKeyValidator)
    : base(logger, portfolioService, apiKeyValidator)
  {
    _portfolioService = portfolioService;
  }

  protected override HttpResponseData? ValidateContentModelFields(HttpRequestData req, PortfolioPieceModel model)
  {
    // Not used for delete operations, but required by base class
    return null;
  }

  [Function("DeletePortfolioPiece")]
  public async Task<HttpResponseData> DeletePiece([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "portfolio/{slug}")] HttpRequestData req)
  {
    return await ProcessDeleteAsync(req, "DeletePortfolioPiece", _portfolioService.DeletePieceAsync);
  }
}
