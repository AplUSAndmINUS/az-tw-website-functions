using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using SharedStorage.Services.Media;
using SharedStorage.Services.BaseServices;
using SharedStorage.Services.Media.Handlers;
using SharedStorage.Models;
using Utils;

namespace Tests.Media;

public class MediaIntegrationTest
{
  private readonly IMediaService _mediaService;
  private readonly string _testPrefix;
  private readonly List<string> _testMediaIds;

  public MediaIntegrationTest()
  {
    _testPrefix = $"test-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
    _testMediaIds = new List<string>();

    // Get environment variables
    var storageAccountName = Environment.GetEnvironmentVariable("AZURE_STORAGE_ACCOUNT_NAME")
        ?? Environment.GetEnvironmentVariable("StorageAccountName")
        ?? throw new InvalidOperationException("Missing AZURE_STORAGE_ACCOUNT_NAME or StorageAccountName environment variable");

    // Create mock loggers
    var blobLogger = new TestAppInsightsLogger<BlobStorageService>();
    var tableLogger = new TestAppInsightsLogger<TableStorageService>();
    var mediaLogger = new TestAppInsightsLogger<MediaService>();
    var imageLogger = new TestAppInsightsLogger<ImageHandler>();
    var videoLogger = new TestAppInsightsLogger<VideoHandler>();

    // Create services
    var blobStorageService = new BlobStorageService(storageAccountName, blobLogger);
    var tableStorageService = new TableStorageService(storageAccountName, tableLogger);

    // Create conversion services
    var thumbnailService = new ThumbnailService(new TestAppInsightsLogger<ThumbnailService>());
    var imageConversionService = new ImageConversionService(new TestAppInsightsLogger<ImageConversionService>());
    var videoThumbnailService = new BasicVideoThumbnailService(
        new TestAppInsightsLogger<BasicVideoThumbnailService>(),
        thumbnailService);

    // Create handlers
    var imageHandler = new ImageHandler(
        blobStorageService,
        tableStorageService,
        imageConversionService,
        thumbnailService,
        imageLogger
    );

    var videoHandler = new VideoHandler(
        blobStorageService,
        tableStorageService,
        videoThumbnailService,
        videoLogger
    );

    var handlers = new List<IMediaTypeHandler> { imageHandler, videoHandler };

    _mediaService = new MediaService(
        handlers,
        tableStorageService,
        mediaLogger
    );
  }

