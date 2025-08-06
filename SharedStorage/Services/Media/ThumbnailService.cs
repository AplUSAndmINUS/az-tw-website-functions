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
      // Copy stream data to ensure we have a clean, fresh copy for processing
      byte[] streamData;
      using (var memoryStream = new MemoryStream())
      {
        input.Position = 0;
        await input.CopyToAsync(memoryStream);
        streamData = memoryStream.ToArray();
      }

      _appLogger.LogInformation("Copied stream to memory buffer: {Length} bytes", streamData.Length);

      // Determine format based on header bytes
      string detectedFormat = "unknown";
      if (streamData.Length >= 4)
      {
        // JPEG: FF D8 FF
        if (streamData[0] == 0xFF && streamData[1] == 0xD8 && streamData[2] == 0xFF)
        {
          detectedFormat = "jpeg";
        }
        // PNG: 89 50 4E 47
        else if (streamData[0] == 0x89 && streamData[1] == 0x50 && streamData[2] == 0x4E && streamData[3] == 0x47)
        {
          detectedFormat = "png";
        }
        // GIF: 47 49 46 38
        else if (streamData[0] == 0x47 && streamData[1] == 0x49 && streamData[2] == 0x46 && streamData[3] == 0x38)
        {
          detectedFormat = "gif";
        }
      }

      _appLogger.LogInformation("Header-based format detection: {Format}", detectedFormat);

      // Create a clean memory stream for image processing
      using var cleanStream = new MemoryStream(streamData);

      // Try multiple loading approaches for maximum compatibility
      Image image;

      try
      {
        // First attempt: Standard loading
        _appLogger.LogInformation("Attempting to load image with standard method...");
        cleanStream.Position = 0;
        image = Image.Load(cleanStream);
        _appLogger.LogInformation("Successfully loaded image using standard method");
      }
      catch (Exception standardEx)
      {
        _appLogger.LogWarning("Failed to load with standard method: {Error}", standardEx.Message);

        try
        {
          // Second attempt: Try with decoder options
          _appLogger.LogInformation("Attempting to load image with explicit decoder options...");
          cleanStream.Position = 0;
          var decoderOptions = new DecoderOptions
          {
            MaxFrames = 1, // Only need first frame
            TargetSize = new Size(4000, 4000) // Reasonable max size limit
          };
          image = await Image.LoadAsync(decoderOptions, cleanStream);
          _appLogger.LogInformation("Successfully loaded image with decoder options");
        }
        catch (Exception streamEx)
        {
          _appLogger.LogWarning("Failed to load with options, trying byte array approach: {Error}", streamEx.Message);

          try
          {
            // Third attempt: Try loading directly from byte array
            _appLogger.LogInformation("Attempting to load from byte array directly...");
            image = Image.Load(streamData);
            _appLogger.LogInformation("Successfully loaded image from byte array");
          }
          catch (Exception byteEx)
          {
            // Final attempt: Try creating a temporary file
            try
            {
              _appLogger.LogInformation("Last resort: Creating temporary file and loading from disk...");

              // Create a temporary file with appropriate extension
              string extension = detectedFormat == "unknown" ? ".tmp" : $".{detectedFormat}";
              string tempFile = Path.Combine(Path.GetTempPath(), $"thumb-temp-{Guid.NewGuid()}{extension}");

              // Write image data to the temporary file
              File.WriteAllBytes(tempFile, streamData);

              // Try to load from the file
              image = Image.Load(tempFile);

              // Delete temporary file
              try { File.Delete(tempFile); } catch { }

              _appLogger.LogInformation("Successfully loaded image from temporary file");
            }
            catch (Exception tempFileEx)
            {
              _appLogger.LogError("All thumbnail image loading approaches failed", tempFileEx);
              _appLogger.LogInformation("Initial error: {0}", standardEx.Message);
              _appLogger.LogInformation("Options loading error: {0}", streamEx.Message);
              _appLogger.LogInformation("Byte array loading error: {0}", byteEx.Message);
              _appLogger.LogInformation("Temp file loading error: {0}", tempFileEx.Message);

              // If we've tried everything and failed, rethrow with details
              throw new InvalidOperationException($"Unable to load image for thumbnail generation. The file may be corrupted or in an unsupported format.", tempFileEx);
            }
          }
        }
      }

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