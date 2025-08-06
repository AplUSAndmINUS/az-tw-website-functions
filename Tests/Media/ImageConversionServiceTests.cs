using SharedStorage.Services.Media;
using System;
using System.IO;
using System.Threading.Tasks;
using Utils;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using System.Reflection;

namespace Tests.Media;

/// <summary>
/// Unit tests for the ImageConversionService class
/// </summary>
public class ImageConversionServiceTests
{
  private readonly Mock<IAppInsightsLogger<ImageConversionService>> _mockLogger;
  private readonly ImageConversionService _imageService;

  public ImageConversionServiceTests()
  {
    _mockLogger = new Mock<IAppInsightsLogger<ImageConversionService>>();
    _imageService = new ImageConversionService(_mockLogger.Object);
  }

  [Fact]
  public void Constructor_InitializesCorrectly()
  {
    // Arrange & Act
    var service = new ImageConversionService(_mockLogger.Object);

    // Assert
    Assert.NotNull(service);
  }

  [Fact]
  public void Constructor_ThrowsExceptionWhenLoggerIsNull()
  {
    // Act & Assert
    Assert.Throws<ArgumentNullException>(() => new ImageConversionService(null));
  }

  [Fact]
  public async Task ConvertToWebPAsync_NullStream_ThrowsArgumentNullException()
  {
    // Arrange
    Stream nullStream = null;

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(() => _imageService.ConvertToWebPAsync(nullStream));
  }

  [Fact]
  public async Task ConvertToWebPAsync_EmptyStream_ThrowsInvalidOperationException()
  {
    // Arrange
    var emptyStream = new MemoryStream();

    // Act & Assert
    await Assert.ThrowsAsync<InvalidOperationException>(() => _imageService.ConvertToWebPAsync(emptyStream));
  }

  [Fact]
  public async Task ConvertToWebPAsync_NonSeekableStream_ThrowsInvalidOperationException()
  {
    // Arrange
    var mockStream = new Mock<Stream>();
    mockStream.Setup(s => s.CanSeek).Returns(false);
    mockStream.Setup(s => s.CanRead).Returns(true);
    mockStream.Setup(s => s.Length).Returns(100);

    // Act & Assert
    await Assert.ThrowsAsync<InvalidOperationException>(() => _imageService.ConvertToWebPAsync(mockStream.Object));
  }

  [Fact]
  public async Task ConvertToWebPAsync_NonReadableStream_ThrowsInvalidOperationException()
  {
    // Arrange
    var mockStream = new Mock<Stream>();
    mockStream.Setup(s => s.CanSeek).Returns(true);
    mockStream.Setup(s => s.CanRead).Returns(false);
    mockStream.Setup(s => s.Length).Returns(100);

    // Act & Assert
    await Assert.ThrowsAsync<InvalidOperationException>(() => _imageService.ConvertToWebPAsync(mockStream.Object));
  }

  [Fact]
  public async Task ConvertToWebPAsync_TooLargeStream_ThrowsInvalidOperationException()
  {
    // Arrange
    var mockStream = new Mock<Stream>();
    mockStream.Setup(s => s.CanSeek).Returns(true);
    mockStream.Setup(s => s.CanRead).Returns(true);
    mockStream.Setup(s => s.Length).Returns(60 * 1024 * 1024); // 60MB, over the default limit

    // Act & Assert
    await Assert.ThrowsAsync<InvalidOperationException>(() => _imageService.ConvertToWebPAsync(mockStream.Object));
  }

