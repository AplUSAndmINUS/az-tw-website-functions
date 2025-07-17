using SharedStorage.Models;
using Utils;
using System.Text.Json;

namespace SharedStorage.Services.Media.Platforms;

/// <summary>
/// Mock Instagram adapter for demonstration purposes
/// In production, this would integrate with Instagram's API
/// </summary>
public class InstagramPlatformAdapter : IPlatformMediaAdapter
{
  private readonly IAppInsightsLogger<InstagramPlatformAdapter> _logger;

  public string PlatformName => "instagram";

  public InstagramPlatformAdapter(IAppInsightsLogger<InstagramPlatformAdapter> logger)
  {
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  public async Task<IEnumerable<MediaEntity>> FetchRecentMediaAsync(string authorId, int limit = 50)
  {
    _logger.LogInformation("Fetching recent Instagram media for author: {AuthorId}", authorId);
    
    // Mock data - in production, this would call Instagram API
    await Task.Delay(100);

    var mockData = new List<MediaEntity>();
    var random = new Random();
    
    for (int i = 0; i < Math.Min(limit, 8); i++)
    {
      var mediaType = i % 3 == 0 ? "video" : "image";
      var entity = CreateBaseMediaEntity(authorId, mediaType);
      entity.ExternalId = $"instagram_post_{i + 1}";
      entity.ExternalUrl = $"https://www.instagram.com/p/ABC{i + 1:D3}XYZ/";
      entity.Description = $"Instagram {mediaType} {i + 1} - Mock content with hashtags";
      entity.Filename = GenerateFileName(entity.ExternalId, mediaType);
      entity.ContentType = mediaType == "video" ? "video/mp4" : "image/jpeg";
      entity.ThumbnailUrl = $"https://scontent-lax3-1.cdninstagram.com/v/mock_thumbnail_{i + 1}.jpg";
      entity.Width = 1080;
      entity.Height = 1080;
      entity.ExternalCreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 30));
      entity.LikeCount = random.Next(50, 10000);
      entity.ShareCount = random.Next(5, 1000);
      entity.ViewCount = mediaType == "video" ? random.Next(1000, 50000) : 0;
      entity.Tags = $"#instagram,#photo,#mock{i + 1}";
      entity.EmbedCode = $"<blockquote class=\"instagram-media\"><a href=\"{entity.ExternalUrl}\">Instagram post</a></blockquote>";
      entity.PlatformMetadata = JsonSerializer.Serialize(new 
      {
        platform = "instagram",
        postId = entity.ExternalId,
        username = "username",
        isVerified = false,
        location = i % 2 == 0 ? "Sample Location" : null,
        mediaType = mediaType,
        hashtags = entity.Tags.Split(','),
        mentions = new[] { "@friend1", "@friend2" }
      });
      
      mockData.Add(entity);
    }

    _logger.LogInformation("Fetched {Count} Instagram media items", mockData.Count);
    return mockData;
  }

  public async Task<MediaEntity?> FetchMediaByExternalIdAsync(string externalId, string authorId)
  {
    _logger.LogInformation("Fetching Instagram media by external ID: {ExternalId}", externalId);
    
    // Mock data - in production, this would call Instagram API
    await Task.Delay(50);

    var entity = CreateBaseMediaEntity(authorId, "image");
    entity.ExternalId = externalId;
    entity.ExternalUrl = $"https://www.instagram.com/p/{externalId}/";
    entity.Description = $"Instagram post {externalId} - Mock content";
    entity.Filename = GenerateFileName(externalId, "image");
    entity.ContentType = "image/jpeg";
    entity.ThumbnailUrl = $"https://scontent-lax3-1.cdninstagram.com/v/mock_thumbnail_{externalId}.jpg";
    entity.Width = 1080;
    entity.Height = 1080;
    entity.ExternalCreatedAt = DateTime.UtcNow.AddDays(-3);
    entity.LikeCount = 1200;
    entity.ShareCount = 80;
    entity.ViewCount = 0;
    entity.Tags = "#instagram,#photo,#mock";
    entity.EmbedCode = $"<blockquote class=\"instagram-media\"><a href=\"{entity.ExternalUrl}\">Instagram post</a></blockquote>";
    entity.PlatformMetadata = JsonSerializer.Serialize(new 
    {
      platform = "instagram",
      postId = externalId,
      username = "username",
      isVerified = false,
      location = "Sample Location",
      mediaType = "image",
      hashtags = entity.Tags.Split(','),
      mentions = new[] { "@friend1", "@friend2" }
    });

    return entity;
  }

  public async Task<bool> ValidateConnectionAsync()
  {
    _logger.LogInformation("Validating Instagram connection");
    
    // Mock validation - in production, this would test API credentials
    await Task.Delay(100);
    
    return true; // Always return true for mock
  }

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

  private string GenerateFileName(string externalId, string mediaType)
  {
    return $"{PlatformName}_{externalId}_{DateTime.UtcNow:yyyyMMdd}.{GetFileExtension(mediaType)}";
  }

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