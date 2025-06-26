using Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Webp;

namespace SharedStorage.Services.MediaServices;

/// <summary>
/// Interface for video thumbnail generation services.
/// For production use, implement this with FFmpeg or similar video processing library.
/// </summary>
public interface IVideoThumbnailService
{
  /// <summary>
  /// Extracts a thumbnail from a video stream at the specified time position
  /// </summary>
  /// <param name="videoStream">The video stream to extract thumbnail from</param>
  /// <param name="timePositionSeconds">Position in seconds to extract frame from (default: 1 second)</param>
  /// <param name="maxWidth">Maximum width for the thumbnail (default: 400)</param>
  /// <param name="maxHeight">Maximum height for the thumbnail (default: 300)</param>
  /// <param name="quality">WebP quality (1-100, default: 75)</param>
  /// <returns>Video thumbnail result</returns>
  Task<VideoThumbnailResult> ExtractThumbnailAsync(Stream videoStream, double timePositionSeconds = 1.0, int maxWidth = 400, int maxHeight = 300, int quality = 75);

  /// <summary>
  /// Gets basic video metadata without full processing
  /// </summary>
  /// <param name="videoStream">The video stream to analyze</param>
  /// <returns>Video metadata including duration, dimensions, and format info</returns>
  Task<VideoMetadata> GetVideoMetadataAsync(Stream videoStream);

  /// <summary>
  /// Creates a placeholder thumbnail for videos when frame extraction is not available
  /// </summary>
  /// <param name="width">Width of the placeholder (default: 400)</param>
  /// <param name="height">Height of the placeholder (default: 300)</param>
  /// <param name="quality">WebP quality (1-100, default: 75)</param>
  /// <returns>Placeholder thumbnail</returns>
  Task<VideoThumbnailResult> CreatePlaceholderThumbnailAsync(int width = 400, int height = 300, int quality = 75);
}

public record VideoThumbnailResult(Stream Content, int Width, int Height, string Format, long FileSize);

public record VideoMetadata(
  TimeSpan Duration,
  int Width,
  int Height,
  string Format,
  string CodecName,
  double FrameRate,
  long BitRate,
  long FileSize);

/// <summary>
/// Basic implementation that creates placeholder thumbnails for videos.
/// For production use, replace with FFmpeg-based implementation.
/// </summary>
public class BasicVideoThumbnailService : IVideoThumbnailService
{
  private readonly IAppInsightsLogger<BasicVideoThumbnailService> _logger;
  private readonly IThumbnailService _thumbnailService;

  public BasicVideoThumbnailService(
    IAppInsightsLogger<BasicVideoThumbnailService> logger,
    IThumbnailService thumbnailService)
  {
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    _thumbnailService = thumbnailService ?? throw new ArgumentNullException(nameof(thumbnailService));
  }

  public async Task<VideoThumbnailResult> ExtractThumbnailAsync(Stream videoStream, double timePositionSeconds = 1.0, int maxWidth = 400, int maxHeight = 300, int quality = 75)
  {
    _logger.LogWarning("Using basic video thumbnail service - no actual frame extraction. Creating placeholder instead.");
    return await CreatePlaceholderThumbnailAsync(maxWidth, maxHeight, quality);
  }

  public async Task<VideoMetadata> GetVideoMetadataAsync(Stream videoStream)
  {
    _logger.LogWarning("Using basic video metadata extraction - returning default values.");

    // Return default metadata - in production, use FFmpeg to analyze the video
    return await Task.FromResult(new VideoMetadata(
      Duration: TimeSpan.FromMinutes(5), // Default 5 minutes
      Width: 1920,
      Height: 1080,
      Format: "mp4",
      CodecName: "h264",
      FrameRate: 30.0,
      BitRate: 2000000, // 2 Mbps
      FileSize: videoStream.Length
    ));
  }

  public async Task<VideoThumbnailResult> CreatePlaceholderThumbnailAsync(int width = 400, int height = 300, int quality = 75)
  {
    _logger.LogInformation("Creating video placeholder thumbnail with dimensions {Width}x{Height}", width, height);

    try
    {
      // Create a simple solid color placeholder
      using var image = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(width, height);

      // Set background to dark gray
      image.Mutate(ctx => ctx.BackgroundColor(SixLabors.ImageSharp.Color.FromRgb(64, 64, 64)));

      var stream = new MemoryStream();
      await image.SaveAsWebpAsync(stream, new SixLabors.ImageSharp.Formats.Webp.WebpEncoder
      {
        Quality = Math.Clamp(quality, 1, 100)
      });

      stream.Position = 0;
      return new VideoThumbnailResult(stream, width, height, "webp", stream.Length);
    }
    catch (Exception ex)
    {
      _logger.LogError("Failed to create video placeholder thumbnail", ex);
      throw new InvalidOperationException("Failed to create video placeholder thumbnail", ex);
    }
  }
}
