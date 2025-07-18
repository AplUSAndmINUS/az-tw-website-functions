# Key Vault Integration for Azure Functions

This document describes the Key Vault integration implemented for the Azure Functions project.

## Configuration Placeholders

Before implementing, replace the following placeholders with your actual values:

-   `{{KEY-VAULT-NAME}}` - Your Azure Key Vault name
-   `{{KEY-VAULT-RESOURCE-GROUP}}` - Resource group containing the Key Vault
-   `{{DEV-SECRET-NAME}}` - Secret name for development environment
-   `{{STAGING-SECRET-NAME}}` - Secret name for staging/test environment
-   `{{PROD-SECRET-NAME}}` - Secret name for production environment
-   `{{DEV-FUNCTION-APP}}` - Development Function App name
-   `{{TEST-FUNCTION-APP}}` - Test/staging Function App name
-   `{{PROD-FUNCTION-APP}}` - Production Function App name
-   `{{DEV-MANAGED-IDENTITY-ID}}` - Development managed identity principal ID
-   `{{TEST-MANAGED-IDENTITY-ID}}` - Test managed identity principal ID
-   `{{PROD-MANAGED-IDENTITY-ID}}` - Production managed identity principal ID

## Overview

The project now uses Azure Key Vault to securely store and retrieve API keys instead of storing them as plain text in environment variables. This provides better security and centralized secret management.

## Key Vault Configuration

-   **Key Vault Name**: `{{KEY-VAULT-NAME}}`
-   **Resource Group**: `{{KEY-VAULT-RESOURCE-GROUP}}`
-   **Authentication**: Managed Identity (no keys or connection strings needed)

## Secret Names

The following secrets are configured in Key Vault:

-   `{{DEV-SECRET-NAME}}` - Used for localhost and development environments
-   `{{STAGING-SECRET-NAME}}` - Used for staging/test environments
-   `{{PROD-SECRET-NAME}}` - Used for production environments

## Implementation Details

### 1. Key Vault Service

**Location**: `Utils/Services/KeyVaultService.cs`

Provides secure access to Key Vault secrets using managed identity:

```csharp
public interface IKeyVaultService
{
    Task<string> GetSecretAsync(string secretName);
    Task<string?> GetSecretOrNullAsync(string secretName);
    Task<bool> SecretExistsAsync(string secretName);
}
```

### 2. Key Vault API Validator

**Location**: `Utils/Validation/KeyVaultApiKeyValidator.cs`

Replaces the previous `ApiKeyValidator` with Key Vault-backed validation:

-   Automatically determines environment (develop/test/production)
-   Retrieves the appropriate API key from Key Vault
-   Validates incoming requests against the Key Vault secret

### 3. Environment Detection

**Location**: `Utils/Configuration/EnvironmentHelper.cs`

Determines the current environment to select the correct secret:

```csharp
public static string GetCurrentEnvironment()
{
    // Logic to determine environment from various sources
    // Returns: "develop", "test", "production", or "localhost"
}
```

## Configuration

### Function App Settings

The following app settings are configured via GitHub Actions:

-   `X_API_ENVIRONMENT_KEY` - Key Vault reference (e.g., `@Microsoft.KeyVault(VaultName={{KEY-VAULT-NAME}};SecretName={{PROD-SECRET-NAME}})`)
-   `KEY_VAULT_URI` - The Key Vault URI (e.g., `https://{{KEY-VAULT-NAME}}.vault.azure.net/`)

### Managed Identity Permissions

Each Function App environment has a managed identity with **Key Vault Secrets User** role assigned to the Key Vault.

## Environment Mapping

| Environment | Function App          | Secret Name             | Managed Identity             |
| ----------- | --------------------- | ----------------------- | ---------------------------- |
| Production  | {{PROD-FUNCTION-APP}} | {{PROD-SECRET-NAME}}    | {{PROD-MANAGED-IDENTITY-ID}} |
| Develop     | {{DEV-FUNCTION-APP}}  | {{DEV-SECRET-NAME}}     | {{DEV-MANAGED-IDENTITY-ID}}  |
| Test        | {{TEST-FUNCTION-APP}} | {{STAGING-SECRET-NAME}} | {{TEST-MANAGED-IDENTITY-ID}} |

## Migration from Environment Variables

### Before (Environment Variables)

```csharp
services.AddSingleton<IAPIKeyValidator>(sp =>
{
    var validApiKey = configuration["X_API_ENVIRONMENT_KEY"];
    return new ApiKeyValidator(validApiKey, appLogger);
});
```

### After (Key Vault)

```csharp
services.AddSingleton<IKeyVaultService>(sp =>
{
    var keyVaultUri = EnvironmentHelper.GetKeyVaultUri();
    var logger = sp.GetRequiredService<ILogger<KeyVaultService>>();
    return new KeyVaultService(keyVaultUri, logger);
});

services.AddSingleton<IAPIKeyValidator>(sp =>
{
    var keyVaultService = sp.GetRequiredService<IKeyVaultService>();
    var environment = EnvironmentHelper.GetCurrentEnvironment();
    var appLogger = sp.GetRequiredService<IAppInsightsLogger<KeyVaultApiKeyValidator>>();

    return new KeyVaultApiKeyValidator(keyVaultService, environment, appLogger);
});
```

## Security Benefits

1. **No Plain Text Secrets**: API keys are no longer stored in environment variables
2. **Centralized Management**: All secrets managed in one secure location
3. **Managed Identity**: No connection strings or keys needed for Key Vault access
4. **Audit Trail**: Key Vault provides access logging and audit capabilities
5. **Rotation Support**: Secrets can be rotated without code changes

## Testing

A test class `KeyVaultIntegrationTest` is provided to verify:

1. Key Vault connectivity
2. Secret retrieval
3. API validator functionality

## Deployment Notes

1. **Managed Identity Setup**: Each Function App must have system-assigned managed identity enabled
2. **Key Vault Permissions**: Managed identity must have "Key Vault Secrets User" role
3. **GitHub Actions**: Updated to use Key Vault references instead of environment variables
4. **Fallback**: Legacy `ApiKeyValidator` is still registered for backward compatibility during migration

## Troubleshooting

### Common Issues

1. **Authentication Errors**: Check that managed identity is enabled and has Key Vault permissions
2. **Wrong Secret**: Verify environment detection logic and secret naming
3. **Network Issues**: Ensure Function App can reach Key Vault (no firewall restrictions)

### Testing Key Vault Access

Use the integration test:

```bash
# Run integration test to verify Key Vault connectivity
dotnet test --filter "KeyVaultIntegrationTest"
```

## Future Enhancements

1. **Secret Caching**: Implement local caching of secrets for performance
2. **Secret Versioning**: Support for specific secret versions
3. **Additional Secrets**: Move other configuration values to Key Vault
4. **Monitoring**: Add Application Insights tracking for Key Vault operations
