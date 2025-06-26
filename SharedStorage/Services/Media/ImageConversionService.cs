using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using Microsoft.Extensions.Logging;
using Utils;

namespace SharedStorage.Services.MediaServices;

public interface IImageService
{
  Task<ImageConversionResult> ConvertToWebPAsync(Stream input, int? maxWidth = null, int? maxHeight = null, int quality = 85);
  Task<ImageConversionResult> ConvertToOptimizedFormatAsync(Stream input, string? preferredFormat = null, int? maxWidth = null, int? maxHeight = null);
  Task<(int width, int height)> GetImageDimensionsAsync(Stream input);
}

public record ImageConversionResult(Stream Content, int Width, int Height, string Format, long FileSize);

public class ImageConversionService : IImageService
{
  private readonly IAppInsightsLogger<ImageConversionService> _appLogger;

  // Configuration for image processing
  private const int DEFAULT_MIN_DIMENSION = 600;
  private const int DEFAULT_MAX_DIMENSION = 2048;
  private const int DEFAULT_DPI = 96;

  public ImageConversionService(IAppInsightsLogger<ImageConversionService> logger)
  {
    _appLogger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  public async Task<ImageConversionResult> ConvertToWebPAsync(Stream input, int? maxWidth = null, int? maxHeight = null, int quality = 85)
  {
    ValidateInputStream(input);

    _appLogger.LogInformation("Starting WebP conversion for input stream of size {Size} bytes with quality {Quality}",
      input.Length, quality);

    try
    {
      // Load and process the image
      input.Position = 0;
      using var image = await Image.LoadAsync(input);

      // Auto-orient to handle EXIF rotation
      image.Mutate(x => x.AutoOrient());

      var originalWidth = image.Width;
      var originalHeight = image.Height;

      _appLogger.LogInformation("Original image dimensions: {Width}x{Height}", originalWidth, originalHeight);

      // Apply size constraints
      ApplySizeConstraints(image, maxWidth, maxHeight);

      // Set metadata
      image.Metadata.HorizontalResolution = DEFAULT_DPI;
      image.Metadata.VerticalResolution = DEFAULT_DPI;

      // Convert to WebP
      var output = new MemoryStream();
      var encoder = new WebpEncoder
      {
        Quality = Math.Clamp(quality, 1, 100),
        Method = WebpEncodingMethod.BestQuality,
        FileFormat = WebpFileFormatType.Lossy
      };

      await image.SaveAsWebpAsync(output, encoder);

      _appLogger.LogInformation("WebP conversion completed. Final size: {Width}x{Height}, File size: {FileSize} bytes",
        image.Width, image.Height, output.Length);

      output.Position = 0;
      return new ImageConversionResult(output, image.Width, image.Height, "webp", output.Length);
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error converting image to WebP format.", ex);
      throw new InvalidOperationException("Failed to convert image to WebP format.", ex);
    }
  }

  public async Task<ImageConversionResult> ConvertToOptimizedFormatAsync(Stream input, string? preferredFormat = null, int? maxWidth = null, int? maxHeight = null)
  {
    ValidateInputStream(input);

    // Default to WebP for best compression and quality
    var targetFormat = preferredFormat?.ToLowerInvariant() ?? "webp";

    return targetFormat switch
    {
      "webp" => await ConvertToWebPAsync(input, maxWidth, maxHeight),
      "jpeg" or "jpg" => await ConvertToJpegAsync(input, maxWidth, maxHeight),
      _ => await ConvertToWebPAsync(input, maxWidth, maxHeight) // Default to WebP
    };
  }

  public async Task<(int width, int height)> GetImageDimensionsAsync(Stream input)
  {
    ValidateInputStream(input);

    try
    {
      input.Position = 0;
      using var image = await Image.LoadAsync(input);

      // Handle EXIF rotation to get correct dimensions
      image.Mutate(x => x.AutoOrient());

      return (image.Width, image.Height);
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Failed to get image dimensions", ex);
      throw new InvalidOperationException("Failed to read image dimensions", ex);
    }
  }

  private async Task<ImageConversionResult> ConvertToJpegAsync(Stream input, int? maxWidth = null, int? maxHeight = null, int quality = 90)
  {
    ValidateInputStream(input);

    try
    {
      input.Position = 0;
      using var image = await Image.LoadAsync(input);

      image.Mutate(x => x.AutoOrient());
      ApplySizeConstraints(image, maxWidth, maxHeight);

      // Set metadata
      image.Metadata.HorizontalResolution = DEFAULT_DPI;
      image.Metadata.VerticalResolution = DEFAULT_DPI;

      var output = new MemoryStream();
      var encoder = new JpegEncoder
      {
        Quality = Math.Clamp(quality, 1, 100)
      };

      await image.SaveAsJpegAsync(output, encoder);

      _appLogger.LogInformation("JPEG conversion completed. Final size: {Width}x{Height}, File size: {FileSize} bytes",
        image.Width, image.Height, output.Length);

      output.Position = 0;
      return new ImageConversionResult(output, image.Width, image.Height, "jpeg", output.Length);
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error converting image to JPEG format.", ex);
      throw new InvalidOperationException("Failed to convert image to JPEG format.", ex);
    }
  }

  private void ApplySizeConstraints(Image image, int? maxWidth = null, int? maxHeight = null)
  {
    var currentWidth = image.Width;
    var currentHeight = image.Height;

    // Apply minimum size constraints
    if (currentWidth < DEFAULT_MIN_DIMENSION || currentHeight < DEFAULT_MIN_DIMENSION)
    {
      var scaleFactor = (double)DEFAULT_MIN_DIMENSION / Math.Min(currentWidth, currentHeight);
      var newWidth = (int)Math.Round(currentWidth * scaleFactor);
      var newHeight = (int)Math.Round(currentHeight * scaleFactor);

      _appLogger.LogInformation("Upscaling image from {OriginalWidth}x{OriginalHeight} to {NewWidth}x{NewHeight}",
        currentWidth, currentHeight, newWidth, newHeight);

      image.Mutate(x => x.Resize(new ResizeOptions
      {
        Size = new Size(newWidth, newHeight),
        Mode = ResizeMode.Max,
        Sampler = KnownResamplers.Lanczos3
      }));

      currentWidth = newWidth;
      currentHeight = newHeight;
    }

    // Apply maximum size constraints
    var effectiveMaxWidth = maxWidth ?? DEFAULT_MAX_DIMENSION;
    var effectiveMaxHeight = maxHeight ?? DEFAULT_MAX_DIMENSION;

    if (currentWidth > effectiveMaxWidth || currentHeight > effectiveMaxHeight)
    {
      _appLogger.LogInformation("Downscaling image from {OriginalWidth}x{OriginalHeight} with max constraints {MaxWidth}x{MaxHeight}",
        currentWidth, currentHeight, effectiveMaxWidth, effectiveMaxHeight);

      image.Mutate(x => x.Resize(new ResizeOptions
      {
        Size = new Size(effectiveMaxWidth, effectiveMaxHeight),
        Mode = ResizeMode.Max,
        Sampler = KnownResamplers.Lanczos3
      }));
    }
  }

  private void ValidateInputStream(Stream input)
  {
    if (input == null)
    {
      _appLogger.LogError("Input stream is null", new ArgumentNullException(nameof(input)));
      throw new ArgumentNullException(nameof(input), "Input stream cannot be null.");
    }

    if (!input.CanRead)
    {
      _appLogger.LogError("Input stream is not readable", new InvalidOperationException("Input stream must be readable."));
      throw new InvalidOperationException("Input stream must be readable.");
    }

    if (input.Length == 0)
    {
      _appLogger.LogError("Input stream is empty", new InvalidOperationException("Input stream cannot be empty."));
      throw new InvalidOperationException("Input stream cannot be empty.");
    }

    // Check for reasonable file size limits (e.g., 50MB)
    const long maxFileSize = 50 * 1024 * 1024;
    if (input.Length > maxFileSize)
    {
      _appLogger.LogError("Input stream too large: {Size} bytes", new InvalidOperationException($"File too large: {input.Length} bytes"), input.Length);
      throw new InvalidOperationException($"File size exceeds maximum allowed size of {maxFileSize / (1024 * 1024)}MB");
    }
  }
}