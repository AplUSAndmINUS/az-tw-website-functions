using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Utils.Configuration;

namespace Functions.Diagnostics;

/// <summary>
/// Diagnostic function to troubleshoot Key Vault connectivity and authentication issues
/// </summary>
public class KeyVaultDiagnostics
{
  private readonly ILogger<KeyVaultDiagnostics> _logger;

  public KeyVaultDiagnostics(ILogger<KeyVaultDiagnostics> logger)
  {
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  [Function("KeyVaultDiagnostics")]
  public async Task<HttpResponseData> Run(
      [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "diagnostics/keyvault")] HttpRequestData req)
  {
    _logger.LogInformation("KeyVault diagnostics function triggered");

    var diagnostics = new
    {
      timestamp = DateTime.UtcNow,
      environment = new { },
      keyVault = new { },
      managedIdentity = new { },
      secretAccess = new { },
      recommendations = new List<string>()
    };

    try
    {
      // 1. Environment Diagnostics
      var currentEnvironment = EnvironmentHelper.GetCurrentEnvironment();
      var keyVaultUri = EnvironmentHelper.GetKeyVaultUri();

      diagnostics = diagnostics with
      {
        environment = new
        {
          detectedEnvironment = currentEnvironment,
          keyVaultUri = keyVaultUri,
          websiteSiteName = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME"),
          azureWebJobsScriptRoot = Environment.GetEnvironmentVariable("AzureWebJobsScriptRoot"),
          environmentVar = Environment.GetEnvironmentVariable("ENVIRONMENT"),
          azureFunctionsEnvironment = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT"),
          aspNetCoreEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
          isLocalDevelopment = Environment.GetEnvironmentVariable("AzureWebJobsScriptRoot") == null
        }
      };

      // 2. Managed Identity Diagnostics
      var managedIdentityDiagnostics = await DiagnoseManagedIdentityAsync();
      diagnostics = diagnostics with { managedIdentity = managedIdentityDiagnostics };

      // 3. Key Vault Access Diagnostics
      var keyVaultDiagnostics = await DiagnoseKeyVaultAccessAsync(keyVaultUri);
      diagnostics = diagnostics with { keyVault = keyVaultDiagnostics };

      // 4. Secret Access Diagnostics
      var secretDiagnostics = await DiagnoseSecretAccessAsync(keyVaultUri, currentEnvironment);
      diagnostics = diagnostics with { secretAccess = secretDiagnostics };

      // 5. Generate Recommendations
      var recommendations = GenerateRecommendations(diagnostics);
      diagnostics = diagnostics with { recommendations = recommendations };

      var response = req.CreateResponse(HttpStatusCode.OK);
      response.Headers.Add("Content-Type", "application/json");

      var jsonOptions = new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
      };

      await response.WriteStringAsync(JsonSerializer.Serialize(diagnostics, jsonOptions));
      return response;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error during Key Vault diagnostics");

      var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
      errorResponse.Headers.Add("Content-Type", "application/json");

      var errorDiagnostics = diagnostics with
      {
        error = new
        {
          message = ex.Message,
          type = ex.GetType().Name,
          stackTrace = ex.StackTrace,
          innerException = ex.InnerException?.Message
        }
      };

      var jsonOptions = new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
      };

