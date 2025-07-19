using Microsoft.Azure.Functions.Worker;
using SharedStorage.Services.Media;
using Functions.Media.Services;
using Utils;

namespace Functions.Media.Functions;

/// <summary>
/// Timer trigger function that runs nightly at 1 AM MT (7 AM UTC) to sync external media metadata
/// </summary>
public class WriteMediaTable
{
    private readonly IMediaItemService _mediaItemService;
    private readonly IExternalMediaSyncService _externalMediaSyncService;
    private readonly IAppInsightsLogger<WriteMediaTable> _logger;

    public WriteMediaTable(
        IMediaItemService mediaItemService,
        IExternalMediaSyncService externalMediaSyncService,
        IAppInsightsLogger<WriteMediaTable> logger)
    {
        _mediaItemService = mediaItemService ?? throw new ArgumentNullException(nameof(mediaItemService));
        _externalMediaSyncService = externalMediaSyncService ?? throw new ArgumentNullException(nameof(externalMediaSyncService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Timer trigger function that runs at 1 AM MT (7 AM UTC) daily
    /// CRON expression: "0 0 7 * * *" (7 AM UTC = 1 AM MT considering DST)
    /// </summary>
    [Function("WriteMediaTable")]
    public async Task Run([TimerTrigger("0 0 7 * * *", RunOnStartup = false)] TimerInfo myTimer)
    {
        _logger.LogInformation("WriteMediaTable timer trigger function started at: {DateTime} UTC", DateTime.UtcNow);

        try
        {
            var totalSynced = 0;
            var platforms = new[] { "TikTok", "Instagram", "YouTube", "Facebook", "LinkedIn", "Pinterest" };

            foreach (var platform in platforms)
            {
                try
                {
                    _logger.LogInformation("Starting sync for platform: {Platform}", platform);

                    var syncedCount = await _externalMediaSyncService.SyncPlatformMediaAsync(platform);
                    totalSynced += syncedCount;

                    _logger.LogInformation("Successfully synced {Count} media items from {Platform}", 
                        syncedCount, platform);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Failed to sync media from {Platform}: {Error}", ex, platform, ex.Message);
                    // Continue with other platforms even if one fails
                }
            }

            _logger.LogInformation("WriteMediaTable completed successfully. Total synced: {TotalSynced} media items across all platforms", totalSynced);
        }
        catch (Exception ex)
        {
            _logger.LogError("Critical error in WriteMediaTable: {Message}", ex, ex.Message);
            throw; // Re-throw to mark the function execution as failed
        }
    }
}