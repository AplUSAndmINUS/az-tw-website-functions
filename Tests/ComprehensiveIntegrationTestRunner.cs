using System;
using System.Threading.Tasks;
using Tests.Authors;
using Tests.BlogPosts;
using Tests.Media;

namespace Tests;

public class IntegrationTestRunner
{
  public static async Task Main(string[] args)
  {
    Console.WriteLine("🚀 Starting Azure Functions Integration Tests");
    Console.WriteLine("===========================================");

    // Check environment variables
    var storageAccountName = Environment.GetEnvironmentVariable("AZURE_STORAGE_ACCOUNT_NAME")
        ?? Environment.GetEnvironmentVariable("StorageAccountName");
    var apiKey = Environment.GetEnvironmentVariable("X_API_ENVIRONMENT_KEY");

    if (string.IsNullOrWhiteSpace(storageAccountName))
    {
      Console.WriteLine("❌ Missing required environment variable: AZURE_STORAGE_ACCOUNT_NAME or StorageAccountName");
      Environment.Exit(1);
    }

    if (string.IsNullOrWhiteSpace(apiKey))
    {
      Console.WriteLine("❌ Missing required environment variable: X_API_ENVIRONMENT_KEY");
      Environment.Exit(1);
    }

    Console.WriteLine($"✅ Using Storage Account: {storageAccountName}");
    Console.WriteLine($"✅ API Key configured");
    Console.WriteLine();

    var totalTests = 0;
    var passedTests = 0;

    try
    {
      // Run Author Integration Tests
      Console.WriteLine("👤 Running Author Integration Tests...");
      totalTests++;
      var authorTest = new CreateAuthorIntegrationTest();
      var authorResult = await authorTest.RunTestAsync();
      if (authorResult)
      {
        passedTests++;
        Console.WriteLine("✅ Author tests completed successfully");
      }
      else
      {
        Console.WriteLine("❌ Author tests failed");
      }
      Console.WriteLine();

      // Run BlogPost Integration Tests
      Console.WriteLine("📝 Running BlogPost Integration Tests...");
      totalTests++;
      var blogPostTest = new BlogPostIntegrationTest();
      var blogPostResult = await blogPostTest.RunTestsAsync();
      if (blogPostResult)
      {
        passedTests++;
        Console.WriteLine("✅ BlogPost tests completed successfully");
      }
      else
      {
        Console.WriteLine("❌ BlogPost tests failed");
      }
      Console.WriteLine();

      // Run Media Integration Tests
      Console.WriteLine("🎬 Running Media Integration Tests...");
      totalTests++;
      var mediaTest = new MediaIntegrationTestV2();
      var mediaResult = await mediaTest.RunTestsAsync();
      if (mediaResult)
      {
        passedTests++;
        Console.WriteLine("✅ Media tests completed successfully");
      }
      else
      {
        Console.WriteLine("❌ Media tests failed");
      }
      Console.WriteLine();

      // Summary
      Console.WriteLine("===========================================");
      Console.WriteLine("📊 Integration Test Summary");
      Console.WriteLine($"Total Test Suites: {totalTests}");
      Console.WriteLine($"Passed Test Suites: {passedTests}");
      Console.WriteLine($"Failed Test Suites: {totalTests - passedTests}");

      if (passedTests == totalTests)
      {
        Console.WriteLine("🎉 All integration tests passed!");
        Environment.Exit(0);
      }
      else
      {
        Console.WriteLine("❌ Some integration tests failed!");
        Environment.Exit(1);
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"❌ Integration test runner failed: {ex.Message}");
      Console.WriteLine($"Stack trace: {ex.StackTrace}");
      Environment.Exit(1);
    }
  }
}
