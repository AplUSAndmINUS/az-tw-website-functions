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
using System.Collections.Generic;
using System.Linq;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;

namespace Tests.Media;

/// <summary>
/// Unit tests for the MediaService class
/// </summary>
public class MediaServiceTests
{
  private readonly Mock<ITableStorageService> _mockTableStorageService;
  private readonly Mock<IMediaTypeHandler> _mockImageHandler;
  private readonly Mock<IAppInsightsLogger<MediaService>> _mockLogger;
  private readonly MediaService _mediaService;

  public MediaServiceTests()
  {
    _mockTableStorageService = new Mock<ITableStorageService>();
    _mockLogger = new Mock<IAppInsightsLogger<MediaService>>();
    _mockImageHandler = new Mock<IMediaTypeHandler>();
    _mockImageHandler.Setup(h => h.SupportedType).Returns("image");

    var handlers = new List<IMediaTypeHandler> { _mockImageHandler.Object };

    _mediaService = new MediaService(
        handlers,
        _mockTableStorageService.Object,
        _mockLogger.Object
    );
  }

  [Fact]
  public void Constructor_InitializesCorrectly()
  {
    // Arrange & Act
    var handlers = new List<IMediaTypeHandler> { _mockImageHandler.Object };

    var service = new MediaService(
        handlers,
        _mockTableStorageService.Object,
        _mockLogger.Object
    );

    // Assert
    Assert.NotNull(service);
  }

  [Fact]
  public async Task UploadMediaAsync_NullMediaType_ThrowsArgumentException()
  {
    // Arrange
    string nullMediaType = null;
    var stream = new MemoryStream(Encoding.UTF8.GetBytes("test-data"));
    string fileName = "test.jpg";

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentException>(() =>
        _mediaService.UploadMediaAsync(nullMediaType, stream, fileName));
  }

  [Fact]
  public async Task UploadMediaAsync_NullStream_ThrowsArgumentException()
  {
    // Arrange
    string mediaType = "image";
    Stream nullStream = null;
    string fileName = "test.jpg";

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentException>(() =>
        _mediaService.UploadMediaAsync(mediaType, nullStream, fileName));
  }

  [Fact]
  public async Task UploadMediaAsync_NullFileName_ThrowsArgumentException()
  {
    // Arrange
    string mediaType = "image";
    var stream = new MemoryStream(Encoding.UTF8.GetBytes("test-data"));
    string nullFileName = null;

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentException>(() =>
        _mediaService.UploadMediaAsync(mediaType, stream, nullFileName));
  }

  [Fact]
  public async Task UploadMediaAsync_UnsupportedMediaType_ThrowsInvalidOperationException()
  {
    // Arrange
    string unsupportedType = "unsupported-type";
    var stream = new MemoryStream(Encoding.UTF8.GetBytes("test-data"));
    string fileName = "test.jpg";

    // Act & Assert
    await Assert.ThrowsAsync<InvalidOperationException>(() =>
        _mediaService.UploadMediaAsync(unsupportedType, stream, fileName));
  }

