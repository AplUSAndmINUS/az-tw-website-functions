using System.Collections.Generic;
using System.Threading.Tasks;
using SharedStorage.Models;
using SharedStorage.Services.Media;

namespace SharedStorage.Services.Media;

/// <summary>
/// Extension methods for IMediaService to support the new unified media model
/// </summary>
public static class MediaServiceExtensions
{
  /// <summary>
  /// Associates a media item with a content item (blog post, portfolio piece, etc.)
  /// </summary>
  /// <param name="service">The media service instance</param>
  /// <param name="mediaId">ID of the media item</param>
  /// <param name="contentId">ID of the content</param>
  /// <param name="contentType">Type of the content (blog, portfolio, author, etc.)</param>
  /// <returns>True if successful</returns>
  public static async Task<bool> AssociateMediaWithContentAsync(
      this IMediaService service,
      string mediaId,
      string contentId,
      string contentType)
  {
    // This is a placeholder implementation that will need to be expanded
    // with actual implementation in the MediaService class

    // For now, we'll call a method that likely doesn't exist yet,
    // but should be implemented in MediaService
    if (service is MediaService mediaService)
    {
      return await mediaService.UpdateMediaContentReferenceAsync(mediaId, contentId, contentType);
    }

    return false;
  }

  /// <summary>
  /// Gets all media items associated with a specific content item
  /// </summary>
  /// <param name="service">The media service instance</param>
  /// <param name="contentId">ID of the content</param>
  /// <param name="contentType">Type of the content (blog, portfolio, author, etc.)</param>
  /// <returns>Collection of MediaEntity objects</returns>
  public static async Task<IEnumerable<MediaEntity>> GetMediaByContentReferenceAsync(
      this IMediaService service,
      string contentId,
      string contentType)
  {
    // This is a placeholder implementation that will need to be expanded
    // with actual implementation in the MediaService class

    // For now, we'll call a method that likely doesn't exist yet,
    // but should be implemented in MediaService
    if (service is MediaService mediaService)
    {
      return await mediaService.GetMediaByContentReferenceInternalAsync(contentId, contentType);
    }

    return new List<MediaEntity>();
  }
}
