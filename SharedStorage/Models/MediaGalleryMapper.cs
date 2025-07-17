namespace SharedStorage.Models;

/// <summary>
/// Mapper for converting MediaEntity to MediaGalleryDTO
/// </summary>
public static class MediaGalleryMapper
{
  public static MediaGalleryDTO ToGalleryDTO(this MediaEntity entity)
  {
    if (entity == null)
      throw new ArgumentNullException(nameof(entity));

    return new MediaGalleryDTO
    {
      Id = entity.Id,
      AuthorId = entity.AuthorId,
      Title = GenerateTitle(entity),
      Description = entity.Description,
      MediaType = entity.MediaType,
      ContentType = entity.ContentType,
      Url = GetDisplayUrl(entity),
      ThumbnailUrl = entity.ThumbnailUrl,
      AltText = entity.AltText,
      Width = entity.Width,
      Height = entity.Height,
      Platform = entity.Platform,
      PlatformDisplayName = GetPlatformDisplayName(entity.Platform),
      ExternalUrl = entity.ExternalUrl,
      EmbedCode = entity.EmbedCode,
      LikeCount = entity.LikeCount,
      ShareCount = entity.ShareCount,
      ViewCount = entity.ViewCount,
      Tags = ParseTags(entity.Tags),
      CreatedAt = entity.ExternalCreatedAt ?? entity.UploadedAt,
      LastUpdated = entity.LastSyncedAt ?? entity.UploadedAt,
      Duration = ExtractDuration(entity),
      VideoQuality = ExtractVideoQuality(entity),
      AudioDuration = ExtractAudioDuration(entity),
      AudioBitrate = ExtractAudioBitrate(entity),
      Purpose = entity.Purpose,
      IsExternal = !string.IsNullOrEmpty(entity.Platform) && entity.Platform != "blob",
      IsAvailable = true, // Default to available, could be enhanced with availability checks
      SortKey = GenerateSortKey(entity),
      Category = GenerateCategory(entity)
    };
  }

  public static IEnumerable<MediaGalleryDTO> ToGalleryDTOs(this IEnumerable<MediaEntity> entities)
  {
    return entities?.Select(ToGalleryDTO) ?? Enumerable.Empty<MediaGalleryDTO>();
  }

  private static string GenerateTitle(MediaEntity entity)
  {
    if (!string.IsNullOrEmpty(entity.Description))
    {
      // Use first line of description as title
      var firstLine = entity.Description.Split('\n').FirstOrDefault()?.Trim();
      if (!string.IsNullOrEmpty(firstLine) && firstLine.Length <= 100)
        return firstLine;
    }

    // Generate title based on platform and media type
    var platform = GetPlatformDisplayName(entity.Platform);
    var mediaType = entity.MediaType switch
    {
      "image" => "Image",
      "video" => "Video",
      "audio" => "Audio",
      _ => "Media"
    };

    return $"{platform} {mediaType}";
  }

  private static string GetDisplayUrl(MediaEntity entity)
  {
    // For external platforms, return external URL
    if (!string.IsNullOrEmpty(entity.ExternalUrl))
      return entity.ExternalUrl;

    // For blob storage, return CDN URL
    return entity.Url;
  }

  private static string GetPlatformDisplayName(string platform)
  {
    return platform?.ToLowerInvariant() switch
    {
      "blob" => "Blob Storage",
      "tiktok" => "TikTok",
      "instagram" => "Instagram",
      "youtube" => "YouTube",
      "facebook" => "Facebook",
      "linkedin" => "LinkedIn",
      "pinterest" => "Pinterest",
      "" => "Unknown",
      null => "Unknown",
      _ => platform.ToUpperInvariant()
    };
  }

  private static string[] ParseTags(string tags)
  {
    if (string.IsNullOrEmpty(tags))
      return Array.Empty<string>();

    return tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
               .Select(tag => tag.Trim())
               .Where(tag => !string.IsNullOrEmpty(tag))
               .ToArray();
  }

  private static int ExtractDuration(MediaEntity entity)
  {
    if (entity.MediaType != "video")
      return 0;

    // Could parse from PlatformMetadata if available
    // For now, return 0 as placeholder
    return 0;
  }

  private static string ExtractVideoQuality(MediaEntity entity)
  {
    if (entity.MediaType != "video")
      return string.Empty;

    // Could parse from PlatformMetadata if available
    // For now, return empty as placeholder
    return string.Empty;
  }

  private static int ExtractAudioDuration(MediaEntity entity)
  {
    if (entity.MediaType != "audio")
      return 0;

    // Could parse from PlatformMetadata if available
    return 0;
  }

  private static string ExtractAudioBitrate(MediaEntity entity)
  {
    if (entity.MediaType != "audio")
      return string.Empty;

    // Could parse from PlatformMetadata if available
    return string.Empty;
  }

  private static string GenerateSortKey(MediaEntity entity)
  {
    // Use created date for sorting (newest first)
    var createdAt = entity.ExternalCreatedAt ?? entity.UploadedAt;
    return createdAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
  }

  private static string GenerateCategory(MediaEntity entity)
  {
    // Generate category based on platform and media type
    var platform = entity.Platform?.ToLowerInvariant() ?? "unknown";
    var mediaType = entity.MediaType?.ToLowerInvariant() ?? "unknown";

    if (platform == "blob")
      return $"Local {mediaType}";
    
    return $"{GetPlatformDisplayName(platform)} {mediaType}";
  }
}