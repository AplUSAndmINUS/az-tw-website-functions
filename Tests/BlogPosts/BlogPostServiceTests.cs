using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Functions.BlogPosts.Services;
using Functions.BlogPosts.Models;
using SharedStorage.Services.ContentServices;
using SharedStorage.Services.Media;
using SharedStorage.Models;
using Utils;

namespace Tests.BlogPosts;

public class BlogPostServiceTests
{
  private readonly Mock<IContentService<BlogPostEntity, BlogPostModel, BlogPostDTO>> _mockContentService;
  private readonly Mock<IMediaService> _mockMediaService;
  private readonly Mock<IAppInsightsLogger<BlogPostService>> _mockLogger;
  private readonly BlogPostService _blogPostService;

  public BlogPostServiceTests()
  {
    _mockContentService = new Mock<IContentService<BlogPostEntity, BlogPostModel, BlogPostDTO>>();
    _mockMediaService = new Mock<IMediaService>();
    _mockLogger = new Mock<IAppInsightsLogger<BlogPostService>>();

    _blogPostService = new BlogPostService(_mockContentService.Object, _mockMediaService.Object, _mockLogger.Object);
  }

  [Fact]
  public async Task UpsertBlogPostAsync_ValidDto_ReturnsSuccess()
  {
    // Arrange
    var blogPostDto = new BlogPostDTO
    {
      Slug = "test-post",
      Title = "Test Post",
      Description = "Test Description",
      Content = "Test Content",
      Category = "Test",
      Tags = new List<string> { "test" },
      Published = true,
      PublishedDate = DateTime.UtcNow,
      Author = "test-author",
      MediaReferences = new List<string>()
    };

    var expectedModel = BlogPostMapper.ToModelStatic(blogPostDto);

    _mockContentService
        .Setup(x => x.UpsertAsync(It.IsAny<BlogPostDTO>()))
        .ReturnsAsync(new ServiceResult<BlogPostModel> { IsSuccess = true, Data = expectedModel });

    // Act
    var result = await _blogPostService.UpsertBlogPostAsync(blogPostDto);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Data);
    Assert.Equal(blogPostDto.Slug, result.Data.Slug);
    Assert.Equal(blogPostDto.Title, result.Data.Title);

