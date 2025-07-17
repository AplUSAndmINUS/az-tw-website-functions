using Xunit;
using SharedStorage.Models;
using SharedStorage.Services.Media.Platforms;
using Utils;
using Moq;

namespace Tests.MediaGallery;

public class PlatformAdapterTests
{
  [Fact]
  public async Task TikTokPlatformAdapter_ShouldReturnMockData()
  {
    // Arrange
    var mockLogger = new Mock<IAppInsightsLogger<TikTokPlatformAdapter>>();
    var adapter = new TikTokPlatformAdapter(mockLogger.Object);

    // Act
    var result = await adapter.FetchRecentMediaAsync("test-author", 3);

    // Assert
    Assert.NotNull(result);
    var mediaList = result.ToList();
    Assert.True(mediaList.Count <= 3);
    Assert.All(mediaList, media => 
    {
      Assert.Equal("tiktok", media.Platform);
      Assert.Equal("test-author", media.AuthorId);
      Assert.Equal("video", media.MediaType);
      Assert.Contains("tiktok", media.ExternalUrl);
      Assert.True(media.LikeCount > 0);
      Assert.True(media.ViewCount > 0);
    });
  }

  [Fact]
  public async Task InstagramPlatformAdapter_ShouldReturnMockData()
  {
    // Arrange
    var mockLogger = new Mock<IAppInsightsLogger<InstagramPlatformAdapter>>();
    var adapter = new InstagramPlatformAdapter(mockLogger.Object);

    // Act
    var result = await adapter.FetchRecentMediaAsync("test-author", 5);

    // Assert
    Assert.NotNull(result);
    var mediaList = result.ToList();
    Assert.True(mediaList.Count <= 5);
    Assert.All(mediaList, media => 
    {
      Assert.Equal("instagram", media.Platform);
      Assert.Equal("test-author", media.AuthorId);
      Assert.Contains("instagram", media.ExternalUrl);
      Assert.True(media.LikeCount >= 0);
    });
  }

  [Fact]
  public async Task YouTubePlatformAdapter_ShouldReturnMockData()
  {
    // Arrange
    var mockLogger = new Mock<IAppInsightsLogger<YouTubePlatformAdapter>>();
    var adapter = new YouTubePlatformAdapter(mockLogger.Object);

    // Act
    var result = await adapter.FetchRecentMediaAsync("test-author", 4);

    // Assert
    Assert.NotNull(result);
    var mediaList = result.ToList();
    Assert.True(mediaList.Count <= 4);
    Assert.All(mediaList, media => 
    {
      Assert.Equal("youtube", media.Platform);
      Assert.Equal("test-author", media.AuthorId);
      Assert.Equal("video", media.MediaType);
      Assert.Contains("youtube", media.ExternalUrl);
      Assert.True(media.LikeCount >= 0);
      Assert.True(media.ViewCount >= 0);
    });
  }

  [Fact]
  public async Task PlatformAdapter_ValidateConnectionAsync_ShouldReturnTrue()
  {
    // Arrange
    var mockLogger = new Mock<IAppInsightsLogger<FacebookPlatformAdapter>>();
    var adapter = new FacebookPlatformAdapter(mockLogger.Object);

    // Act
    var result = await adapter.ValidateConnectionAsync();

    // Assert
    Assert.True(result);
  }

  [Fact]
  public async Task PlatformAdapter_FetchMediaByExternalIdAsync_ShouldReturnSpecificMedia()
  {
    // Arrange
    var mockLogger = new Mock<IAppInsightsLogger<LinkedInPlatformAdapter>>();
    var adapter = new LinkedInPlatformAdapter(mockLogger.Object);

    // Act
    var result = await adapter.FetchMediaByExternalIdAsync("test-external-id", "test-author");

    // Assert
    Assert.NotNull(result);
    Assert.Equal("linkedin", result.Platform);
    Assert.Equal("test-author", result.AuthorId);
    Assert.Equal("test-external-id", result.ExternalId);
    Assert.Contains("linkedin", result.ExternalUrl);
  }

  [Fact]
  public async Task PinterestPlatformAdapter_ShouldReturnMockData()
  {
    // Arrange
    var mockLogger = new Mock<IAppInsightsLogger<PinterestPlatformAdapter>>();
    var adapter = new PinterestPlatformAdapter(mockLogger.Object);

    // Act
    var result = await adapter.FetchRecentMediaAsync("test-author", 10);

    // Assert
    Assert.NotNull(result);
    var mediaList = result.ToList();
    Assert.True(mediaList.Count <= 10);
    Assert.All(mediaList, media => 
    {
      Assert.Equal("pinterest", media.Platform);
      Assert.Equal("test-author", media.AuthorId);
      Assert.Equal("image", media.MediaType); // Pinterest is primarily images
      Assert.Contains("pinterest", media.ExternalUrl);
      Assert.True(media.Width > 0);
      Assert.True(media.Height > 0);
    });
  }
}