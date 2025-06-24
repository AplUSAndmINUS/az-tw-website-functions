using System;
using System.Threading.Tasks;
using Tests.Authors;

namespace Tests.Authors;

/// <summary>
/// Simple console program to run the CreateAuthor integration tests.
/// This allows you to test against your DEV storage without needing a test runner.
/// 
/// Before running, ensure these environment variables are set:
/// - StorageAccountName: Your DEV storage account name
/// - X_API_ENVIRONMENT_KEY: Your DEV API key  
/// - AUTHORS_TABLE_NAME: Table name (optional, defaults to "authors")
/// </summary>
public class Program
{
  public static async Task<int> Main(string[] args)
  {
    Console.WriteLine("CreateAuthor Integration Test Runner");
    Console.WriteLine("===================================");

    // Check required environment variables
    var storageAccount = Environment.GetEnvironmentVariable("StorageAccountName");
    var apiKey = Environment.GetEnvironmentVariable("X_API_ENVIRONMENT_KEY");

    if (string.IsNullOrEmpty(storageAccount))
    {
      Console.WriteLine("❌ Error: StorageAccountName environment variable is required");
      return 1;
    }

    if (string.IsNullOrEmpty(apiKey))
    {
      Console.WriteLine("❌ Error: X_API_ENVIRONMENT_KEY environment variable is required");
      return 1;
    }

    Console.WriteLine($"Using storage account: {storageAccount}");
    Console.WriteLine($"API key configured: {(!string.IsNullOrEmpty(apiKey) ? "✅" : "❌")}");
    Console.WriteLine();

    // Run the integration tests
    using var integrationTest = new CreateAuthorIntegrationTest();
    var success = await integrationTest.RunAllTests();

    return success ? 0 : 1;
  }
}
