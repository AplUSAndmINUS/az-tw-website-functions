using SharedStorage.Services.Media;
using Utils;

namespace Functions.Services;

public class BasicVideoThumbnailService : IVideoThumbnailService
{
  private readonly IAppInsightsLogger<BasicVideoThumbnailService> _logger;

  public BasicVideoThumbnailService(IAppInsightsLogger<BasicVideoThumbnailService> logger)
  {
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  /// <summary>
  /// Creates a placeholder thumbnail for a video
  /// In a real implementation, this would extract a frame from the video
  /// </summary>
  public Task<ThumbnailResult> CreatePlaceholderThumbnailAsync()
  {
    _logger.LogInformation("Creating placeholder thumbnail for video");

    try
    {
      // Create a simple placeholder image (a black square)
      var thumbnailStream = new MemoryStream();

      // Note: In a real implementation, we would extract a frame from the video
      // and create a proper thumbnail using a video processing library

      // For now, we're just creating a very basic placeholder
      byte[] placeholderBytes = new byte[]
      {
                // WebP header and minimal data for a 1x1 black pixel
                0x52, 0x49, 0x46, 0x46, 0x1A, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50,
                0x56, 0x50, 0x38, 0x20, 0x0E, 0x00, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x41, 0x4C, 0x50, 0x48, 0x00, 0x00,
                0x00, 0x00, 0x56, 0x50, 0x38, 0x4C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
      };

      thumbnailStream.Write(placeholderBytes, 0, placeholderBytes.Length);
      thumbnailStream.Position = 0;

      return Task.FromResult(new ThumbnailResult
      {
        Content = thumbnailStream,
        Width = 320,
        Height = 240
      });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to create placeholder thumbnail: {Error}", ex.Message);
      throw;
    }
  }

  /// <summary>
  /// Gets metadata from a video file
  /// In a real implementation, this would use a video processing library to extract real metadata
  /// </summary>
  public Task<VideoMetadata> GetVideoMetadataAsync(Stream videoStream)
  {
    _logger.LogInformation("Getting video metadata from stream of {Length} bytes", videoStream.Length);

    // In a real implementation, we would extract actual metadata from the video
    // using a library like FFmpeg or MediaToolkit

    // For now, we're just creating placeholder metadata based on file size
    // to simulate different resolutions for testing

    // Simple heuristic: bigger file = higher resolution
    int width, height;

    if (videoStream.Length > 5000000) // > 5MB
    {
      width = 1920;
      height = 1080;
    }
    else if (videoStream.Length > 2000000) // > 2MB
    {
      width = 1280;
      height = 720;
    }
    else if (videoStream.Length > 500000) // > 500KB
    {
      width = 854;
      height = 480;
    }
    else
    {
      width = 640;
      height = 360;
    }

    return Task.FromResult(new VideoMetadata
    {
      Width = width,
      Height = height,
      Duration = TimeSpan.FromSeconds(30), // Placeholder duration
      Bitrate = 1000000, // Placeholder bitrate (1 Mbps)
      Format = "mp4" // Placeholder format
    });
  }
}

/// <summary>
/// Video metadata information
/// </summary>
public class VideoMetadata
{
  public int Width { get; set; }
  public int Height { get; set; }
  public TimeSpan Duration { get; set; }
  public int Bitrate { get; set; }
  public string Format { get; set; } = string.Empty;
}
