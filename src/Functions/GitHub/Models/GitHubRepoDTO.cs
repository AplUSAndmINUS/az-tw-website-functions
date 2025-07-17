namespace Functions.GitHub.Models;

/// <summary>
/// Data Transfer Object for GitHub repositories used in API responses
/// </summary>
public class GitHubRepoDTO
{
  public string Id { get; set; } = string.Empty;
  public long GitHubId { get; set; }
  public string Name { get; set; } = string.Empty;
  public string FullName { get; set; } = string.Empty;
  public string Description { get; set; } = string.Empty;
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
  public string[] Topics { get; set; } = [];
  public DateTime LastModified { get; set; }
  public string Slug { get; set; } = string.Empty;
  public string Category { get; set; } = string.Empty;
}