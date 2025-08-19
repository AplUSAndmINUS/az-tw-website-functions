using System.Text.Json;
using System.Text.Json.Serialization;
using Functions.GitHub.Models;
using Utils;

namespace Functions.GitHub.Services;

/// <summary>
/// Service for calling GitHub REST API
/// </summary>
public interface IGitHubApiService
{
  Task<IEnumerable<GitHubRepoModel>> GetRepositoriesAsync(string username);
  Task<IEnumerable<GitHubActivityGridDTO>> GetActivityGridAsync(string username);
}

public class GitHubApiService : IGitHubApiService
{
  private readonly HttpClient _httpClient;
  private readonly IAppInsightsLogger<GitHubApiService> _logger;

  public GitHubApiService(HttpClient httpClient, IAppInsightsLogger<GitHubApiService> logger)
  {
    _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    // Configure HttpClient for GitHub API
    _httpClient.DefaultRequestHeaders.Add("User-Agent", "az-tw-website-functions");
    _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
  }

  public async Task<IEnumerable<GitHubRepoModel>> GetRepositoriesAsync(string username)
  {
    if (string.IsNullOrWhiteSpace(username))
      throw new ArgumentException("Username cannot be null or empty", nameof(username));

    try
    {
      _logger.LogInformation("Fetching repositories for user: {Username}", username);

      var response = await _httpClient.GetAsync($"https://api.github.com/users/{username}/repos?type=all&sort=updated&per_page=100");
      
      if (!response.IsSuccessStatusCode)
      {
        _logger.LogWarning("GitHub API request failed with status: {StatusCode}", response.StatusCode);
        return [];
      }

      var content = await response.Content.ReadAsStringAsync();
      var githubRepos = JsonSerializer.Deserialize<GitHubApiRepository[]>(content, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });

      if (githubRepos == null)
      {
        _logger.LogWarning("Failed to deserialize GitHub repositories response");
        return [];
      }

      var models = githubRepos.Select(MapToModel).ToList();
      _logger.LogInformation("Successfully fetched {Count} repositories for user: {Username}", models.Count, username);

      return models;
    }
    catch (Exception ex)
    {
      _logger.LogError("Error fetching repositories for user {Username}", ex, username);
      return [];
    }
  }

  public async Task<IEnumerable<GitHubActivityGridDTO>> GetActivityGridAsync(string username)
  {
    if (string.IsNullOrWhiteSpace(username))
      throw new ArgumentException("Username cannot be null or empty", nameof(username));

    try
    {
      _logger.LogInformation("Fetching activity grid for user: {Username}", username);

      // Try to get real contribution data using GitHub GraphQL API
      var realActivityData = await GetContributionDataFromGraphQLAsync(username);
      if (realActivityData.Any())
      {
        _logger.LogInformation("Successfully fetched real GitHub contribution data for user: {Username} with {Count} data points", username, realActivityData.Count());
        return realActivityData;
      }

      // Fallback: Try to get basic user info to validate the username exists
      var userResponse = await _httpClient.GetAsync($"https://api.github.com/users/{username}");
      
      if (!userResponse.IsSuccessStatusCode)
      {
        _logger.LogWarning("GitHub user {Username} not found or API request failed with status: {StatusCode}", username, userResponse.StatusCode);
        
        // Return a valid empty activity grid instead of empty array to avoid 500 errors
        return GenerateEmptyActivityGrid();
      }

      // Fallback: Generate activity grid data based on available public repositories
      var activityData = await GenerateActivityGridFromReposAsync(username);
      
      _logger.LogInformation("Successfully generated fallback activity grid for user: {Username} with {Count} data points", username, activityData.Count());
      return activityData;
    }
    catch (Exception ex)
    {
      _logger.LogError("Error fetching activity grid for user {Username}", ex, username);
      
      // Return a valid empty activity grid instead of empty array to avoid 500 errors
      return GenerateEmptyActivityGrid();
    }
  }

  private async Task<IEnumerable<GitHubActivityGridDTO>> GetContributionDataFromGraphQLAsync(string username)
  {
    try
    {
      // Check if GitHub token is available
      var githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? Environment.GetEnvironmentVariable("GITHUB_PAT");
      
      if (string.IsNullOrEmpty(githubToken))
      {
        _logger.LogInformation("No GitHub token found - falling back to REST API approach");
        return [];
      }

      // GraphQL query to get contribution calendar for the last year
      var query = new
      {
        query = @"
          query($username: String!) {
            user(login: $username) {
              contributionsCollection {
                contributionCalendar {
                  weeks {
                    contributionDays {
                      date
                      contributionCount
                      contributionLevel
                    }
                  }
                }
              }
            }
          }",
        variables = new { username }
      };

      var content = new StringContent(
        JsonSerializer.Serialize(query),
        System.Text.Encoding.UTF8,
        "application/json");

      // Create a new HttpClient for GraphQL with proper headers
      using var graphqlClient = new HttpClient();
      graphqlClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {githubToken}");
      graphqlClient.DefaultRequestHeaders.Add("User-Agent", "az-tw-website-functions");

      var response = await graphqlClient.PostAsync("https://api.github.com/graphql", content);

      if (!response.IsSuccessStatusCode)
      {
        var errorContent = await response.Content.ReadAsStringAsync();
        _logger.LogWarning("GitHub GraphQL API request failed with status: {StatusCode}, Error: {Error}", 
          response.StatusCode, errorContent);
        return [];
      }

      var responseContent = await response.Content.ReadAsStringAsync();
      var graphqlResponse = JsonSerializer.Deserialize<GraphQLResponse>(responseContent);

      if (graphqlResponse?.Data?.User?.ContributionsCollection?.ContributionCalendar?.Weeks == null)
      {
        _logger.LogWarning("Invalid GraphQL response structure for user: {Username}", username);
        return [];
      }

      // Convert GraphQL response to our DTO format
      var activityData = new List<GitHubActivityGridDTO>();
      
      foreach (var week in graphqlResponse.Data.User.ContributionsCollection.ContributionCalendar.Weeks ?? Enumerable.Empty<GraphQLWeek>())
      {
        if (week.ContributionDays != null)
        {
          foreach (var day in week.ContributionDays)
          {
            activityData.Add(new GitHubActivityGridDTO
            {
              Date = day.Date,
              ContributionCount = day.ContributionCount,
              ContributionLevel = day.ContributionLevel?.ToUpperInvariant() ?? "NONE"
            });
          }
        }
      }

      _logger.LogInformation("Successfully fetched {Count} contribution days from GitHub GraphQL API for user: {Username}", 
        activityData.Count, username);
      
      return activityData.OrderBy(d => d.Date);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error fetching contribution data from GitHub GraphQL API for user {Username}", username);
      return [];
    }
  }

  private IEnumerable<GitHubActivityGridDTO> GenerateEmptyActivityGrid()
  {
    var activityData = new List<GitHubActivityGridDTO>();
    var today = DateTime.UtcNow.Date;
    
    // Generate the last 365 days with no activity
    for (int i = 364; i >= 0; i--)
    {
      var date = today.AddDays(-i);
      activityData.Add(new GitHubActivityGridDTO
      {
        Date = date.ToString("yyyy-MM-dd"),
        ContributionCount = 0,
        ContributionLevel = "NONE"
      });
    }
    
    return activityData;
  }

  private async Task<IEnumerable<GitHubActivityGridDTO>> GenerateActivityGridFromReposAsync(string username)
  {
    try
    {
      // Get repositories to analyze activity
      var repos = await GetRepositoriesAsync(username);
      var activityData = new List<GitHubActivityGridDTO>();
      
      // Generate the last 365 days of activity data
      var today = DateTime.UtcNow.Date;
      for (int i = 364; i >= 0; i--)
      {
        var date = today.AddDays(-i);
        var dateStr = date.ToString("yyyy-MM-dd");
        
        // Simple heuristic: check if any repos were updated on this date
        var contributionCount = repos.Count(r => 
          r.GitHubUpdatedAt.Date == date || 
          (r.GitHubPushedAt?.Date == date) ||
          r.GitHubCreatedAt.Date == date);
        
        var level = contributionCount switch
        {
          0 => "NONE",
          1 => "FIRST_QUARTILE", 
          2 => "SECOND_QUARTILE",
          3 => "THIRD_QUARTILE",
          _ => "FOURTH_QUARTILE"
        };
        
        activityData.Add(new GitHubActivityGridDTO
        {
          Date = dateStr,
          ContributionCount = contributionCount,
          ContributionLevel = level
        });
      }
      
      return activityData;
    }
    catch (Exception ex)
    {
      _logger.LogError("Error generating activity grid data", ex);
      
      // Return a valid activity grid to avoid 500 errors
      return GenerateEmptyActivityGrid();
    }
  }

  private static GitHubRepoModel MapToModel(GitHubApiRepository apiRepo)
  {
    return new GitHubRepoModel
    {
      GitHubId = apiRepo.Id,
      Name = apiRepo.Name ?? string.Empty,
      FullName = apiRepo.FullName ?? string.Empty,
      Description = apiRepo.Description ?? string.Empty,
      HtmlUrl = apiRepo.HtmlUrl ?? string.Empty,
      Language = apiRepo.Language,
      StargazersCount = apiRepo.StargazersCount,
      ForksCount = apiRepo.ForksCount,
      WatchersCount = apiRepo.WatchersCount,
      OpenIssuesCount = apiRepo.OpenIssuesCount,
      IsPrivate = apiRepo.Private,
      IsFork = apiRepo.Fork,
      IsArchived = apiRepo.Archived,
      GitHubCreatedAt = apiRepo.CreatedAt,
      GitHubUpdatedAt = apiRepo.UpdatedAt,
      GitHubPushedAt = apiRepo.PushedAt,
      DefaultBranch = apiRepo.DefaultBranch ?? "main",
      TopicsList = apiRepo.Topics ?? [],
      Slug = (apiRepo.Name ?? string.Empty).ToLowerInvariant().Replace(" ", "-"),
      Category = "repository",
      Title = apiRepo.Name ?? string.Empty,
      Content = apiRepo.Description ?? string.Empty,
      PublishDate = apiRepo.CreatedAt,
      LastModified = apiRepo.UpdatedAt
    };
  }
}

