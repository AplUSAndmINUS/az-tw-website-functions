using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Formats;
using Microsoft.Extensions.Logging;
using Utils;

namespace SharedStorage.Services.Media;

public interface IThumbnailService
{
  Task<ThumbnailResult> GenerateWebPThumbnailAsync(Stream input, int maxSize = 400, int minSize = 200, int quality = 75);
}

public record ThumbnailResult(Stream Content, int Width, int Height, string Format);

public class ThumbnailService : IThumbnailService
{
  private readonly IAppInsightsLogger<ThumbnailService> _appLogger;
  private readonly IImageService _imageService;

  public ThumbnailService(
      IAppInsightsLogger<ThumbnailService> logger,
      IImageService imageService)
  {
    _appLogger = logger ?? throw new ArgumentNullException(nameof(logger));
    _imageService = imageService ?? throw new ArgumentNullException(nameof(imageService));
  }

  public async Task<ThumbnailResult> GenerateWebPThumbnailAsync(Stream input, int maxSize = 400, int minSize = 200, int quality = 75)
  {
    ValidateInput(input);

    _appLogger.LogInformation("Starting WebP thumbnail generation for input stream of size {Size} bytes with max size {MaxSize}px and quality {Quality}.",
      input.Length, maxSize, quality);

    try
    {
      // Simply use ImageConversionService directly for thumbnail generation
      // This ensures we use the same robust image loading approach that's already working
      _appLogger.LogInformation("Using ImageService for thumbnail generation with max size {MaxSize}px", maxSize);

      // Copy input to a memory stream to ensure it's fresh
      var memStream = new MemoryStream();
      input.Position = 0;
      await input.CopyToAsync(memStream);
      memStream.Position = 0;

      // Use the ImageService to directly convert and resize in one operation
      // This bypasses the need to separately get dimensions first
      var result = await _imageService.ConvertToWebPAsync(
          memStream,
          maxWidth: maxSize,
          maxHeight: maxSize,
          quality: quality
      );

      _appLogger.LogInformation("Successfully generated thumbnail. Final size: {Width}x{Height}, File size: {Size} bytes",
          result.Width, result.Height, result.Content.Length);

      // Return our simplified result
      return new ThumbnailResult(result.Content, result.Width, result.Height, result.Format);
    }
    catch (Exception ex)
    {
      string errorDetail = ex.InnerException != null ?
          $"{ex.Message} -> {ex.InnerException.Message}" : ex.Message;

      _appLogger.LogError("Failed to generate WebP thumbnail: {ErrorDetail}", ex, errorDetail);
      throw new InvalidOperationException("Failed to generate WebP thumbnail.", ex);
    }
  }

  private void ValidateInput(Stream input)
  {
    if (input == null)
    {
      _appLogger.LogError("Input stream is null. Cannot generate thumbnail.", new ArgumentNullException(nameof(input)));
      throw new ArgumentNullException(nameof(input), "Input stream cannot be null.");
    }

    if (!input.CanRead)
    {
      _appLogger.LogError("Input stream is not readable. Cannot generate thumbnail.", new InvalidOperationException("Input stream must be readable."));
      throw new InvalidOperationException("Input stream must be readable.");
    }

    if (input.Length == 0)
    {
      _appLogger.LogError("Input stream is empty. Cannot generate thumbnail.", new InvalidOperationException("Input stream cannot be empty."));
      throw new InvalidOperationException("Input stream cannot be empty.");
    }

    // Check for reasonable file size limits (e.g., 50MB)
    const long maxFileSize = 50 * 1024 * 1024;
    if (input.Length > maxFileSize)
    {
      _appLogger.LogError("Input stream too large for thumbnail generation: {Size} bytes", new InvalidOperationException($"File too large: {input.Length} bytes"), input.Length);
      throw new InvalidOperationException($"File size exceeds maximum allowed size of {maxFileSize / (1024 * 1024)}MB");
    }
  }

  private static (int width, int height) CalculateThumbnailDimensions(int originalWidth, int originalHeight, int maxSize, int minSize)
  {
    // Calculate scale factor to fit within maxSize while maintaining aspect ratio
    var scaleFactor = Math.Min((double)maxSize / originalWidth, (double)maxSize / originalHeight);

    var newWidth = (int)Math.Round(originalWidth * scaleFactor);
    var newHeight = (int)Math.Round(originalHeight * scaleFactor);

    // Ensure minimum size constraints
    if (newWidth < minSize || newHeight < minSize)
    {
      var minScaleFactor = (double)minSize / Math.Min(newWidth, newHeight);
      newWidth = (int)Math.Round(newWidth * minScaleFactor);
      newHeight = (int)Math.Round(newHeight * minScaleFactor);
    }

    return (newWidth, newHeight);
  }
}