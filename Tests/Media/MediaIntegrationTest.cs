using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SharedStorage.Services.MediaServices;
using SharedStorage.Services.BaseServices;
using SharedStorage.Services.Media.Handlers;
using SharedStorage.Models;
using Utils;
using Tests.Helpers;

namespace Tests.Media;

public class MediaIntegrationTest
{
  private readonly IMediaService _mediaService;
  private readonly string _testPrefix;

  public MediaIntegrationTest()
  {
    _testPrefix = $"test-{DateTime.UtcNow:yyyyMMdd-HHmmss}";

    // Get environment variables
    var storageAccountName = Environment.GetEnvironmentVariable("AZURE_STORAGE_ACCOUNT_NAME")
        ?? Environment.GetEnvironmentVariable("StorageAccountName")
        ?? throw new InvalidOperationException("Missing AZURE_STORAGE_ACCOUNT_NAME or StorageAccountName environment variable");

    // Create mock loggers
    var blobLogger = new MockAppInsightsLogger<BlobStorageService>();
    var tableLogger = new MockAppInsightsLogger<TableStorageService>();
    var mediaLogger = new MockAppInsightsLogger<MediaService>();
    var imageLogger = new MockAppInsightsLogger<ImageHandler>();
    var videoLogger = new MockAppInsightsLogger<VideoHandler>();

    // Create services
    var blobStorageService = new BlobStorageService(storageAccountName, blobLogger);
    var tableStorageService = new TableStorageService(storageAccountName, tableLogger);

    // Create conversion services
    var thumbnailService = new ThumbnailService(new MockAppInsightsLogger<ThumbnailService>());
    var imageConversionService = new ImageConversionService(new MockAppInsightsLogger<ImageConversionService>());
    var videoThumbnailService = new BasicVideoThumbnailService(new MockAppInsightsLogger<BasicVideoThumbnailService>());

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
        blobStorageService,
        tableStorageService,
        handlers,
        mediaLogger
    );
  }

  public async Task<bool> RunTestsAsync()
  {
    Console.WriteLine("🎬 Starting Media Integration Tests...");

    try
    {
      // Test 1: Upload an image (simple test data)
      Console.WriteLine("📷 Test 1: Uploading test image...");
      var imageData = CreateTestImageData();
      var imageName = $"{_testPrefix}-test.jpg";

      var uploadResult = await _mediaService.UploadMediaAsync(
          imageData,
          imageName,
          "image/jpeg",
          "Test integration image"
      );

      string imageId = null;
      if (uploadResult.IsSuccess && uploadResult.Data != null)
      {
        imageId = uploadResult.Data.Id;
        Console.WriteLine($"✅ Image uploaded successfully with ID: {imageId}");
      }
      else
      {
        Console.WriteLine($"❌ Failed to upload image: {uploadResult.ErrorMessage}");
        return false;
      }

      // Test 2: Retrieve the uploaded image
      Console.WriteLine("🔍 Test 2: Retrieving uploaded image...");
      var getResult = await _mediaService.GetMediaAsync(imageId);
      if (getResult.IsSuccess && getResult.Data != null)
      {
        var retrieved = getResult.Data;
        if (retrieved.Id == imageId &&
            retrieved.FileName == imageName &&
            retrieved.ContentType == "image/jpeg")
        {
          Console.WriteLine("✅ Image retrieved and validated successfully");
        }
        else
        {
          Console.WriteLine("❌ Retrieved image data doesn't match");
          return false;
        }
      }
      else
      {
        Console.WriteLine($"❌ Failed to retrieve image: {getResult.ErrorMessage}");
        return false;
      }

      // Test 3: Upload another image for batch testing
      Console.WriteLine("📷 Test 3: Uploading second test image...");
      var imageData2 = CreateTestImageData();
      var imageName2 = $"{_testPrefix}-test2.jpg";

      var uploadResult2 = await _mediaService.UploadMediaAsync(
          imageData2,
          imageName2,
          "image/jpeg",
          "Second test integration image"
      );

      string imageId2 = null;
      if (uploadResult2.IsSuccess && uploadResult2.Data != null)
      {
        imageId2 = uploadResult2.Data.Id;
        Console.WriteLine($"✅ Second image uploaded successfully with ID: {imageId2}");
      }
      else
      {
        Console.WriteLine($"❌ Failed to upload second image: {uploadResult2.ErrorMessage}");
        return false;
      }

      // Test 4: Batch retrieve
      Console.WriteLine("📋 Test 4: Batch retrieving images...");
      var batchGetResult = await _mediaService.GetMediaBatchAsync(new List<string> { imageId, imageId2 });
      if (batchGetResult.IsSuccess && batchGetResult.Data != null)
      {
        var retrievedBatch = batchGetResult.Data.ToList();
        if (retrievedBatch.Count == 2)
        {
          Console.WriteLine("✅ Batch retrieval successful");
        }
        else
        {
          Console.WriteLine($"❌ Expected 2 images, got {retrievedBatch.Count}");
          return false;
        }
      }
      else
      {
        Console.WriteLine($"❌ Failed to batch retrieve images: {batchGetResult.ErrorMessage}");
        return false;
      }

      // Test 5: Delete first image
      Console.WriteLine("🗑️ Test 5: Deleting first image...");
      var deleteResult = await _mediaService.DeleteMediaAsync(imageId);
      if (deleteResult.IsSuccess && deleteResult.Data)
      {
        Console.WriteLine("✅ First image deleted successfully");
      }
      else
      {
        Console.WriteLine($"❌ Failed to delete first image: {deleteResult.ErrorMessage}");
        return false;
      }

      // Test 6: Batch delete remaining images
      Console.WriteLine("🗑️ Test 6: Batch deleting remaining images...");
      var batchDeleteResult = await _mediaService.DeleteMediaBatchAsync(new List<string> { imageId2 });
      if (batchDeleteResult.IsSuccess && batchDeleteResult.Data)
      {
        Console.WriteLine("✅ Batch delete successful");
      }
      else
      {
        Console.WriteLine($"❌ Failed to batch delete: {batchDeleteResult.ErrorMessage}");
        return false;
      }

      // Test 7: Verify deletions
      Console.WriteLine("🔍 Test 7: Verifying deletions...");
      var verifyResult1 = await _mediaService.GetMediaAsync(imageId);
      var verifyResult2 = await _mediaService.GetMediaAsync(imageId2);

      if ((!verifyResult1.IsSuccess || verifyResult1.Data == null) &&
          (!verifyResult2.IsSuccess || verifyResult2.Data == null))
      {
        Console.WriteLine("✅ Media deletions verified successfully");
      }
      else
      {
        Console.WriteLine("❌ Some media items were not deleted properly");
        return false;
      }

      Console.WriteLine("🎉 All Media integration tests passed!");
      return true;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"❌ Media integration test failed with exception: {ex.Message}");
      Console.WriteLine($"Stack trace: {ex.StackTrace}");
      return false;
    }
  }

  private byte[] CreateTestImageData()
  {
    // Create a minimal valid JPEG header for testing
    // This is a very basic JPEG structure that should be processable
    var jpegHeader = new byte[]
    {
            0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01,
            0x01, 0x01, 0x00, 0x48, 0x00, 0x48, 0x00, 0x00, 0xFF, 0xDB, 0x00, 0x43,
            0x00, 0x08, 0x06, 0x06, 0x07, 0x06, 0x05, 0x08, 0x07, 0x07, 0x07, 0x09,
            0x09, 0x08, 0x0A, 0x0C, 0x14, 0x0D, 0x0C, 0x0B, 0x0B, 0x0C, 0x19, 0x12,
            0x13, 0x0F, 0x14, 0x1D, 0x1A, 0x1F, 0x1E, 0x1D, 0x1A, 0x1C, 0x1C, 0x20,
            0x24, 0x2E, 0x27, 0x20, 0x22, 0x2C, 0x23, 0x1C, 0x1C, 0x28, 0x37, 0x29,
            0x2C, 0x30, 0x31, 0x34, 0x34, 0x34, 0x1F, 0x27, 0x39, 0x3D, 0x38, 0x32,
            0x3C, 0x2E, 0x33, 0x34, 0x32, 0xFF, 0xD9
    };

    return jpegHeader;
  }

  var videoHandler = new VideoHandler(
      blobStorageService,
      videoThumbnailService,
      new MockAppInsightsLogger<VideoHandler>()
  );

  var handlers = new List<IMediaTypeHandler> { imageHandler, videoHandler };

  _mediaService = new MediaService(blobStorageService, tableStorageService, handlers, mediaLogger);
    }

    public async Task<bool> RunTestsAsync()
  {
    Console.WriteLine("🎬 Starting Media Service Integration Tests...");

    try
    {
      // Test 1: Upload an image (simulated)
      Console.WriteLine("🖼️ Test 1: Uploading test image...");

      // Create a simple test image file (1x1 pixel PNG)
      var testImageData = CreateTestPngImage();
      var imageFileName = $"{_testPrefix}-test-image.png";

      var uploadResult = await _mediaService.UploadMediaAsync(
          testImageData,
          imageFileName,
          "image/png",
          "Test image for integration testing"
      );

      if (uploadResult.IsSuccess && uploadResult.Data != null)
      {
        _testMediaIds.Add(uploadResult.Data.Id);
        Console.WriteLine($"✅ Image uploaded successfully with ID: {uploadResult.Data.Id}");
      }
      else
      {
        Console.WriteLine($"❌ Failed to upload image: {uploadResult.ErrorMessage}");
        return false;
      }

      // Test 2: Retrieve uploaded media
      Console.WriteLine("📖 Test 2: Retrieving uploaded media...");
      var mediaId = uploadResult.Data.Id;
      var getResult = await _mediaService.GetMediaAsync(mediaId);

      if (getResult.IsSuccess && getResult.Data != null)
      {
        var media = getResult.Data;
        if (media.Id == mediaId &&
            media.FileName == imageFileName &&
            media.MediaType == "image" &&
            media.ContentType == "image/png")
        {
          Console.WriteLine("✅ Media retrieved and validated successfully");
        }
        else
        {
          Console.WriteLine("❌ Retrieved media data doesn't match");
          return false;
        }
      }
      else
      {
        Console.WriteLine($"❌ Failed to retrieve media: {getResult.ErrorMessage}");
        return false;
      }

      // Test 3: Batch retrieval
      Console.WriteLine("📚 Test 3: Testing batch retrieval...");
      var batchResult = await _mediaService.GetMediaBatchAsync(new List<string> { mediaId });

      if (batchResult.IsSuccess && batchResult.Data != null && batchResult.Data.Count == 1)
      {
        var batchMedia = batchResult.Data[0];
        if (batchMedia.Id == mediaId)
        {
          Console.WriteLine("✅ Batch retrieval successful");
        }
        else
        {
          Console.WriteLine("❌ Batch retrieval returned incorrect data");
          return false;
        }
      }
      else
      {
        Console.WriteLine($"❌ Failed batch retrieval: {batchResult.ErrorMessage}");
        return false;
      }

      // Test 4: Upload a video (simulated)
      Console.WriteLine("🎥 Test 4: Uploading test video...");

      // Create a simple test video file (minimal data)
      var testVideoData = CreateTestVideoData();
      var videoFileName = $"{_testPrefix}-test-video.mp4";

      var videoUploadResult = await _mediaService.UploadMediaAsync(
          testVideoData,
          videoFileName,
          "video/mp4",
          "Test video for integration testing"
      );

      if (videoUploadResult.IsSuccess && videoUploadResult.Data != null)
      {
        _testMediaIds.Add(videoUploadResult.Data.Id);
        Console.WriteLine($"✅ Video uploaded successfully with ID: {videoUploadResult.Data.Id}");
      }
      else
      {
        Console.WriteLine($"❌ Failed to upload video: {videoUploadResult.ErrorMessage}");
        return false;
      }

      // Test 5: Delete media
      Console.WriteLine("🗑️ Test 5: Deleting uploaded media...");
      var deleteResult = await _mediaService.DeleteMediaAsync(mediaId);

      if (deleteResult.IsSuccess)
      {
        Console.WriteLine("✅ Media deleted successfully");
      }
      else
      {
        Console.WriteLine($"❌ Failed to delete media: {deleteResult.ErrorMessage}");
        return false;
      }

      // Test 6: Verify deletion
      Console.WriteLine("🔍 Test 6: Verifying deletion...");
      var deletedResult = await _mediaService.GetMediaAsync(mediaId);

      if (!deletedResult.IsSuccess || deletedResult.Data == null)
      {
        Console.WriteLine("✅ Media deletion verified successfully");
      }
      else
      {
        Console.WriteLine("❌ Media was not deleted properly");
        return false;
      }

      // Test 7: Batch delete remaining media
      Console.WriteLine("🗑️ Test 7: Batch deleting remaining media...");
      if (_testMediaIds.Count > 0)
      {
        var batchDeleteResult = await _mediaService.DeleteMediaBatchAsync(_testMediaIds);
        if (batchDeleteResult.IsSuccess)
        {
          Console.WriteLine("✅ Batch delete successful");
        }
        else
        {
          Console.WriteLine($"❌ Batch delete failed: {batchDeleteResult.ErrorMessage}");
          return false;
        }
      }

      Console.WriteLine("🎉 All Media Service integration tests passed!");
      return true;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"❌ Integration test failed with exception: {ex.Message}");
      Console.WriteLine($"Stack trace: {ex.StackTrace}");
      return false;
    }
    finally
    {
      // Cleanup any remaining test media
      await CleanupTestMediaAsync();
    }
  }

  private async Task CleanupTestMediaAsync()
  {
    try
    {
      if (_testMediaIds.Count > 0)
      {
        Console.WriteLine("🧹 Cleaning up test media...");
        await _mediaService.DeleteMediaBatchAsync(_testMediaIds);
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"⚠️ Warning: Failed to cleanup test media: {ex.Message}");
    }
  }

  private byte[] CreateTestPngImage()
  {
    // Create a minimal 1x1 pixel PNG image (base64 decoded)
    var base64Png = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";
    return Convert.FromBase64String(base64Png);
  }

  private byte[] CreateTestVideoData()
  {
    // Create minimal test video data (just header bytes for MP4)
    // This is not a valid video but enough for testing upload/storage
    return new byte[] {
            0x00, 0x00, 0x00, 0x20, 0x66, 0x74, 0x79, 0x70,
            0x69, 0x73, 0x6F, 0x6D, 0x00, 0x00, 0x02, 0x00,
            0x69, 0x73, 0x6F, 0x6D, 0x69, 0x73, 0x6F, 0x32,
            0x61, 0x76, 0x63, 0x31, 0x6D, 0x70, 0x34, 0x31
        };
  }
}
