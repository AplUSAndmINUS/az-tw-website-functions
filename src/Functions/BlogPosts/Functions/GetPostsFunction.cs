using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace BlogPosts
{
  public class PlaceholderFunction
  {
    private readonly ILogger _logger;

    public PlaceholderFunction(ILogger<PlaceholderFunction> logger)
    {
      _logger = logger;
    }

    [Function("Ping")]
    public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
      _logger.LogInformation("Ping function triggered.");

      var response = req.CreateResponse(HttpStatusCode.OK);
      response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
      response.WriteString("OK");

      return response;
    }
  }
}