    _mockContentService.Verify(x => x.UpsertAsync(It.IsAny<BlogPostDTO>()), Times.Once);
  }

  [Fact]
  public async Task UpsertBlogPostAsync_NullDto_ReturnsFailure()
  {
    // Act
    var result = await _blogPostService.UpsertBlogPostAsync(null);

    // Assert
    Assert.False(result.IsSuccess);
    Assert.Contains("BlogPost DTO cannot be null", result.ErrorMessage);

    _mockContentService.Verify(x => x.UpsertAsync(It.IsAny<BlogPostDTO>()), Times.Never);
  }

  [Fact]
  public async Task UpsertBlogPostAsync_EmptySlug_ReturnsFailure()
  {
    // Arrange
    var blogPostDto = new BlogPostDTO
    {
      Slug = "",
      Title = "Test Post",
      Description = "Test Description",
      Content = "Test Content"
    };

    // Act
    var result = await _blogPostService.UpsertBlogPostAsync(blogPostDto);

    // Assert
    Assert.False(result.IsSuccess);
    Assert.Contains("Slug cannot be null or empty", result.ErrorMessage);

    _mockContentService.Verify(x => x.UpsertAsync(It.IsAny<BlogPostDTO>()), Times.Never);
  }

  [Fact]
  public async Task GetBlogPostAsync_ValidSlug_ReturnsSuccess()
  {
    // Arrange
    var slug = "test-post";
    var expectedModel = new BlogPostModel
    {
      Slug = slug,
      Title = "Test Post",
      Description = "Test Description",
      Content = "Test Content",
      Category = "Test",
      Tags = new List<string> { "test" },
      Published = true,
      PublishedDate = DateTime.UtcNow,
      Author = "test-author",
      MediaReferences = new List<string>()
    };

    _mockContentService
        .Setup(x => x.GetByIdAsync(slug))
        .ReturnsAsync(new ServiceResult<BlogPostModel> { IsSuccess = true, Data = expectedModel });

    // Act
    var result = await _blogPostService.GetBlogPostAsync(slug);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Data);
    Assert.Equal(slug, result.Data.Slug);

    _mockContentService.Verify(x => x.GetByIdAsync(slug), Times.Once);
  }

  [Fact]
  public async Task GetBlogPostAsync_EmptySlug_ReturnsFailure()
  {
    // Act
    var result = await _blogPostService.GetBlogPostAsync("");

    // Assert
    Assert.False(result.IsSuccess);
    Assert.Contains("Slug cannot be null or empty", result.ErrorMessage);

    _mockContentService.Verify(x => x.GetByIdAsync(It.IsAny<string>()), Times.Never);
  }

  [Fact]
  public async Task GetBlogPostsAsync_NoFilters_ReturnsAllPosts()
  {
    // Arrange
    var expectedModels = new List<BlogPostModel>
        {
            new BlogPostModel { Slug = "post-1", Title = "Post 1", Published = true, Category = "Tech" },
            new BlogPostModel { Slug = "post-2", Title = "Post 2", Published = false, Category = "Life" }
        };

    _mockContentService
        .Setup(x => x.GetAllAsync())
        .ReturnsAsync(new ServiceResult<IEnumerable<BlogPostModel>> { IsSuccess = true, Data = expectedModels });

    // Act
    var result = await _blogPostService.GetBlogPostsAsync(null, null, null);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Data);
    Assert.Equal(2, result.Data.Count());

    _mockContentService.Verify(x => x.GetAllAsync(), Times.Once);
  }

  [Fact]
  public async Task GetBlogPostsAsync_FilterByPublished_ReturnsFilteredPosts()
  {
    // Arrange
    var allModels = new List<BlogPostModel>
        {
            new BlogPostModel { Slug = "post-1", Title = "Post 1", Published = true, Category = "Tech" },
            new BlogPostModel { Slug = "post-2", Title = "Post 2", Published = false, Category = "Life" }
        };

    _mockContentService
        .Setup(x => x.GetAllAsync())
        .ReturnsAsync(new ServiceResult<IEnumerable<BlogPostModel>> { IsSuccess = true, Data = allModels });

    // Act
    var result = await _blogPostService.GetBlogPostsAsync(true, null, null);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Data);
    Assert.Single(result.Data);
    Assert.Equal("post-1", result.Data.First().Slug);
  }

  [Fact]
  public async Task GetBlogPostsAsync_FilterByCategory_ReturnsFilteredPosts()
  {
    // Arrange
    var allModels = new List<BlogPostModel>
        {
            new BlogPostModel { Slug = "post-1", Title = "Post 1", Published = true, Category = "Tech" },
            new BlogPostModel { Slug = "post-2", Title = "Post 2", Published = true, Category = "Life" }
        };

    _mockContentService
        .Setup(x => x.GetAllAsync())
        .ReturnsAsync(new ServiceResult<IEnumerable<BlogPostModel>> { IsSuccess = true, Data = allModels });

    // Act
    var result = await _blogPostService.GetBlogPostsAsync(null, "Tech", null);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Data);
    Assert.Single(result.Data);
    Assert.Equal("post-1", result.Data.First().Slug);
  }

  [Fact]
  public async Task DeleteBlogPostAsync_ValidSlug_ReturnsSuccess()
  {
    // Arrange
    var slug = "test-post";

    _mockContentService
        .Setup(x => x.DeleteAsync(slug))
        .ReturnsAsync(new ServiceResult<bool> { IsSuccess = true, Data = true });

    // Act
    var result = await _blogPostService.DeleteBlogPostAsync(slug);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.True(result.Data);

    _mockContentService.Verify(x => x.DeleteAsync(slug), Times.Once);
  }

  [Fact]
  public async Task DeleteBlogPostAsync_EmptySlug_ReturnsFailure()
  {
    // Act
    var result = await _blogPostService.DeleteBlogPostAsync("");

    // Assert
    Assert.False(result.IsSuccess);
    Assert.Contains("Slug cannot be null or empty", result.ErrorMessage);

    _mockContentService.Verify(x => x.DeleteAsync(It.IsAny<string>()), Times.Never);
  }
}
