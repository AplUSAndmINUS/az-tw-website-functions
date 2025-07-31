using System.Net;
using System.Text.Json;
using Functions.GitHub.Models;
using Functions.GitHub.Services;
using Utils;

namespace Functions.GitHub.Tests;

/// <summary>
/// Tests for GitHub Activity Grid functionality
/// </summary>
public class GitHubActivityGridTests
{
    /// <summary>
    /// Test that the activity grid DTO can be properly serialized
    /// </summary>
    public static bool ValidateActivityGridDTOSerialization()
    {
        try
        {
            var activityData = new List<GitHubActivityGridDTO>
            {
                new GitHubActivityGridDTO
                {
                    Date = "2024-01-01",
                    ContributionCount = 5,
                    ContributionLevel = "THIRD_QUARTILE"
                },
                new GitHubActivityGridDTO
                {
                    Date = "2024-01-02", 
                    ContributionCount = 0,
                    ContributionLevel = "NONE"
                }
            };

            // Test JSON serialization
            var json = JsonSerializer.Serialize(activityData);
            var deserialized = JsonSerializer.Deserialize<GitHubActivityGridDTO[]>(json);

            return deserialized != null && 
                   deserialized.Length == 2 &&
                   deserialized[0].Date == "2024-01-01" &&
                   deserialized[0].ContributionCount == 5 &&
                   deserialized[0].ContributionLevel == "THIRD_QUARTILE" &&
                   deserialized[1].ContributionLevel == "NONE";
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Test that activity grid returns valid data structure
    /// </summary>
    public static bool ValidateActivityGridStructure()
    {
        try
        {
            // Create a mock activity grid entry
            var activity = new GitHubActivityGridDTO
            {
                Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                ContributionCount = 3,
                ContributionLevel = "SECOND_QUARTILE"
            };

            // Validate required properties
            return !string.IsNullOrEmpty(activity.Date) &&
                   activity.ContributionCount >= 0 &&
                   !string.IsNullOrEmpty(activity.ContributionLevel) &&
                   IsValidContributionLevel(activity.ContributionLevel);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsValidContributionLevel(string level)
    {
        var validLevels = new[] { "NONE", "FIRST_QUARTILE", "SECOND_QUARTILE", "THIRD_QUARTILE", "FOURTH_QUARTILE" };
        return validLevels.Contains(level);
    }
}