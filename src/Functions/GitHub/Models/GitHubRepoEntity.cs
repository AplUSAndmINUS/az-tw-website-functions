using System.Text.Json;
using SharedStorage.Models;
using Utils.Extensions;
using Utils.Validation;

namespace Functions.GitHub.Models;

/// <summary>
/// Entity class for GitHub repositories stored in Azure Table Storage
/// </summary>
public class GitHubRepoEntity : BaseContentEntity
{
  public GitHubRepoEntity() : base()
  {
  }

  public GitHubRepoEntity(DateTime lastUpdated) : base(lastUpdated)
  {
  }

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
  public string TopicsJson { get; set; } = "[]";

  /// <summary>
  /// Converts the entity to a model
  /// </summary>
  /// <typeparam name="T">The type of model to convert to</typeparam>
  /// <returns>The converted model</returns>
  public override T ToModel<T>()
  {
    if (typeof(T) != typeof(GitHubRepoModel))
      throw new ArgumentException($"Cannot convert GitHubRepoEntity to {typeof(T).Name}");

    var model = new GitHubRepoModel
    {
      Id = Id,
      PartitionKey = PartitionKey,
      RowKey = RowKey,
      Timestamp = Timestamp,
      ETag = ETag,
      Title = Title,
      AuthorSlug = AuthorSlug,
      Description = Description,
      Content = Content,
      Slug = Slug,
      Category = Category,
      Status = Status,
      FeaturedImageId = FeaturedImageId,
      FeaturedMediaId = FeaturedMediaId,
      FeaturedVideoId = FeaturedVideoId,
      MediaReferencesJson = MediaReferencesJson ?? "[]",
      PublishDate = PublishDate.EnsureUtc(),
      LastModified = LastModified.EnsureUtc(),
      TagsList = DeserializeTopics(TopicsJson),
      GitHubId = GitHubId,
      Name = Name,
      FullName = FullName,
      HtmlUrl = HtmlUrl,
      Language = Language,
      StargazersCount = StargazersCount,
      ForksCount = ForksCount,
      WatchersCount = WatchersCount,
      OpenIssuesCount = OpenIssuesCount,
      IsPrivate = IsPrivate,
      IsFork = IsFork,
      IsArchived = IsArchived,
      GitHubCreatedAt = GitHubCreatedAt.EnsureUtc(),
      GitHubUpdatedAt = GitHubUpdatedAt.EnsureUtc(),
      GitHubPushedAt = GitHubPushedAt?.EnsureUtc(),
      DefaultBranch = DefaultBranch,
      TopicsList = DeserializeTopics(TopicsJson)
    };

    return (T)(object)model;
  }

  private string[] DeserializeTopics(string topicsJson)
  {
    return DataValidation.DeserializeTags(topicsJson);
  }

  public static GitHubRepoEntity FromModel(GitHubRepoModel model)
  {
    // Validate required fields
    ArgumentNullException.ThrowIfNull(model);
    ArgumentNullException.ThrowIfNull(model.Name);
    ArgumentNullException.ThrowIfNull(model.FullName);

    var entity = new GitHubRepoEntity
    {
      Id = model.Id ?? Guid.NewGuid().ToString(),
      Title = DataValidation.Required(DataValidation.SafeTrim(model.Name), nameof(model.Name)),
      AuthorSlug = "system", // GitHub repos are pulled by system
      Description = DataValidation.SafeTrim(model.Description) ?? string.Empty,
      Content = DataValidation.SafeTrim(model.Content) ?? string.Empty,
      Slug = DataValidation.Required(DataValidation.SafeTrim(model.Slug ?? model.Name.ToLowerInvariant().Replace(" ", "-")), nameof(model.Slug)),
      Category = DataValidation.SafeTrim(model.Category) ?? "repository",
      Status = "Published", // GitHub repos are always considered published
      PublishDate = model.GitHubCreatedAt.EnsureUtc(),
      LastModified = model.GitHubUpdatedAt.EnsureUtc(),
      GitHubId = model.GitHubId,
      Name = DataValidation.Required(DataValidation.SafeTrim(model.Name), nameof(model.Name)),
      FullName = DataValidation.Required(DataValidation.SafeTrim(model.FullName), nameof(model.FullName)),
      HtmlUrl = model.HtmlUrl ?? string.Empty,
      Language = model.Language,
      StargazersCount = model.StargazersCount,
      ForksCount = model.ForksCount,
      WatchersCount = model.WatchersCount,
      OpenIssuesCount = model.OpenIssuesCount,
      IsPrivate = model.IsPrivate,
      IsFork = model.IsFork,
      IsArchived = model.IsArchived,
      GitHubCreatedAt = model.GitHubCreatedAt.EnsureUtc(),
      GitHubUpdatedAt = model.GitHubUpdatedAt.EnsureUtc(),
      GitHubPushedAt = model.GitHubPushedAt?.EnsureUtc(),
      DefaultBranch = model.DefaultBranch ?? "main",
      TopicsJson = JsonSerializer.Serialize(model.TopicsList ?? [])
    };

    // NOTE: Keys should be set by the service layer for consistency
    // Do not set PartitionKey/RowKey here to avoid conflicts
    return entity;
  }

  public GitHubRepoModel ToModel()
  {
    return new GitHubRepoModel
    {
      Id = Id,
      PartitionKey = PartitionKey,
      RowKey = RowKey,
      Timestamp = Timestamp,
      ETag = ETag,
      Title = Title,
      AuthorSlug = AuthorSlug,
      Description = Description,
      Content = Content,
      Slug = Slug,
      Category = Category,
      Status = Status,
      FeaturedImageId = FeaturedImageId,
      FeaturedMediaId = FeaturedMediaId,
      MediaReferencesJson = MediaReferencesJson,
      PublishDate = PublishDate,
      LastModified = LastModified,
      TagsList = string.IsNullOrEmpty(TopicsJson) ? [] : JsonSerializer.Deserialize<string[]>(TopicsJson) ?? [],
      GitHubId = GitHubId,
      Name = Name,
      FullName = FullName,
      HtmlUrl = HtmlUrl,
      Language = Language,
      StargazersCount = StargazersCount,
      ForksCount = ForksCount,
      WatchersCount = WatchersCount,
      OpenIssuesCount = OpenIssuesCount,
      IsPrivate = IsPrivate,
      IsFork = IsFork,
      IsArchived = IsArchived,
      GitHubCreatedAt = GitHubCreatedAt,
      GitHubUpdatedAt = GitHubUpdatedAt,
      GitHubPushedAt = GitHubPushedAt,
      DefaultBranch = DefaultBranch,
      TopicsList = string.IsNullOrEmpty(TopicsJson) ? [] : JsonSerializer.Deserialize<string[]>(TopicsJson) ?? []
    };
  }
}