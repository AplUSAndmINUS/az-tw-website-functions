using System;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using Functions.Authors.Functions;
using Functions.Authors.Models;
using Functions.Authors.Services;
using Tests.Helpers;
using SharedStorage.Services;
using Utils;
using Utils.Validation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Tests.Authors;

/// <summary>
/// Integration test for UpsertAuthor function that tests against DEV storage.
/// This requires DEV environment variables to be configured:
/// - StorageAccountName: Your DEV storage account name
/// - X_API_ENVIRONMENT_KEY: Your DEV API key
/// - AUTHORS_TABLE_NAME: Table name (optional, defaults to "authors")
/// </summary>
public class UpsertAuthorIntegrationTest : IDisposable
{
  private readonly IServiceProvider _serviceProvider;
  private readonly UpsertAuthorFunction _upsertAuthorFunction;
  private readonly string _testAuthorUsername;

  public UpsertAuthorIntegrationTest()
  {
    // Generate a unique username for this test run to avoid conflicts
    _testAuthorUsername = $"testuser_{Guid.NewGuid():N}_{DateTime.UtcNow:yyyyMMddHHmmss}";

    // Build a minimal host with the same configuration as the actual function
    var host = Host.CreateDefaultBuilder()
        .ConfigureAppConfiguration((context, config) =>
        {
          // Add environment variables (this will pick up DEV storage settings)
          config.AddEnvironmentVariables();
        })
        .ConfigureServices((context, services) =>
        {
          var configuration = context.Configuration;

          // Use DEV storage account from environment variables
          var storageAccountName = configuration["StorageAccountName"]
                  ?? Environment.GetEnvironmentVariable("StorageAccountName")
                  ?? throw new InvalidOperationException("StorageAccountName not configured for integration test");

          // Use DEV API key from environment variables  
          var apiKey = configuration["X_API_ENVIRONMENT_KEY"]
                  ?? Environment.GetEnvironmentVariable("X_API_ENVIRONMENT_KEY")
                  ?? throw new InvalidOperationException("X_API_ENVIRONMENT_KEY not configured for integration test");

          // Register services (simplified version of Program.cs)
          services.AddSingleton<ITableStorageService>(sp =>
              {
              var logger = sp.GetRequiredService<IAppInsightsLogger<TableStorageService>>();
              return new TableStorageService(storageAccountName, logger);
            });

          services.AddSingleton<IAPIKeyValidator>(sp =>
              {
              var appLogger = sp.GetRequiredService<IAppInsightsLogger<ApiKeyValidator>>();
              return new ApiKeyValidator(apiKey, appLogger);
            });

          services.AddSingleton<IAuthorService, AuthorService>();

          // Register a simple logger implementation for testing
          services.AddSingleton(typeof(IAppInsightsLogger<>), typeof(TestAppInsightsLogger<>));
          services.AddSingleton(typeof(ILogger<>), typeof(TestLogger<>));
        })
        .Build();

    _serviceProvider = host.Services;

    // Create the function with real dependencies
    _upsertAuthorFunction = new UpsertAuthorFunction(
        _serviceProvider.GetRequiredService<IAppInsightsLogger<BaseContentFunctions<IAuthorService, AuthorModel, AuthorDTO, AuthorWithMediaDTO>>>(),
        _serviceProvider.GetRequiredService<IAPIKeyValidator>(),
        _serviceProvider.GetRequiredService<IAuthorService>()
    );
  }

