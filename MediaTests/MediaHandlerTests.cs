using SharedStorage.Services.Media;
using SharedStorage.Services.Media.Handlers;
using SharedStorage.Services.BaseServices;
using SharedStorage.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Utils;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;

namespace MediaTests;

/// <summary>
/// Base test for Media handlers
/// </summary>
public class MediaHandlerTests
{
  // Define a test implementation of MediaHandler
  private class TestMediaHandler : MediaHandler
  {
    public TestMediaHandler() : base("test") { }

    public override Task<MediaEntity> UploadAsync(Stream stream, string fileName, string contentType, string? authorId = null, string? contentId = null, string? relatedContentType = null)
    {
      var entity = new MediaEntity
      {
        Id = Guid.NewGuid().ToString(),
        MediaType = this.SupportedType,
        Filename = fileName,
        ContentType = contentType,
        AuthorId = authorId ?? "system"
      };
      return Task.FromResult(entity);
    }

    public override Task<MediaEntity> GetAsync(string id)
    {
      var entity = new MediaEntity
      {
        Id = id,
        MediaType = this.SupportedType
      };
      return Task.FromResult(entity);
    }

    public override Task<IEnumerable<MediaEntity>> GetAllAsync(string? authorSlug = null, int? limit = null)
    {
      var entities = new List<MediaEntity>
            {
                new MediaEntity { Id = "1", MediaType = this.SupportedType },
                new MediaEntity { Id = "2", MediaType = this.SupportedType }
            };
      return Task.FromResult((IEnumerable<MediaEntity>)entities);
    }

    public override Task<bool> DeleteAsync(string id)
    {
      // Simulate successful deletion
      return Task.FromResult(true);
    }
  }

  [Fact]
  public void Constructor_SetsCorrectType()
  {
    // Arrange & Act
    var handler = new TestMediaHandler();

    // Assert
    Assert.Equal("test", handler.SupportedType);
  }

  [Fact]
  public async Task UploadAsync_SetsExpectedProperties()
  {
    // Arrange
    var handler = new TestMediaHandler();
    var stream = new MemoryStream(new byte[100]);
    var fileName = "test.txt";
    var contentType = "text/plain";
    var authorId = "test-author";

    // Act
    var result = await handler.UploadAsync(stream, fileName, contentType, authorId);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("test", result.MediaType);
    Assert.Equal(fileName, result.Filename);
    Assert.Equal(contentType, result.ContentType);
    Assert.Equal(authorId, result.AuthorId);
  }

  [Fact]
  public async Task GetAsync_ReturnsEntityWithCorrectId()
  {
    // Arrange
    var handler = new TestMediaHandler();
    var id = "test-id";

    // Act
    var result = await handler.GetAsync(id);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(id, result.Id);
    Assert.Equal("test", result.MediaType);
  }

  [Fact]
  public async Task GetAllAsync_ReturnsExpectedCount()
  {
    // Arrange
    var handler = new TestMediaHandler();

    // Act
    var result = await handler.GetAllAsync();

    // Assert
    Assert.NotNull(result);
    Assert.Equal(2, result.Count());
  }

  [Fact]
  public async Task DeleteAsync_ReturnsTrue()
  {
    // Arrange
    var handler = new TestMediaHandler();
    var id = "test-id";

    // Act
    var result = await handler.DeleteAsync(id);

    // Assert
    Assert.True(result);
  }
}
