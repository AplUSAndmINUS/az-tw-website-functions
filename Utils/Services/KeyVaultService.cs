using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Utils.Services;

/// <summary>
/// Service for retrieving secrets from Azure Key Vault using managed identity
/// </summary>
public class KeyVaultService : IKeyVaultService
{
  private readonly SecretClient _secretClient;
  private readonly ILogger<KeyVaultService> _logger;
  private readonly string _keyVaultUri;

  public KeyVaultService(string keyVaultUri, ILogger<KeyVaultService> logger)
  {
    _keyVaultUri = keyVaultUri ?? throw new ArgumentNullException(nameof(keyVaultUri));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    // Create credential using managed identity
    var credential = new DefaultAzureCredential();

    // Initialize the Secret client
    _secretClient = new SecretClient(new Uri(_keyVaultUri), credential);

    _logger.LogInformation("KeyVaultService initialized for vault: {KeyVaultUri}", _keyVaultUri);
  }

  /// <summary>
  /// Retrieves a secret from Azure Key Vault by name
  /// </summary>
  /// <param name="secretName">The name of the secret to retrieve</param>
  /// <returns>The secret value</returns>
  /// <exception cref="InvalidOperationException">Thrown when secret is not found</exception>
  public async Task<string> GetSecretAsync(string secretName)
  {
    try
    {
      _logger.LogDebug("Retrieving secret: {SecretName}", secretName);

      var response = await _secretClient.GetSecretAsync(secretName);
      var secretValue = response.Value.Value;

      _logger.LogDebug("Successfully retrieved secret: {SecretName}", secretName);
      return secretValue;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to retrieve secret: {SecretName}", secretName);
      throw new InvalidOperationException($"Failed to retrieve secret '{secretName}' from Key Vault", ex);
    }
  }

  /// <summary>
  /// Retrieves a secret from Azure Key Vault by name, returning null if not found
  /// </summary>
  /// <param name="secretName">The name of the secret to retrieve</param>
  /// <returns>The secret value or null if not found</returns>
  public async Task<string?> GetSecretOrNullAsync(string secretName)
  {
    try
    {
      _logger.LogDebug("Retrieving secret (nullable): {SecretName}", secretName);

      var response = await _secretClient.GetSecretAsync(secretName);
      var secretValue = response.Value.Value;

      _logger.LogDebug("Successfully retrieved secret: {SecretName}", secretName);
      return secretValue;
    }
    catch (Exception ex)
    {
      _logger.LogWarning(ex, "Could not retrieve secret: {SecretName}", secretName);
      return null;
    }
  }

  /// <summary>
  /// Checks if a secret exists in Azure Key Vault
  /// </summary>
  /// <param name="secretName">The name of the secret to check</param>
  /// <returns>True if the secret exists, false otherwise</returns>
  public async Task<bool> SecretExistsAsync(string secretName)
  {
    try
    {
      _logger.LogDebug("Checking if secret exists: {SecretName}", secretName);

      await _secretClient.GetSecretAsync(secretName);
      _logger.LogDebug("Secret exists: {SecretName}", secretName);
      return true;
    }
    catch (Exception ex)
    {
      _logger.LogDebug(ex, "Secret does not exist: {SecretName}", secretName);
      return false;
    }
  }
}
