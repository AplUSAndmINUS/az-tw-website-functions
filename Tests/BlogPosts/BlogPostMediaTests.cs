using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Functions.BlogPosts.Services;
using Functions.BlogPosts.Models;
using SharedStorage.Services.BaseServices;
using SharedStorage.Services.Media;
using SharedStorage.Models;
using Moq;
using Xunit;
using Utils;
using Microsoft.Extensions.Logging;

namespace Tests.BlogPosts
{
    public class BlogPostMediaTests
    {
        private readonly Mock<ITableStorageService> _mockTableStorage;
        private readonly Mock<IMediaService> _mockMediaService;
        private readonly Mock<IAppInsightsLogger<ContentService<BlogPostEntity, BlogPostModel, BlogPostDTO>>> _mockLogger;
        private readonly BlogPostService _blogPostService;
        private readonly string _testBlogSlug = "test-blog-post";
        private readonly string _testImageId = "test-image-id";
        private readonly string _testVideoId = "test-video-id";
        private readonly string _testMediaId = "test-media-id";

        public BlogPostMediaTests()
        {
            _mockTableStorage = new Mock<ITableStorageService>();
            _mockMediaService = new Mock<IMediaService>();
            _mockLogger = new Mock<IAppInsightsLogger<ContentService<BlogPostEntity, BlogPostModel, BlogPostDTO>>>();

            // Setup the blog post service with mocked dependencies
            _blogPostService = new BlogPostService(
                _mockTableStorage.Object,
                _mockMediaService.Object,
                _mockLogger.Object
            );

            // Setup mock blog post entity
            var blogPostEntity = new BlogPostEntity
            {
                PartitionKey = _testBlogSlug,
                RowKey = "post",
                Title = "Test Blog Post",
                Slug = _testBlogSlug,
                Content = "Test content",
                AuthorSlug = "test-author",
                Category = "Test",
                Status = "Published",
                PublishDate = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                FeaturedImageId = "",
                FeaturedVideoId = "",
                FeaturedMediaId = "",
                MediaReferencesJson = "[]"
            };

            // Setup mock image media entity
            var imageMedia = new MediaEntity
            {
                Id = _testImageId,
                MediaType = "image",
                FileName = "test-image.jpg",
                FileExtension = ".jpg",
                MimeType = "image/jpeg",
                Size = 1024,
                Url = "https://example.com/test-image.jpg",
                ThumbnailUrl = "https://example.com/test-image-thumb.jpg",
                UploadDate = DateTime.UtcNow
            };

            // Setup mock video media entity
            var videoMedia = new MediaEntity
            {
                Id = _testVideoId,
                MediaType = "video",
                FileName = "test-video.mp4",
                FileExtension = ".mp4",
                MimeType = "video/mp4",
                Size = 10240,
                Url = "https://example.com/test-video.mp4",
                ThumbnailUrl = "https://example.com/test-video-thumb.jpg",
                UploadDate = DateTime.UtcNow
            };

            // Setup mock generic media entity
            var genericMedia = new MediaEntity
            {
                Id = _testMediaId,
                MediaType = "document",
                FileName = "test-document.pdf",
                FileExtension = ".pdf",
                MimeType = "application/pdf",
                Size = 2048,
                Url = "https://example.com/test-document.pdf",
                ThumbnailUrl = null,
                UploadDate = DateTime.UtcNow
            };

            // Setup mock table storage to return the blog post entity
            _mockTableStorage
                .Setup(m => m.GetEntityAsync<BlogPostEntity>(It.IsAny<string>(), _testBlogSlug, "post"))
                .ReturnsAsync(blogPostEntity);

            // Setup mock media service to return media entities
            _mockMediaService
                .Setup(m => m.GetMediaAsync(_testImageId))
                .ReturnsAsync(imageMedia);

            _mockMediaService
                .Setup(m => m.GetMediaAsync(_testVideoId))
                .ReturnsAsync(videoMedia);

            _mockMediaService
                .Setup(m => m.GetMediaAsync(_testMediaId))
                .ReturnsAsync(genericMedia);
        }

