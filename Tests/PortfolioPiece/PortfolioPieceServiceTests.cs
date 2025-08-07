using Functions.PortfolioPiece.Models;
using Functions.PortfolioPiece.Services;
using SharedStorage.Services.BaseServices;
using SharedStorage.Services.ContentServices;
using SharedStorage.Services.Media;
using SharedStorage.Models;
using Utils;
using Moq;
using Xunit;
using Azure.Data.Tables;
using Azure;

namespace Tests.PortfolioPiece;

/// <summary>
/// Unit tests for PortfolioPieceService CRUD operations
/// </summary>
public class PortfolioPieceServiceTests
{
    private readonly Mock<ITableStorageService> _mockTableStorageService;
    private readonly Mock<IMediaService> _mockMediaService;
    private readonly Mock<IAppInsightsLogger<ContentService<PortfolioPieceEntity, PortfolioPieceModel, PortfolioPieceDTO>>> _mockLogger;
    private readonly PortfolioPieceService _portfolioPieceService;
    private readonly string _tableName = "mocktestportfoliopieces";

    public PortfolioPieceServiceTests()
    {
        // Setup environment variables for testing
        Environment.SetEnvironmentVariable("USE_MOCK_STORAGE", "true");
        Environment.SetEnvironmentVariable("PORTFOLIOPIECES_TABLE_NAME", "testportfoliopieces");

        _mockTableStorageService = new Mock<ITableStorageService>();
        _mockMediaService = new Mock<IMediaService>();
        _mockLogger = new Mock<IAppInsightsLogger<ContentService<PortfolioPieceEntity, PortfolioPieceModel, PortfolioPieceDTO>>>();

        _portfolioPieceService = new PortfolioPieceService(_mockTableStorageService.Object, _mockMediaService.Object, _mockLogger.Object);
    }

    #region GET Tests

    [Fact]
    public async Task GetPieceAsync_ExistingSlug_ReturnsPiece()
    {
        // Arrange
        var slug = "test-piece";
        var portfolioPieceEntity = CreateSamplePortfolioPieceEntity(slug);
        
        _mockTableStorageService.Setup(x => x.GetEntityAsync<PortfolioPieceEntity>(
            It.IsAny<string>(), slug, "piece"))
            .ReturnsAsync(portfolioPieceEntity);

        // Act
        var result = await _portfolioPieceService.GetPieceAsync(slug);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(slug, result.Slug);
        Assert.Equal(portfolioPieceEntity.Title, result.Title);
        Assert.Equal(portfolioPieceEntity.AuthorSlug, result.AuthorSlug);
    }

