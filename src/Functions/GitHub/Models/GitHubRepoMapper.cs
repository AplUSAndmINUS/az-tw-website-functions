namespace Functions.GitHub.Models;

/// <summary>
/// Mapper for converting between GitHub repository models and DTOs
/// </summary>
public static class GitHubRepoMapper
{
  /// <summary>
  /// Maps a GitHubRepoModel to a GitHubRepoDTO
  /// </summary>
  public static GitHubRepoDTO ToDTO(GitHubRepoModel model)
  {
    ArgumentNullException.ThrowIfNull(model);

    return new GitHubRepoDTO
    {
      Id = model.Id ?? string.Empty,
      GitHubId = model.GitHubId,
      Name = model.Name,
      FullName = model.FullName,
      Description = model.Description ?? string.Empty,
      HtmlUrl = model.HtmlUrl,
      Language = model.Language,
      StargazersCount = model.StargazersCount,
      ForksCount = model.ForksCount,
      WatchersCount = model.WatchersCount,
      OpenIssuesCount = model.OpenIssuesCount,
      IsPrivate = model.IsPrivate,
      IsFork = model.IsFork,
      IsArchived = model.IsArchived,
      GitHubCreatedAt = model.GitHubCreatedAt,
      GitHubUpdatedAt = model.GitHubUpdatedAt,
      GitHubPushedAt = model.GitHubPushedAt,
      DefaultBranch = model.DefaultBranch,
      Topics = model.TopicsList ?? [],
      LastModified = model.LastModified,
      Slug = model.Slug ?? string.Empty,
      Category = model.Category ?? "repository"
    };
  }

  /// <summary>
  /// Maps multiple GitHubRepoModels to GitHubRepoDTOs
  /// </summary>
  public static IEnumerable<GitHubRepoDTO> ToDTOs(IEnumerable<GitHubRepoModel> models)
  {
    ArgumentNullException.ThrowIfNull(models);
    return models.Select(ToDTO);
  }
}