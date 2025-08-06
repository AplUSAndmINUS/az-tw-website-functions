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

    // In ImageSharp 3.x, encoders and decoders are automatically registered
    // We'll just log the initialization
    _appLogger.LogInformation("ImageConversionService initialized - using ImageSharp v3.1.11");

    // Add additional logging for diagnostic purposes
    try
    {
      // List the supported formats by checking which encoders are registered
      var formats = new[] { "webp", "jpeg", "png", "gif", "bmp" };
      _appLogger.LogInformation("Checking for supported image formats...");

      foreach (var format in formats)
      {
        var isSupported = format switch
        {
          "webp" => Configuration.Default.ImageFormatsManager.TryFindFormatByFileExtension(".webp", out _),
          "jpeg" => Configuration.Default.ImageFormatsManager.TryFindFormatByFileExtension(".jpg", out _),
          "png" => Configuration.Default.ImageFormatsManager.TryFindFormatByFileExtension(".png", out _),
          "gif" => Configuration.Default.ImageFormatsManager.TryFindFormatByFileExtension(".gif", out _),
          "bmp" => Configuration.Default.ImageFormatsManager.TryFindFormatByFileExtension(".bmp", out _),
          _ => false
        };

        _appLogger.LogInformation("Format {Format} is {Status}", format, isSupported ? "supported" : "NOT supported");
      }
    }
    catch (Exception ex)
    {
      _appLogger.LogWarning("Error checking supported formats: {Error}", ex.Message);
    }
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

      // Try multiple loading approaches to ensure compatibility
      Image image;

      // Reset stream position
      cleanStream.Position = 0;

      // Determine likely image format based on headers for more targeted loading
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
        // BMP: 42 4D
        else if (streamData[0] == 0x42 && streamData[1] == 0x4D)
        {
          detectedFormat = "bmp";
        }
        // TIFF: 49 49 2A 00 or 4D 4D 00 2A
        else if ((streamData[0] == 0x49 && streamData[1] == 0x49 && streamData[2] == 0x2A && streamData[3] == 0x00) ||
                (streamData[0] == 0x4D && streamData[1] == 0x4D && streamData[2] == 0x00 && streamData[3] == 0x2A))
        {
          detectedFormat = "tiff";
        }
        // WebP: RIFF....WEBP (check positions 0-3 and 8-11)
        else if (streamData.Length >= 12 &&
                streamData[0] == 0x52 && streamData[1] == 0x49 && streamData[2] == 0x46 && streamData[3] == 0x46 &&
                streamData[8] == 0x57 && streamData[9] == 0x45 && streamData[10] == 0x42 && streamData[11] == 0x50)
        {
          detectedFormat = "webp";
        }
      }

      _appLogger.LogInformation("Header-based format detection: {Format}", detectedFormat);

      // Directly try file extension handling for known formats
      cleanStream.Position = 0;

      // First attempt: Use standard loading with detailed error catching
      try
      {
        _appLogger.LogInformation("Attempting to load image with standard method...");
        cleanStream.Position = 0;

        // For v3.1.11 compatibility - use the standard Load method
        image = Image.Load(cleanStream);
        _appLogger.LogInformation("Successfully loaded image using standard method");
      }
      catch (Exception standardEx)
      {
        _appLogger.LogWarning("Failed to load with standard method: {Error}", standardEx.Message);

        // Second attempt: Try with decoder options
        try
        {
          _appLogger.LogInformation("Attempting to load image with explicit decoder options...");
          cleanStream.Position = 0;
          image = await Image.LoadAsync(decoderOptions, cleanStream);
          _appLogger.LogInformation("Successfully loaded image with decoder options");
        }
        catch (Exception streamEx)
        {
          _appLogger.LogWarning("Failed to load from stream with options, trying byte array approach: {Error}", streamEx.Message);

          // Third attempt: Try loading directly from byte array
          try
          {
            _appLogger.LogInformation("Attempting to load from byte array directly...");
            image = Image.Load(streamData);
            _appLogger.LogInformation("Successfully loaded image from byte array");
          }
          catch (Exception byteEx)
          {
            // Fourth attempt: Try with a temporary file
            try
            {
              _appLogger.LogInformation("Last resort: Creating temporary file and loading from disk...");

              // Create a temporary file with appropriate extension based on detected format
              string extension = detectedFormat == "unknown" ? ".tmp" : $".{detectedFormat}";
              string tempFile = Path.Combine(Path.GetTempPath(), $"image-temp-{Guid.NewGuid()}{extension}");

              // Write image data to the temporary file
              File.WriteAllBytes(tempFile, streamData);

              // Try to load from the file
              image = Image.Load(tempFile);

              // Delete temporary file
              try { File.Delete(tempFile); } catch { }

              _appLogger.LogInformation("Successfully loaded image from temporary file with extension: {Extension}", extension);
            }
            catch (Exception tempFileEx)
            {
              // Last attempt: If it's a JPEG file with incorrect headers, try fixing common corruption patterns
              if (detectedFormat == "jpeg" || (detectedFormat == "unknown" && streamData.Length > 4))
              {
                try
                {
                  _appLogger.LogInformation("Attempting JPEG recovery for possibly corrupted file...");

                  // Create fixed version with proper JPEG header if needed
                  var fixedData = new byte[streamData.Length + 2]; // Add space for potential missing bytes

                  // Copy existing data
                  Array.Copy(streamData, 0, fixedData, 0, streamData.Length);

                  // Ensure JPEG header is present and correct
                  if (!(fixedData[0] == 0xFF && fixedData[1] == 0xD8 && fixedData[2] == 0xFF))
                  {
                    fixedData[0] = 0xFF;
                    fixedData[1] = 0xD8;
                    fixedData[2] = 0xFF;
                  }

                  // Try load with fixed data
                  image = Image.Load(fixedData);
                  _appLogger.LogInformation("Successfully loaded image after JPEG header repair");
                }
                catch (Exception recoveryEx)
                {
                  _appLogger.LogError("All image loading approaches failed, including recovery attempts", recoveryEx);

                  // Comprehensive error logging for diagnostics
                  _appLogger.LogInformation("Initial error: {0}", standardEx.Message);
                  _appLogger.LogInformation("Options loading error: {0}", streamEx.Message);
                  _appLogger.LogInformation("Byte array loading error: {0}", byteEx.Message);
                  _appLogger.LogInformation("Temp file loading error: {0}", tempFileEx.Message);
                  _appLogger.LogInformation("Recovery error: {0}", recoveryEx.Message);

                  throw new InvalidOperationException($"Unable to load image with any method. The file may be corrupted or in an unsupported format. Error: {recoveryEx.Message}", recoveryEx);
                }
              }
              else
              {
                _appLogger.LogError("All image loading approaches failed", tempFileEx);

                // Comprehensive error logging for diagnostics
                _appLogger.LogInformation("Initial error: {0}", standardEx.Message);
                _appLogger.LogInformation("Options loading error: {0}", streamEx.Message);
                _appLogger.LogInformation("Byte array loading error: {0}", byteEx.Message);
                _appLogger.LogInformation("Temp file loading error: {0}", tempFileEx.Message);

                throw new InvalidOperationException($"Unable to load image with any method. The file may be corrupted or in an unsupported format. Error: {tempFileEx.Message}", tempFileEx);
              }
            }
          }
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