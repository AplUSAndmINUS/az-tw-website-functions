using SharedStorage.Models;

namespace SharedStorage.Services.Media.Platforms;

/// <summary>
/// Interface for platform-specific media adapters that fetch content from external platforms
/// </summary>
public interface IPlatformMediaAdapter
{
  /// <summary>
  /// The platform name this adapter handles (e.g., "tiktok", "instagram", "youtube")
  /// </summary>
  string PlatformName { get; }

  /// <summary>
  /// Fetches recent media from the external platform
  /// </summary>
  /// <param name="authorId">The author ID to associate with the media</param>
  /// <param name="limit">Maximum number of items to fetch (default: 50)</param>
  /// <returns>Collection of MediaEntity objects with external platform metadata</returns>
  Task<IEnumerable<MediaEntity>> FetchRecentMediaAsync(string authorId, int limit = 50);

  /// <summary>
  /// Fetches a specific media item by its external platform ID
  /// </summary>
  /// <param name="externalId">The external platform's unique identifier</param>
  /// <param name="authorId">The author ID to associate with the media</param>
  /// <returns>MediaEntity object or null if not found</returns>
  Task<MediaEntity?> FetchMediaByExternalIdAsync(string externalId, string authorId);

  /// <summary>
  /// Validates if the platform credentials/configuration are valid
  /// </summary>
  /// <returns>True if the adapter can successfully connect to the platform</returns>
  Task<bool> ValidateConnectionAsync();
}