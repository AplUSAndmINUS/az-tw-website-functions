using System.Text.Json;
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

      // Note: GitHub's contribution graph is not available via REST API
      // This would typically require GraphQL API or scraping
      // For now, returning empty data with a logged message
      _logger.LogWarning("GitHub activity grid fetching is not implemented yet - requires GraphQL API");
      
      return [];
    }
    catch (Exception ex)
    {
      _logger.LogError("Error fetching activity grid for user {Username}", ex, username);
      return [];
    }
  }

  private static GitHubRepoModel MapToModel(GitHubApiRepository apiRepo)
  {
    return new GitHubRepoModel
    {
      GitHubId = apiRepo.Id,
      Name = apiRepo.Name ?? string.Empty,
      FullName = apiRepo.FullName ?? string.Empty,
      Description = apiRepo.Description,
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