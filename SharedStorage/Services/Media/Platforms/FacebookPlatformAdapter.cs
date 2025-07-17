using SharedStorage.Models;
using Utils;
using System.Text.Json;

namespace SharedStorage.Services.Media.Platforms;

/// <summary>
/// Mock Facebook adapter for demonstration purposes
/// In production, this would integrate with Facebook's API
/// </summary>
public class FacebookPlatformAdapter : IPlatformMediaAdapter
{
  private readonly IAppInsightsLogger<FacebookPlatformAdapter> _logger;

  public string PlatformName => "facebook";

  public FacebookPlatformAdapter(IAppInsightsLogger<FacebookPlatformAdapter> logger)
  {
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  public async Task<IEnumerable<MediaEntity>> FetchRecentMediaAsync(string authorId, int limit = 50)
  {
    _logger.LogInformation("Fetching recent Facebook media for author: {AuthorId}", authorId);
    
    await Task.Delay(100);
    var mockData = new List<MediaEntity>();
    var random = new Random();
    
    for (int i = 0; i < Math.Min(limit, 4); i++)
    {
      var mediaType = i % 2 == 0 ? "image" : "video";
      var entity = CreateBaseMediaEntity(authorId, mediaType);
      entity.ExternalId = $"facebook_post_{i + 1}";
      entity.ExternalUrl = $"https://www.facebook.com/username/posts/{123456789 + i}";
      entity.Description = $"Facebook {mediaType} {i + 1} - Mock social content";
      entity.Filename = GenerateFileName(entity.ExternalId, mediaType);
      entity.ContentType = mediaType == "video" ? "video/mp4" : "image/jpeg";
      entity.ThumbnailUrl = $"https://scontent-lax3-1.xx.fbcdn.net/v/mock_thumbnail_{i + 1}.jpg";
      entity.Width = 1200;
      entity.Height = 630;
      entity.ExternalCreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 30));
      entity.LikeCount = random.Next(10, 1000);
      entity.ShareCount = random.Next(5, 100);
      entity.ViewCount = mediaType == "video" ? random.Next(500, 10000) : 0;
      entity.Tags = $"#facebook,#social,#mock{i + 1}";
      entity.EmbedCode = $"<div class=\"fb-post\" data-href=\"{entity.ExternalUrl}\"></div>";
      entity.PlatformMetadata = JsonSerializer.Serialize(new 
      {
        platform = "facebook",
        postId = entity.ExternalId,
        pageId = "123456789",
        mediaType = mediaType,
        reactions = new { like = entity.LikeCount, love = 10, wow = 5 }
      });
      
      mockData.Add(entity);
    }

    _logger.LogInformation("Fetched {Count} Facebook media items", mockData.Count);
    return mockData;
  }

  public async Task<MediaEntity?> FetchMediaByExternalIdAsync(string externalId, string authorId)
  {
    _logger.LogInformation("Fetching Facebook media by external ID: {ExternalId}", externalId);
    await Task.Delay(50);

    var entity = CreateBaseMediaEntity(authorId, "image");
    entity.ExternalId = externalId;
    entity.ExternalUrl = $"https://www.facebook.com/username/posts/{externalId}";
    entity.Description = $"Facebook post {externalId} - Mock social content";
    entity.Filename = GenerateFileName(externalId, "image");
    entity.ContentType = "image/jpeg";
    entity.ThumbnailUrl = $"https://scontent-lax3-1.xx.fbcdn.net/v/mock_thumbnail_{externalId}.jpg";
    entity.Width = 1200;
    entity.Height = 630;
    entity.ExternalCreatedAt = DateTime.UtcNow.AddDays(-5);
    entity.LikeCount = 250;
    entity.ShareCount = 25;
    entity.ViewCount = 0;
    entity.Tags = "#facebook,#social,#mock";
    entity.EmbedCode = $"<div class=\"fb-post\" data-href=\"{entity.ExternalUrl}\"></div>";
    entity.PlatformMetadata = JsonSerializer.Serialize(new 
    {
      platform = "facebook",
      postId = externalId,
      pageId = "123456789",
      mediaType = "image",
      reactions = new { like = entity.LikeCount, love = 10, wow = 5 }
    });

    return entity;
  }

  public async Task<bool> ValidateConnectionAsync()
  {
    _logger.LogInformation("Validating Facebook connection");
    await Task.Delay(100);
    return true;
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