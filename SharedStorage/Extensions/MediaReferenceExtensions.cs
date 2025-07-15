using SharedStorage.Models;

namespace SharedStorage.Extensions;

/// <summary>
/// Extension methods for mapping between different types of media references
/// </summary>
public static class MediaReferenceExtensions
{
  /// <summary>
  /// Maps a MediaReference to a MediaItemModel, ensuring the CDN URLs are set correctly
  /// </summary>
  /// <param name="mediaReference">The MediaReference to map from</param>
  /// <param name="mediaId">The ID for the new MediaItemModel</param>
  /// <param name="authorId">The author ID for the new MediaItemModel</param>
  /// <param name="filename">The filename for the new MediaItemModel</param>
  /// <param name="contentType">The content type for the new MediaItemModel</param>
  /// <returns>A new MediaItemModel with CDN URLs set from the MediaReference</returns>
  public static MediaItemModel MapFromMediaReference(
      this MediaReference mediaReference,
      string mediaId,
      string authorId,
      string filename,
      string contentType)
  {
    ArgumentNullException.ThrowIfNull(mediaReference);

    var model = new MediaItemModel
    {
      Id = mediaId,
      AuthorId = authorId,
      Filename = filename,
      MediaType = DetermineMediaTypeFromContentType(contentType),
      ContentType = contentType,
      Url = mediaReference.CdnUrl,
      ThumbnailUrl = mediaReference.ThumbnailCdnUrl,
      UploadedAt = DateTime.UtcNow,
      LastModified = DateTime.UtcNow,
      ContentId = mediaReference.ContentId ?? string.Empty,
      RelatedContentType = mediaReference.RelatedContentType ?? string.Empty
    };

    return model.EnsureValidCdnUrls();
  }

  /// <summary>
  /// Determines the media type from the content type
  /// </summary>
  private static string DetermineMediaTypeFromContentType(string contentType)
  {
    if (string.IsNullOrWhiteSpace(contentType))
      return "unknown";

    if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
      return "image";

    if (contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
      return "video";

    if (contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
      return "audio";

    return "file";
  }
}
