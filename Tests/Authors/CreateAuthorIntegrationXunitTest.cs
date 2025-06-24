using System;
using System.Threading.Tasks;
using Tests.Authors;
using Xunit;

namespace Tests.Authors;

/// <summary>
/// XUnit wrapper for the integration tests.
/// This allows running the integration tests through normal test runners.
/// 
/// Before running, ensure these environment variables are set:
/// - StorageAccountName: Your DEV storage account name
/// - X_API_ENVIRONMENT_KEY: Your DEV API key  
/// - AUTHORS_TABLE_NAME: Table name (optional, defaults to "authors")
/// </summary>
public class CreateAuthorIntegrationXunitTest
{
  [Fact]
  public async Task CreateAuthor_IntegrationTest_ShouldPassAllTests()
  {
    // Skip the test if environment variables are not configured
    var storageAccount = Environment.GetEnvironmentVariable("StorageAccountName");
    var apiKey = Environment.GetEnvironmentVariable("X_API_ENVIRONMENT_KEY");

    if (string.IsNullOrEmpty(storageAccount) || string.IsNullOrEmpty(apiKey))
    {
      // Skip this test if DEV environment is not configured
      // This allows the test suite to run without failing on machines without DEV access
      return;
    }

    // Run the actual integration test
    using var integrationTest = new CreateAuthorIntegrationTest();
    var success = await integrationTest.RunAllTests();

    Assert.True(success, "Integration tests should pass");
  }
}
