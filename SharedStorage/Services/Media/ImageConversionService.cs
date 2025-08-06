using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Tga;
using Utils;

namespace SharedStorage.Services.Media;

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
  private const int DEFAULT_MIN_WIDTH_LANDSCAPE = 1440;
  private const int DEFAULT_MIN_HEIGHT_LANDSCAPE = 900;
  private const int DEFAULT_MIN_WIDTH_PORTRAIT = 900;
  private const int DEFAULT_MIN_HEIGHT_PORTRAIT = 1440;
  private const int DEFAULT_MAX_DIMENSION = 2500;
  private const int DEFAULT_DPI = 96;

  public ImageConversionService(IAppInsightsLogger<ImageConversionService> logger)
  {
    _appLogger = logger ?? throw new ArgumentNullException(nameof(logger));

    // Explicitly ensure all decoders are registered
    Configuration.Default.ImageFormatsManager.SetEncoder(WebpFormat.Instance, new WebpEncoder());
    Configuration.Default.ImageFormatsManager.SetEncoder(JpegFormat.Instance, new JpegEncoder());
    Configuration.Default.ImageFormatsManager.SetEncoder(PngFormat.Instance, new PngEncoder());
    Configuration.Default.ImageFormatsManager.SetEncoder(GifFormat.Instance, new GifEncoder());
    Configuration.Default.ImageFormatsManager.SetEncoder(BmpFormat.Instance, new BmpEncoder());

    _appLogger.LogInformation("ImageConversionService initialized with all encoders registered");
  }

  public async Task<ImageConversionResult> ConvertToWebPAsync(Stream input, int? maxWidth = null, int? maxHeight = null, int quality = 85)
  {
    // Simple validation without complex stream manipulation
    if (input == null)
      throw new ArgumentNullException(nameof(input), "Input stream cannot be null.");

    if (!input.CanRead)
      throw new InvalidOperationException("Input stream must be readable.");

    if (!input.CanSeek)
      throw new InvalidOperationException("Input stream must be seekable for image processing.");

    if (input.Length == 0)
      throw new InvalidOperationException("Input stream cannot be empty.");

    _appLogger.LogInformation("Starting WebP conversion for input stream of size {Size} bytes with quality {Quality}",
      input.Length, quality);

    try
    {
      // Ensure stream is at beginning
      input.Position = 0;

      // Log stream details for debugging
      _appLogger.LogInformation("Attempting to load image from stream - Position: {Position}, Length: {Length}, CanRead: {CanRead}, CanSeek: {CanSeek}",
        input.Position, input.Length, input.CanRead, input.CanSeek);

      // Copy stream data to a completely fresh byte array and create new MemoryStream
      // This ensures we have a completely clean, seekable stream for ImageSharp
      var streamData = new byte[input.Length];
      var totalBytesRead = 0;
      var bytesRead = 0;

      // Read all data from the input stream
      while (totalBytesRead < input.Length)
      {
        bytesRead = await input.ReadAsync(streamData, totalBytesRead, (int)(input.Length - totalBytesRead));
        if (bytesRead == 0) break;
        totalBytesRead += bytesRead;
      }

      _appLogger.LogInformation("Read {TotalBytes} bytes from input stream", totalBytesRead);

      // Log the first few bytes to debug what we actually received
      var headerBytes = Math.Min(16, totalBytesRead);
      var headerHex = Convert.ToHexString(streamData, 0, headerBytes);
      _appLogger.LogInformation("Stream header (first {HeaderBytes} bytes): {Header}", headerBytes, headerHex);

      // Validate we have a reasonable amount of data
      if (totalBytesRead < 10)
      {
        throw new InvalidOperationException($"Insufficient image data: only {totalBytesRead} bytes received");
      }

      // Check for common image file signatures
      var isValidImageFormat = IsValidImageHeader(streamData);
      if (!isValidImageFormat)
      {
        _appLogger.LogWarning("Data does not appear to be a valid image format. Header: {Header}", headerHex);
      }

      // Create a completely fresh MemoryStream from the byte array
      using var cleanStream = new MemoryStream(streamData, 0, totalBytesRead, false);

      _appLogger.LogInformation("Created clean stream - Length: {Length}, Position: {Position}, CanRead: {CanRead}, CanSeek: {CanSeek}",
        cleanStream.Length, cleanStream.Position, cleanStream.CanRead, cleanStream.CanSeek);

      // Create decoder options with more relaxed settings
      var decoderOptions = new DecoderOptions
      {
        MaxFrames = 1, // Only need first frame
        TargetSize = new Size(4000, 4000) // Reasonable max size limit to prevent decompression bombs
      };

      // Alternative approach: try loading from byte array directly
      Image image;
      try
      {
        _appLogger.LogInformation("Attempting to load image with explicit decoder options...");
        cleanStream.Position = 0;
        image = await Image.LoadAsync(decoderOptions, cleanStream);
        _appLogger.LogInformation("Successfully loaded image from stream with decoder options");
      }
      catch (Exception streamEx)
      {
        _appLogger.LogWarning("Failed to load from stream, trying byte array approach: {Error}", streamEx.Message);

        // Fallback: try loading directly from byte array
        try
        {
          image = Image.Load(decoderOptions, streamData);
          _appLogger.LogInformation("Successfully loaded image from byte array");
        }
        catch (Exception byteEx)
        {
          _appLogger.LogError("Failed to load image from both stream and byte array. Stream error: {StreamError}, Byte array error: {ByteError}",
            byteEx, streamEx.Message, byteEx.Message);

          throw new InvalidOperationException($"Unable to load image. Stream error: {streamEx.Message}, Byte array error: {byteEx.Message}");
        }
      }

      // Don't use using statement here - we need to keep the image open until we finish saving
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
    catch (UnknownImageFormatException ex)
    {
      _appLogger.LogError("Unsupported image format in WebP conversion: {Message}. Stream details - Position: {Position}, Length: {Length}, CanRead: {CanRead}, CanSeek: {CanSeek}",
        ex, ex.Message, input.Position, input.Length, input.CanRead, input.CanSeek);
      throw new InvalidOperationException($"Unsupported image format. Supported formats: JPEG, PNG, GIF, BMP, TIFF. Error: {ex.Message}", ex);
    }
    catch (InvalidImageContentException ex)
    {
      _appLogger.LogError("Invalid image content in WebP conversion: {Message}. Stream details - Position: {Position}, Length: {Length}, CanRead: {CanRead}, CanSeek: {CanSeek}",
        ex, ex.Message, input.Position, input.Length, input.CanRead, input.CanSeek);
      throw new InvalidOperationException($"Invalid or corrupted image file. Error: {ex.Message}", ex);
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error converting image to WebP format: {Message}. Stream details - Position: {Position}, Length: {Length}, CanRead: {CanRead}, CanSeek: {CanSeek}",
        ex, ex.Message, input.Position, input.Length, input.CanRead, input.CanSeek);
      throw new InvalidOperationException($"Failed to convert image to WebP format. Error: {ex.Message}", ex);
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
    catch (UnknownImageFormatException ex)
    {
      _appLogger.LogError("Unsupported image format when reading dimensions: {Message}", ex, ex.Message);
      throw new InvalidOperationException($"Unsupported image format. Error: {ex.Message}", ex);
    }
    catch (InvalidImageContentException ex)
    {
      _appLogger.LogError("Invalid image content when reading dimensions: {Message}", ex, ex.Message);
      throw new InvalidOperationException($"Invalid or corrupted image file. Error: {ex.Message}", ex);
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Failed to get image dimensions: {Message}", ex, ex.Message);
      throw new InvalidOperationException($"Failed to read image dimensions. Error: {ex.Message}", ex);
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
    catch (UnknownImageFormatException ex)
    {
      _appLogger.LogError("Unsupported image format in JPEG conversion: {Message}", ex, ex.Message);
      throw new InvalidOperationException($"Unsupported image format. Error: {ex.Message}", ex);
    }
    catch (InvalidImageContentException ex)
    {
      _appLogger.LogError("Invalid image content in JPEG conversion: {Message}", ex, ex.Message);
      throw new InvalidOperationException($"Invalid or corrupted image file. Error: {ex.Message}", ex);
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error converting image to JPEG format: {Message}", ex, ex.Message);
      throw new InvalidOperationException($"Failed to convert image to JPEG format. Error: {ex.Message}", ex);
    }
  }

  private void ApplySizeConstraints(Image image, int? maxWidth = null, int? maxHeight = null)
  {
    var currentWidth = image.Width;
    var currentHeight = image.Height;
    bool isLandscape = currentWidth >= currentHeight;

    // Apply minimum size constraints based on orientation
    bool needsUpscaling = isLandscape
      ? (currentWidth < DEFAULT_MIN_WIDTH_LANDSCAPE || currentHeight < DEFAULT_MIN_HEIGHT_LANDSCAPE)
      : (currentWidth < DEFAULT_MIN_WIDTH_PORTRAIT || currentHeight < DEFAULT_MIN_HEIGHT_PORTRAIT);

    if (needsUpscaling)
    {
      // Calculate scale factor based on orientation
      double scaleFactor;
      if (isLandscape)
      {
        scaleFactor = Math.Max(
          (double)DEFAULT_MIN_WIDTH_LANDSCAPE / currentWidth,
          (double)DEFAULT_MIN_HEIGHT_LANDSCAPE / currentHeight
        );
      }
      else
      {
        scaleFactor = Math.Max(
          (double)DEFAULT_MIN_WIDTH_PORTRAIT / currentWidth,
          (double)DEFAULT_MIN_HEIGHT_PORTRAIT / currentHeight
        );
      }

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

    if (!input.CanSeek)
    {
      _appLogger.LogError("Input stream is not seekable", new InvalidOperationException("Input stream must be seekable."));
      throw new InvalidOperationException("Input stream must be seekable for image processing.");
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

    // Try to peek at the beginning of the stream to ensure it contains data
    var currentPosition = input.Position;
    try
    {
      input.Position = 0;
      var buffer = new byte[16];
      var bytesRead = input.Read(buffer, 0, buffer.Length);

      if (bytesRead == 0)
      {
        _appLogger.LogError("No data could be read from input stream", new InvalidOperationException("Input stream contains no readable data"));
        throw new InvalidOperationException("Input stream contains no readable data.");
      }

      // Check for common image file headers
      var hasValidHeader = IsValidImageHeader(buffer);
      if (!hasValidHeader)
      {
        _appLogger.LogWarning("Input stream does not appear to contain a valid image file header");
      }
    }
    finally
    {
      input.Position = currentPosition;
    }
  }

  private bool IsValidImageHeader(byte[] buffer)
  {
    if (buffer.Length < 4) return false;

    // Check for common image file signatures
    // JPEG: FF D8 FF
    if (buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF) return true;

    // PNG: 89 50 4E 47
    if (buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47) return true;

    // GIF: 47 49 46 38
    if (buffer[0] == 0x47 && buffer[1] == 0x49 && buffer[2] == 0x46 && buffer[3] == 0x38) return true;

    // BMP: 42 4D
    if (buffer[0] == 0x42 && buffer[1] == 0x4D) return true;

    // TIFF: 49 49 2A 00 or 4D 4D 00 2A
    if ((buffer[0] == 0x49 && buffer[1] == 0x49 && buffer[2] == 0x2A && buffer[3] == 0x00) ||
        (buffer[0] == 0x4D && buffer[1] == 0x4D && buffer[2] == 0x00 && buffer[3] == 0x2A)) return true;

    // WebP: RIFF....WEBP (check positions 0-3 and 8-11)
    if (buffer.Length >= 12 &&
        buffer[0] == 0x52 && buffer[1] == 0x49 && buffer[2] == 0x46 && buffer[3] == 0x46 &&
        buffer[8] == 0x57 && buffer[9] == 0x45 && buffer[10] == 0x42 && buffer[11] == 0x50) return true;

    return false;
  }
}