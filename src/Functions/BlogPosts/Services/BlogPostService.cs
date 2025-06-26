using Functions.BlogPosts.Models;
using SharedStorage.Services.ContentServices;
using SharedStorage.Services.MediaServices;
using SharedStorage.Validators;
using Utils;

namespace Functions.BlogPosts.Services;

public interface IBlogPostService
{
  // Table storage functions
  Task<BlogPostDTO?> GetPostAsync(string slug, bool? isPublished = true);
  Task<BlogPostDTO?> GetPostsAsync(string? authorSlug = null, string? category = null, string? status = null, string? tag = null, int? limit = null, int? offset = null);
  Task<BlogPostDTO?> GetPostsByCategoryAsync(string category, bool? isPublished = true, int? limit = null, int? offset = null);
  Task<BlogPostDTO?> DeletePostAsync(string slug);
  Task<BlogPostDTO> UpsertPostAsync(string slug, BlogPostModel model);

  // Blob storage functions
  Task UploadBlogPostImageAsync(string slug, string imageUrl, string? description = null);
  Task UploadBlogPostMediaAsync(string slug, string mediaUrl, string? description = null);
}

public class BlogPostService : IBlogPostService
{
  private readonly IContentService _contentService;
  private readonly IMediaService _mediaService;
  private readonly IAppInsightsLogger<BlogPostService> _appLogger;
  private readonly string _tableName;

  public static BlogPostService(IBlobStorageService blobStorageService, ITableStorageService tableStorageService, IAppInsightsLogger<BlogPostService> appLogger)
  {
    // Get table name from environment variable with fallback to "blogposts"
    var rawTableName = Environment.GetEnvironmentVariable("BLOGPOSTS_TABLE_NAME") ?? "blog";

    _tableName = TableNameValidator.ValidateTableName(rawTableName);
    _tableStorageService = tableStorageService;
    _appLogger = appLogger;
    _appLogger.LogInformation($"Instantiated table in BlogPostService using table name: {_tableName} -- will be updated if mock storage later.");
  }

  public async Task<BlogPostDTO> DeleteBlogAsync(string slug)
  {
    _appLogger.LogInformation("Deleting blog post with slug: {Slug}", slug);

    if (string.IsNullOrWhiteSpace(slug))
    {
      _appLogger.LogError("Slug cannot be null or empty.", new Exception("Invalid slug"));
      return null;
    }

    var entity = await _tableStorageService.GetEntityAsync<BlogPostModel>(_tableName, slug, "post");
    if (entity == null)
    {
      _appLogger.LogWarning("Blog post with slug {Slug} not found.", slug);
      return null;
    }

    await _tableStorageService.DeleteEntityAsync(_tableName, entity.PartitionKey, entity.RowKey);
    _appLogger.LogInformation("Deleted blog post with slug: {Slug}", slug);

    return BlogPostMapper.ToDTO(entity);
  }

  public async Task<BlogPostDTO> GetPostsAsync(string slug, bool? IsPublished = true)
  {
    _appLogger.LogInformation("Retrieving blog post with slug: {Slug}", slug);

    if (string.IsNullOrWhiteSpace(slug))
    {
      _appLogger.LogError("Slug cannot be null or empty.", new Exception("Invalid slug"));
      return null;
    }

    var entity = await _tableStorageService.GetEntityAsync<BlogPostModel>(_tableName, slug, "post");
    if (entity == null)
    {
      _appLogger.LogWarning("Blog post with slug {Slug} not found.", slug);
      return null;
    }

    if (IsPublished.HasValue && IsPublished.Value && !entity.IsPublished)
    {
      _appLogger.LogInformation("Blog post with slug {Slug} is not published.", slug);
      return null;
    }

    return BlogPostMapper.ToDTO(entity);
  }

  public async Task<BlogPostDTO?> UpsertPostAsync(string slug, BlogPostModel model)
  {
    _appLogger.LogInformation("Creating a new blog post with slug: {Slug}", slug);

    if (string.IsNullOrWhiteSpace(slug))
    {
      _appLogger.LogError("Slug cannot be null or empty.", new Exception("Invalid slug"));
      return null;
    }

    var newPost = new BlogPostModel
    {
      Slug = slug,
      Title = "New Post",
      Content = "Content goes here...",
      AuthorSlug = "default-author",
      Category = "Uncategorized",
      Status = "Draft",
      MediaUrl = string.Empty,
      MediaDescription = string.Empty,
      ImageUrl = string.Empty,
      ImageDescription = string.Empty,
      PublishDate = DateTime.UtcNow,
      LastModified = DateTime.UtcNow,
      TagsList = []
    };
  }
}