  public async Task<bool> RunTestsAsync()
  {
    Console.WriteLine("🎬 Starting Media Integration Tests...");

    try
    {
      // Test 1: Upload an image (simple test data)
      Console.WriteLine("🖼️ Test 1: Uploading test image...");

      // Create a simple test image file (1x1 pixel PNG)
      var testImageData = CreateTestPngImage();
      var imageFileName = $"{_testPrefix}-test-image.png";

      using var imageStream = new MemoryStream(testImageData);
      var uploadResult = await _mediaService.UploadMediaAsync(
          imageFileName,
          imageStream,
          "image/png",
          "Test integration image"
      );

      string? imageId = null;
      if (uploadResult != null && !string.IsNullOrEmpty(uploadResult.Id))
      {
        imageId = uploadResult.Id;
        _testMediaIds.Add(imageId);
        Console.WriteLine($"✅ Image uploaded successfully with ID: {imageId}");
      }
      else
      {
        Console.WriteLine("❌ Failed to upload image");
        return false;
      }

      // Test 2: Retrieve the uploaded image
      Console.WriteLine("📥 Test 2: Retrieving uploaded image...");
      var getResult = await _mediaService.GetMediaAsync(imageId);

      if (getResult != null)
      {
        var retrieved = getResult;
        Console.WriteLine($"✅ Image retrieved successfully:");
        Console.WriteLine($"   - ID: {retrieved.Id}");
        Console.WriteLine($"   - Name: {retrieved.Name}");
        Console.WriteLine($"   - Type: {retrieved.ContentType}");
        Console.WriteLine($"   - Size: {retrieved.SizeBytes} bytes");
        Console.WriteLine($"   - Created: {retrieved.CreatedAt}");
        Console.WriteLine($"   - CDN URL: {retrieved.CdnUrl}");
      }
      else
      {
        Console.WriteLine("❌ Failed to retrieve image");
        return false;
      }

      // Test 3: Upload another image for batch operations
      Console.WriteLine("🖼️ Test 3: Uploading second test image...");
      var testImageData2 = CreateTestJpegImage();
      var imageFileName2 = $"{_testPrefix}-test-image-2.jpg";

      using var imageStream2 = new MemoryStream(testImageData2);
      var uploadResult2 = await _mediaService.UploadMediaAsync(
          imageFileName2,
          imageStream2,
          "image/jpeg",
          "Second test integration image"
      );

      string? imageId2 = null;
      if (uploadResult2 != null && !string.IsNullOrEmpty(uploadResult2.Id))
      {
        imageId2 = uploadResult2.Id;
        _testMediaIds.Add(imageId2);
        Console.WriteLine($"✅ Second image uploaded successfully with ID: {imageId2}");
      }
      else
      {
        Console.WriteLine("❌ Failed to upload second image");
        return false;
      }

      // Test 4: Batch retrieve
      Console.WriteLine("📥 Test 4: Batch retrieving images...");
      var batchGetResult = await _mediaService.GetMediaBatchAsync(new string[] { imageId, imageId2 });

      if (batchGetResult != null && batchGetResult.Any())
      {
        Console.WriteLine($"✅ Batch retrieval successful. Retrieved {batchGetResult.Count()} items:");
        foreach (var item in batchGetResult)
        {
          Console.WriteLine($"   - {item.Name} ({item.Id})");
        }
      }
      else
      {
        Console.WriteLine("❌ Batch retrieval failed");
        return false;
      }

      // Test 5: Delete first image
      Console.WriteLine("🗑️ Test 5: Deleting first image...");
      var deleteResult = await _mediaService.DeleteMediaAsync(imageId);

      if (deleteResult)
      {
        Console.WriteLine("✅ First image deleted successfully");
        _testMediaIds.Remove(imageId);
      }
      else
      {
        Console.WriteLine("❌ Failed to delete first image");
        return false;
      }

      // Test 6: Batch delete remaining images
      Console.WriteLine("🗑️ Test 6: Batch deleting remaining images...");
      var batchDeleteResult = await _mediaService.DeleteMediaBatchAsync(new string[] { imageId2 });

      if (batchDeleteResult)
      {
        Console.WriteLine("✅ Batch deletion successful");
        _testMediaIds.Remove(imageId2);
      }
      else
      {
        Console.WriteLine("❌ Batch deletion failed");
        return false;
      }

      // Test 7: Verify deletion
      Console.WriteLine("🔍 Test 7: Verifying deletion...");
      var verifyResult1 = await _mediaService.GetMediaAsync(imageId);
      var verifyResult2 = await _mediaService.GetMediaAsync(imageId2);

      if (verifyResult1 == null && verifyResult2 == null)
      {
        Console.WriteLine("✅ Deletion verification successful - both images are gone");
      }
      else
      {
        Console.WriteLine("❌ Deletion verification failed - some images still exist");
        return false;
      }

      Console.WriteLine("🎉 All Media Integration Tests passed!");
      return true;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"❌ Media Integration Tests failed with exception: {ex.Message}");
      Console.WriteLine($"Stack trace: {ex.StackTrace}");
      return false;
    }
    finally
    {
      // Cleanup: Delete any remaining test media
      await CleanupTestMedia();
    }
  }

  private async Task CleanupTestMedia()
  {
    Console.WriteLine("🧹 Cleaning up test media...");
    foreach (var mediaId in _testMediaIds.ToList())
    {
      try
      {
        await _mediaService.DeleteMediaAsync(mediaId);
        Console.WriteLine($"✅ Cleaned up media: {mediaId}");
      }
      catch (Exception ex)
      {
        Console.WriteLine($"⚠️ Failed to cleanup media {mediaId}: {ex.Message}");
      }
    }
    _testMediaIds.Clear();
  }

  /// <summary>
  /// Creates a minimal 1x1 pixel PNG image for testing
  /// </summary>
  private static byte[] CreateTestPngImage()
  {
    // This is a minimal 1x1 pixel transparent PNG
    return new byte[]
    {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG signature
            0x00, 0x00, 0x00, 0x0D, // IHDR chunk length
            0x49, 0x48, 0x44, 0x52, // IHDR
            0x00, 0x00, 0x00, 0x01, // Width: 1
            0x00, 0x00, 0x00, 0x01, // Height: 1
            0x08, 0x06, 0x00, 0x00, 0x00, // Bit depth, color type, compression, filter, interlace
            0x1F, 0x15, 0xC4, 0x89, // CRC
            0x00, 0x00, 0x00, 0x0A, // IDAT chunk length
            0x49, 0x44, 0x41, 0x54, // IDAT
            0x78, 0x9C, 0x62, 0x00, 0x00, 0x00, 0x02, 0x00, 0x01, // Compressed image data
            0xE2, 0x21, 0xBC, 0x33, // CRC
            0x00, 0x00, 0x00, 0x00, // IEND chunk length
            0x49, 0x45, 0x4E, 0x44, // IEND
            0xAE, 0x42, 0x60, 0x82  // CRC
    };
  }

  /// <summary>
  /// Creates a minimal JPEG image for testing
  /// </summary>
  private static byte[] CreateTestJpegImage()
  {
    // This is a minimal 1x1 pixel JPEG
    return new byte[]
    {
            0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01,
            0x01, 0x01, 0x00, 0x48, 0x00, 0x48, 0x00, 0x00, 0xFF, 0xDB, 0x00, 0x43,
            0x00, 0x03, 0x02, 0x02, 0x02, 0x02, 0x02, 0x03, 0x02, 0x02, 0x02, 0x03,
            0x03, 0x03, 0x03, 0x04, 0x06, 0x04, 0x04, 0x04, 0x04, 0x04, 0x08, 0x06,
            0x06, 0x05, 0x06, 0x09, 0x08, 0x0A, 0x0A, 0x09, 0x08, 0x09, 0x09, 0x0A,
            0x0C, 0x0F, 0x0C, 0x0A, 0x0B, 0x0E, 0x0B, 0x09, 0x09, 0x0D, 0x11, 0x0D,
            0x0E, 0x0F, 0x10, 0x10, 0x11, 0x10, 0x0A, 0x0C, 0x12, 0x13, 0x12, 0x10,
            0x13, 0x0F, 0x10, 0x10, 0x10, 0xFF, 0xC0, 0x00, 0x11, 0x08, 0x00, 0x01,
            0x00, 0x01, 0x01, 0x01, 0x11, 0x00, 0x02, 0x11, 0x01, 0x03, 0x11, 0x01,
            0xFF, 0xC4, 0x00, 0x14, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x03, 0xFF, 0xC4,
            0x00, 0x14, 0x10, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xDA, 0x00, 0x0C,
            0x03, 0x01, 0x00, 0x02, 0x11, 0x03, 0x11, 0x00, 0x3F, 0x00, 0x8A, 0x00,
            0xFF, 0xD9
    };
  }
}

// Test logger implementation
public class TestAppInsightsLogger<T> : IAppInsightsLogger<T>
    where T : notnull
{
  public void LogInformation(string message, params object[] args)
  {
    Console.WriteLine($"[INFO] {typeof(T).Name}: {string.Format(message, args)}");
  }

  public void LogWarning(string message, params object[] args)
  {
    Console.WriteLine($"[WARN] {typeof(T).Name}: {string.Format(message, args)}");
  }

  public void LogError(string message, Exception ex, params object[] args)
  {
    Console.WriteLine($"[ERROR] {typeof(T).Name}: {string.Format(message, args)}");
    if (ex != null)
    {
      Console.WriteLine($"Exception: {ex}");
    }
  }

  public void LogBlobQuery(string containerName, string functionName, string? prefix, int pageSize, string? continuationToken)
  {
    Console.WriteLine($"[INFO] {typeof(T).Name}: Blob query - Container: {containerName}, Function: {functionName}");
  }

  public void LogTableQuery(string tableName, string functionName, string? filter, int pageSize, string? continuationToken)
  {
    Console.WriteLine($"[INFO] {typeof(T).Name}: Table query - Table: {tableName}, Function: {functionName}");
  }
}