  [Fact]
  public async Task UploadMediaAsync_ValidData_UploadsAndSavesMetadata()
  {
    // Arrange
    string mediaType = "image";
    var stream = new MemoryStream(Encoding.UTF8.GetBytes("test-data"));
    string fileName = "test.jpg";
    string authorId = "author-123";
    string description = "Test image";
    string altText = "A test image";
    string purpose = "coverImage";
    string contentId = "content-123";
    string relatedContentType = "blog";

    var mediaEntity = new MediaEntity
    {
      Id = "media-123",
      MediaType = "image",
      Filename = "test.webp",
      AuthorId = authorId,
      Url = "https://example.com/test.webp",
      ThumbnailUrl = "https://example.com/thumb_test.webp"
    };

    _mockImageHandler
        .Setup(h => h.UploadAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
        .ReturnsAsync(mediaEntity);

    _mockTableStorageService
        .Setup(s => s.UpsertEntityAsync(
            It.IsAny<string>(),
            It.IsAny<MediaEntity>()))
        .Returns(Task.CompletedTask);

    // Act
    var result = await _mediaService.UploadMediaAsync(
        mediaType, stream, fileName, authorId, description, altText, purpose, contentId, relatedContentType);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("media-123", result.Id);
    Assert.Equal(mediaType, result.MediaType);
    Assert.Equal(description, result.Description);
    Assert.Equal(altText, result.AltText);
    Assert.Equal(purpose, result.Purpose);

    _mockImageHandler.Verify(
        h => h.UploadAsync(
            stream,
            fileName,
            It.IsAny<string>(),
            authorId,
            contentId,
            relatedContentType),
        Times.Once);

    _mockTableStorageService.Verify(
        s => s.UpsertEntityAsync(
            It.IsAny<string>(),
            It.Is<MediaEntity>(e =>
                e.Id == "media-123" &&
                e.Description == description &&
                e.AltText == altText &&
                e.Purpose == purpose)),
        Times.Once);
  }

  [Fact]
  public async Task GetMediaAsync_WithValidId_ReturnsMediaEntity()
  {
    // Arrange
    string mediaId = "media-123";

    var tableEntity = new TableEntity("partition", "row")
    {
      ["Id"] = mediaId,
      ["MediaType"] = "image",
      ["Filename"] = "test.webp",
      ["AuthorId"] = "author-123",
      ["Url"] = "https://example.com/test.webp",
      ["ThumbnailUrl"] = "https://example.com/thumb_test.webp",
      ["Description"] = "Test image",
      ["AltText"] = "A test image",
      ["Purpose"] = "coverImage",
      ["ContentType"] = "image/webp",
      ["Width"] = 800,
      ["Height"] = 600,
      ["UploadedAt"] = DateTime.UtcNow
    };

    var tableResult = new TableStorageResult<TableEntity>
    {
      Entities = new List<TableEntity> { tableEntity }
    };

    _mockTableStorageService
        .Setup(s => s.GetEntitiesAsync(
            It.IsAny<string>(),
            It.Is<string>(f => f.Contains(mediaId)),
            It.IsAny<int>()))
        .ReturnsAsync(tableResult);

    // Act
    var result = await _mediaService.GetMediaAsync(mediaId);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(mediaId, result.Id);
    Assert.Equal("image", result.MediaType);
    Assert.Equal("test.webp", result.Filename);
    Assert.Equal("https://example.com/test.webp", result.Url);
  }

  [Fact]
  public async Task GetMediaAsync_WithInvalidId_ReturnsNull()
  {
    // Arrange
    string mediaId = "nonexistent";

    var tableResult = new TableStorageResult<TableEntity>
    {
      Entities = new List<TableEntity>()
    };

    _mockTableStorageService
        .Setup(s => s.GetEntitiesAsync(
            It.IsAny<string>(),
            It.Is<string>(f => f.Contains(mediaId)),
            It.IsAny<int>()))
        .ReturnsAsync(tableResult);

    // Act
    var result = await _mediaService.GetMediaAsync(mediaId);

    // Assert
    Assert.Null(result);
  }

  [Fact]
  public async Task DeleteMediaAsync_WithValidId_DeletesEntity()
  {
    // Arrange
    string mediaId = "media-123";

    var tableEntity = new TableEntity("partition", "row")
    {
      ["Id"] = mediaId,
      ["MediaType"] = "image",
      ["Filename"] = "test.webp",
      ["AuthorId"] = "author-123",
      ["PartitionKey"] = "partition",
      ["RowKey"] = "row"
    };

    var tableResult = new TableStorageResult<TableEntity>
    {
      Entities = new List<TableEntity> { tableEntity }
    };

    _mockTableStorageService
        .Setup(s => s.GetEntitiesAsync(
            It.IsAny<string>(),
            It.Is<string>(f => f.Contains(mediaId)),
            It.IsAny<int>()))
        .ReturnsAsync(tableResult);

    _mockTableStorageService
        .Setup(s => s.DeleteEntityAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
        .Returns(Task.CompletedTask);

    _mockImageHandler
        .Setup(h => h.DeleteAsync(It.IsAny<string>()))
        .ReturnsAsync(true);

    // Act
    var result = await _mediaService.DeleteMediaAsync(mediaId);

    // Assert
    Assert.True(result);

    _mockImageHandler.Verify(
        h => h.DeleteAsync(mediaId),
        Times.Once);

    _mockTableStorageService.Verify(
        s => s.DeleteEntityAsync(
            It.IsAny<string>(),
            "partition",
            "row"),
        Times.Once);
  }

  [Fact]
  public async Task UploadImageAsync_ValidData_CallsUploadMediaAsync()
  {
    // Arrange
    var stream = new MemoryStream(Encoding.UTF8.GetBytes("test-data"));
    string fileName = "test.jpg";
    string authorId = "author-123";
    string description = "Test image";
    string altText = "A test image";
    string purpose = "coverImage";
    string contentId = "content-123";
    string relatedContentType = "blog";

    var mediaEntity = new MediaEntity
    {
      Id = "media-123",
      MediaType = "image",
      Filename = "test.webp"
    };

    _mockImageHandler
        .Setup(h => h.UploadAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
        .ReturnsAsync(mediaEntity);

    _mockTableStorageService
        .Setup(s => s.UpsertEntityAsync(
            It.IsAny<string>(),
            It.IsAny<MediaEntity>()))
        .Returns(Task.CompletedTask);

    // Act
    var result = await _mediaService.UploadImageAsync(
        stream, fileName, authorId, description, altText, purpose, contentId, relatedContentType);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("media-123", result.Id);

    _mockImageHandler.Verify(
        h => h.UploadAsync(
            stream,
            fileName,
            It.IsAny<string>(),
            authorId,
            contentId,
            relatedContentType),
        Times.Once);
  }
}