  [Fact]
  public async Task ConvertToWebPAsync_WithJpegImage_ReturnsWebPStream()
  {
    // Arrange
    // Create a simple JPEG image
    using var image = new Image<Rgba32>(100, 100);
    for (int x = 0; x < 100; x++)
    {
      for (int y = 0; y < 100; y++)
      {
        image[x, y] = new Rgba32((byte)(x % 256), (byte)(y % 256), (byte)((x + y) % 256), 255);
      }
    }

    // Save as JPEG to memory stream
    var jpegStream = new MemoryStream();
    await image.SaveAsJpegAsync(jpegStream);
    jpegStream.Position = 0;

    // Act
    var webpStream = await _imageService.ConvertToWebPAsync(jpegStream);

    // Assert
    Assert.NotNull(webpStream);
    Assert.True(webpStream.Length > 0);

    // Verify we can read it back as an image
    webpStream.Position = 0;
    var loadedImage = await Image.LoadAsync(webpStream);
    Assert.Equal(100, loadedImage.Width);
    Assert.Equal(100, loadedImage.Height);
  }

  [Fact]
  public async Task ConvertToWebPAsync_WithPngImage_ReturnsWebPStream()
  {
    // Arrange
    // Create a simple PNG image
    using var image = new Image<Rgba32>(100, 100);
    for (int x = 0; x < 100; x++)
    {
      for (int y = 0; y < 100; y++)
      {
        image[x, y] = new Rgba32((byte)(x % 256), (byte)(y % 256), (byte)((x + y) % 256), 255);
      }
    }

    // Save as PNG to memory stream
    var pngStream = new MemoryStream();
    await image.SaveAsPngAsync(pngStream);
    pngStream.Position = 0;

    // Act
    var webpStream = await _imageService.ConvertToWebPAsync(pngStream);

    // Assert
    Assert.NotNull(webpStream);
    Assert.True(webpStream.Length > 0);

    // Verify we can read it back as an image
    webpStream.Position = 0;
    var loadedImage = await Image.LoadAsync(webpStream);
    Assert.Equal(100, loadedImage.Width);
    Assert.Equal(100, loadedImage.Height);
  }

  [Fact]
  public async Task ConvertToWebPAsync_WithWebPImage_ReturnsSameWebPStream()
  {
    // Arrange
    // Create a simple WebP image
    using var image = new Image<Rgba32>(100, 100);
    for (int x = 0; x < 100; x++)
    {
      for (int y = 0; y < 100; y++)
      {
        image[x, y] = new Rgba32((byte)(x % 256), (byte)(y % 256), (byte)((x + y) % 256), 255);
      }
    }

    // Save as WebP to memory stream
    var webpInputStream = new MemoryStream();
    await image.SaveAsWebpAsync(webpInputStream);
    webpInputStream.Position = 0;

    // Act
    var webpOutputStream = await _imageService.ConvertToWebPAsync(webpInputStream);

    // Assert
    Assert.NotNull(webpOutputStream);
    Assert.True(webpOutputStream.Length > 0);

    // Verify we can read it back as an image
    webpOutputStream.Position = 0;
    var loadedImage = await Image.LoadAsync(webpOutputStream);
    Assert.Equal(100, loadedImage.Width);
    Assert.Equal(100, loadedImage.Height);
  }

  [Fact]
  public async Task ConvertToOptimizedFormatAsync_DefaultsToWebP()
  {
    // Arrange
    // Create a simple JPEG image
    using var image = new Image<Rgba32>(100, 100);
    for (int x = 0; x < 100; x++)
    {
      for (int y = 0; y < 100; y++)
      {
        image[x, y] = new Rgba32((byte)(x % 256), (byte)(y % 256), (byte)((x + y) % 256), 255);
      }
    }

    // Save as JPEG to memory stream
    var jpegStream = new MemoryStream();
    await image.SaveAsJpegAsync(jpegStream);
    jpegStream.Position = 0;

    // Act
    var result = await _imageService.ConvertToOptimizedFormatAsync(jpegStream);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("webp", result.Format);
    Assert.Equal(100, result.Width);
    Assert.Equal(100, result.Height);
    Assert.True(result.OutputBytes > 0);
  }

