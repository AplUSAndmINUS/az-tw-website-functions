using SharedStorage.Models;

namespace Functions.Media.Services;

/// <summary>
/// Service for syncing media metadata from external platforms
/// </summary>
public interface IExternalMediaSyncService
{
    /// <summary>
    /// Syncs media metadata from a specific platform
    /// </summary>
    /// <param name="platform">Platform name (TikTok, Instagram, YouTube, etc.)</param>
    /// <returns>Number of media items synced</returns>
    Task<int> SyncPlatformMediaAsync(string platform);

    /// <summary>
    /// Syncs media from all supported platforms
    /// </summary>
    /// <returns>Total number of media items synced</returns>
    Task<int> SyncAllPlatformsAsync();
}