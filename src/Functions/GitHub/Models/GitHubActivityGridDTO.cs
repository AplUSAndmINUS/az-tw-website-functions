namespace Functions.GitHub.Models;

/// <summary>
/// Data Transfer Object for GitHub activity grid data
/// </summary>
public class GitHubActivityGridDTO
{
  public string Date { get; set; } = string.Empty; // YYYY-MM-DD format
  public int ContributionCount { get; set; }
  public string ContributionLevel { get; set; } = "NONE"; // NONE, FIRST_QUARTILE, SECOND_QUARTILE, THIRD_QUARTILE, FOURTH_QUARTILE
}