  [Fact]
  public async Task ConvertToOptimizedFormatAsync_RespectsMaxDimensions()
  {
    // Arrange
    // Create a simple JPEG image
    using var image = new Image<Rgba32>(1000, 800);
    for (int x = 0; x < 1000; x++)
    {
      for (int y = 0; y < 800; y++)
      {
        image[x, y] = new Rgba32((byte)(x % 256), (byte)(y % 256), (byte)((x + y) % 256), 255);
      }
    }

    // Save as JPEG to memory stream
    var jpegStream = new MemoryStream();
    await image.SaveAsJpegAsync(jpegStream);
    jpegStream.Position = 0;

    // Act
    var result = await _imageService.ConvertToOptimizedFormatAsync(jpegStream, 400);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("webp", result.Format);
    Assert.Equal(400, result.Width);  // Should be scaled down to 400
    Assert.Equal(320, result.Height); // Should maintain aspect ratio (400 * 800/1000 = 320)
    Assert.True(result.OutputBytes > 0);
  }

  [Fact]
  public async Task ConvertToOptimizedFormatAsync_HandlesNullMaxDimension()
  {
    // Arrange
    // Create a simple JPEG image
    using var image = new Image<Rgba32>(200, 100);
    for (int x = 0; x < 200; x++)
    {
      for (int y = 0; y < 100; y++)
      {
        image[x, y] = new Rgba32((byte)(x % 256), (byte)(y % 256), (byte)((x + y) % 256), 255);
      }
    }

    // Save as JPEG to memory stream
    var jpegStream = new MemoryStream();
    await image.SaveAsJpegAsync(jpegStream);
    jpegStream.Position = 0;

    // Act
    var result = await _imageService.ConvertToOptimizedFormatAsync(jpegStream, null);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("webp", result.Format);
    Assert.Equal(200, result.Width);  // Original size since no max dimension specified
    Assert.Equal(100, result.Height);
    Assert.True(result.OutputBytes > 0);
  }

  [Fact]
  public void ValidateInputStream_ValidatesLengthLimits()
  {
    // Use reflection to access the private ValidateInputStream method
    var validateMethod = typeof(ImageConversionService).GetMethod("ValidateInputStream",
        BindingFlags.NonPublic | BindingFlags.Instance);

    Assert.NotNull(validateMethod);

    // Test with null stream
    var exception = Assert.Throws<TargetInvocationException>(() =>
        validateMethod.Invoke(_imageService, new object[] { null }));
    Assert.IsType<ArgumentNullException>(exception.InnerException);

    // Test with non-readable stream
    var mockNonReadableStream = new Mock<Stream>();
    mockNonReadableStream.Setup(s => s.CanRead).Returns(false);

    exception = Assert.Throws<TargetInvocationException>(() =>
        validateMethod.Invoke(_imageService, new object[] { mockNonReadableStream.Object }));
    Assert.IsType<InvalidOperationException>(exception.InnerException);

    // Test with empty stream
    var emptyStream = new MemoryStream();

    exception = Assert.Throws<TargetInvocationException>(() =>
        validateMethod.Invoke(_imageService, new object[] { emptyStream }));
    Assert.IsType<InvalidOperationException>(exception.InnerException);

    // Test with oversized stream
    var mockOversizedStream = new Mock<Stream>();
    mockOversizedStream.Setup(s => s.CanRead).Returns(true);
    mockOversizedStream.Setup(s => s.Length).Returns(60 * 1024 * 1024); // 60MB

    exception = Assert.Throws<TargetInvocationException>(() =>
        validateMethod.Invoke(_imageService, new object[] { mockOversizedStream.Object }));
    Assert.IsType<InvalidOperationException>(exception.InnerException);

    // Test with valid stream
    var validStream = new MemoryStream(new byte[1000]);

    var result = validateMethod.Invoke(_imageService, new object[] { validStream });
    // No exception means validation passed
    Assert.Null(result); // Void method returns null
  }
}
