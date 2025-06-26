using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Moq;
using SharedStorage.Services.MediaServices;
using SharedStorage.Services.BaseServices;
using SharedStorage.Services.Media.Handlers;
using SharedStorage.Models;
using Utils;

namespace Tests.Media;

public class MediaServiceTests
{
  private readonly Mock<IBlobStorageService> _mockBlobStorageService;
  private readonly Mock<ITableStorageService> _mockTableStorageService;
  private readonly Mock<IMediaTypeHandler> _mockImageHandler;
  private readonly Mock<IMediaTypeHandler> _mockVideoHandler;
  private readonly Mock<IAppInsightsLogger<MediaService>> _mockLogger;
  private readonly MediaService _mediaService;

  public MediaServiceTests()
  {
    _mockBlobStorageService = new Mock<IBlobStorageService>();
    _mockTableStorageService = new Mock<ITableStorageService>();
    _mockImageHandler = new Mock<IMediaTypeHandler>();
    _mockVideoHandler = new Mock<IMediaTypeHandler>();
    _mockLogger = new Mock<IAppInsightsLogger<MediaService>>();

    _mockImageHandler.Setup(x => x.CanHandle(It.Is<string>(ct => ct.StartsWith("image/")))).Returns(true);
    _mockVideoHandler.Setup(x => x.CanHandle(It.Is<string>(ct => ct.StartsWith("video/")))).Returns(true);

    var handlers = new List<IMediaTypeHandler> { _mockImageHandler.Object, _mockVideoHandler.Object };

    _mediaService = new MediaService(
        _mockBlobStorageService.Object,
        _mockTableStorageService.Object,
        handlers,
        _mockLogger.Object);
  }

  [Fact]
  public async Task UploadMediaAsync_ValidImage_ReturnsSuccess()
  {
    // Arrange
    var fileData = new byte[] { 1, 2, 3, 4 };
    var fileName = "test.jpg";
    var contentType = "image/jpeg";
    var description = "Test image";

    var expectedMedia = new MediaEntity
    {
      Id = "test-id",
      FileName = fileName,
      ContentType = contentType,
      Description = description,
      MediaType = "image",
      FileSize = fileData.Length,
      UploadDate = DateTime.UtcNow,
      BlobUrl = "https://test.blob.core.windows.net/test.jpg",
      ThumbnailUrl = "https://test.blob.core.windows.net/test_thumb.jpg"
    };

    _mockImageHandler
        .Setup(x => x.ProcessMediaAsync(It.IsAny<byte[]>(), fileName, contentType, description))
        .ReturnsAsync(new ServiceResult<MediaEntity> { IsSuccess = true, Data = expectedMedia });

    // Act
    var result = await _mediaService.UploadMediaAsync(fileData, fileName, contentType, description);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Data);
    Assert.Equal(fileName, result.Data.FileName);
    Assert.Equal(contentType, result.Data.ContentType);