        [Fact]
        public async Task SetFeaturedImageAsync_ValidImageId_ShouldUpdateBlogPost()
        {
            // Arrange
            _mockTableStorage
                .Setup(m => m.UpsertEntityAsync(It.IsAny<string>(), It.IsAny<BlogPostEntity>()))
                .Returns(Task.CompletedTask);

            // Also setup for metadata table updates
            _mockTableStorage
                .Setup(m => m.UpsertEntityAsync(It.IsAny<string>(), It.IsAny<Azure.Data.Tables.TableEntity>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _blogPostService.SetFeaturedImageAsync(_testBlogSlug, _testImageId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(_testImageId, result.FeaturedImageId);

            // Verify that table storage was called to update the entity
            _mockTableStorage.Verify(
                m => m.UpsertEntityAsync(
                    It.IsAny<string>(),
                    It.Is<BlogPostEntity>(e => e.FeaturedImageId == _testImageId)
                ),
                Times.Once
            );

            // Verify that metadata was updated
            _mockTableStorage.Verify(
                m => m.UpsertEntityAsync(
                    It.Is<string>(tableName => tableName.Contains("metadata")),
                    It.Is<Azure.Data.Tables.TableEntity>(e =>
                        e.PartitionKey == _testBlogSlug &&
                        e.RowKey == _testImageId &&
                        e.GetString("MediaType") == "image"
                    )
                ),
                Times.Once
            );
        }

        [Fact]
        public async Task SetFeaturedVideoAsync_ValidVideoId_ShouldUpdateBlogPost()
        {
            // Arrange
            _mockTableStorage
                .Setup(m => m.UpsertEntityAsync(It.IsAny<string>(), It.IsAny<BlogPostEntity>()))
                .Returns(Task.CompletedTask);

            // Also setup for metadata table updates
            _mockTableStorage
                .Setup(m => m.UpsertEntityAsync(It.IsAny<string>(), It.IsAny<Azure.Data.Tables.TableEntity>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _blogPostService.SetFeaturedVideoAsync(_testBlogSlug, _testVideoId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(_testVideoId, result.FeaturedVideoId);

            // Verify that table storage was called to update the entity
            _mockTableStorage.Verify(
                m => m.UpsertEntityAsync(
                    It.IsAny<string>(),
                    It.Is<BlogPostEntity>(e => e.FeaturedVideoId == _testVideoId)
                ),
                Times.Once
            );

            // Verify that metadata was updated
            _mockTableStorage.Verify(
                m => m.UpsertEntityAsync(
                    It.Is<string>(tableName => tableName.Contains("metadata")),
                    It.Is<Azure.Data.Tables.TableEntity>(e =>
                        e.PartitionKey == _testBlogSlug &&
                        e.RowKey == _testVideoId &&
                        e.GetString("MediaType") == "video"
                    )
                ),
                Times.Once
            );
        }

        [Fact]
        public async Task SetFeaturedVideoAsync_NonVideoMediaId_ShouldReturnNull()
        {
            // Act
            var result = await _blogPostService.SetFeaturedVideoAsync(_testBlogSlug, _testImageId);

            // Assert
            Assert.Null(result);

            // Verify that table storage was NOT called to update the entity
            _mockTableStorage.Verify(
                m => m.UpsertEntityAsync(
                    It.IsAny<string>(),
                    It.IsAny<BlogPostEntity>()
                ),
                Times.Never
            );
        }

        [Fact]
        public async Task UpsertPostAsync_WithFeaturedVideoId_ShouldUpdateMetadataTables()
        {
            // Arrange
            var model = new BlogPostModel
            {
                Slug = _testBlogSlug,
                Title = "Updated Blog Post",
                Content = "Updated content",
                AuthorSlug = "test-author",
                Category = "Test",
                Status = "Published",
                PublishDate = DateTime.UtcNow,
                FeaturedImageId = _testImageId,
                FeaturedVideoId = _testVideoId,
                FeaturedMediaId = _testMediaId,
                MediaReferencesJson = "[]"
            };

            _mockTableStorage
                .Setup(m => m.UpsertEntityAsync(It.IsAny<string>(), It.IsAny<BlogPostEntity>()))
                .Returns(Task.CompletedTask);

            // Also setup for metadata table updates
            _mockTableStorage
                .Setup(m => m.UpsertEntityAsync(It.IsAny<string>(), It.IsAny<Azure.Data.Tables.TableEntity>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _blogPostService.UpsertPostAsync(_testBlogSlug, model);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(_testVideoId, result.FeaturedVideoId);

            // Verify that metadata was updated for video
            _mockTableStorage.Verify(
                m => m.UpsertEntityAsync(
                    It.Is<string>(tableName => tableName.Contains("videometadata")),
                    It.Is<Azure.Data.Tables.TableEntity>(e =>
                        e.PartitionKey == _testBlogSlug &&
                        e.RowKey == _testVideoId
                    )
                ),
                Times.Once
            );
        }
    }
}
