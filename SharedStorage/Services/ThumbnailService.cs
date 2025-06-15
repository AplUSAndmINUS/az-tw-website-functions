using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using Microsoft.Extensions.Logging;

namespace SharedStorage.Services;

public interface IThumbnailService
{
  Task<ThumbnailResult> GenerateWebPThumbnailAsync(Stream input);
}

public record ThumbnailResult(Stream Content, int Width, int Height, string Format);

public class ThumbnailService : IThumbnailService
{
  private readonly ILogger<ThumbnailService> _logger;

  public ThumbnailService(ILogger<ThumbnailService> logger)
  {
    _logger = logger;
  }

  public async Task<ThumbnailResult> GenerateWebPThumbnailAsync(Stream input)
  {
    if (input == null)
    {
      _logger.LogError("Input stream is null. Cannot generate thumbnail.");
      throw new ArgumentNullException(nameof(input), "Input stream cannot be null.");
    }

    try
    {
      // Load the image from the input stream
      _logger.LogInformation("Loading image from input stream for WebP thumbnail generation.");
      input.Position = 0; // Reset stream position to the beginning
      using var image = await Image.LoadAsync(input);
      // remove JPG EXIF rotation if present
      image.Mutate(x => x.AutoOrient());

      _logger.LogInformation("Image loaded successfully. Dimensions: {Width}x{Height}", image.Width, image.Height);
      // Calculate the new dimensions for the thumbnail, w/ 2/3 scaling
      var width = image.Width * 2 / 3;
      var height = image.Height * 2 / 3;

      // Ensure it has a minimum size
      if (width < 400 || height < 400)
      {
        double scaleFactor = 400.0 / Math.Min(image.Width, image.Height);
        width = (int)Math.Round(image.Width * scaleFactor);
        height = (int)Math.Round(image.Height * scaleFactor);
      }

      image.Metadata.HorizontalResolution = 96;
      image.Metadata.VerticalResolution = 96;

      _logger.LogInformation("Resizing image to {Width}x{Height} for WebP thumbnail.", width, height);

      image.Mutate(x => x.Resize(new ResizeOptions
      {
        Size = new Size(width, height),
        Mode = ResizeMode.Max
      }));

      var output = new MemoryStream();
      await image.SaveAsWebpAsync(output, new WebpEncoder
      {
        Quality = 75 // Adjust quality as needed
      });

      _logger.LogInformation("WebP thumbnail generated successfully. Size: {Size} bytes", output.Length);
      output.Position = 0; // Reset stream position to the beginning for reading
      return new ThumbnailResult(output, width, height, "webp");
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to generate WebP thumbnail.");
      throw new InvalidOperationException("Failed to generate WebP thumbnail.", ex);
    }
  }
}