using System.Threading.Tasks;

namespace Utils.Services;

/// <summary>
/// Service for retrieving secrets from Azure Key Vault
/// </summary>
public interface IKeyVaultService
{
  /// <summary>
  /// Retrieves a secret from Azure Key Vault by name
  /// </summary>
  /// <param name="secretName">The name of the secret to retrieve</param>
  /// <returns>The secret value</returns>
  Task<string> GetSecretAsync(string secretName);

  /// <summary>
  /// Retrieves a secret from Azure Key Vault by name, returning null if not found
  /// </summary>
  /// <param name="secretName">The name of the secret to retrieve</param>
  /// <returns>The secret value or null if not found</returns>
  Task<string?> GetSecretOrNullAsync(string secretName);

  /// <summary>
  /// Checks if a secret exists in Azure Key Vault
  /// </summary>
  /// <param name="secretName">The name of the secret to check</param>
  /// <returns>True if the secret exists, false otherwise</returns>
  Task<bool> SecretExistsAsync(string secretName);
}
