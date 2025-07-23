using System;
using System.IO;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace ImageDebugTest
{
  class Program
  {
    static async Task Main(string[] args)
    {
      Console.WriteLine("Creating a simple test image...");

      // Create a simple 100x100 red image
      using var image = new Image<SixLabors.ImageSharp.PixelFormats.Rgb24>(100, 100);
      for (int y = 0; y < 100; y++)
      {
        for (int x = 0; x < 100; x++)
        {
          image[x, y] = new SixLabors.ImageSharp.PixelFormats.Rgb24(255, 0, 0); // Red pixel
        }
      }

      // Save to memory stream as JPEG
      var memoryStream = new MemoryStream();
      await image.SaveAsJpegAsync(memoryStream, new JpegEncoder { Quality = 80 });

      Console.WriteLine($"Created test image: {memoryStream.Length} bytes");

      // Reset position and test loading
      memoryStream.Position = 0;

      try
      {
        using var testImage = await Image.LoadAsync(memoryStream);
        Console.WriteLine($"Successfully loaded test image: {testImage.Width}x{testImage.Height}");
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Failed to load test image: {ex.Message}");
      }

      // Save to file for manual testing
      memoryStream.Position = 0;
      await File.WriteAllBytesAsync("test-debug-image.jpg", memoryStream.ToArray());
      Console.WriteLine("Saved test image to test-debug-image.jpg");
    }
  }
}