  /// <summary>
  /// Test creating an author with valid data against DEV storage
  /// </summary>
  public async Task<bool> TestUpsertAuthorWithValidData()
  {
    try
    {
      Console.WriteLine("=== Testing UpsertAuthor with valid data ===");

      // Arrange - Create a valid author model
      var authorModel = new AuthorModel
      {
        FirstName = "Integration",
        LastName = "Test",
        Email = "integration.test@example.com",
        Username = _testAuthorUsername,
        DisplayName = "Integration Test Author",
        Location = "Test City, TC",
        Bio = "This is a test author created during integration testing.",
        Website = "https://example.com"
      };

      Console.WriteLine($"Creating test author with username: {_testAuthorUsername}");

      // Create test request with real API key from environment
      var context = TestFactory.CreateFunctionContext();
      var apiKey = Environment.GetEnvironmentVariable("X_API_ENVIRONMENT_KEY")
          ?? throw new InvalidOperationException("X_API_ENVIRONMENT_KEY required for integration test");

      var request = TestFactory.CreateJsonRequestWithApiKey(
          context,
          authorModel,
          apiKey,
          "PUT",
          $"authors/{_testAuthorUsername}"
      );

      // Act - Call the actual function
      var response = await _upsertAuthorFunction.Run(request, _testAuthorUsername, context);

      // Assert - Verify the response
      if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.Created)
      {
        Console.WriteLine($"❌ Expected OK (200) or Created (201), got {response.StatusCode}");
        return false;
      }

      Console.WriteLine($"✅ Author upserted successfully with status: {response.StatusCode}");
      return true;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"❌ Test failed with exception: {ex.Message}");
      Console.WriteLine($"Stack trace: {ex.StackTrace}");
      return false;
    }
  }

  /// <summary>
  /// Test updating an existing author
  /// </summary>
  public async Task<bool> TestUpdateExistingAuthor()
  {
    try
    {
      Console.WriteLine("=== Testing UpsertAuthor with updated data ===");

      // First create an author
      await TestUpsertAuthorWithValidData();

      // Now update the author with new data
      var updatedAuthorModel = new AuthorModel
      {
        FirstName = "Updated",
        LastName = "Test",
        Email = "updated.test@example.com",
        Username = _testAuthorUsername,
        DisplayName = "Updated Test Author",
        Location = "Updated City, UC",
        Bio = "This is an updated test author.",
        Website = "https://updated.example.com"
      };

      Console.WriteLine($"Updating test author with username: {_testAuthorUsername}");

      // Create test request with real API key from environment
      var context = TestFactory.CreateFunctionContext();
      var apiKey = Environment.GetEnvironmentVariable("X_API_ENVIRONMENT_KEY")
          ?? throw new InvalidOperationException("X_API_ENVIRONMENT_KEY required for integration test");

      var request = TestFactory.CreateJsonRequestWithApiKey(
          context,
          updatedAuthorModel,
          apiKey,
          "PUT",
          $"authors/{_testAuthorUsername}"
      );

      // Act - Call the actual function
      var response = await _upsertAuthorFunction.Run(request, _testAuthorUsername, context);

      // Assert - Verify the response
      if (response.StatusCode != HttpStatusCode.OK)
      {
        Console.WriteLine($"❌ Expected OK (200), got {response.StatusCode}");
        return false;
      }

      Console.WriteLine("✅ Author updated successfully");
      return true;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"❌ Test failed with exception: {ex.Message}");
      Console.WriteLine($"Stack trace: {ex.StackTrace}");
      return false;
    }
  }

  /// <summary>
  /// Test creating an author with invalid data
  /// </summary>
  public async Task<bool> TestUpsertAuthorWithInvalidData()
  {
    try
    {
      Console.WriteLine("=== Testing UpsertAuthor with invalid data ===");

      // Arrange - Create an invalid author model (missing required fields)
      var invalidAuthorModel = new AuthorModel
      {
        FirstName = "", // Invalid - empty
        LastName = "Test",
        Email = "invalid-email", // Invalid email format
        Username = "usr", // Invalid - too short
        DisplayName = "" // Invalid - empty
      };

      var context = TestFactory.CreateFunctionContext();
      var apiKey = Environment.GetEnvironmentVariable("X_API_ENVIRONMENT_KEY")
          ?? throw new InvalidOperationException("X_API_ENVIRONMENT_KEY required for integration test");

      var request = TestFactory.CreateJsonRequestWithApiKey(
          context,
          invalidAuthorModel,
          apiKey,
          "PUT",
          $"authors/{invalidAuthorModel.Username}"
      );

      // Act
      var response = await _upsertAuthorFunction.Run(request, invalidAuthorModel.Username, context);

      // Assert
      if (response.StatusCode != HttpStatusCode.BadRequest)
      {
        Console.WriteLine($"❌ Expected BadRequest (400), got {response.StatusCode}");
        return false;
      }

      Console.WriteLine("✅ Invalid data correctly rejected");
      return true;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"❌ Test failed with exception: {ex.Message}");
      return false;
    }
  }

  /// <summary>
  /// Run all integration tests
  /// </summary>
  public async Task<bool> RunAllTests()
  {
    Console.WriteLine("Starting UpsertAuthor Integration Tests");
    Console.WriteLine("=====================================");

    var test1 = await TestUpsertAuthorWithValidData();
    var test2 = await TestUpdateExistingAuthor();
    var test3 = await TestUpsertAuthorWithInvalidData();

    var allPassed = test1 && test2 && test3;

    var allPassed = test1 && test2;

    Console.WriteLine("=====================================");
    Console.WriteLine($"Integration Tests Result: {(allPassed ? "✅ PASSED" : "❌ FAILED")}");

    return allPassed;
  }

  public void Dispose()
  {
    // Cleanup: Remove the test author from DEV storage after the test
    try
    {
      var tableService = _serviceProvider.GetRequiredService<ITableStorageService>();
      var tableName = Environment.GetEnvironmentVariable("AUTHORS_TABLE_NAME") ?? "authors";

      Console.WriteLine($"Cleaning up test author: {_testAuthorUsername}");
      // Delete the test entity
      tableService.DeleteEntityAsync(tableName, _testAuthorUsername, "profile").Wait();
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Warning: Failed to cleanup test data: {ex.Message}");
    }
  }
}

/// <summary>
/// Simple test logger implementation that writes to console
/// </summary>
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

  public void LogTableEntryUpsert(string tableName, string functionName, string partitionKey, string rowKey)
  {
    Console.WriteLine($"[INFO] {typeof(T).Name}: Table upsert - Table: {tableName}, PK: {partitionKey}, RK: {rowKey}");
  }

  public void LogTableEntryDelete(string tableName, string functionName, string partitionKey, string rowKey)
  {
    Console.WriteLine($"[INFO] {typeof(T).Name}: Table delete - Table: {tableName}, PK: {partitionKey}, RK: {rowKey}");
  }

  public void LogBlobDownload(string containerName, string functionName, string blobName)
  {
    Console.WriteLine($"[INFO] {typeof(T).Name}: Blob download - Container: {containerName}, Blob: {blobName}");
  }

  public void LogBlobUpload(string containerName, string functionName, string blobName, long size)
  {
    Console.WriteLine($"[INFO] {typeof(T).Name}: Blob upload - Container: {containerName}, Blob: {blobName}, Size: {size}");
  }
}

/// <summary>
/// Simple test logger implementation for ILogger
/// </summary>
public class TestLogger<T> : ILogger<T>
{
  public IDisposable? BeginScope<TState>(TState state) where TState : notnull => new NoOpDisposable();
  public bool IsEnabled(LogLevel logLevel) => true;
  public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
  {
    Console.WriteLine($"[{logLevel}] {typeof(T).Name}: {formatter(state, exception)}");
  }

  private class NoOpDisposable : IDisposable
  {
    public void Dispose() { }
  }
}
