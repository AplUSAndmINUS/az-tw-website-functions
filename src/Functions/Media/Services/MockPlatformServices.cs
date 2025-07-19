using SharedStorage.Models;
using Utils;

namespace Functions.Media.Services;

/// <summary>
/// Mock implementation of TikTok service for demonstration purposes
/// In production, this would integrate with TikTok's API
/// </summary>
public class MockTikTokService : ITikTokService
{
    private readonly IAppInsightsLogger<MockTikTokService> _logger;

    public MockTikTokService(IAppInsightsLogger<MockTikTokService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<MediaItemModel>> GetLatestMediaAsync(int limit = 50)
    {
        _logger.LogInformation("Fetching latest TikTok media (mock data)");

        await Task.Delay(100); // Simulate API call

        var mockMedia = new List<MediaItemModel>();

        for (int i = 1; i <= Math.Min(limit, 5); i++)
        {
            mockMedia.Add(new MediaItemModel
            {
                Id = Guid.NewGuid().ToString(),
                Platform = "TikTok",
                IsExternal = true,
                ExternalId = $"tiktok_{i}_{DateTime.UtcNow:yyyyMMdd}",
                ExternalUrl = $"https://www.tiktok.com/@user/video/{i}234567890",
                MediaType = "video",
                Filename = $"tiktok_video_{i}.mp4",
                Description = $"TikTok video {i} - Mock content for testing",
                Url = $"https://www.tiktok.com/@user/video/{i}234567890",
                ThumbnailUrl = $"https://p16-sign-va.tiktokcdn.com/thumbnail{i}.jpg",
                Width = 720,
                Height = 1280,
                Duration = 30 + i * 5, // 30-50 seconds
                VideoQuality = "HD",
                UploadedAt = DateTime.UtcNow.AddDays(-i),
                LastModified = DateTime.UtcNow,
                AuthorId = "tiktok_user_123",
                Purpose = "social_media_content"
            });
        }

        _logger.LogInformation("Retrieved {Count} mock TikTok media items", mockMedia.Count);
        return mockMedia;
    }
}

/// <summary>
/// Mock implementation of Instagram service for demonstration purposes
/// </summary>
public class MockInstagramService : IInstagramService
{
    private readonly IAppInsightsLogger<MockInstagramService> _logger;

    public MockInstagramService(IAppInsightsLogger<MockInstagramService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<MediaItemModel>> GetLatestMediaAsync(int limit = 50)
    {
        _logger.LogInformation("Fetching latest Instagram media (mock data)");

        await Task.Delay(100);

        var mockMedia = new List<MediaItemModel>();

        for (int i = 1; i <= Math.Min(limit, 6); i++)
        {
            var isVideo = i % 3 == 0; // Every 3rd item is a video
            
            mockMedia.Add(new MediaItemModel
            {
                Id = Guid.NewGuid().ToString(),
                Platform = "Instagram",
                IsExternal = true,
                ExternalId = $"instagram_{i}_{DateTime.UtcNow:yyyyMMdd}",
                ExternalUrl = $"https://www.instagram.com/p/ABC{i}XYZ/",
                MediaType = isVideo ? "video" : "image",
                Filename = isVideo ? $"instagram_video_{i}.mp4" : $"instagram_photo_{i}.jpg",
                Description = $"Instagram {(isVideo ? "video" : "photo")} {i} - Mock content for testing",
                Url = $"https://scontent.cdninstagram.com/media{i}.{(isVideo ? "mp4" : "jpg")}",
                ThumbnailUrl = $"https://scontent.cdninstagram.com/thumb{i}.jpg",
                Width = 1080,
                Height = 1080,
                Duration = isVideo ? 15 + i * 3 : 0,
                VideoQuality = isVideo ? "Full HD" : "",
                UploadedAt = DateTime.UtcNow.AddHours(-i * 2),
                LastModified = DateTime.UtcNow,
                AuthorId = "instagram_user_456",
                Purpose = "social_media_content"
            });
        }

        _logger.LogInformation("Retrieved {Count} mock Instagram media items", mockMedia.Count);
        return mockMedia;
    }
}

/// <summary>
/// Mock implementation of YouTube service for demonstration purposes
/// </summary>
public class MockYouTubeService : IYouTubeService
{
    private readonly IAppInsightsLogger<MockYouTubeService> _logger;

