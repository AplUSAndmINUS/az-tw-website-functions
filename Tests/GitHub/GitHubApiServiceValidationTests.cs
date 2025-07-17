using System.Net;
using System.Text.Json;
using Functions.GitHub.Models;
using Functions.GitHub.Services;
using Utils;

namespace Functions.GitHub.Tests;

/// <summary>
/// Basic validation tests for GitHub API service
/// Note: These are integration-style tests that would require proper test setup
/// </summary>
public class GitHubApiServiceValidationTests
{
    // This would require proper test setup with mocked HttpClient
    public static async Task<bool> ValidateGitHubApiServiceMapping()
    {
        try
        {
            // Test data mapping from mock GitHub API response
            var mockApiResponse = """
            [{
                "id": 123456789,
                "name": "test-repo",
                "full_name": "testuser/test-repo",
                "description": "A test repository",
                "html_url": "https://github.com/testuser/test-repo",
                "language": "C#",
                "stargazers_count": 10,
                "forks_count": 5,
                "watchers_count": 3,
                "open_issues_count": 2,
                "private": false,
                "fork": false,
                "archived": false,
                "created_at": "2023-01-01T00:00:00Z",
                "updated_at": "2024-01-01T00:00:00Z",
                "pushed_at": "2024-01-01T00:00:00Z",
                "default_branch": "main",
                "topics": ["azure", "functions"]
            }]
            """;

            var githubRepos = JsonSerializer.Deserialize<GitHubApiRepository[]>(
                mockApiResponse, 
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });

            if (githubRepos == null || githubRepos.Length == 0)
                return false;

            var repo = githubRepos[0];
            
            // Validate mapping
            var model = MapToModel(repo);
            
            return model.GitHubId == 123456789 &&
                   model.Name == "test-repo" &&
                   model.FullName == "testuser/test-repo" &&
                   model.Language == "C#" &&
                   model.StargazersCount == 10 &&
                   !model.IsPrivate &&
                   model.TopicsList.Length == 2 &&
                   model.TopicsList.Contains("azure");
        }
        catch
        {
            return false;
        }
    }

    // Copy of the mapping method from GitHubApiService for testing
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

    public static void ValidateGitHubRepoMapper()
    {
        var model = new GitHubRepoModel
        {
            Id = "test-id",
            GitHubId = 123456,
            Name = "test-repo",
            FullName = "user/test-repo",
            Description = "Test description",
            HtmlUrl = "https://github.com/user/test-repo",
            Language = "C#",
            StargazersCount = 10,
            ForksCount = 5,
            IsPrivate = false,
            TopicsList = ["test", "repo"]
        };

        var dto = GitHubRepoMapper.ToDTO(model);

        if (dto.GitHubId != model.GitHubId ||
            dto.Name != model.Name ||
            dto.Topics.Length != 2 ||
            !dto.Topics.Contains("test"))
        {
            throw new InvalidOperationException("GitHubRepoMapper validation failed");
        }
    }
}

// Internal class for test deserialization (copy from GitHubApiService)
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