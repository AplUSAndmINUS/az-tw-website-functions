using SharedStorage.Models;

namespace SharedStorage.Extensions;

/// <summary>
/// Extension methods for media-related operations
/// </summary>
public static class MediaExtensions
{
  /// <summary>
  /// Ensures that a MediaItemModel has valid CDN URLs set
  /// </summary>
  public static MediaItemModel EnsureValidCdnUrls(this MediaItemModel model)
  {
    // For external media, don't require CDN URLs - they use external URLs
    if (model.IsExternal)
    {
      // For external media, ensure ExternalUrl is set
      if (string.IsNullOrWhiteSpace(model.ExternalUrl))
      {
        Console.WriteLine($"Warning: External MediaItemModel {model.Id} from {model.Platform} has no ExternalUrl");
      }
      return model;
    }

    // For blob storage media, ensure CDN URLs are set
    if (string.IsNullOrWhiteSpace(model.Url) ||
        !model.Url.Contains("azureedge.net", StringComparison.OrdinalIgnoreCase))
    {
      Console.WriteLine($"Warning: MediaItemModel {model.Id} has invalid CDN URL: {model.Url}");
    }

    // If ThumbnailUrl is missing or doesn't contain the CDN domain, log a warning
    if (string.IsNullOrWhiteSpace(model.ThumbnailUrl) ||
        !model.ThumbnailUrl.Contains("azureedge.net", StringComparison.OrdinalIgnoreCase))
    {
      Console.WriteLine($"Warning: MediaItemModel {model.Id} has invalid thumbnail CDN URL: {model.ThumbnailUrl}");
    }

    return model;
  }

  /// <summary>
  /// Ensures that a MediaItemDTO has valid CDN URLs set
  /// </summary>
  public static MediaItemDTO EnsureValidCdnUrls(this MediaItemDTO dto)
  {
    // For external media, don't require CDN URLs - they use external URLs
    if (dto.IsExternal)
    {
      // For external media, ensure ExternalUrl is set
      if (string.IsNullOrWhiteSpace(dto.ExternalUrl))
      {
        Console.WriteLine($"Warning: External MediaItemDTO {dto.Id} from {dto.Platform} has no ExternalUrl");
      }
      return dto;
    }

    // For blob storage media, ensure CDN URLs are set
    if (string.IsNullOrWhiteSpace(dto.Url) ||
        !dto.Url.Contains("azureedge.net", StringComparison.OrdinalIgnoreCase))
    {
      Console.WriteLine($"Warning: MediaItemDTO {dto.Id} has invalid CDN URL: {dto.Url}");
    }

    // If ThumbnailUrl is missing or doesn't contain the CDN domain, log a warning
    if (string.IsNullOrWhiteSpace(dto.ThumbnailUrl) ||
        !dto.ThumbnailUrl.Contains("azureedge.net", StringComparison.OrdinalIgnoreCase))
    {
      Console.WriteLine($"Warning: MediaItemDTO {dto.Id} has invalid thumbnail CDN URL: {dto.ThumbnailUrl}");
    }

    return dto;
  }
}
