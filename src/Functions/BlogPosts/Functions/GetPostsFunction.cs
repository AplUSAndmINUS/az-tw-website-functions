using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SharedStorage.Services.ContentServices;
using System.Net;
using Utils;

namespace Functions.BlogPosts.Functions;
public class GetPostsFunction
{
  private readonly IAppInsightsLogger<GetPostsFunction> _appLogger;
  private readonly IAPIKeyValidator _apiKeyValidator;
  private readonly IContentService _contentService;

  public GetPostsFunction(IAppInsightsLogger<GetPostsFunction> logger)
  {
    _appLogger = logger;
  }

  [Function("Ping")]
  public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
  {
    _appLogger.LogInformation("Ping function triggered.");

    var response = req.CreateResponse(HttpStatusCode.OK);
    response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
    response.WriteString("OK");

    return response;
  }
}
