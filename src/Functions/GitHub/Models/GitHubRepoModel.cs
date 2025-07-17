using SharedStorage.Models;

namespace Functions.GitHub.Models;

/// <summary>
/// Model class for GitHub repositories used in business logic
/// </summary>
public class GitHubRepoModel : BaseContentModel
{
  // GitHub-specific properties
  public long GitHubId { get; set; }
  public string Name { get; set; } = string.Empty;
  public string FullName { get; set; } = string.Empty;
  public string HtmlUrl { get; set; } = string.Empty;
  public string? Language { get; set; }
  public int StargazersCount { get; set; }
  public int ForksCount { get; set; }
  public int WatchersCount { get; set; }
  public int OpenIssuesCount { get; set; }
  public bool IsPrivate { get; set; }
  public bool IsFork { get; set; }
  public bool IsArchived { get; set; }
  public DateTime GitHubCreatedAt { get; set; }
  public DateTime GitHubUpdatedAt { get; set; }
  public DateTime? GitHubPushedAt { get; set; }
  public string DefaultBranch { get; set; } = "main";
  public string[] TopicsList { get; set; } = [];
}