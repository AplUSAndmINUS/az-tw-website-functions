using Microsoft.Extensions.Logging;
using Moq;
using SharedStorage.Services.BaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;
using Utils.Constants;
using Xunit;

namespace Tests.Media
{
  public class ContainerNameTests
  {
    [Fact]
    public void ContentNameResolver_GeneratesCorrectContainerNames()
    {
      // Test with mock storage false
      var blogImagesContainer = ContentNameResolver.GetBlobContainerName(ContentSections.Blog, AssetType.Images, false);
      Assert.Equal("blog-images", blogImagesContainer);

      // Test with mock storage true
      var mockBlogImagesContainer = ContentNameResolver.GetBlobContainerName(ContentSections.Blog, AssetType.Images, true);
      Assert.Equal("mock-blog-images", mockBlogImagesContainer);

      // Test with no asset type
      var blogContainer = ContentNameResolver.GetBlobContainerName(ContentSections.Blog, null, false);
      Assert.Equal("blog", blogContainer);

      // Test with mock storage true and no asset type
      var mockBlogContainer = ContentNameResolver.GetBlobContainerName(ContentSections.Blog, null, true);
      Assert.Equal("mock-blog", mockBlogContainer);
    }

    [Fact]
    public void BlobStorageService_ParseContainerName_HandlesNonMockContainers()
    {
      // Setup
      var mockLogger = new Mock<IAppInsightsLogger<BlobStorageService>>();
      var service = new BlobStorageService("testaccount", mockLogger.Object);

      // Set environment variable for test
      Environment.SetEnvironmentVariable("USE_MOCK_STORAGE", "false");

      // Test method is internal, use reflection to access it
      var methodInfo = typeof(BlobStorageService).GetMethod("ParseContainerName",
          System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

      // Test with simple container name
      var result = methodInfo.Invoke(service, new object[] { "blog-images" });
      var tuple = ((ContentSections, AssetType?))result;

      Assert.Equal(ContentSections.Blog, tuple.Item1);
      Assert.Equal(AssetType.Images, tuple.Item2);

      // Test without asset type
      result = methodInfo.Invoke(service, new object[] { "blog" });
      tuple = ((ContentSections, AssetType?))result;

      Assert.Equal(ContentSections.Blog, tuple.Item1);
      Assert.Null(tuple.Item2);
    }

    [Fact]
    public void BlobStorageService_ParseContainerName_HandlesMockContainers()
    {
      // Setup
      var mockLogger = new Mock<IAppInsightsLogger<BlobStorageService>>();
      var service = new BlobStorageService("testaccount", mockLogger.Object);

      // Set environment variable for test
      Environment.SetEnvironmentVariable("USE_MOCK_STORAGE", "true");

      // Test method is internal, use reflection to access it
      var methodInfo = typeof(BlobStorageService).GetMethod("ParseContainerName",
          System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

      // Test with mock container name
      var result = methodInfo.Invoke(service, new object[] { "mock-blog-images" });
      var tuple = ((ContentSections, AssetType?))result;

      Assert.Equal(ContentSections.Blog, tuple.Item1);
      Assert.Equal(AssetType.Images, tuple.Item2);

      // Test without asset type
      result = methodInfo.Invoke(service, new object[] { "mock-blog" });
      tuple = ((ContentSections, AssetType?))result;

      Assert.Equal(ContentSections.Blog, tuple.Item1);
      Assert.Null(tuple.Item2);

      // Test that it can also handle non-mock names even when USE_MOCK_STORAGE is true
      result = methodInfo.Invoke(service, new object[] { "blog-images" });
      tuple = ((ContentSections, AssetType?))result;

      Assert.Equal(ContentSections.Blog, tuple.Item1);
      Assert.Equal(AssetType.Images, tuple.Item2);
    }

    [Fact]
    public void BlobStorageService_ParseContainerName_HandlesMixedSettings()
    {
      // Setup
      var mockLogger = new Mock<IAppInsightsLogger<BlobStorageService>>();
      var service = new BlobStorageService("testaccount", mockLogger.Object);

      // Test method is internal, use reflection to access it
      var methodInfo = typeof(BlobStorageService).GetMethod("ParseContainerName",
          System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

      // Test with mock container name but USE_MOCK_STORAGE=false
      Environment.SetEnvironmentVariable("USE_MOCK_STORAGE", "false");
      var result = methodInfo.Invoke(service, new object[] { "mock-blog-images" });
      var tuple = ((ContentSections, AssetType?))result;

      Assert.Equal(ContentSections.Blog, tuple.Item1);
      Assert.Equal(AssetType.Images, tuple.Item2);

      // Test with non-mock container name but USE_MOCK_STORAGE=true
      Environment.SetEnvironmentVariable("USE_MOCK_STORAGE", "true");
      result = methodInfo.Invoke(service, new object[] { "blog-images" });
      tuple = ((ContentSections, AssetType?))result;

      Assert.Equal(ContentSections.Blog, tuple.Item1);
      Assert.Equal(AssetType.Images, tuple.Item2);
    }
  }
}
