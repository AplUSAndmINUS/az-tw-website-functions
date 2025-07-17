using System;
using System.Threading.Tasks;
using Tests.Authors;

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
      var authorTest = new UpsertAuthorIntegrationTest();
      var authorResult = await authorTest.RunAllTests();
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

      // Additional integration tests can be added here as they are created
      // For now, just run the basic author integration test
      
    }
    catch (Exception ex)
    {
      Console.WriteLine($"❌ Unexpected error during tests: {ex.Message}");
      Console.WriteLine($"Stack trace: {ex.StackTrace}");
    }

    // Summary
    Console.WriteLine("===========================================");
    Console.WriteLine($"📊 Test Results: {passedTests}/{totalTests} tests passed");
    
    if (passedTests == totalTests)
    {
      Console.WriteLine("🎉 All integration tests passed!");
      Environment.Exit(0);
    }
    else
    {
      Console.WriteLine("💥 Some integration tests failed!");
      Environment.Exit(1);
    }
  }
}