using Microsoft.Azure.Functions.Worker;
using Functions.GitHub.Services;
using Utils;

namespace Functions.GitHub.Functions;

public class GetGitHubRepos
{
  private readonly IGitHubRepoService _gitHubRepoService;
  private readonly IAppInsightsLogger<GetGitHubRepos> _logger;

  public GetGitHubRepos(
    IGitHubRepoService gitHubRepoService,
    IAppInsightsLogger<GetGitHubRepos> logger)
  {
    _gitHubRepoService = gitHubRepoService ?? throw new ArgumentNullException(nameof(gitHubRepoService));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  [Function("GetGitHubRepos")]
  public async Task Run([TimerTrigger("0 0 */4 * * *", RunOnStartup = false)] TimerInfo myTimer)
  {
    var useMockStorage = Environment.GetEnvironmentVariable("USE_MOCK_STORAGE");
    var expectedTableName = useMockStorage?.ToLowerInvariant() == "true" ? "mockgithub" : "github";
    
    _logger.LogInformation("GetGitHubRepos timer trigger function started at: {DateTime}. USE_MOCK_STORAGE='{UseMockStorage}', Expected table: '{TableName}'", 
      DateTime.UtcNow, useMockStorage ?? "null", expectedTableName);

    try
    {
      // Get GitHub username from environment variable
      var githubUsername = Environment.GetEnvironmentVariable("GITHUB_USERNAME") ?? "AplUSAndmINUS";
      
      _logger.LogInformation("Starting sync of GitHub repositories for user: {Username}", githubUsername);

      // Sync repositories from GitHub
      var syncedCount = await _gitHubRepoService.SyncRepositoriesFromGitHubAsync(githubUsername);

      _logger.LogInformation("Successfully synced {SyncedCount} repositories for user: {Username}", syncedCount, githubUsername);
    }
    catch (Exception ex)
    {
      _logger.LogError("Error during GitHub repositories sync", ex);
    }

    _logger.LogInformation("GetGitHubRepos timer trigger function completed at: {DateTime}", DateTime.UtcNow);
  }
}