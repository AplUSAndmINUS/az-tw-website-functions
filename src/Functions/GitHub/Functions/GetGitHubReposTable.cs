using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Functions.GitHub.Services;
using Functions.GitHub.Models;
using Functions.Shared;
using System.Net;
using Utils;
using Utils.Validation;

namespace Functions.GitHub.Functions;

public class GetGitHubReposTable : BaseContentFunctions<IGitHubRepoService, GitHubRepoModel, GitHubRepoDTO, GitHubRepoDTO>
{
  private readonly IGitHubRepoService _gitHubRepoService;

  public GetGitHubReposTable(
    IAppInsightsLogger<BaseContentFunctions<IGitHubRepoService, GitHubRepoModel, GitHubRepoDTO, GitHubRepoDTO>> logger,
    IGitHubRepoService gitHubRepoService,
    IAPIKeyValidator apiKeyValidator)
    : base(logger, gitHubRepoService, apiKeyValidator)
  {
    _gitHubRepoService = gitHubRepoService;
  }

  protected override HttpResponseData? ValidateContentModelFields(HttpRequestData req, GitHubRepoModel model)
  {
    if (string.IsNullOrWhiteSpace(model.Name))
    {
      _appLogger.LogWarning("GitHub repository model name is required");
      return CreateBadRequestResponse(req, "Repository name is required");
    }

    if (string.IsNullOrWhiteSpace(model.FullName))
    {
      _appLogger.LogWarning("GitHub repository model full name is required");
      return CreateBadRequestResponse(req, "Repository full name is required");
    }

    if (model.GitHubId <= 0)
    {
      _appLogger.LogWarning("GitHub repository model GitHub ID is required");
      return CreateBadRequestResponse(req, "GitHub ID is required");
    }

    return null;
  }

  [Function("GetGitHubReposTable")]
  public async Task<HttpResponseData> GetRepos([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "github/repos")] HttpRequestData req)
  {
    _appLogger.LogInformation("GetGitHubReposTable function triggered");

    // Validate API key using base class helper method
    var apiValidationResult = await ValidateApiKeyAsync(req, "GetGitHubReposTable");
    if (apiValidationResult != null)
    {
      return apiValidationResult;
    }

    try
    {
      // Parse query parameters
      var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
      var category = query["category"];
      var limitStr = query["limit"];
      var isPublishedStr = query["isPublished"];

      // Parse parameters
      int? limit = null;
      if (!string.IsNullOrEmpty(limitStr) && int.TryParse(limitStr, out var parsedLimit))
      {
        limit = parsedLimit;
      }

      bool? isPublished = null;
      if (!string.IsNullOrEmpty(isPublishedStr) && bool.TryParse(isPublishedStr, out var parsedIsPublished))
      {
        isPublished = parsedIsPublished;
      }
      else
      {
        isPublished = true; // Default to published only
      }

      // Get GitHub repositories
      var result = await _gitHubRepoService.GetReposAsync(category, isPublished, limit);

      // Create response using base class method
      var response = await CreateJsonResponseAsync(req, result);

      _appLogger.LogInformation("Successfully retrieved {Count} GitHub repositories", result.Count());
      return response;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error retrieving GitHub repositories", ex);
      return CreateServerErrorResponse(req);
    }
  }

  [Function("GetGitHubRepo")]
  public async Task<HttpResponseData> GetRepo([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "github/repos/{slug}")] HttpRequestData req)
  {
    _appLogger.LogInformation("GetGitHubRepo function triggered");

    // Validate API key using base class helper method
    var apiValidationResult = await ValidateApiKeyAsync(req, "GetGitHubRepo");
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

      // Parse query parameters
      var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
      var isPublishedStr = query["isPublished"];

      bool? isPublished = null;
      if (!string.IsNullOrEmpty(isPublishedStr) && bool.TryParse(isPublishedStr, out var parsedIsPublished))
      {
        isPublished = parsedIsPublished;
      }
      else
      {
        isPublished = true; // Default to published only
      }

      // Get the GitHub repository
      var result = await _gitHubRepoService.GetRepoAsync(slug!, isPublished);

      if (result == null)
      {
        _appLogger.LogInformation("GitHub repository with slug {Slug} not found", slug ?? "unknown");
        return CreateNotFoundResponse(req, "GitHub repository not found");
      }

      // Create response using base class method
      var response = await CreateJsonResponseAsync(req, result);

      _appLogger.LogInformation("Successfully retrieved GitHub repository with slug: {Slug}", slug ?? "unknown");
      return response;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error retrieving GitHub repository", ex);
      return CreateServerErrorResponse(req);
    }
  }

  [Function("GetGitHubRepoByGitHubId")]
  public async Task<HttpResponseData> GetRepoByGitHubId([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "github/repos/githubid/{githubId:long}")] HttpRequestData req)
  {
    _appLogger.LogInformation("GetGitHubRepoByGitHubId function triggered");

    // Validate API key using base class helper method
    var apiValidationResult = await ValidateApiKeyAsync(req, "GetGitHubRepoByGitHubId");
    if (apiValidationResult != null)
    {
      return apiValidationResult;
    }

    try
    {
      // Extract GitHub ID from route
      var routeParts = req.Url.AbsolutePath.Split('/');
      var githubIdStr = routeParts.LastOrDefault();

      if (string.IsNullOrEmpty(githubIdStr) || !long.TryParse(githubIdStr, out var githubId))
      {
        _appLogger.LogWarning("Invalid GitHub ID provided: {GitHubId}", githubIdStr ?? "null");
        return CreateBadRequestResponse(req, "Invalid GitHub ID");
      }

      // Get the GitHub repository by GitHub ID
      var result = await _gitHubRepoService.GetRepoByGitHubIdAsync(githubId);

      if (result == null)
      {
        _appLogger.LogInformation("GitHub repository with GitHub ID {GitHubId} not found", githubId);
        return CreateNotFoundResponse(req, "GitHub repository not found");
      }

      // Create response using base class method
      var response = await CreateJsonResponseAsync(req, result);

      _appLogger.LogInformation("Successfully retrieved GitHub repository with GitHub ID: {GitHubId}", githubId);
      return response;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error retrieving GitHub repository by GitHub ID", ex);
      return CreateServerErrorResponse(req);
    }
  }
}