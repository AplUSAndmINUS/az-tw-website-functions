using SharedStorage.Models;
using Utils;
using System.Text.Json;

namespace SharedStorage.Services.Media.Platforms;

/// <summary>
/// Mock LinkedIn adapter for demonstration purposes
/// In production, this would integrate with LinkedIn's API
/// </summary>
public class LinkedInPlatformAdapter : IPlatformMediaAdapter
{
  private readonly IAppInsightsLogger<LinkedInPlatformAdapter> _logger;

  public string PlatformName => "linkedin";

  public LinkedInPlatformAdapter(IAppInsightsLogger<LinkedInPlatformAdapter> logger)
  {
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  public async Task<IEnumerable<MediaEntity>> FetchRecentMediaAsync(string authorId, int limit = 50)
  {
    _logger.LogInformation("Fetching recent LinkedIn media for author: {AuthorId}", authorId);
    
    await Task.Delay(100);
    var mockData = new List<MediaEntity>();
    var random = new Random();
    
    for (int i = 0; i < Math.Min(limit, 3); i++)
    {
      var entity = CreateBaseMediaEntity(authorId, "image");
      entity.ExternalId = $"linkedin_post_{i + 1}";
      entity.ExternalUrl = $"https://www.linkedin.com/posts/username_{i + 1}";
      entity.Description = $"LinkedIn post {i + 1} - Professional content";
      entity.Filename = GenerateFileName(entity.ExternalId, "image");
      entity.ContentType = "image/jpeg";
      entity.ThumbnailUrl = $"https://media.licdn.com/dms/image/mock_thumbnail_{i + 1}.jpg";
      entity.Width = 1200;
      entity.Height = 627;
      entity.ExternalCreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 14));
      entity.LikeCount = random.Next(5, 500);
      entity.ShareCount = random.Next(1, 50);
      entity.ViewCount = random.Next(100, 5000);
      entity.Tags = $"#linkedin,#professional,#career{i + 1}";
      entity.EmbedCode = $"<iframe src=\"https://www.linkedin.com/embed/feed/update/urn:li:share:{i + 1}\" frameborder=\"0\" allowfullscreen></iframe>";
      entity.PlatformMetadata = JsonSerializer.Serialize(new 
      {
        platform = "linkedin",
        postId = entity.ExternalId,
        authorUrn = "urn:li:person:123456789",
        mediaType = "image",
        reactions = new { like = entity.LikeCount, celebrate = 5, support = 3 }
      });
      
      mockData.Add(entity);
    }

    _logger.LogInformation("Fetched {Count} LinkedIn media items", mockData.Count);
    return mockData;
  }

  public async Task<MediaEntity?> FetchMediaByExternalIdAsync(string externalId, string authorId)
  {
    _logger.LogInformation("Fetching LinkedIn media by external ID: {ExternalId}", externalId);
    await Task.Delay(50);

    var entity = CreateBaseMediaEntity(authorId, "image");
    entity.ExternalId = externalId;
    entity.ExternalUrl = $"https://www.linkedin.com/posts/username_{externalId}";
    entity.Description = $"LinkedIn post {externalId} - Professional content";
    entity.Filename = GenerateFileName(externalId, "image");
    entity.ContentType = "image/jpeg";
    entity.ThumbnailUrl = $"https://media.licdn.com/dms/image/mock_thumbnail_{externalId}.jpg";
    entity.Width = 1200;
    entity.Height = 627;
    entity.ExternalCreatedAt = DateTime.UtcNow.AddDays(-2);
    entity.LikeCount = 125;
    entity.ShareCount = 15;
    entity.ViewCount = 2000;
    entity.Tags = "#linkedin,#professional,#career";
    entity.EmbedCode = $"<iframe src=\"https://www.linkedin.com/embed/feed/update/urn:li:share:{externalId}\" frameborder=\"0\" allowfullscreen></iframe>";
    entity.PlatformMetadata = JsonSerializer.Serialize(new 
    {
      platform = "linkedin",
      postId = externalId,
      authorUrn = "urn:li:person:123456789",
      mediaType = "image",
      reactions = new { like = entity.LikeCount, celebrate = 5, support = 3 }
    });

    return entity;
  }

  public async Task<bool> ValidateConnectionAsync()
  {
    _logger.LogInformation("Validating LinkedIn connection");
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