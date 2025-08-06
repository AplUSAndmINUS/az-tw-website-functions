using SharedStorage.Services.Media;
using System;
using System.IO;
using System.Threading.Tasks;
using Utils;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Webp;

namespace MediaTests;

/// <summary>
/// Unit tests for the ImageConversionService class
/// </summary>
public class ImageConversionServiceTests
{
  private readonly Mock<IAppInsightsLogger<ImageConversionService>> _mockLogger;
  private readonly ImageConversionService _service;

  public ImageConversionServiceTests()
  {
    _mockLogger = new Mock<IAppInsightsLogger<ImageConversionService>>();
    _service = new ImageConversionService(_mockLogger.Object);
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
    Assert.Throws<ArgumentNullException>(() => new ImageConversionService(null!));
  }

  [Fact]
  public async Task ConvertToWebPAsync_NullStream_ThrowsArgumentNullException()
  {
    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(() => _service.ConvertToWebPAsync(null!));
  }

  [Fact]
  public async Task ConvertToWebPAsync_EmptyStream_ThrowsInvalidOperationException()
  {
    // Arrange
    var emptyStream = new MemoryStream();

    // Act & Assert
    await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ConvertToWebPAsync(emptyStream));
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
    await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ConvertToWebPAsync(mockStream.Object));
  }

  [Fact]
  public async Task ConvertToWebPAsync_ValidImage_ReturnsWebPResult()
  {
    // This test checks that a valid image can be converted to WebP format

    // Arrange - Create a small test image
    using var image = new Image<Rgba32>(100, 100);
    for (int x = 0; x < image.Width; x++)
    {
      for (int y = 0; y < image.Height; y++)
      {
        image[x, y] = new Rgba32((byte)(x % 256), (byte)(y % 256), 100, 255);
      }
    }

    // Save the image to a memory stream
    var inputStream = new MemoryStream();
    await image.SaveAsPngAsync(inputStream);
    inputStream.Position = 0;

    // Act
    var result = await _service.ConvertToWebPAsync(inputStream);

    // Assert
    Assert.NotNull(result);
    Assert.True(result.Width > 0);
    Assert.True(result.Height > 0);
    Assert.Equal("webp", result.Format);
    Assert.True(result.Content.Length > 0);
  }
}
