using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Functions.BlogPosts.Services;
using Functions.BlogPosts.Models;
using SharedStorage.Services.BaseServices;
using SharedStorage.Services.MediaServices;
using SharedStorage.Services.ContentServices;
using SharedStorage.Models;
using Utils;
using Tests.Helpers;

namespace Tests.BlogPosts;

public class BlogPostIntegrationTest
{
  private readonly IBlogPostService _blogPostService;
  private readonly ITableStorageService _tableStorageService;
  private readonly IMediaService _mediaService;
  private readonly string _testPrefix;

  public BlogPostIntegrationTest()
  {
    _testPrefix = $"test-{DateTime.UtcNow:yyyyMMdd-HHmmss}";

    // Get environment variables
    var storageAccountName = Environment.GetEnvironmentVariable("AZURE_STORAGE_ACCOUNT_NAME")
        ?? Environment.GetEnvironmentVariable("StorageAccountName")
        ?? throw new InvalidOperationException("Missing AZURE_STORAGE_ACCOUNT_NAME or StorageAccountName environment variable");

    // Create mock logger
    var mockLogger = new MockAppInsightsLogger<TableStorageService>();

    // Create services
    _tableStorageService = new TableStorageService(storageAccountName, mockLogger);

    var blobLogger = new MockAppInsightsLogger<BlobStorageService>();
    var blobStorageService = new BlobStorageService(storageAccountName, blobLogger);

    var mediaLogger = new MockAppInsightsLogger<MediaService>();
    var mediaService = new MediaService(
        blobStorageService,
        _tableStorageService,
        new List<IMediaTypeHandler>(), // Empty for basic test
        mediaLogger
    );

    var contentService = new ContentService<BlogPostEntity, BlogPostModel, BlogPostDTO>(
        _tableStorageService,
        new MockAppInsightsLogger<ContentService<BlogPostEntity, BlogPostModel, BlogPostDTO>>()
    );

    var blogPostLogger = new MockAppInsightsLogger<BlogPostService>();
    _blogPostService = new BlogPostService(contentService, mediaService, blogPostLogger);
    _mediaService = mediaService;
  }

  public async Task<bool> RunTestsAsync()
  {
    Console.WriteLine("🧪 Starting BlogPost Integration Tests...");

    try
    {
      var testSlug = $"{_testPrefix}-test-blog-post";

      // Test 1: Create a blog post
      Console.WriteLine("📝 Test 1: Creating blog post...");
      var blogPostDto = new BlogPostDTO
      {
        Slug = testSlug,
        Title = "Test Blog Post",
        Description = "Integration test blog post",
        Content = "This is test content for integration testing.",
        Category = "Testing",
        Tags = new List<string> { "test", "integration" },
        Published = true,
        PublishedDate = DateTime.UtcNow,
        Author = "test-author",
        MediaReferences = new List<string>() // No media for basic test
      };

      var result = await _blogPostService.UpsertBlogPostAsync(blogPostDto);
      if (result.IsSuccess && result.Data != null)
      {
        Console.WriteLine("✅ Blog post created successfully");
      }
      else
      {
        Console.WriteLine($"❌ Failed to create blog post: {result.ErrorMessage}");
        return false;
      }

      // Test 2: Retrieve the blog post
      Console.WriteLine("📖 Test 2: Retrieving blog post...");
      var getResult = await _blogPostService.GetBlogPostAsync(testSlug);
      if (getResult.IsSuccess && getResult.Data != null)
      {
        var retrieved = getResult.Data;
        if (retrieved.Slug == testSlug &&
            retrieved.Title == "Test Blog Post" &&
            retrieved.Content == "This is test content for integration testing.")
        {
          Console.WriteLine("✅ Blog post retrieved and validated successfully");
        }
        else
        {
          Console.WriteLine("❌ Retrieved blog post data doesn't match");
          return false;
        }
      }
      else
      {
        Console.WriteLine($"❌ Failed to retrieve blog post: {getResult.ErrorMessage}");
        return false;
      }

      // Test 3: Update the blog post
      Console.WriteLine("✏️ Test 3: Updating blog post...");
      blogPostDto.Title = "Updated Test Blog Post";
      blogPostDto.Content = "Updated content for integration testing.";

      var updateResult = await _blogPostService.UpsertBlogPostAsync(blogPostDto);
      if (updateResult.IsSuccess)
      {
        Console.WriteLine("✅ Blog post updated successfully");
      }
      else
      {
        Console.WriteLine($"❌ Failed to update blog post: {updateResult.ErrorMessage}");
        return false;
      }

      // Test 4: Verify update
      Console.WriteLine("🔍 Test 4: Verifying update...");
      var updatedResult = await _blogPostService.GetBlogPostAsync(testSlug);
      if (updatedResult.IsSuccess && updatedResult.Data != null)
      {
        var updated = updatedResult.Data;
        if (updated.Title == "Updated Test Blog Post" &&
            updated.Content == "Updated content for integration testing.")
        {
          Console.WriteLine("✅ Blog post update verified successfully");
        }
        else
        {
          Console.WriteLine("❌ Updated blog post data doesn't match");
          return false;
        }
      }
      else
      {
        Console.WriteLine($"❌ Failed to verify blog post update: {updatedResult.ErrorMessage}");
        return false;
      }

      // Test 5: List blog posts
      Console.WriteLine("📋 Test 5: Listing blog posts...");
      var listResult = await _blogPostService.GetBlogPostsAsync(published: null, category: null, tag: null);
      if (listResult.IsSuccess && listResult.Data != null)
      {
        var found = false;
        foreach (var post in listResult.Data)
        {
          if (post.Slug == testSlug)
          {
            found = true;
            break;
          }
        }

        if (found)
        {
          Console.WriteLine("✅ Blog post found in list successfully");
        }
        else
        {
          Console.WriteLine("❌ Blog post not found in list");
          return false;
        }
      }
      else
      {
        Console.WriteLine($"❌ Failed to list blog posts: {listResult.ErrorMessage}");
        return false;
      }

      // Test 6: Delete the blog post (cleanup)
      Console.WriteLine("🗑️ Test 6: Deleting blog post...");
      var deleteResult = await _blogPostService.DeleteBlogPostAsync(testSlug);
      if (deleteResult.IsSuccess)
      {
        Console.WriteLine("✅ Blog post deleted successfully");
      }
      else
      {
        Console.WriteLine($"❌ Failed to delete blog post: {deleteResult.ErrorMessage}");
        return false;
      }

      // Test 7: Verify deletion
      Console.WriteLine("🔍 Test 7: Verifying deletion...");
      var deletedResult = await _blogPostService.GetBlogPostAsync(testSlug);
      if (!deletedResult.IsSuccess || deletedResult.Data == null)
      {
        Console.WriteLine("✅ Blog post deletion verified successfully");
      }
      else
      {
        Console.WriteLine("❌ Blog post was not deleted properly");
        return false;
      }

      Console.WriteLine("🎉 All BlogPost integration tests passed!");
      return true;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"❌ Integration test failed with exception: {ex.Message}");
      Console.WriteLine($"Stack trace: {ex.StackTrace}");
      return false;
    }
  }
}
