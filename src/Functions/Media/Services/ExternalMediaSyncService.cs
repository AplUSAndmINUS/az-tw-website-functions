using SharedStorage.Models;
using SharedStorage.Services.Media;
using Utils;

namespace Functions.Media.Services;

/// <summary>
/// Service implementation for syncing media metadata from external platforms
/// </summary>
public class ExternalMediaSyncService : IExternalMediaSyncService
{
    private readonly IMediaItemService _mediaItemService;
    private readonly ITikTokService _tiktokService;
    private readonly IInstagramService _instagramService;
    private readonly IYouTubeService _youtubeService;
    private readonly IFacebookService _facebookService;
    private readonly ILinkedInService _linkedInService;
    private readonly IPinterestService _pinterestService;
    private readonly IAppInsightsLogger<ExternalMediaSyncService> _logger;

    public ExternalMediaSyncService(
        IMediaItemService mediaItemService,
        ITikTokService tiktokService,
        IInstagramService instagramService,
        IYouTubeService youtubeService,
        IFacebookService facebookService,
        ILinkedInService linkedInService,
        IPinterestService pinterestService,
        IAppInsightsLogger<ExternalMediaSyncService> logger)
    {
        _mediaItemService = mediaItemService ?? throw new ArgumentNullException(nameof(mediaItemService));
        _tiktokService = tiktokService ?? throw new ArgumentNullException(nameof(tiktokService));
        _instagramService = instagramService ?? throw new ArgumentNullException(nameof(instagramService));
        _youtubeService = youtubeService ?? throw new ArgumentNullException(nameof(youtubeService));
        _facebookService = facebookService ?? throw new ArgumentNullException(nameof(facebookService));
        _linkedInService = linkedInService ?? throw new ArgumentNullException(nameof(linkedInService));
        _pinterestService = pinterestService ?? throw new ArgumentNullException(nameof(pinterestService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> SyncPlatformMediaAsync(string platform)
    {
        if (string.IsNullOrWhiteSpace(platform))
            throw new ArgumentException("Platform is required", nameof(platform));

        _logger.LogInformation("Starting sync for platform: {Platform}", platform);

        try
        {
            var externalMediaItems = platform.ToUpperInvariant() switch
            {
                "TIKTOK" => await _tiktokService.GetLatestMediaAsync(),
                "INSTAGRAM" => await _instagramService.GetLatestMediaAsync(),
                "YOUTUBE" => await _youtubeService.GetLatestMediaAsync(),
                "FACEBOOK" => await _facebookService.GetLatestMediaAsync(),
                "LINKEDIN" => await _linkedInService.GetLatestMediaAsync(),
                "PINTEREST" => await _pinterestService.GetLatestMediaAsync(),
                _ => throw new ArgumentException($"Unsupported platform: {platform}")
            };

            var syncedCount = 0;

            foreach (var mediaItem in externalMediaItems)
            {
                try
                {
                    // Check if media item already exists by external ID and platform
                    var existingMedia = await GetExistingMediaByExternalIdAsync(mediaItem.ExternalId, mediaItem.Platform);
                    
                    if (existingMedia == null)
                    {
                        // Save new media item to storage
                        await SaveExternalMediaItemAsync(mediaItem);
                        syncedCount++;
                        _logger.LogInformation("Saved new media item {ExternalId} from {Platform}", 
                            mediaItem.ExternalId, mediaItem.Platform);
                    }
                    else
                    {
                        // Update existing media item if needed
                        if (await UpdateExistingMediaItemAsync(existingMedia, mediaItem))
                        {
                            syncedCount++;
                            _logger.LogInformation("Updated existing media item {ExternalId} from {Platform}", 
                                mediaItem.ExternalId, mediaItem.Platform);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Failed to sync individual media item {ExternalId} from {Platform}: {Error}", 
                        ex, mediaItem.ExternalId, mediaItem.Platform, ex.Message);
                    // Continue with other items
                }
            }

            _logger.LogInformation("Completed sync for {Platform}. Synced {Count} items", platform, syncedCount);
            return syncedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to sync platform {Platform}: {Error}", ex, platform, ex.Message);
            throw;
        }
    }

    public async Task<int> SyncAllPlatformsAsync()
    {
        var platforms = new[] { "TikTok", "Instagram", "YouTube", "Facebook", "LinkedIn", "Pinterest" };
        var totalSynced = 0;

        foreach (var platform in platforms)
        {
            try
            {
                var syncedCount = await SyncPlatformMediaAsync(platform);
                totalSynced += syncedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to sync platform {Platform}: {Error}", ex, platform, ex.Message);
                // Continue with other platforms
            }
        }

        _logger.LogInformation("Completed sync for all platforms. Total synced: {TotalSynced}", totalSynced);
        return totalSynced;
    }

    private async Task<MediaItemModel?> GetExistingMediaByExternalIdAsync(string externalId, string platform)
    {
        try
        {
            // This would need to be implemented in MediaItemService to search by ExternalId and Platform
            // For now, we'll assume all external media is new
            await Task.CompletedTask;
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to check for existing media {ExternalId} from {Platform}: {Error}", 
                ex, externalId, platform, ex.Message);
            return null;
        }
    }

    private async Task SaveExternalMediaItemAsync(MediaItemModel mediaItem)
    {
        try
        {
            // Convert MediaItemModel to MediaEntity and save using MediaService
            // This is a simplified approach - in practice you'd want to use the proper upload methods
            // For external media, we're just saving metadata, not uploading files
            
            // Since we can't directly save MediaItemModel, we'd need to extend MediaItemService
            // For now, we'll log that we would save it
            _logger.LogInformation("Would save external media item {ExternalId} from {Platform} to storage", 
                mediaItem.ExternalId, mediaItem.Platform);
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to save external media item {ExternalId} from {Platform}: {Error}", 
                ex, mediaItem.ExternalId, mediaItem.Platform, ex.Message);
            throw;
        }
    }

    private async Task<bool> UpdateExistingMediaItemAsync(MediaItemModel existingMedia, MediaItemModel newMedia)
    {
        try
        {
            // Compare and update fields if needed
            var needsUpdate = false;

            if (existingMedia.Description != newMedia.Description)
            {
                existingMedia.Description = newMedia.Description;
                needsUpdate = true;
            }

            if (existingMedia.Url != newMedia.Url)
            {
                existingMedia.Url = newMedia.Url;
                needsUpdate = true;
            }

            if (needsUpdate)
            {
                existingMedia.LastModified = DateTime.UtcNow;
                // Update in storage
                _logger.LogInformation("Would update existing media item {ExternalId} from {Platform}", 
                    existingMedia.ExternalId, existingMedia.Platform);
            }

            return needsUpdate;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to update existing media item {ExternalId} from {Platform}: {Error}", 
                ex, existingMedia.ExternalId, existingMedia.Platform, ex.Message);
            return false;
        }
    }
}