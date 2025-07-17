using SharedStorage.Models;
using Utils;
using System.Text.Json;

namespace SharedStorage.Services.Media.Platforms;

/// <summary>
/// Mock YouTube adapter for demonstration purposes
/// In production, this would integrate with YouTube's API
/// </summary>
public class YouTubePlatformAdapter : IPlatformMediaAdapter
{
  private readonly IAppInsightsLogger<YouTubePlatformAdapter> _logger;

  public string PlatformName => "youtube";

  public YouTubePlatformAdapter(IAppInsightsLogger<YouTubePlatformAdapter> logger)
  {
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  public async Task<IEnumerable<MediaEntity>> FetchRecentMediaAsync(string authorId, int limit = 50)
  {
    _logger.LogInformation("Fetching recent YouTube media for author: {AuthorId}", authorId);
    
    // Mock data - in production, this would call YouTube API
    await Task.Delay(100);

    var mockData = new List<MediaEntity>();
    var random = new Random();
    
    for (int i = 0; i < Math.Min(limit, 6); i++)
    {
      var entity = CreateBaseMediaEntity(authorId, "video");
      entity.ExternalId = $"youtube_video_{i + 1}";
      entity.ExternalUrl = $"https://www.youtube.com/watch?v=ABC{i + 1:D3}XYZ";
      entity.Description = $"YouTube video {i + 1} - Mock educational content";
      entity.Filename = GenerateFileName(entity.ExternalId, "video");
      entity.ContentType = "video/mp4";
      entity.ThumbnailUrl = $"https://i.ytimg.com/vi/ABC{i + 1:D3}XYZ/hqdefault.jpg";
      entity.Width = 1920;
      entity.Height = 1080;
      entity.ExternalCreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 60));
      entity.LikeCount = random.Next(100, 5000);
      entity.ShareCount = random.Next(10, 500);
      entity.ViewCount = random.Next(1000, 100000);
      entity.Tags = $"#youtube,#education,#tutorial{i + 1}";
      entity.EmbedCode = $"<iframe width=\"560\" height=\"315\" src=\"https://www.youtube.com/embed/ABC{i + 1:D3}XYZ\" frameborder=\"0\" allowfullscreen></iframe>";
      entity.PlatformMetadata = JsonSerializer.Serialize(new 
      {
        platform = "youtube",
        videoId = entity.ExternalId,
        channelId = "UC123456789",
        channelTitle = "Mock Channel",
        duration = "PT" + random.Next(1, 20) + "M" + random.Next(1, 60) + "S",
        categoryId = "22", // People & Blogs
        definition = "hd",
        caption = "false",
        tags = entity.Tags.Split(',')
      });
      
      mockData.Add(entity);
    }

    _logger.LogInformation("Fetched {Count} YouTube media items", mockData.Count);
    return mockData;
  }

  public async Task<MediaEntity?> FetchMediaByExternalIdAsync(string externalId, string authorId)
  {
    _logger.LogInformation("Fetching YouTube media by external ID: {ExternalId}", externalId);
    
    // Mock data - in production, this would call YouTube API
    await Task.Delay(50);

    var entity = CreateBaseMediaEntity(authorId, "video");
    entity.ExternalId = externalId;
    entity.ExternalUrl = $"https://www.youtube.com/watch?v={externalId}";
    entity.Description = $"YouTube video {externalId} - Mock educational content";
    entity.Filename = GenerateFileName(externalId, "video");
    entity.ContentType = "video/mp4";
    entity.ThumbnailUrl = $"https://i.ytimg.com/vi/{externalId}/hqdefault.jpg";
    entity.Width = 1920;
    entity.Height = 1080;
    entity.ExternalCreatedAt = DateTime.UtcNow.AddDays(-14);
    entity.LikeCount = 2500;
    entity.ShareCount = 150;
    entity.ViewCount = 45000;
    entity.Tags = "#youtube,#education,#tutorial";
    entity.EmbedCode = $"<iframe width=\"560\" height=\"315\" src=\"https://www.youtube.com/embed/{externalId}\" frameborder=\"0\" allowfullscreen></iframe>";
    entity.PlatformMetadata = JsonSerializer.Serialize(new 
    {
      platform = "youtube",
      videoId = externalId,
      channelId = "UC123456789",
      channelTitle = "Mock Channel",
      duration = "PT15M30S",
      categoryId = "22",
      definition = "hd",
      caption = "false",
      tags = entity.Tags.Split(',')
    });

    return entity;
  }

  public async Task<bool> ValidateConnectionAsync()
  {
    _logger.LogInformation("Validating YouTube connection");
    
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