using Xunit;
using SharedStorage.Models;
using SharedStorage.Extensions;

namespace Tests.Media
{
    public class MediaCdnUrlTests
    {
        [Fact]
        public void MediaItem_ShouldGenerateCorrectCdnUrl_ForImage()
        {
            // Arrange
            var mediaItem = new MediaItemModel
            {
                Id = "test-image-id",
                ContentType = "image/jpeg",
                FileName = "test-image.jpg",
                ContainerName = "media-images"
            };

            // Act
            var cdnUrl = mediaItem.GenerateCdnUrl();

            // Assert
            Assert.NotNull(cdnUrl);
            Assert.Contains("test-image", cdnUrl);
            Assert.Contains(".jpg", cdnUrl);
        }

        [Fact]
        public void MediaItem_ShouldGenerateCorrectCdnUrl_ForVideo()
        {
            // Arrange
            var mediaItem = new MediaItemModel
            {
                Id = "test-video-id",
                ContentType = "video/mp4",
                FileName = "test-video.mp4",
                ContainerName = "media-videos"
            };

            // Act
            var cdnUrl = mediaItem.GenerateCdnUrl();

            // Assert
            Assert.NotNull(cdnUrl);
            Assert.Contains("test-video", cdnUrl);
            Assert.Contains(".mp4", cdnUrl);
        }

        [Fact]
        public void MediaItem_ShouldGenerateCorrectThumbnailUrl()
        {
            // Arrange
            var mediaItem = new MediaItemModel
            {
                Id = "test-thumbnail-id",
                ContentType = "image/jpeg",
                FileName = "test-thumbnail.jpg",
                ContainerName = "media-thumbnails"
            };

            // Act
            var thumbnailUrl = mediaItem.GenerateThumbnailUrl();

            // Assert
            Assert.NotNull(thumbnailUrl);
            Assert.Contains("test-thumbnail", thumbnailUrl);
            Assert.Contains(".jpg", thumbnailUrl);
        }

        [Fact]
        public void MediaItem_ShouldHandleNullValues()
        {
            // Arrange
            var mediaItem = new MediaItemModel
            {
                Id = null,
                ContentType = null,
                FileName = null,
                ContainerName = null
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => mediaItem.GenerateCdnUrl());
        }
    }
}