      await errorResponse.WriteStringAsync(JsonSerializer.Serialize(errorDiagnostics, jsonOptions));
      return errorResponse;
    }
  }

  private async Task<object> DiagnoseManagedIdentityAsync()
  {
    try
    {
      var credential = new DefaultAzureCredential();

      // Try to get a token for Key Vault
      var tokenRequestContext = new Azure.Core.TokenRequestContext(new[] { "https://vault.azure.net/.default" });
      var tokenResult = await credential.GetTokenAsync(tokenRequestContext);

      return new
      {
        status = "success",
        hasToken = !string.IsNullOrEmpty(tokenResult.Token),
        tokenExpiresOn = tokenResult.ExpiresOn,
        credentialType = credential.GetType().Name,
        msiEndpoint = Environment.GetEnvironmentVariable("MSI_ENDPOINT"),
        msiSecret = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MSI_SECRET")) ? "***SET***" : "NOT_SET",
        identityEndpoint = Environment.GetEnvironmentVariable("IDENTITY_ENDPOINT"),
        identityHeader = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("IDENTITY_HEADER")) ? "***SET***" : "NOT_SET"
      };
    }
    catch (Exception ex)
    {
      return new
      {
        status = "failed",
        error = ex.Message,
        errorType = ex.GetType().Name,
        msiEndpoint = Environment.GetEnvironmentVariable("MSI_ENDPOINT"),
        msiSecret = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MSI_SECRET")) ? "***SET***" : "NOT_SET",
        identityEndpoint = Environment.GetEnvironmentVariable("IDENTITY_ENDPOINT"),
        identityHeader = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("IDENTITY_HEADER")) ? "***SET***" : "NOT_SET"
      };
    }
  }

  private async Task<object> DiagnoseKeyVaultAccessAsync(string keyVaultUri)
  {
    try
    {
      var credential = new DefaultAzureCredential();
      var secretClient = new SecretClient(new Uri(keyVaultUri), credential);

      // Try to list secrets (this requires Key Vault access)
      var secretsPages = secretClient.GetPropertiesOfSecretsAsync();
      var secrets = new List<string>();

      await foreach (var secretProperty in secretsPages)
      {
        secrets.Add(secretProperty.Name);
        if (secrets.Count >= 10) break; // Limit to first 10 secrets
      }

      return new
      {
        status = "success",
        keyVaultUri = keyVaultUri,
        canListSecrets = true,
        secretCount = secrets.Count,
        availableSecrets = secrets
      };
    }
    catch (Exception ex)
    {
      return new
      {
        status = "failed",
        keyVaultUri = keyVaultUri,
        canListSecrets = false,
        error = ex.Message,
        errorType = ex.GetType().Name,
        innerException = ex.InnerException?.Message
      };
    }
  }

  private async Task<object> DiagnoseSecretAccessAsync(string keyVaultUri, string environment)
  {
    var secretName = GetSecretNameForEnvironment(environment);

    try
    {
      var credential = new DefaultAzureCredential();
      var secretClient = new SecretClient(new Uri(keyVaultUri), credential);

      var secretResponse = await secretClient.GetSecretAsync(secretName);
      var hasValue = !string.IsNullOrEmpty(secretResponse.Value.Value);

      return new
      {
        status = "success",
        environment = environment,
        secretName = secretName,
        secretExists = true,
        hasValue = hasValue,
        secretLength = secretResponse.Value.Value?.Length ?? 0,
        secretVersion = secretResponse.Value.Properties.Version,
        createdOn = secretResponse.Value.Properties.CreatedOn,
        updatedOn = secretResponse.Value.Properties.UpdatedOn
      };
    }
    catch (Exception ex)
    {
      return new
      {
        status = "failed",
        environment = environment,
        secretName = secretName,
        secretExists = false,
        error = ex.Message,
        errorType = ex.GetType().Name,
        innerException = ex.InnerException?.Message
      };
    }
  }

  private static string GetSecretNameForEnvironment(string environment)
  {
    return environment.ToLowerInvariant() switch
    {
      "develop" or "localhost" => "DEV-X-API-ENVIRONMENT-KEY",
      "test" => "STAGING-X-API-ENVIRONMENT-KEY",
      "production" => "PROD-X-API-ENVIRONMENT-KEY",
      _ => throw new InvalidOperationException($"Unknown environment: {environment}")
    };
  }

  private List<string> GenerateRecommendations(dynamic diagnostics)
  {
    var recommendations = new List<string>();

    // Check managed identity
    if (diagnostics.managedIdentity.status == "failed")
    {
      recommendations.Add("Enable system-assigned managed identity on your Azure Function App");
      recommendations.Add("Verify that MSI_ENDPOINT and MSI_SECRET environment variables are present");
    }

    // Check Key Vault access
    if (diagnostics.keyVault.status == "failed")
    {
      recommendations.Add("Assign 'Key Vault Secrets User' role to the Function App's managed identity");
      recommendations.Add("Check Key Vault firewall settings - ensure Function App can access Key Vault");
      recommendations.Add("Verify Key Vault URI is correct: " + diagnostics.environment.keyVaultUri);
    }

    // Check secret access
    if (diagnostics.secretAccess.status == "failed")
    {
      recommendations.Add($"Create secret '{diagnostics.secretAccess.secretName}' in Key Vault");
      recommendations.Add("Verify environment detection logic - detected environment: " + diagnostics.environment.detectedEnvironment);
      recommendations.Add("Check that the secret name mapping is correct for your environment");
    }

    // Check environment detection
    var env = diagnostics.environment;
    if (env.detectedEnvironment == "develop" && string.IsNullOrEmpty(env.websiteSiteName))
    {
      recommendations.Add("For production deployments, ensure WEBSITE_SITE_NAME is set and contains environment indicator");
    }

    if (recommendations.Count == 0)
    {
      recommendations.Add("All Key Vault diagnostics passed! The issue might be in request processing or API key validation logic.");
    }

    return recommendations;
  }
}