    [Fact]
    public async Task GetPieceAsync_NonExistingSlug_ReturnsNull()
    {
        // Arrange
        var slug = "non-existing-piece";
        
        _mockTableStorageService.Setup(x => x.GetEntityAsync<PortfolioPieceEntity>(
            It.IsAny<string>(), slug, "piece"))
            .ReturnsAsync((PortfolioPieceEntity?)null);

        // Act
        var result = await _portfolioPieceService.GetPieceAsync(slug);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPiecesAsync_WithNoFilters_ReturnsAllPublishedPieces()
    {
        // Arrange
        var entities = new List<PortfolioPieceEntity>
        {
            CreateSamplePortfolioPieceEntity("piece1", isPublished: true),
            CreateSamplePortfolioPieceEntity("piece2", isPublished: true)
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
        var pieces = await _portfolioPieceService.GetPiecesAsync();

        // Assert
        Assert.NotNull(pieces);
        Assert.Equal(2, pieces.Count());
        Assert.All(pieces, piece => Assert.Equal("Published", piece.Status));
    }

    [Fact]
    public async Task GetPiecesAsync_WithAuthorFilter_ReturnsFilteredPieces()
    {
        // Arrange
        var authorSlug = "test-author";
        var entities = new List<PortfolioPieceEntity>
        {
            CreateSamplePortfolioPieceEntity("piece1", authorSlug: authorSlug, isPublished: true),
            CreateSamplePortfolioPieceEntity("piece2", authorSlug: "other-author", isPublished: true)
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
        var pieces = await _portfolioPieceService.GetPiecesAsync(authorSlug: authorSlug);

        // Assert
        Assert.NotNull(pieces);
        Assert.Single(pieces);
        Assert.All(pieces, piece => Assert.Equal(authorSlug, piece.AuthorSlug));
    }

    [Fact]
    public async Task GetPiecesAsync_WithCategoryFilter_ReturnsFilteredPieces()
    {
        // Arrange
        var category = "Web Development";
        var entities = new List<PortfolioPieceEntity>
        {
            CreateSamplePortfolioPieceEntity("piece1", category: category, isPublished: true),
            CreateSamplePortfolioPieceEntity("piece2", category: "Mobile Apps", isPublished: true)
        };

        var filteredEntities = entities.Where(e => e.Category == category).ToList();
        var tableEntities = filteredEntities.Select(e => ConvertToTableEntity(e)).ToList();
        var result = new TablePageResult(
            Entities: tableEntities,
            ContinuationToken: null,
            TotalCount: tableEntities.Count,
            HasMore: false
        );

        _mockTableStorageService.Setup(x => x.GetEntitiesAsync(
            It.IsAny<string>(), $"Category eq '{category}' and IsPublished eq true", It.IsAny<int>(), null))
            .ReturnsAsync(result);

        // Act
        var pieces = await _portfolioPieceService.GetPiecesAsync(category: category);

        // Assert
        Assert.NotNull(pieces);
        Assert.Single(pieces);
        Assert.All(pieces, piece => Assert.Equal(category, piece.Category));
    }

    #endregion

    #region UPSERT Tests

    [Fact]
    public async Task UpsertPieceAsync_NewPiece_CreatesPiece()
    {
        // Arrange
        var slug = "new-piece";
        var model = CreateSamplePortfolioPieceModel(slug);

        _mockTableStorageService.Setup(x => x.GetEntityAsync<PortfolioPieceEntity>(
            It.IsAny<string>(), slug, "piece"))
            .ReturnsAsync((PortfolioPieceEntity?)null); // Piece doesn't exist

        _mockTableStorageService.Setup(x => x.UpsertEntityAsync(
            It.IsAny<string>(), It.IsAny<PortfolioPieceEntity>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _portfolioPieceService.UpsertPieceAsync(slug, model);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(slug, result.Slug);
        Assert.Equal(model.Title, result.Title);
        
        _mockTableStorageService.Verify(x => x.UpsertEntityAsync(
            It.IsAny<string>(), It.IsAny<PortfolioPieceEntity>()), Times.Once);
    }

    [Fact]
    public async Task UpsertPieceAsync_ExistingPiece_UpdatesPiece()
    {
        // Arrange
        var slug = "existing-piece";
        var model = CreateSamplePortfolioPieceModel(slug);
        model.Title = "Updated Title";
        
        var existingEntity = CreateSamplePortfolioPieceEntity(slug);
        existingEntity.Title = "Original Title";

        _mockTableStorageService.Setup(x => x.GetEntityAsync<PortfolioPieceEntity>(
            It.IsAny<string>(), slug, "piece"))
            .ReturnsAsync(existingEntity);

        _mockTableStorageService.Setup(x => x.UpsertEntityAsync(
            It.IsAny<string>(), It.IsAny<PortfolioPieceEntity>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _portfolioPieceService.UpsertPieceAsync(slug, model);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Title", result.Title);
        
        _mockTableStorageService.Verify(x => x.UpsertEntityAsync(
            It.IsAny<string>(), It.IsAny<PortfolioPieceEntity>()), Times.Once);
    }

    [Fact]
    public async Task UpsertPieceAsync_InvalidModel_ThrowsException()
    {
        // Arrange
        var slug = "test-piece";
        var model = CreateSamplePortfolioPieceModel(slug);
        model.Title = ""; // Invalid - empty title

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _portfolioPieceService.UpsertPieceAsync(slug, model));
    }

    #endregion

    #region DELETE Tests

    [Fact]
    public async Task DeletePieceAsync_ExistingPiece_DeletesSuccessfully()
    {
        // Arrange
        var slug = "test-piece";
        var entity = CreateSamplePortfolioPieceEntity(slug);

        _mockTableStorageService.Setup(x => x.GetEntityAsync<PortfolioPieceEntity>(
            It.IsAny<string>(), slug, "piece"))
            .ReturnsAsync(entity);

        _mockTableStorageService.Setup(x => x.DeleteEntityAsync(
            It.IsAny<string>(), slug, "piece"))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _portfolioPieceService.DeletePieceAsync(slug);

        // Assert
        Assert.True(result);
        _mockTableStorageService.Verify(x => x.DeleteEntityAsync(
            It.IsAny<string>(), slug, "piece"), Times.Once);
    }

    [Fact]
    public async Task DeletePieceAsync_NonExistingPiece_ReturnsFalse()
    {
        // Arrange
        var slug = "non-existing-piece";

        _mockTableStorageService.Setup(x => x.GetEntityAsync<PortfolioPieceEntity>(
            It.IsAny<string>(), slug, "piece"))
            .ReturnsAsync((PortfolioPieceEntity?)null);

        // Act
        var result = await _portfolioPieceService.DeletePieceAsync(slug);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Media Operations Tests

    [Fact]
    public async Task SetFeaturedImageAsync_ValidMediaId_UpdatesFeaturedImage()
    {
        // Arrange
        var slug = "test-piece";
        var mediaId = "test-image-id";
        var entity = CreateSamplePortfolioPieceEntity(slug);
        var mediaEntity = new MediaEntity { Id = mediaId, MediaType = "image" };

        _mockMediaService.Setup(x => x.GetMediaAsync(mediaId))
            .ReturnsAsync(mediaEntity);

        _mockTableStorageService.Setup(x => x.GetEntityAsync<PortfolioPieceEntity>(
            It.IsAny<string>(), slug, "piece"))
            .ReturnsAsync(entity);

        // Mock the upsert operation that will be called internally
        _mockTableStorageService.Setup(x => x.UpsertEntityAsync(
            It.IsAny<string>(), It.IsAny<PortfolioPieceEntity>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _portfolioPieceService.SetFeaturedImageAsync(slug, mediaId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(mediaId, result.FeaturedImageId);
        _mockMediaService.Verify(x => x.GetMediaAsync(mediaId), Times.Once);
    }

    [Fact]
    public async Task SetFeaturedVideoAsync_ValidMediaId_UpdatesFeaturedVideo()
    {
        // Arrange
        var slug = "test-piece";
        var mediaId = "test-video-id";
        var entity = CreateSamplePortfolioPieceEntity(slug);
        var mediaEntity = new MediaEntity { Id = mediaId, MediaType = "video" };

        _mockMediaService.Setup(x => x.GetMediaAsync(mediaId))
            .ReturnsAsync(mediaEntity);

        _mockTableStorageService.Setup(x => x.GetEntityAsync<PortfolioPieceEntity>(
            It.IsAny<string>(), slug, "piece"))
            .ReturnsAsync(entity);

        // Mock the upsert operation that will be called internally
        _mockTableStorageService.Setup(x => x.UpsertEntityAsync(
            It.IsAny<string>(), It.IsAny<PortfolioPieceEntity>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _portfolioPieceService.SetFeaturedVideoAsync(slug, mediaId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(mediaId, result.FeaturedVideoId);
        _mockMediaService.Verify(x => x.GetMediaAsync(mediaId), Times.Once);
    }

    [Fact]
    public async Task AddMediaReferenceAsync_ValidMediaId_AddsReference()
    {
        // Arrange
        var slug = "test-piece";
        var mediaId = "test-media-id";
        var entity = CreateSamplePortfolioPieceEntity(slug);
        entity.MediaReferencesJson = "[]";
        
        var mediaEntity = new MediaEntity { Id = mediaId, MediaType = "image" };

        _mockMediaService.Setup(x => x.GetMediaAsync(mediaId))
            .ReturnsAsync(mediaEntity);

        _mockTableStorageService.Setup(x => x.GetEntityAsync<PortfolioPieceEntity>(
            It.IsAny<string>(), slug, "piece"))
            .ReturnsAsync(entity);

        // Mock the upsert operation that will be called internally
        _mockTableStorageService.Setup(x => x.UpsertEntityAsync(
            It.IsAny<string>(), It.IsAny<PortfolioPieceEntity>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _portfolioPieceService.AddMediaReferenceAsync(slug, mediaId);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(mediaId, result.MediaReferencesJson);
        _mockMediaService.Verify(x => x.GetMediaAsync(mediaId), Times.Once);
    }

    [Fact]
    public async Task RemoveMediaReferenceAsync_ExistingMediaId_RemovesReference()
    {
        // Arrange
        var slug = "test-piece";
        var mediaId = "test-media-id";
        var entity = CreateSamplePortfolioPieceEntity(slug);
        entity.MediaReferencesJson = $"[\"{mediaId}\"]";
        entity.FeaturedImageId = mediaId; // Also set as featured image
        
        _mockTableStorageService.Setup(x => x.GetEntityAsync<PortfolioPieceEntity>(
            It.IsAny<string>(), slug, "piece"))
            .ReturnsAsync(entity);

        // Mock the upsert operation that will be called internally
        _mockTableStorageService.Setup(x => x.UpsertEntityAsync(
            It.IsAny<string>(), It.IsAny<PortfolioPieceEntity>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _portfolioPieceService.RemoveMediaReferenceAsync(slug, mediaId);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain(mediaId, result.MediaReferencesJson);
        Assert.Null(result.FeaturedImageId); // Should be cleared since it was referencing the removed media
    }

    #endregion

    #region Helper Methods

    private static PortfolioPieceEntity CreateSamplePortfolioPieceEntity(string slug, string authorSlug = "test-author", string category = "Test Category", bool isPublished = true)
    {
        return new PortfolioPieceEntity
        {
            Id = Guid.NewGuid().ToString(),
            PartitionKey = slug,
            RowKey = "piece",
            Slug = slug,
            Title = $"Test Piece {slug}",
            Content = "Test content for portfolio piece",
            AuthorSlug = authorSlug,
            Category = category,
            Status = isPublished ? "Published" : "Draft",
            PublishDate = DateTime.UtcNow,
            LastModified = DateTime.UtcNow,
            TagsJson = "[\"tag1\", \"tag2\"]",
            Description = "Test description for portfolio piece"
        };
    }

    private static PortfolioPieceModel CreateSamplePortfolioPieceModel(string slug, string authorSlug = "test-author", string category = "Test Category", bool isPublished = true)
    {
        return new PortfolioPieceModel
        {
            Id = Guid.NewGuid().ToString(),
            Slug = slug,
            Title = $"Test Piece {slug}",
            Content = "Test content for portfolio piece",
            AuthorSlug = authorSlug,
            Category = category,
            Status = isPublished ? "Published" : "Draft",
            PublishDate = DateTime.UtcNow,
            TagsList = new[] { "tag1", "tag2" },
            Description = "Test description for portfolio piece"
        };
    }

    private static TableEntity ConvertToTableEntity(PortfolioPieceEntity entity)
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