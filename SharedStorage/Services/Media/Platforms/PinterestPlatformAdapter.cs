using SharedStorage.Models;
using Utils;
using System.Text.Json;

namespace SharedStorage.Services.Media.Platforms;

/// <summary>
/// Mock Pinterest adapter for demonstration purposes
/// In production, this would integrate with Pinterest's API
/// </summary>
public class PinterestPlatformAdapter : BasePlatformMediaAdapter
{
  public override string PlatformName => "pinterest";

  public PinterestPlatformAdapter(IAppInsightsLogger<PinterestPlatformAdapter> logger, string authorId) 
    : base(logger.As<BasePlatformMediaAdapter>(), authorId) { }

  public override async Task<IEnumerable<MediaEntity>> FetchRecentMediaAsync(string authorId, int limit = 50)
  {
    _logger.LogInformation("Fetching recent Pinterest media for author: {AuthorId}", authorId);
    
    await Task.Delay(100);
    var mockData = new List<MediaEntity>();
    var random = new Random();
    
    for (int i = 0; i < Math.Min(limit, 10); i++)
    {
      var entity = CreateBaseMediaEntity(authorId, "image");
      entity.ExternalId = $"pinterest_pin_{i + 1}";
      entity.ExternalUrl = $"https://www.pinterest.com/pin/{123456789 + i}";
      entity.Description = $"Pinterest pin {i + 1} - Creative inspiration";
      entity.Filename = GenerateFileName(entity.ExternalId, "image");
      entity.ContentType = "image/jpeg";
      entity.ThumbnailUrl = $"https://i.pinimg.com/236x/mock_thumbnail_{i + 1}.jpg";
      entity.Width = 600;
      entity.Height = 900; // Pinterest typically uses vertical images
      entity.ExternalCreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 90));
      entity.LikeCount = random.Next(5, 1000);
      entity.ShareCount = random.Next(1, 100);
      entity.ViewCount = random.Next(50, 10000);
      entity.Tags = $"#pinterest,#inspiration,#creative{i + 1}";
      entity.EmbedCode = $"<a data-pin-do=\"embedPin\" data-pin-width=\"medium\" href=\"{entity.ExternalUrl}\"></a>";
      entity.PlatformMetadata = JsonSerializer.Serialize(new 
      {
        platform = "pinterest",
        pinId = entity.ExternalId,
        boardId = "987654321",
        boardName = "Mock Board",
        mediaType = "image",
        originalUrl = "https://example.com/source",
        repinCount = entity.ShareCount,
        commentCount = random.Next(0, 50)
      });
      
      mockData.Add(entity);
    }

    _logger.LogInformation("Fetched {Count} Pinterest media items", mockData.Count);
    return mockData;
  }

  public override async Task<MediaEntity?> FetchMediaByExternalIdAsync(string externalId, string authorId)
  {
    _logger.LogInformation("Fetching Pinterest media by external ID: {ExternalId}", externalId);
    await Task.Delay(50);

    var entity = CreateBaseMediaEntity(authorId, "image");
    entity.ExternalId = externalId;
    entity.ExternalUrl = $"https://www.pinterest.com/pin/{externalId}";
    entity.Description = $"Pinterest pin {externalId} - Creative inspiration";
    entity.Filename = GenerateFileName(externalId, "image");
    entity.ContentType = "image/jpeg";
    entity.ThumbnailUrl = $"https://i.pinimg.com/236x/mock_thumbnail_{externalId}.jpg";
    entity.Width = 600;
    entity.Height = 900;
    entity.ExternalCreatedAt = DateTime.UtcNow.AddDays(-10);
    entity.LikeCount = 350;
    entity.ShareCount = 45;
    entity.ViewCount = 3500;
    entity.Tags = "#pinterest,#inspiration,#creative";
    entity.EmbedCode = $"<a data-pin-do=\"embedPin\" data-pin-width=\"medium\" href=\"{entity.ExternalUrl}\"></a>";
    entity.PlatformMetadata = JsonSerializer.Serialize(new 
    {
      platform = "pinterest",
      pinId = externalId,
      boardId = "987654321",
      boardName = "Mock Board",
      mediaType = "image",
      originalUrl = "https://example.com/source",
      repinCount = entity.ShareCount,
      commentCount = 15
    });

    return entity;
  }

  public override async Task<bool> ValidateConnectionAsync()
  {
    _logger.LogInformation("Validating Pinterest connection");
    await Task.Delay(100);
    return true;
  }
}