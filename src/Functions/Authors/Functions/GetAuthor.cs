using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using Utils;

namespace Functions.Authors.Functions;

public class GetAuthorFunction
{
  private readonly IAppInsightsLogger<GetAuthorFunction> _appLogger;

  public GetAuthorFunction(IAppInsightsLogger<GetAuthorFunction> logger, string? query)
  {
    _appLogger = logger;
    _appLogger.LogInformation("GetAuthorFunction initialized with query: {Query}", query ?? "null");
  }

  [Function("GetAuthor")]
  public HttpResponseData Run(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "authors/{slug}")] HttpRequestData req)
  {
    _appLogger.LogInformation("GetAuthor function triggered with request: {Request}", req);

    var response = req.CreateResponse(HttpStatusCode.OK);
    response.Headers.Add("Content-Type", "application/json; charset=utf-8");
    response.WriteString("200 OK");

    return response; 
  }
}