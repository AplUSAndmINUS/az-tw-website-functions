using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SharedStorage.Services.Media;
using System.Net;
using System.Text.Json;
using Utils;

namespace Functions.MediaGallery;

/// <summary>
/// Timer-triggered function for syncing media from external platforms
/// Runs nightly at 1 AM Mountain Time
/// </summary>
public class WriteMediaTableFunction
{
  private readonly IAppInsightsLogger<WriteMediaTableFunction> _appLogger;
  private readonly IMediaGalleryService _mediaGalleryService;

  public WriteMediaTableFunction(
    IAppInsightsLogger<WriteMediaTableFunction> logger,
    IMediaGalleryService mediaGalleryService)
  {
    _appLogger = logger ?? throw new ArgumentNullException(nameof(logger));
    _mediaGalleryService = mediaGalleryService ?? throw new ArgumentNullException(nameof(mediaGalleryService));
    _appLogger.LogInformation("WriteMediaTableFunction initialized");
  }

  /// <summary>
  /// Timer trigger function that runs nightly at 1 AM Mountain Time
  /// Syncs media from all external platforms
  /// </summary>
  [Function("WriteMediaTable")]
  public async Task WriteMediaTable(
    [TimerTrigger("0 0 1 * * *", RunOnStartup = false)] TimerInfo myTimer)
  {
    _appLogger.LogInformation("WriteMediaTable timer trigger function started at: {Time}", DateTime.UtcNow);

    try
    {
      // Get the default author ID from configuration
      // In a real implementation, this would iterate through all authors
      var defaultAuthorId = Environment.GetEnvironmentVariable("DEFAULT_AUTHOR_ID") ?? "terence-waters";
      
      _appLogger.LogInformation("Starting media sync for author: {AuthorId}", defaultAuthorId);

      // Sync all platforms for the author
      var totalSynced = await _mediaGalleryService.SyncAllPlatformsAsync(defaultAuthorId);

      _appLogger.LogInformation("Media sync completed successfully. Total items synced: {TotalSynced}", totalSynced);

      // Log timing information
      if (myTimer.IsPastDue)
      {
        _appLogger.LogWarning("WriteMediaTable timer is running late");
      }

      _appLogger.LogInformation("WriteMediaTable timer trigger function completed at: {Time}", DateTime.UtcNow);
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error during media sync", ex);
      throw; // Re-throw to ensure Azure Functions marks this as failed
    }
  }

  /// <summary>
  /// HTTP trigger for manual media sync (for testing and admin purposes)
  /// </summary>
  [Function("WriteMediaTableManual")]
  public async Task<HttpResponseData> WriteMediaTableManual(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = "admin/sync-media")] HttpRequestData req)
  {
    _appLogger.LogInformation("WriteMediaTableManual HTTP trigger function started");

    try
    {
      // Get author ID from query parameters or use default
      var authorId = req.Query["authorId"] ?? Environment.GetEnvironmentVariable("DEFAULT_AUTHOR_ID") ?? "terence-waters";
      var platform = req.Query["platform"]; // Optional: sync specific platform only

      _appLogger.LogInformation("Starting manual media sync for author: {AuthorId}, platform: {Platform}", 
        authorId, platform ?? "all");

      int totalSynced;
      
      if (!string.IsNullOrWhiteSpace(platform))
      {
        // Sync specific platform
        totalSynced = await _mediaGalleryService.SyncPlatformAsync(platform, authorId);
      }
      else
      {
        // Sync all platforms
        totalSynced = await _mediaGalleryService.SyncAllPlatformsAsync(authorId);
      }

      // Create success response
      var response = req.CreateResponse(HttpStatusCode.OK);
      response.Headers.Add("Content-Type", "application/json");

      var responseBody = JsonSerializer.Serialize(new
      {
        success = true,
        message = "Media sync completed successfully",
        totalSynced = totalSynced,
        authorId = authorId,
        platform = platform ?? "all",
        syncTime = DateTime.UtcNow
      }, new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
      });

      await response.WriteStringAsync(responseBody);

      _appLogger.LogInformation("Manual media sync completed successfully. Total items synced: {TotalSynced}", totalSynced);
      return response;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error during manual media sync", ex);
      
      var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
      await errorResponse.WriteStringAsync("Internal server error during media sync");
      return errorResponse;
    }
  }
}

/// <summary>
/// Timer info structure for the timer trigger
/// </summary>
public class TimerInfo
{
  public bool IsPastDue { get; set; }
}