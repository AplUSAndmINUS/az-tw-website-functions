using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace az_tw_website_functions;

public class AZTWWebsiteFunctions
{
    private readonly ILogger<AZTWWebsiteFunctions> _logger;

    public AZTWWebsiteFunctions(ILogger<AZTWWebsiteFunctions> logger)
    {
        _logger = logger;
    }

    [Function("AZTWWebsiteFunctions")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }
}
