using Xunit;
using SharedStorage.Models;

namespace Tests.Media;

/// <summary>
/// Basic unit tests for Media models and functionality
/// </summary>
public class MediaModelTests
{
    [Fact]
    public void MediaItemModel_CanBeCreatedWithExternalPlatformData()
    {
        // Arrange & Act
        var mediaItem = new MediaItemModel
        {
            Id = "test-123",
            Platform = "Instagram",
            IsExternal = true,
            ExternalId = "instagram_abc123",
            ExternalUrl = "https://instagram.com/p/abc123",
            MediaType = "image",
            Description = "Test Instagram post"
        };

        // Assert
        Assert.NotNull(mediaItem);
        Assert.Equal("test-123", mediaItem.Id);
        Assert.Equal("Instagram", mediaItem.Platform);
        Assert.True(mediaItem.IsExternal);
        Assert.Equal("instagram_abc123", mediaItem.ExternalId);
        Assert.Equal("https://instagram.com/p/abc123", mediaItem.ExternalUrl);
        Assert.Equal("image", mediaItem.MediaType);
        Assert.Equal("Test Instagram post", mediaItem.Description);
    }

    [Fact]
    public void MediaItemModel_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var mediaItem = new MediaItemModel();

        // Assert
        Assert.False(mediaItem.IsExternal);
        Assert.Equal(string.Empty, mediaItem.Platform);
        Assert.Equal(string.Empty, mediaItem.ExternalId);
        Assert.Equal(string.Empty, mediaItem.ExternalUrl);
        Assert.Equal(string.Empty, mediaItem.MediaType);
    }

    [Theory]
    [InlineData("TikTok", "video")]
    [InlineData("Instagram", "image")]
    [InlineData("YouTube", "video")]
    [InlineData("Facebook", "image")]
    [InlineData("LinkedIn", "image")]
    [InlineData("Pinterest", "image")]
    [InlineData("BlobStorage", "image")]
    public void MediaItemModel_ValidPlatformAndMediaTypeCombinations(string platform, string mediaType)
    {
        // Arrange & Act
        var mediaItem = new MediaItemModel
        {
            Platform = platform,
            MediaType = mediaType,
            IsExternal = platform != "BlobStorage"
        };

        // Assert
        Assert.Equal(platform, mediaItem.Platform);
        Assert.Equal(mediaType, mediaItem.MediaType);
        Assert.Equal(platform != "BlobStorage", mediaItem.IsExternal);
    }

    [Fact]
    public void MediaEntity_CanBeCreatedWithExternalPlatformData()
    {
        // Arrange & Act
        var mediaEntity = new MediaEntity
        {
            Id = "test-456",
            Platform = "YouTube",
            IsExternal = true,
            ExternalId = "youtube_xyz789",
            ExternalUrl = "https://youtube.com/watch?v=xyz789",
            MediaType = "video",
            Description = "Test YouTube video"
        };

        // Assert
        Assert.NotNull(mediaEntity);
        Assert.Equal("test-456", mediaEntity.Id);
        Assert.Equal("YouTube", mediaEntity.Platform);
        Assert.True(mediaEntity.IsExternal);
        Assert.Equal("youtube_xyz789", mediaEntity.ExternalId);
        Assert.Equal("https://youtube.com/watch?v=xyz789", mediaEntity.ExternalUrl);
        Assert.Equal("video", mediaEntity.MediaType);
        Assert.Equal("Test YouTube video", mediaEntity.Description);
    }

    [Fact]
    public void MediaItemDTO_CanBeCreatedWithExternalPlatformData()
    {
        // Arrange & Act
        var mediaDto = new MediaItemDTO
        {
            Id = "test-789",
            Platform = "TikTok",
            IsExternal = true,
            ExternalId = "tiktok_def456",
            ExternalUrl = "https://tiktok.com/@user/video/def456",
            MediaType = "video",
            Description = "Test TikTok video"
        };

        // Assert
        Assert.NotNull(mediaDto);
        Assert.Equal("test-789", mediaDto.Id);
        Assert.Equal("TikTok", mediaDto.Platform);
        Assert.True(mediaDto.IsExternal);
        Assert.Equal("tiktok_def456", mediaDto.ExternalId);
        Assert.Equal("https://tiktok.com/@user/video/def456", mediaDto.ExternalUrl);
        Assert.Equal("video", mediaDto.MediaType);
        Assert.Equal("Test TikTok video", mediaDto.Description);
    }
}