    _mockImageHandler.Verify(x => x.ProcessMediaAsync(fileData, fileName, contentType, description), Times.Once);
  }

  [Fact]
  public async Task UploadMediaAsync_NullFileData_ReturnsFailure()
  {
    // Act
    var result = await _mediaService.UploadMediaAsync(null, "test.jpg", "image/jpeg", "Test");

    // Assert
    Assert.False(result.IsSuccess);
    Assert.Contains("File data cannot be null or empty", result.ErrorMessage);
  }

  [Fact]
  public async Task UploadMediaAsync_EmptyFileName_ReturnsFailure()
  {
    // Act
    var result = await _mediaService.UploadMediaAsync(new byte[] { 1, 2, 3 }, "", "image/jpeg", "Test");

    // Assert
    Assert.False(result.IsSuccess);
    Assert.Contains("File name cannot be null or empty", result.ErrorMessage);
  }

  [Fact]
  public async Task UploadMediaAsync_UnsupportedContentType_ReturnsFailure()
  {
    // Act
    var result = await _mediaService.UploadMediaAsync(new byte[] { 1, 2, 3 }, "test.txt", "text/plain", "Test");

    // Assert
    Assert.False(result.IsSuccess);
    Assert.Contains("Unsupported media type", result.ErrorMessage);
  }

  [Fact]
  public async Task GetMediaAsync_ValidId_ReturnsSuccess()
  {
    // Arrange
    var mediaId = "test-media-id";
    var expectedMedia = new MediaEntity
    {
      Id = mediaId,
      FileName = "test.jpg",
      ContentType = "image/jpeg",
      MediaType = "image"
    };

    _mockTableStorageService
        .Setup(x => x.GetEntityAsync<MediaEntity>("media", mediaId))
        .ReturnsAsync(expectedMedia);

    // Act
    var result = await _mediaService.GetMediaAsync(mediaId);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Data);
    Assert.Equal(mediaId, result.Data.Id);

    _mockTableStorageService.Verify(x => x.GetEntityAsync<MediaEntity>("media", mediaId), Times.Once);
  }

  [Fact]
  public async Task GetMediaAsync_EmptyId_ReturnsFailure()
  {
    // Act
    var result = await _mediaService.GetMediaAsync("");

    // Assert
    Assert.False(result.IsSuccess);
    Assert.Contains("Media ID cannot be null or empty", result.ErrorMessage);
  }

  [Fact]
  public async Task GetMediaBatchAsync_ValidIds_ReturnsSuccess()
  {
    // Arrange
    var mediaIds = new List<string> { "media-1", "media-2" };
    var expectedMedia = new List<MediaEntity>
        {
            new MediaEntity { Id = "media-1", FileName = "test1.jpg" },
            new MediaEntity { Id = "media-2", FileName = "test2.jpg" }
        };

    _mockTableStorageService
        .Setup(x => x.GetEntitiesBatchAsync<MediaEntity>("media", mediaIds))
        .ReturnsAsync(expectedMedia);

    // Act
    var result = await _mediaService.GetMediaBatchAsync(mediaIds);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Data);
    Assert.Equal(2, result.Data.Count);

    _mockTableStorageService.Verify(x => x.GetEntitiesBatchAsync<MediaEntity>("media", mediaIds), Times.Once);
  }

  [Fact]
  public async Task DeleteMediaAsync_ValidId_ReturnsSuccess()
  {
    // Arrange
    var mediaId = "test-media-id";
    var existingMedia = new MediaEntity
    {
      Id = mediaId,
      FileName = "test.jpg",
      BlobUrl = "https://test.blob.core.windows.net/test.jpg",
      ThumbnailUrl = "https://test.blob.core.windows.net/test_thumb.jpg"
    };

    _mockTableStorageService
        .Setup(x => x.GetEntityAsync<MediaEntity>("media", mediaId))
        .ReturnsAsync(existingMedia);

    _mockBlobStorageService
        .Setup(x => x.DeleteBlobAsync("media", "test.jpg"))
        .ReturnsAsync(true);

    _mockBlobStorageService
        .Setup(x => x.DeleteBlobAsync("media", "test_thumb.jpg"))
        .ReturnsAsync(true);

    _mockTableStorageService
        .Setup(x => x.DeleteEntityAsync("media", mediaId))
        .ReturnsAsync(true);

    // Act
    var result = await _mediaService.DeleteMediaAsync(mediaId);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.True(result.Data);

    _mockTableStorageService.Verify(x => x.GetEntityAsync<MediaEntity>("media", mediaId), Times.Once);
    _mockTableStorageService.Verify(x => x.DeleteEntityAsync("media", mediaId), Times.Once);
  }

  [Fact]
  public async Task DeleteMediaAsync_EmptyId_ReturnsFailure()
  {
    // Act
    var result = await _mediaService.DeleteMediaAsync("");

    // Assert
    Assert.False(result.IsSuccess);
    Assert.Contains("Media ID cannot be null or empty", result.ErrorMessage);
  }

  [Fact]
  public async Task DeleteMediaBatchAsync_ValidIds_ReturnsSuccess()
  {
    // Arrange
    var mediaIds = new List<string> { "media-1", "media-2" };
    var existingMedia = new List<MediaEntity>
        {
            new MediaEntity { Id = "media-1", FileName = "test1.jpg", BlobUrl = "url1", ThumbnailUrl = "thumb1" },
            new MediaEntity { Id = "media-2", FileName = "test2.jpg", BlobUrl = "url2", ThumbnailUrl = "thumb2" }
        };

    _mockTableStorageService
        .Setup(x => x.GetEntitiesBatchAsync<MediaEntity>("media", mediaIds))
        .ReturnsAsync(existingMedia);

    _mockBlobStorageService
        .Setup(x => x.DeleteBlobAsync(It.IsAny<string>(), It.IsAny<string>()))
        .ReturnsAsync(true);

    _mockTableStorageService
        .Setup(x => x.DeleteEntitiesBatchAsync("media", mediaIds))
        .ReturnsAsync(true);

    // Act
    var result = await _mediaService.DeleteMediaBatchAsync(mediaIds);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.True(result.Data);

    _mockTableStorageService.Verify(x => x.GetEntitiesBatchAsync<MediaEntity>("media", mediaIds), Times.Once);
    _mockTableStorageService.Verify(x => x.DeleteEntitiesBatchAsync("media", mediaIds), Times.Once);
  }
}
