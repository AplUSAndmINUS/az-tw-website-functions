using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Functions.GitHub.Services;
using Functions.GitHub.Models;
using Utils;
using Utils.Validation;
using System.Net;

namespace Functions.GitHub.Functions;

public class GetGitHubActivityGrid
{
  private readonly IGitHubApiService _gitHubApiService;
  private readonly IAppInsightsLogger<GetGitHubActivityGrid> _logger;
  private readonly IAPIKeyValidator _apiKeyValidator;

  public GetGitHubActivityGrid(
    IGitHubApiService gitHubApiService,
    IAppInsightsLogger<GetGitHubActivityGrid> logger,
    IAPIKeyValidator apiKeyValidator)
  {
    _gitHubApiService = gitHubApiService ?? throw new ArgumentNullException(nameof(gitHubApiService));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    _apiKeyValidator = apiKeyValidator ?? throw new ArgumentNullException(nameof(apiKeyValidator));
  }

  [Function("GetGitHubActivityGrid")]
  public async Task<HttpResponseData> GetActivityGrid([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "github/activity")] HttpRequestData req)
  {
    _logger.LogInformation("GetGitHubActivityGrid function triggered");

    // Validate API key
    try
    {
      var apiValidationResult = await _apiKeyValidator.ValidateApiKeyAsync(req, _logger, "GetGitHubActivityGrid");
      if (apiValidationResult != null)
      {
        return apiValidationResult;
      }
    }
    catch (Exception ex)
    {
      _logger.LogError("Error validating API key", ex);
      var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
      await errorResponse.WriteStringAsync("Internal server error");
      return errorResponse;
    }

    try
    {
      // Get GitHub username from environment variable or query parameter
      var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
      var username = query["username"] ?? Environment.GetEnvironmentVariable("GITHUB_USERNAME") ?? "AplUSAndmINUS";

      _logger.LogInformation("Getting GitHub activity grid for user: {Username}", username);

      // Get activity grid data
      var activityData = await _gitHubApiService.GetActivityGridAsync(username);

      // Create response
      var response = req.CreateResponse(HttpStatusCode.OK);
      response.Headers.Add("Content-Type", "application/json; charset=utf-8");
      
      await response.WriteAsJsonAsync(activityData);

      _logger.LogInformation("Successfully retrieved GitHub activity grid for user: {Username}", username);
      return response;
    }
    catch (Exception ex)
    {
      _logger.LogError("Error retrieving GitHub activity grid", ex);
      var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
      await errorResponse.WriteStringAsync("Internal server error");
      return errorResponse;
    }
  }
}