// Internal classes for GitHub API response deserialization
internal class GitHubApiRepository
{
  public long Id { get; set; }
  public string? Name { get; set; }
  public string? FullName { get; set; }
  public string? Description { get; set; }
  public string? HtmlUrl { get; set; }
  public string? Language { get; set; }
  public int StargazersCount { get; set; }
  public int ForksCount { get; set; }
  public int WatchersCount { get; set; }
  public int OpenIssuesCount { get; set; }
  public bool Private { get; set; }
  public bool Fork { get; set; }
  public bool Archived { get; set; }
  public DateTime CreatedAt { get; set; }
  public DateTime UpdatedAt { get; set; }
  public DateTime? PushedAt { get; set; }
  public string? DefaultBranch { get; set; }
  public string[]? Topics { get; set; }
}

// GitHub GraphQL API response classes
internal class GraphQLResponse
{
  [JsonPropertyName("data")]
  public GraphQLData? Data { get; set; }
}

internal class GraphQLData
{
  [JsonPropertyName("user")]
  public GraphQLUser? User { get; set; }
}

internal class GraphQLUser
{
  [JsonPropertyName("contributionsCollection")]
  public GraphQLContributionsCollection? ContributionsCollection { get; set; }
}

internal class GraphQLContributionsCollection
{
  [JsonPropertyName("contributionCalendar")]
  public GraphQLContributionCalendar? ContributionCalendar { get; set; }
}

internal class GraphQLContributionCalendar
{
  [JsonPropertyName("weeks")]
  public GraphQLWeek[]? Weeks { get; set; }
}

internal class GraphQLWeek
{
  [JsonPropertyName("contributionDays")]
  public GraphQLContributionDay[]? ContributionDays { get; set; }
}

internal class GraphQLContributionDay
{
  [JsonPropertyName("date")]
  public string Date { get; set; } = string.Empty;
  
  [JsonPropertyName("contributionCount")]
  public int ContributionCount { get; set; }
  
  [JsonPropertyName("contributionLevel")]
  public string? ContributionLevel { get; set; }
}