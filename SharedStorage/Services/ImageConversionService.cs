using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using Microsoft.Extensions.Logging;

namespace SharedStorage.Services;

public interface IImageService
{
  Task<ImageConversionResult> ConvertToWebPAsync(Stream input);
}

public record ImageConversionResult(Stream Content, int Width, int Height, string Format);

public class ImageConversionService : IImageService
{
  private readonly ILogger<ImageConversionService> _logger;

  public ImageConversionService(ILogger<ImageConversionService> logger)
  {
    _logger = logger;
  }

  public async Task<ImageConversionResult> ConvertToWebPAsync(Stream input)
  {
    if (input == null)
    {
      _logger.LogError("Input stream is null. Cannot convert to WebP.");
      throw new ArgumentNullException(nameof(input), "Input stream cannot be null.");
    }

    try
    {
      // Load the image from the input stream
      _logger.LogInformation("Loading image from input stream for WebP conversion.");
      input.Position = 0; // Reset stream position to the beginning
      using var image = await Image.LoadAsync(input);
      // remove JPG EXIF rotation if present
      image.Mutate(x => x.AutoOrient());

      _logger.LogInformation("Image loaded successfully. Dimensions: {Width}x{Height}", image.Width, image.Height);

      if (image.Width < 600 || image.Height < 600)
      {
        _logger.LogInformation("Resizing image for WebP conversion due to insufficient dimensions: {Width}x{Height}.", image.Width, image.Height);
        double scaleFactor = 600.0 / Math.Min(image.Width, image.Height);
        image.Mutate(x => x.Resize(new ResizeOptions
        {
          Size = new Size((int)Math.Round(image.Width * scaleFactor), (int)Math.Round(image.Height * scaleFactor)),
          Mode = ResizeMode.Max
        }));
      }

      var resizedWidth = image.Width;
      var resizedHeight = image.Height;

      image.Metadata.HorizontalResolution = 96;
      image.Metadata.VerticalResolution = 96;
      // Create a memory stream to hold the converted image
      var output = new MemoryStream();

      // Save the image as WebP
      await image.SaveAsWebpAsync(output, new WebpEncoder { Quality = 85 });
      
      _logger.LogInformation("WebP conversion completed successfully. Size: {Size} bytes", output.Length);
      
      output.Position = 0; // Reset stream position to the beginning for reading
      return new ImageConversionResult(output, resizedWidth, resizedHeight, "webp");
    }

    catch (Exception ex)
    {
      _logger.LogError(ex, "Error converting image to WebP format.");
      throw new InvalidOperationException("Failed to convert image to WebP format.", ex);
    }
  }
}