    public MockYouTubeService(IAppInsightsLogger<MockYouTubeService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<MediaItemModel>> GetLatestMediaAsync(int limit = 50)
    {
        _logger.LogInformation("Fetching latest YouTube media (mock data)");

        await Task.Delay(150);

        var mockMedia = new List<MediaItemModel>();

        for (int i = 1; i <= Math.Min(limit, 4); i++)
        {
            mockMedia.Add(new MediaItemModel
            {
                Id = Guid.NewGuid().ToString(),
                Platform = "YouTube",
                IsExternal = true,
                ExternalId = $"youtube_video_{i}_{DateTime.UtcNow:yyyyMMdd}",
                ExternalUrl = $"https://www.youtube.com/watch?v=ABC{i}XYZ123",
                MediaType = "video",
                Filename = $"youtube_video_{i}.mp4",
                Description = $"YouTube video {i} - Mock content for testing. This is a longer description that might include keywords and detailed information about the video content.",
                Url = $"https://www.youtube.com/watch?v=ABC{i}XYZ123",
                ThumbnailUrl = $"https://i.ytimg.com/vi/ABC{i}XYZ123/hqdefault.jpg",
                Width = 1920,
                Height = 1080,
                Duration = 300 + i * 60, // 5-8 minutes
                VideoQuality = "Full HD",
                UploadedAt = DateTime.UtcNow.AddDays(-i * 2),
                LastModified = DateTime.UtcNow,
                AuthorId = "youtube_channel_789",
                Purpose = "educational_content"
            });
        }

        _logger.LogInformation("Retrieved {Count} mock YouTube media items", mockMedia.Count);
        return mockMedia;
    }
}

/// <summary>
/// Mock implementation of Facebook service for demonstration purposes
/// </summary>
public class MockFacebookService : IFacebookService
{
    private readonly IAppInsightsLogger<MockFacebookService> _logger;

    public MockFacebookService(IAppInsightsLogger<MockFacebookService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<MediaItemModel>> GetLatestMediaAsync(int limit = 50)
    {
        _logger.LogInformation("Fetching latest Facebook media (mock data)");

        await Task.Delay(120);

        var mockMedia = new List<MediaItemModel>();

        for (int i = 1; i <= Math.Min(limit, 3); i++)
        {
            var isVideo = i % 2 == 0;

            mockMedia.Add(new MediaItemModel
            {
                Id = Guid.NewGuid().ToString(),
                Platform = "Facebook",
                IsExternal = true,
                ExternalId = $"facebook_{i}_{DateTime.UtcNow:yyyyMMdd}",
                ExternalUrl = $"https://www.facebook.com/photo.php?fbid=123456789{i}",
                MediaType = isVideo ? "video" : "image",
                Filename = isVideo ? $"facebook_video_{i}.mp4" : $"facebook_photo_{i}.jpg",
                Description = $"Facebook {(isVideo ? "video" : "photo")} {i} - Mock content for testing",
                Url = $"https://scontent.xx.fbcdn.net/media{i}.{(isVideo ? "mp4" : "jpg")}",
                ThumbnailUrl = $"https://scontent.xx.fbcdn.net/thumb{i}.jpg",
                Width = isVideo ? 1280 : 1200,
                Height = isVideo ? 720 : 800,
                Duration = isVideo ? 45 + i * 10 : 0,
                VideoQuality = isVideo ? "HD" : "",
                UploadedAt = DateTime.UtcNow.AddDays(-i * 3),
                LastModified = DateTime.UtcNow,
                AuthorId = "facebook_page_101112",
                Purpose = "social_media_content"
            });
        }

        _logger.LogInformation("Retrieved {Count} mock Facebook media items", mockMedia.Count);
        return mockMedia;
    }
}

/// <summary>
/// Mock implementation of LinkedIn service for demonstration purposes
/// </summary>
public class MockLinkedInService : ILinkedInService
{
    private readonly IAppInsightsLogger<MockLinkedInService> _logger;

