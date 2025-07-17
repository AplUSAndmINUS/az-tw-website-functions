using SharedStorage.Models;
using Utils;

namespace SharedStorage.Services.Media.Platforms;

/// <summary>
/// Base class for platform media adapters with common functionality
/// </summary>
public abstract class BasePlatformMediaAdapter : IPlatformMediaAdapter
{
  protected readonly IAppInsightsLogger<BasePlatformMediaAdapter> _logger;

  public abstract string PlatformName { get; }

  protected BasePlatformMediaAdapter(IAppInsightsLogger<BasePlatformMediaAdapter> logger)
  {
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  public abstract Task<IEnumerable<MediaEntity>> FetchRecentMediaAsync(string authorId, int limit = 50);
  public abstract Task<MediaEntity?> FetchMediaByExternalIdAsync(string externalId, string authorId);
  public abstract Task<bool> ValidateConnectionAsync();

  /// <summary>
  /// Creates a base MediaEntity with common properties set
  /// </summary>
  protected MediaEntity CreateBaseMediaEntity(string authorId, string mediaType = "image")
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
  /// Sanitizes text content from external platforms
  /// </summary>
  protected string SanitizeText(string? text)
  {
    if (string.IsNullOrWhiteSpace(text))
      return string.Empty;

    // Basic sanitization - remove potentially harmful content
    return text.Trim()
      .Replace("<script>", "")
      .Replace("</script>", "")
      .Replace("javascript:", "")
      .Replace("data:", "");
  }

  /// <summary>
  /// Generates a safe filename from platform content
  /// </summary>
  protected string GenerateFileName(string externalId, string mediaType)
  {
    return $"{PlatformName}_{externalId}_{DateTime.UtcNow:yyyyMMdd}.{GetFileExtension(mediaType)}";
  }

  /// <summary>
  /// Gets file extension based on media type
  /// </summary>
  protected string GetFileExtension(string mediaType)
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