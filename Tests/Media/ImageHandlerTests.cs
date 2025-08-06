using SharedStorage.Services.Media;
using SharedStorage.Services.Media.Handlers;
using SharedStorage.Services.BaseServices;
using SharedStorage.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using Utils;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Tests.Media;

/// <summary>
/// Unit tests for the ImageHandler class
/// </summary>
public class ImageHandlerTests
{
  private readonly Mock<IBlobStorageService> _mockBlobStorageService;
  private readonly Mock<IThumbnailService> _mockThumbnailService;
  private readonly Mock<IImageService> _mockImageService;
  private readonly Mock<IAppInsightsLogger<ImageHandler>> _mockLogger;
  private readonly ImageHandler _imageHandler;

  public ImageHandlerTests()
  {
    _mockBlobStorageService = new Mock<IBlobStorageService>();
    _mockThumbnailService = new Mock<IThumbnailService>();
    _mockImageService = new Mock<IImageService>();
    _mockLogger = new Mock<IAppInsightsLogger<ImageHandler>>();

    _imageHandler = new ImageHandler(
        _mockBlobStorageService.Object,
        _mockThumbnailService.Object,
        _mockImageService.Object,
        _mockLogger.Object
    );
  }

  [Fact]
  public void Constructor_InitializesCorrectly()
  {
    // Arrange & Act
    var handler = new ImageHandler(
        _mockBlobStorageService.Object,
        _mockThumbnailService.Object,
        _mockImageService.Object,
        _mockLogger.Object
    );

    // Assert
    Assert.NotNull(handler);
  }

  [Fact]
  public async Task UploadAsync_NullStream_ThrowsArgumentNullException()
  {
    // Arrange
    Stream nullStream = null;
    string fileName = "test.jpg";
    string contentType = "image/jpeg";

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentException>(() =>
        _imageHandler.UploadAsync(nullStream, fileName, contentType));
  }

  [Fact]
  public async Task UploadAsync_NonSeekableStream_ThrowsArgumentException()
  {
    // Arrange
    var mockStream = new Mock<Stream>();
    mockStream.Setup(s => s.CanRead).Returns(true);
    mockStream.Setup(s => s.CanSeek).Returns(false);
    string fileName = "test.jpg";
    string contentType = "image/jpeg";

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentException>(() =>
        _imageHandler.UploadAsync(mockStream.Object, fileName, contentType));
  }

  [Fact]
  public async Task UploadAsync_EmptyStream_ThrowsArgumentException()
  {
    // Arrange
    var emptyStream = new MemoryStream();
    emptyStream.SetLength(0);
    string fileName = "test.jpg";
    string contentType = "image/jpeg";

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentException>(() =>
        _imageHandler.UploadAsync(emptyStream, fileName, contentType));
  }

  [Fact]
  public async Task UploadAsync_ValidData_UploadsImageAndThumbnail()
  {
    // Arrange
    var mockStream = new MemoryStream(new byte[100]); // Non-empty stream
    string fileName = "test.jpg";
    string contentType = "image/jpeg";

    var webpConversionResult = new ImageConversionResult(
        new MemoryStream(Encoding.UTF8.GetBytes("webp-data")),
        100, 100, "webp", 1000);

    var thumbnailResult = new ImageConversionResult(
        new MemoryStream(Encoding.UTF8.GetBytes("thumbnail-data")),
        50, 50, "webp", 500);

    _mockImageService
        .Setup(s => s.ConvertToWebPAsync(It.IsAny<Stream>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int>()))
        .ReturnsAsync(webpConversionResult);

    _mockThumbnailService
        .Setup(s => s.GenerateWebPThumbnailAsync(It.IsAny<Stream>()))
        .ReturnsAsync(thumbnailResult);

    var mediaReference = new MediaReference
    {
      CdnUrl = "https://cdn.example.com/images/123/test.webp",
      ThumbnailCdnUrl = "https://cdn.example.com/images/123/thumb_test.webp"
    };

    _mockBlobStorageService
        .Setup(s => s.UploadBlobAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
        .ReturnsAsync(mediaReference);

    // Act
    var result = await _imageHandler.UploadAsync(mockStream, fileName, contentType);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("image", result.MediaType);
    Assert.Equal("image/webp", result.ContentType);
    Assert.Equal(100, result.Width);
    Assert.Equal(100, result.Height);
    Assert.Equal(mediaReference.CdnUrl, result.Url);
    Assert.Equal(mediaReference.ThumbnailCdnUrl, result.ThumbnailUrl);

    _mockImageService.Verify(
        s => s.ConvertToWebPAsync(It.IsAny<Stream>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int>()),
        Times.Once);

    _mockThumbnailService.Verify(
        s => s.GenerateWebPThumbnailAsync(It.IsAny<Stream>()),
        Times.Once);

    _mockBlobStorageService.Verify(
        s => s.UploadBlobAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>()),
        Times.Exactly(2));
  }

  [Fact]
  public async Task UploadAsync_ConversionFails_LogsAndRethrows()
  {
    // Arrange
    var mockStream = new MemoryStream(new byte[100]); // Non-empty stream
    string fileName = "test.jpg";
    string contentType = "image/jpeg";

    var expectedException = new InvalidOperationException("Image conversion failed");

    _mockImageService
        .Setup(s => s.ConvertToWebPAsync(It.IsAny<Stream>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int>()))
        .ThrowsAsync(expectedException);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
        _imageHandler.UploadAsync(mockStream, fileName, contentType));

    Assert.Equal(expectedException.Message, exception.Message);

    _mockLogger.Verify(
        l => l.LogError(
            It.IsAny<Exception>(),
            It.IsAny<string>(),
            It.IsAny<object[]>()),
        Times.Once);
  }

  [Fact]
  public async Task DeleteAsync_ValidId_DeletesBlobs()
  {
    // Arrange
    string mediaId = "test-media-id";

    var mockBlobList = new BlobStorageResult
    {
      Blobs = new System.Collections.Generic.List<BlobItem>
            {
                new BlobItem { Name = $"images/{mediaId}/test.webp" },
                new BlobItem { Name = $"images/{mediaId}/thumb_test.webp" }
            }
    };

    _mockBlobStorageService
        .Setup(s => s.GetBlobsAsync(It.IsAny<string>(), It.IsAny<string>()))
        .ReturnsAsync(mockBlobList);

    _mockBlobStorageService
        .Setup(s => s.DeleteBlobAsync(It.IsAny<string>(), It.IsAny<string>()))
        .Returns(Task.CompletedTask);

    // Act
    var result = await _imageHandler.DeleteAsync(mediaId);

    // Assert
    Assert.True(result);

    _mockBlobStorageService.Verify(
        s => s.GetBlobsAsync(It.IsAny<string>(), $"images/{mediaId}/"),
        Times.Once);

    _mockBlobStorageService.Verify(
        s => s.DeleteBlobAsync(It.IsAny<string>(), It.IsAny<string>()),
        Times.Exactly(2));
  }
}
