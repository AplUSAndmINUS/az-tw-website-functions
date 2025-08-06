using Functions.BlogPosts.Models;
using Functions.BlogPosts.Services;
using SharedStorage.Services.BaseServices;
using SharedStorage.Services.ContentServices;
using SharedStorage.Services.Media;
using SharedStorage.Models;
using Utils;
using Moq;
using Xunit;
using Azure.Data.Tables;
using Azure;

namespace Tests.BlogPosts;

/// <summary>
/// Unit tests for BlogPostService CRUD operations
/// </summary>
public class BlogPostServiceTests
{
    private readonly Mock<ITableStorageService> _mockTableStorageService;
    private readonly Mock<IMediaService> _mockMediaService;
    private readonly Mock<IAppInsightsLogger<ContentService<BlogPostEntity, BlogPostModel, BlogPostDTO>>> _mockLogger;
    private readonly BlogPostService _blogPostService;
    private readonly string _tableName = "mocktestblogposts";

    public BlogPostServiceTests()
    {
        // Setup environment variables for testing
        Environment.SetEnvironmentVariable("USE_MOCK_STORAGE", "true");
        Environment.SetEnvironmentVariable("BLOGPOSTS_TABLE_NAME", "testblogposts");

        _mockTableStorageService = new Mock<ITableStorageService>();
        _mockMediaService = new Mock<IMediaService>();
        _mockLogger = new Mock<IAppInsightsLogger<ContentService<BlogPostEntity, BlogPostModel, BlogPostDTO>>>();

        _blogPostService = new BlogPostService(_mockTableStorageService.Object, _mockMediaService.Object, _mockLogger.Object);
    }

    #region GET Tests

    [Fact]
    public async Task GetPostAsync_ExistingSlug_ReturnsPost()
    {
        // Arrange
        var slug = "test-post";
        var blogPostEntity = CreateSampleBlogPostEntity(slug);
        
        _mockTableStorageService.Setup(x => x.GetEntityAsync<BlogPostEntity>(
            It.IsAny<string>(), slug, "post"))
            .ReturnsAsync(blogPostEntity);

        // Act
        var result = await _blogPostService.GetPostAsync(slug);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(slug, result.Slug);
        Assert.Equal(blogPostEntity.Title, result.Title);
        Assert.Equal(blogPostEntity.AuthorSlug, result.AuthorSlug);
    }

