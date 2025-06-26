using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using Microsoft.Extensions.Logging;
using Utils;

namespace SharedStorage.Services.MediaServices;

public interface IThumbnailService
{
  Task<ThumbnailResult> GenerateWebPThumbnailAsync(Stream input, int maxSize = 400, int minSize = 200, int quality = 75);
}

public record ThumbnailResult(Stream Content, int Width, int Height, string Format);

public class ThumbnailService : IThumbnailService
{
  private readonly IAppInsightsLogger<ThumbnailService> _appLogger;

  public ThumbnailService(IAppInsightsLogger<ThumbnailService> logger)
  {
    _appLogger = logger;
  }

  public async Task<ThumbnailResult> GenerateWebPThumbnailAsync(Stream input, int maxSize = 400, int minSize = 200, int quality = 75)
  {
    ValidateInput(input);
    
    _appLogger.LogInformation("Starting WebP thumbnail generation for input stream of size {Size} bytes with max size {MaxSize}px and quality {Quality}.", 
      input.Length, maxSize, quality);

    try
    {
      // Load the image from the input stream
      input.Position = 0; // Reset stream position to the beginning
      using var image = await Image.LoadAsync(input);
      
      // Auto-orient to handle EXIF rotation
      image.Mutate(x => x.AutoOrient());

      _appLogger.LogInformation("Image loaded successfully. Original dimensions: {Width}x{Height}", image.Width, image.Height);
      
      // Calculate optimal thumbnail dimensions
      var (thumbnailWidth, thumbnailHeight) = CalculateThumbnailDimensions(image.Width, image.Height, maxSize, minSize);
      
      _appLogger.LogInformation("Resizing image to {Width}x{Height} for WebP thumbnail.", thumbnailWidth, thumbnailHeight);

      // Resize the image
      image.Mutate(x => x.Resize(new ResizeOptions
      {
        Size = new Size(thumbnailWidth, thumbnailHeight),
        Mode = ResizeMode.Max,
        Sampler = KnownResamplers.Lanczos3
      }));

      // Set metadata
      image.Metadata.HorizontalResolution = 96;
      image.Metadata.VerticalResolution = 96;

      // Convert to WebP
      var output = new MemoryStream();
      await image.SaveAsWebpAsync(output, new WebpEncoder
      {
        Quality = Math.Clamp(quality, 1, 100),
        Method = WebpEncodingMethod.BestQuality,
        FileFormat = WebpFileFormatType.Lossy
      });

      _appLogger.LogInformation("WebP thumbnail generated successfully. Final size: {Width}x{Height}, File size: {Size} bytes", 
        thumbnailWidth, thumbnailHeight, output.Length);
      
      output.Position = 0; // Reset stream position to the beginning for reading
      return new ThumbnailResult(output, thumbnailWidth, thumbnailHeight, "webp");
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Failed to generate WebP thumbnail.", ex);
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