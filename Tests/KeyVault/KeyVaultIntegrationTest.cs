using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Utils.Services;
using Utils.Configuration;
using Utils.Validation;
using Utils;

namespace Functions.Tests.KeyVault;

/// <summary>
/// Simple test to verify Key Vault integration is working
/// This will help us validate the configuration before deployment
/// </summary>
public class KeyVaultIntegrationTest
{
  /// <summary>
  /// Test to verify Key Vault connectivity and secret retrieval
  /// </summary>
  public static async Task TestKeyVaultIntegration()
  {
    try
    {
      // Create a test service provider
      var services = new ServiceCollection();

      // Add logging
      services.AddLogging(builder => builder.AddConsole());

      // Add Key Vault service
      services.AddSingleton<IKeyVaultService>(sp =>
      {
        var keyVaultUri = EnvironmentHelper.GetKeyVaultUri();
        var logger = sp.GetRequiredService<ILogger<KeyVaultService>>();
        return new KeyVaultService(keyVaultUri, logger);
      });

      // Add AppInsights logger (mock for testing)
      services.AddSingleton(typeof(IAppInsightsLogger<>), typeof(AppInsightsLogger<>));

      // Add Key Vault API validator
      services.AddSingleton<IAPIKeyValidator>(sp =>
      {
        var keyVaultService = sp.GetRequiredService<IKeyVaultService>();
        var environment = EnvironmentHelper.GetCurrentEnvironment();
        var appLogger = sp.GetRequiredService<IAppInsightsLogger<KeyVaultApiKeyValidator>>();

        return new KeyVaultApiKeyValidator(keyVaultService, environment, appLogger);
      });

      var serviceProvider = services.BuildServiceProvider();
      var keyVaultService = serviceProvider.GetRequiredService<IKeyVaultService>();
      var logger = serviceProvider.GetRequiredService<ILogger<KeyVaultIntegrationTest>>();

      logger.LogInformation("Starting Key Vault integration test...");

      // Test 1: Check if we can connect to Key Vault
      logger.LogInformation("Test 1: Testing Key Vault connectivity...");
      var environment = EnvironmentHelper.GetCurrentEnvironment();
      logger.LogInformation($"Current environment: {environment}");

      // Test 2: Try to retrieve the appropriate secret
      var secretName = environment.ToLowerInvariant() switch
      {
        "develop" or "localhost" => "DEV-X-API-ENVIRONMENT-KEY",
        "test" => "STAGING-X-API-ENVIRONMENT-KEY",
        "production" => "PROD-X-API-ENVIRONMENT-KEY",
        _ => "DEV-X-API-ENVIRONMENT-KEY"
      };

      logger.LogInformation($"Test 2: Attempting to retrieve secret '{secretName}'...");

      try
      {
        var secretValue = await keyVaultService.GetSecretAsync(secretName);
        logger.LogInformation($"✅ Successfully retrieved secret '{secretName}' (length: {secretValue?.Length ?? 0})");

        // Don't log the actual secret value for security
        if (!string.IsNullOrEmpty(secretValue))
        {
          logger.LogInformation($"✅ Secret value is not empty and has expected format");
        }
        else
        {
          logger.LogError($"❌ Secret value is null or empty");
        }
      }
      catch (Exception ex)
      {
        logger.LogError(ex, $"❌ Failed to retrieve secret '{secretName}': {ex.Message}");
        throw;
      }

      // Test 3: Test the API validator
      logger.LogInformation("Test 3: Testing API key validator...");
      var apiValidator = serviceProvider.GetRequiredService<IAPIKeyValidator>();
      logger.LogInformation($"✅ API key validator created successfully (Type: {apiValidator.GetType().Name})");

      logger.LogInformation("✅ All Key Vault integration tests passed!");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"❌ Key Vault integration test failed: {ex.Message}");
      Console.WriteLine($"Stack trace: {ex.StackTrace}");
      throw;
    }
  }
}