    public MockLinkedInService(IAppInsightsLogger<MockLinkedInService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<MediaItemModel>> GetLatestMediaAsync(int limit = 50)
    {
        _logger.LogInformation("Fetching latest LinkedIn media (mock data)");

        await Task.Delay(130);

        var mockMedia = new List<MediaItemModel>();

        for (int i = 1; i <= Math.Min(limit, 2); i++)
        {
            mockMedia.Add(new MediaItemModel
            {
                Id = Guid.NewGuid().ToString(),
                Platform = "LinkedIn",
                IsExternal = true,
                ExternalId = $"linkedin_{i}_{DateTime.UtcNow:yyyyMMdd}",
                ExternalUrl = $"https://www.linkedin.com/posts/user_activity-{i}234567890",
                MediaType = "image",
                Filename = $"linkedin_post_{i}.jpg",
                Description = $"LinkedIn professional post {i} - Mock content for testing. Professional insights and business content.",
                Url = $"https://media.licdn.com/dms/image/media{i}.jpg",
                ThumbnailUrl = $"https://media.licdn.com/dms/image/thumb{i}.jpg",
                Width = 1200,
                Height = 630,
                Duration = 0,
                VideoQuality = "",
                UploadedAt = DateTime.UtcNow.AddDays(-i * 5),
                LastModified = DateTime.UtcNow,
                AuthorId = "linkedin_profile_131415",
                Purpose = "professional_content"
            });
        }

        _logger.LogInformation("Retrieved {Count} mock LinkedIn media items", mockMedia.Count);
        return mockMedia;
    }
}

/// <summary>
/// Mock implementation of Pinterest service for demonstration purposes
/// </summary>
public class MockPinterestService : IPinterestService
{
    private readonly IAppInsightsLogger<MockPinterestService> _logger;

    public MockPinterestService(IAppInsightsLogger<MockPinterestService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<MediaItemModel>> GetLatestMediaAsync(int limit = 50)
    {
        _logger.LogInformation("Fetching latest Pinterest media (mock data)");

        await Task.Delay(110);

        var mockMedia = new List<MediaItemModel>();

        for (int i = 1; i <= Math.Min(limit, 4); i++)
        {
            mockMedia.Add(new MediaItemModel
            {
                Id = Guid.NewGuid().ToString(),
                Platform = "Pinterest",
                IsExternal = true,
                ExternalId = $"pinterest_{i}_{DateTime.UtcNow:yyyyMMdd}",
                ExternalUrl = $"https://www.pinterest.com/pin/{i}234567890/",
                MediaType = "image",
                Filename = $"pinterest_pin_{i}.jpg",
                Description = $"Pinterest pin {i} - Mock content for testing. Creative and inspirational content.",
                Url = $"https://i.pinimg.com/originals/media{i}.jpg",
                ThumbnailUrl = $"https://i.pinimg.com/236x/thumb{i}.jpg",
                Width = 736,
                Height = 1104, // Typical Pinterest ratio
                Duration = 0,
                VideoQuality = "",
                UploadedAt = DateTime.UtcNow.AddDays(-i * 4),
                LastModified = DateTime.UtcNow,
                AuthorId = "pinterest_user_161718",
                Purpose = "creative_content"
            });
        }

        _logger.LogInformation("Retrieved {Count} mock Pinterest media items", mockMedia.Count);
        return mockMedia;
    }
}