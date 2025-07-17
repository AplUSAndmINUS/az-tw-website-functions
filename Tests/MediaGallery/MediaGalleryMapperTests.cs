using Xunit;
using SharedStorage.Models;
using SharedStorage.Services.Media.Platforms;
using Utils;
using Moq;

namespace Tests.MediaGallery;

public class MediaGalleryMapperTests
{
  [Fact]
  public void ToGalleryDTO_ShouldMapBasicProperties()
  {
    // Arrange
    var mediaEntity = new MediaEntity
    {
      Id = "test-id",
      AuthorId = "test-author",
      MediaType = "image",
      ContentType = "image/jpeg",
      Description = "Test description",
      Url = "https://example.com/image.jpg",
      ThumbnailUrl = "https://example.com/thumb.jpg",
      AltText = "Test alt text",
      Width = 800,
      Height = 600,
      Platform = "instagram",
      ExternalUrl = "https://instagram.com/post/123",
      LikeCount = 100,
      ShareCount = 50,
      ViewCount = 1000,
      Tags = "tag1,tag2,tag3",
      UploadedAt = DateTime.UtcNow,
      Purpose = "gallery"
    };

    // Act
    var dto = mediaEntity.ToGalleryDTO();

    // Assert
    Assert.Equal(mediaEntity.Id, dto.Id);
    Assert.Equal(mediaEntity.AuthorId, dto.AuthorId);
    Assert.Equal(mediaEntity.MediaType, dto.MediaType);
    Assert.Equal(mediaEntity.ContentType, dto.ContentType);
    Assert.Equal(mediaEntity.Description, dto.Description);
    Assert.Equal(mediaEntity.ExternalUrl, dto.Url); // Should use external URL for external platforms
    Assert.Equal(mediaEntity.ThumbnailUrl, dto.ThumbnailUrl);
    Assert.Equal(mediaEntity.AltText, dto.AltText);
    Assert.Equal(mediaEntity.Width, dto.Width);
    Assert.Equal(mediaEntity.Height, dto.Height);
    Assert.Equal(mediaEntity.Platform, dto.Platform);
    Assert.Equal("Instagram", dto.PlatformDisplayName);
    Assert.Equal(mediaEntity.ExternalUrl, dto.ExternalUrl);
    Assert.Equal(mediaEntity.LikeCount, dto.LikeCount);
    Assert.Equal(mediaEntity.ShareCount, dto.ShareCount);
    Assert.Equal(mediaEntity.ViewCount, dto.ViewCount);
    Assert.Equal(new[] { "tag1", "tag2", "tag3" }, dto.Tags);
    Assert.Equal(mediaEntity.Purpose, dto.Purpose);
    Assert.True(dto.IsExternal);
  }

  [Fact]
  public void ToGalleryDTO_ShouldHandleBlobStorageContent()
  {
    // Arrange
    var mediaEntity = new MediaEntity
    {
      Id = "blob-id",
      AuthorId = "test-author",
      MediaType = "image",
      ContentType = "image/jpeg",
      Platform = "blob",
      Url = "https://cdn.example.com/image.jpg",
      ExternalUrl = string.Empty,
      UploadedAt = DateTime.UtcNow
    };

    // Act
    var dto = mediaEntity.ToGalleryDTO();

    // Assert
    Assert.Equal("blob", dto.Platform);
    Assert.Equal("Blob Storage", dto.PlatformDisplayName);
    Assert.Equal(mediaEntity.Url, dto.Url); // Should use regular URL for blob storage
    Assert.False(dto.IsExternal);
  }

  [Fact]
  public void ToGalleryDTO_ShouldHandleEmptyTags()
  {
    // Arrange
    var mediaEntity = new MediaEntity
    {
      Id = "test-id",
      AuthorId = "test-author",
      Tags = string.Empty,
      UploadedAt = DateTime.UtcNow
    };

    // Act
    var dto = mediaEntity.ToGalleryDTO();

    // Assert
    Assert.Empty(dto.Tags);
  }

  [Fact]
  public void ToGalleryDTOs_ShouldMapCollectionCorrectly()
  {
    // Arrange
    var entities = new[]
    {
      new MediaEntity { Id = "1", AuthorId = "author1", MediaType = "image", UploadedAt = DateTime.UtcNow },
      new MediaEntity { Id = "2", AuthorId = "author2", MediaType = "video", UploadedAt = DateTime.UtcNow.AddDays(-1) }
    };

    // Act
    var dtos = entities.ToGalleryDTOs().ToList();

    // Assert
    Assert.Equal(2, dtos.Count);
    Assert.Equal("1", dtos[0].Id);
    Assert.Equal("2", dtos[1].Id);
  }
}