    [Fact]
    public async Task GetPostAsync_NonExistingSlug_ReturnsNull()
    {
        // Arrange
        var slug = "non-existing-post";
        
        _mockTableStorageService.Setup(x => x.GetEntityAsync<BlogPostEntity>(
            It.IsAny<string>(), slug, "post"))
            .ReturnsAsync((BlogPostEntity?)null);

        // Act
        var result = await _blogPostService.GetPostAsync(slug);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPostsAsync_WithNoFilters_ReturnsAllPublishedPosts()
    {
        // Arrange
        var entities = new List<BlogPostEntity>
        {
            CreateSampleBlogPostEntity("post1", isPublished: true),
            CreateSampleBlogPostEntity("post2", isPublished: true)
        };

        var tableEntities = entities.Select(e => ConvertToTableEntity(e)).ToList();
        var result = new TablePageResult(
            Entities: tableEntities,
            ContinuationToken: null,
            TotalCount: tableEntities.Count,
            HasMore: false
        );

        _mockTableStorageService.Setup(x => x.GetEntitiesAsync(
            It.IsAny<string>(), "IsPublished eq true", It.IsAny<int>(), null))
            .ReturnsAsync(result);

        // Act
        var posts = await _blogPostService.GetPostsAsync();

        // Assert
        Assert.NotNull(posts);
        Assert.Equal(2, posts.Count());
        Assert.All(posts, post => Assert.Equal("Published", post.Status));
    }

    [Fact]
    public async Task GetPostsAsync_WithAuthorFilter_ReturnsFilteredPosts()
    {
        // Arrange
        var authorSlug = "test-author";
        var entities = new List<BlogPostEntity>
        {
            CreateSampleBlogPostEntity("post1", authorSlug: authorSlug, isPublished: true),
            CreateSampleBlogPostEntity("post2", authorSlug: "other-author", isPublished: true)
        };

        var filteredEntities = entities.Where(e => e.AuthorSlug == authorSlug).ToList();
        var tableEntities = filteredEntities.Select(e => ConvertToTableEntity(e)).ToList();
        var result = new TablePageResult(
            Entities: tableEntities,
            ContinuationToken: null,
            TotalCount: tableEntities.Count,
            HasMore: false
        );

        _mockTableStorageService.Setup(x => x.GetEntitiesAsync(
            It.IsAny<string>(), $"AuthorSlug eq '{authorSlug}' and IsPublished eq true", It.IsAny<int>(), null))
            .ReturnsAsync(result);

        // Act
        var posts = await _blogPostService.GetPostsAsync(authorSlug: authorSlug);

        // Assert
        Assert.NotNull(posts);
        Assert.Single(posts);
        Assert.All(posts, post => Assert.Equal(authorSlug, post.AuthorSlug));
    }

    #endregion

    #region UPSERT Tests

    [Fact]
    public async Task UpsertPostAsync_NewPost_CreatesPost()
    {
        // Arrange
        var slug = "new-post";
        var model = CreateSampleBlogPostModel(slug);

        _mockTableStorageService.Setup(x => x.GetEntityAsync<BlogPostEntity>(
            It.IsAny<string>(), slug, "post"))
            .ReturnsAsync((BlogPostEntity?)null); // Post doesn't exist

        _mockTableStorageService.Setup(x => x.UpsertEntityAsync(
            It.IsAny<string>(), It.IsAny<BlogPostEntity>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _blogPostService.UpsertPostAsync(slug, model);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(slug, result.Slug);
        Assert.Equal(model.Title, result.Title);
        
        _mockTableStorageService.Verify(x => x.UpsertEntityAsync(
            It.IsAny<string>(), It.IsAny<BlogPostEntity>()), Times.Once);
    }

    [Fact]
    public async Task UpsertPostAsync_ExistingPost_UpdatesPost()
    {
        // Arrange
        var slug = "existing-post";
        var model = CreateSampleBlogPostModel(slug);
        model.Title = "Updated Title";
        
        var existingEntity = CreateSampleBlogPostEntity(slug);
        existingEntity.Title = "Original Title";

        _mockTableStorageService.Setup(x => x.GetEntityAsync<BlogPostEntity>(
            It.IsAny<string>(), slug, "post"))
            .ReturnsAsync(existingEntity);

        _mockTableStorageService.Setup(x => x.UpsertEntityAsync(
            It.IsAny<string>(), It.IsAny<BlogPostEntity>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _blogPostService.UpsertPostAsync(slug, model);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Title", result.Title);
        
        _mockTableStorageService.Verify(x => x.UpsertEntityAsync(
            It.IsAny<string>(), It.IsAny<BlogPostEntity>()), Times.Once);
    }

    [Fact]
    public async Task UpsertPostAsync_InvalidModel_ThrowsException()
    {
        // Arrange
        var slug = "test-post";
        var model = CreateSampleBlogPostModel(slug);
        model.Title = ""; // Invalid - empty title

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _blogPostService.UpsertPostAsync(slug, model));
    }

    #endregion

    #region DELETE Tests

    [Fact]
    public async Task DeletePostAsync_ExistingPost_DeletesSuccessfully()
    {
        // Arrange
        var slug = "test-post";
        var entity = CreateSampleBlogPostEntity(slug);

        _mockTableStorageService.Setup(x => x.GetEntityAsync<BlogPostEntity>(
            It.IsAny<string>(), slug, "post"))
            .ReturnsAsync(entity);

        _mockTableStorageService.Setup(x => x.DeleteEntityAsync(
            It.IsAny<string>(), slug, "post"))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _blogPostService.DeletePostAsync(slug);

        // Assert
        Assert.True(result);
        _mockTableStorageService.Verify(x => x.DeleteEntityAsync(
            It.IsAny<string>(), slug, "post"), Times.Once);
    }

    [Fact]
    public async Task DeletePostAsync_NonExistingPost_ReturnsFalse()
    {
        // Arrange
        var slug = "non-existing-post";

        _mockTableStorageService.Setup(x => x.GetEntityAsync<BlogPostEntity>(
            It.IsAny<string>(), slug, "post"))
            .ReturnsAsync((BlogPostEntity?)null);

        // Act
        var result = await _blogPostService.DeletePostAsync(slug);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Media Operations Tests

    [Fact]
    public async Task SetFeaturedImageAsync_ValidMediaId_UpdatesFeaturedImage()
    {
        // Arrange
        var slug = "test-post";
        var mediaId = "test-image-id";
        var entity = CreateSampleBlogPostEntity(slug);
        var mediaEntity = new MediaEntity { Id = mediaId, MediaType = "image" };

        _mockMediaService.Setup(x => x.GetMediaAsync(mediaId))
            .ReturnsAsync(mediaEntity);

        _mockTableStorageService.Setup(x => x.GetEntityAsync<BlogPostEntity>(
            It.IsAny<string>(), slug, "post"))
            .ReturnsAsync(entity);

        entity.FeaturedImageId = mediaId;
        _mockTableStorageService.Setup(x => x.UpsertEntityAsync(
            It.IsAny<string>(), It.IsAny<BlogPostEntity>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _blogPostService.SetFeaturedImageAsync(slug, mediaId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(mediaId, result.FeaturedImageId);
        _mockMediaService.Verify(x => x.GetMediaAsync(mediaId), Times.Once);
        _mockTableStorageService.Verify(x => x.UpsertEntityAsync(
            It.IsAny<string>(), It.IsAny<BlogPostEntity>()), Times.Once);
    }

    [Fact]
    public async Task AddMediaReferenceAsync_ValidMediaId_AddsReference()
    {
        // Arrange
        var slug = "test-post";
        var mediaId = "test-media-id";
        var entity = CreateSampleBlogPostEntity(slug);
        entity.MediaReferencesJson = "[]";
        
        var mediaEntity = new MediaEntity { Id = mediaId, MediaType = "image" };

        _mockMediaService.Setup(x => x.GetMediaAsync(mediaId))
            .ReturnsAsync(mediaEntity);

        _mockTableStorageService.Setup(x => x.GetEntityAsync<BlogPostEntity>(
            It.IsAny<string>(), slug, "post"))
            .ReturnsAsync(entity);

        // Update entity to include the new media reference
        entity.MediaReferencesJson = $"[\"{mediaId}\"]";
        _mockTableStorageService.Setup(x => x.UpsertEntityAsync(
            It.IsAny<string>(), It.IsAny<BlogPostEntity>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _blogPostService.AddMediaReferenceAsync(slug, mediaId);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(mediaId, result.MediaReferencesJson);
        _mockMediaService.Verify(x => x.GetMediaAsync(mediaId), Times.Once);
        _mockTableStorageService.Verify(x => x.UpsertEntityAsync(
            It.IsAny<string>(), It.IsAny<BlogPostEntity>()), Times.Once);
    }

    #endregion

    #region Helper Methods

    private static BlogPostEntity CreateSampleBlogPostEntity(string slug, string authorSlug = "test-author", bool isPublished = true)
    {
        return new BlogPostEntity
        {
            Id = Guid.NewGuid().ToString(),
            PartitionKey = slug,
            RowKey = "post",
            Slug = slug,
            Title = $"Test Post {slug}",
            Content = "Test content",
            AuthorSlug = authorSlug,
            Category = "Test Category",
            Status = isPublished ? "Published" : "Draft",
            PublishDate = DateTime.UtcNow,
            LastModified = DateTime.UtcNow,
            TagsJson = "[\"tag1\", \"tag2\"]",
            Description = "Test description"
        };
    }

    private static BlogPostModel CreateSampleBlogPostModel(string slug, string authorSlug = "test-author", bool isPublished = true)
    {
        return new BlogPostModel
        {
            Id = Guid.NewGuid().ToString(),
            Slug = slug,
            Title = $"Test Post {slug}",
            Content = "Test content",
            AuthorSlug = authorSlug,
            Category = "Test Category",
            Status = isPublished ? "Published" : "Draft",
            PublishDate = DateTime.UtcNow,
            TagsList = new[] { "tag1", "tag2" },
            Description = "Test description"
        };
    }

    private static TableEntity ConvertToTableEntity(BlogPostEntity entity)
    {
        var tableEntity = new TableEntity(entity.PartitionKey, entity.RowKey)
        {
            ["Id"] = entity.Id,
            ["Slug"] = entity.Slug,
            ["Title"] = entity.Title,
            ["Content"] = entity.Content,
            ["AuthorSlug"] = entity.AuthorSlug,
            ["Category"] = entity.Category,
            ["Status"] = entity.Status,
            ["IsPublished"] = entity.IsPublished,
            ["PublishDate"] = entity.PublishDate,
            ["LastModified"] = entity.LastModified,
            ["TagsJson"] = entity.TagsJson,
            ["Description"] = entity.Description,
            ["FeaturedImageId"] = entity.FeaturedImageId,
            ["FeaturedMediaId"] = entity.FeaturedMediaId,
            ["FeaturedVideoId"] = entity.FeaturedVideoId,
            ["MediaReferencesJson"] = entity.MediaReferencesJson
        };
        
        return tableEntity;
    }

    #endregion
}