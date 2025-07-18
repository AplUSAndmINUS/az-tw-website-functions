using System;

namespace Utils.Configuration;

/// <summary>
/// Helper class for environment configuration
/// </summary>
public static class EnvironmentHelper
{
  /// <summary>
  /// Gets the current environment based on Azure Functions configuration
  /// </summary>
  /// <returns>The environment name (develop, test, production, or localhost)</returns>
  public static string GetCurrentEnvironment()
  {
    // Check common environment variables
    var environmentName = Environment.GetEnvironmentVariable("ENVIRONMENT")
                      ?? Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT")
                      ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

    // Check if running locally
    if (string.IsNullOrEmpty(environmentName))
    {
      var isLocal = Environment.GetEnvironmentVariable("AzureWebJobsScriptRoot") == null;
      if (isLocal)
      {
        return "localhost";
      }
    }

    // Check based on function app name pattern
    var functionAppName = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME");
    if (!string.IsNullOrEmpty(functionAppName))
    {
      if (functionAppName.Contains("develop", StringComparison.OrdinalIgnoreCase))
        return "develop";
      if (functionAppName.Contains("test", StringComparison.OrdinalIgnoreCase))
        return "test";
      if (functionAppName.Contains("production", StringComparison.OrdinalIgnoreCase))
        return "production";
    }

    // Default fallback
    return environmentName?.ToLowerInvariant() switch
    {
      "development" => "develop",
      "staging" => "test",
      "production" => "production",
      _ => "develop" // Default to develop
    };
  }

  /// <summary>
  /// Gets the Key Vault URI based on environment
  /// </summary>
  /// <returns>The Key Vault URI</returns>
  public static string GetKeyVaultUri()
  {
    // You can customize this based on your Key Vault naming convention
    return Environment.GetEnvironmentVariable("KEY_VAULT_URI")
           ?? "https://kv-tw-website-vault.vault.azure.net/";
  }
}
