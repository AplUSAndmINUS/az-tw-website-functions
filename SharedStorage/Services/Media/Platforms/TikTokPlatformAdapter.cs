using SharedStorage.Models;
using Utils;
using System.Text.Json;

namespace SharedStorage.Services.Media.Platforms;

/// <summary>
/// Mock TikTok adapter for demonstration purposes
/// In production, this would integrate with TikTok's API
/// </summary>
public class TikTokPlatformAdapter : IPlatformMediaAdapter
{
  private readonly IAppInsightsLogger<TikTokPlatformAdapter> _logger;

  public string PlatformName => "tiktok";

  public TikTokPlatformAdapter(IAppInsightsLogger<TikTokPlatformAdapter> logger)
  {
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  public override async Task<IEnumerable<MediaEntity>> FetchRecentMediaAsync(string authorId, int limit = 50)
  {
    _logger.LogInformation("Fetching recent TikTok media for author: {AuthorId}", authorId);
    
    // Mock data - in production, this would call TikTok API
    await Task.Delay(100); // Simulate API call

    var mockData = new List<MediaEntity>();
    var random = new Random();
    
    for (int i = 0; i < Math.Min(limit, 5); i++)
    {
      var entity = CreateBaseMediaEntity(authorId, "video");
      entity.ExternalId = $"tiktok_video_{i + 1}";
      entity.ExternalUrl = $"https://www.tiktok.com/@username/video/{7000000000000000000 + i}";
      entity.Description = $"TikTok video {i + 1} - Mock content";
      entity.Filename = GenerateFileName(entity.ExternalId, "video");
      entity.ContentType = "video/mp4";
      entity.ThumbnailUrl = $"https://p16-sign-sg.tiktokcdn.com/obj/tos-maliva-p-0068/mock_thumbnail_{i + 1}.jpg";
      entity.Width = 720;
      entity.Height = 1280;
      entity.ExternalCreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 30));
      entity.LikeCount = random.Next(100, 50000);
      entity.ShareCount = random.Next(10, 5000);
      entity.ViewCount = random.Next(1000, 100000);
      entity.Tags = $"#trending,#tiktok,#video{i + 1}";
      entity.EmbedCode = $"<blockquote class=\"tiktok-embed\"><a href=\"{entity.ExternalUrl}\">@username</a></blockquote>";
      entity.PlatformMetadata = JsonSerializer.Serialize(new 
      {
        platform = "tiktok",
        videoId = entity.ExternalId,
        username = "username",
        isVerified = true,
        musicTitle = $"Original Sound - Mock {i + 1}",
        effects = new[] { "effect1", "effect2" },
        hashtags = entity.Tags.Split(',')
      });
      
      mockData.Add(entity);
    }

    _logger.LogInformation("Fetched {Count} TikTok media items", mockData.Count);
    return mockData;
  }

  public override async Task<MediaEntity?> FetchMediaByExternalIdAsync(string externalId, string authorId)
  {
    _logger.LogInformation("Fetching TikTok media by external ID: {ExternalId}", externalId);
    
    // Mock data - in production, this would call TikTok API
    await Task.Delay(50);

    var entity = CreateBaseMediaEntity(authorId, "video");
    entity.ExternalId = externalId;
    entity.ExternalUrl = $"https://www.tiktok.com/@username/video/{externalId}";
    entity.Description = $"TikTok video {externalId} - Mock content";
    entity.Filename = GenerateFileName(externalId, "video");
    entity.ContentType = "video/mp4";
    entity.ThumbnailUrl = $"https://p16-sign-sg.tiktokcdn.com/obj/tos-maliva-p-0068/mock_thumbnail_{externalId}.jpg";
    entity.Width = 720;
    entity.Height = 1280;
    entity.ExternalCreatedAt = DateTime.UtcNow.AddDays(-7);
    entity.LikeCount = 15000;
    entity.ShareCount = 500;
    entity.ViewCount = 75000;
    entity.Tags = "#trending,#tiktok,#mock";
    entity.EmbedCode = $"<blockquote class=\"tiktok-embed\"><a href=\"{entity.ExternalUrl}\">@username</a></blockquote>";
    entity.PlatformMetadata = JsonSerializer.Serialize(new 
    {
      platform = "tiktok",
      videoId = externalId,
      username = "username",
      isVerified = true,
      musicTitle = "Original Sound - Mock",
      effects = new[] { "effect1", "effect2" },
      hashtags = entity.Tags.Split(',')
    });

    return entity;
  }

  public override async Task<bool> ValidateConnectionAsync()
  {
    _logger.LogInformation("Validating TikTok connection");
    
    // Mock validation - in production, this would test API credentials
    await Task.Delay(100);
    
    return true; // Always return true for mock
  }

  /// <summary>
  /// Creates a base MediaEntity with common properties set
  /// </summary>
  private MediaEntity CreateBaseMediaEntity(string authorId, string mediaType = "image")
  {
    return new MediaEntity
    {
      Id = Guid.NewGuid().ToString(),
      AuthorId = authorId,
      PartitionKey = authorId,
      RowKey = Guid.NewGuid().ToString(),
      Platform = PlatformName,
      MediaType = mediaType,
      Purpose = "gallery",
      UploadedAt = DateTime.UtcNow,
      LastSyncedAt = DateTime.UtcNow
    };
  }

  /// <summary>
  /// Generates a safe filename from platform content
  /// </summary>
  private string GenerateFileName(string externalId, string mediaType)
  {
    return $"{PlatformName}_{externalId}_{DateTime.UtcNow:yyyyMMdd}.{GetFileExtension(mediaType)}";
  }

  /// <summary>
  /// Gets file extension based on media type
  /// </summary>
  private string GetFileExtension(string mediaType)
  {
    return mediaType.ToLowerInvariant() switch
    {
      "image" => "jpg",
      "video" => "mp4",
      "audio" => "mp3",
      _ => "dat"
    };
  }
}