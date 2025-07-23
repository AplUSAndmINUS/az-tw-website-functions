# Key Vault Authentication Troubleshooting Guide

## Issue Description

You're experiencing 500 internal server errors when trying to authenticate using Key Vault-stored API keys in your Azure Functions deployed to DEV, TEST, and PROD environments, while the same keys work fine when testing locally.

## Common Root Causes & Solutions

### 1. **Managed Identity Not Enabled or Configured**

**Issue**: The Azure Function App doesn't have system-assigned managed identity enabled.

**Solution**:

```bash
# Enable system-assigned managed identity for your Function App
az functionapp identity assign --name <function-app-name> --resource-group <resource-group>

# Get the principal ID (needed for Key Vault permissions)
az functionapp identity show --name <function-app-name> --resource-group <resource-group> --query principalId --output tsv
```

### 2. **Missing Key Vault Permissions**

**Issue**: The Function App's managed identity doesn't have permission to read secrets from Key Vault.

**Solution**:

```bash
# Assign Key Vault Secrets User role to the Function App's managed identity
az role assignment create \
  --role "Key Vault Secrets User" \
  --assignee <function-app-principal-id> \
  --scope "/subscriptions/<subscription-id>/resourceGroups/<resource-group>/providers/Microsoft.KeyVault/vaults/<key-vault-name>"

# Alternative: Use Key Vault access policies (legacy approach)
az keyvault set-policy \
  --name <key-vault-name> \
  --object-id <function-app-principal-id> \
  --secret-permissions get list
```

### 3. **Incorrect Key Vault URI**

**Issue**: The Key Vault URI environment variable is not set or is incorrect.

**Solution**:
Set the `KEY_VAULT_URI` environment variable in your Function App:

```bash
az functionapp config appsettings set \
  --name <function-app-name> \
  --resource-group <resource-group> \
  --settings KEY_VAULT_URI="https://<key-vault-name>.vault.azure.net/"
```

### 4. **Environment Detection Issues**

**Issue**: The environment detection logic is not correctly identifying DEV/TEST/PROD environments.

**Solution**:
Ensure your Function App names contain environment indicators, or set explicit environment variables:

```bash
# Option 1: Set explicit ENVIRONMENT variable
az functionapp config appsettings set \
  --name <function-app-name> \
  --resource-group <resource-group> \
  --settings ENVIRONMENT="production"  # or "develop", "test"

# Option 2: Ensure WEBSITE_SITE_NAME follows naming convention
# Function App names should contain "develop", "test", or "production"
```

### 5. **Wrong Secret Names**

**Issue**: The secrets in Key Vault don't match the expected naming convention.

**Expected Secret Names**:

-   `DEV-X-API-ENVIRONMENT-KEY` - for develop/localhost environments
-   `STAGING-X-API-ENVIRONMENT-KEY` - for test environments
-   `PROD-X-API-ENVIRONMENT-KEY` - for production environments

**Solution**:
Create or rename secrets in Key Vault to match these exact names.

### 6. **Key Vault Firewall/Network Restrictions**

**Issue**: Key Vault has network restrictions that block Azure Functions.

**Solution**:

```bash
# Option 1: Allow Azure Services
az keyvault update \
  --name <key-vault-name> \
  --resource-group <resource-group> \
  --default-action Allow

# Option 2: Add Function App subnet to allowed networks (if using VNet integration)
az keyvault network-rule add \
  --name <key-vault-name> \
  --resource-group <resource-group> \
  --subnet <subnet-id>
```

### 7. **DefaultAzureCredential Chain Issues**

**Issue**: `DefaultAzureCredential` is trying authentication methods in the wrong order or failing on earlier methods.

**Solution**: This is handled in the `KeyVaultService` implementation, but you can create a more explicit credential:

```csharp
// In KeyVaultService.cs, replace DefaultAzureCredential with:
var credential = new ChainedTokenCredential(
    new ManagedIdentityCredential(),
    new DefaultAzureCredential()
);
```

## Testing Steps

### Step 1: Deploy Diagnostic Functions

Use the diagnostic functions I created to test your setup:

1. Deploy `KeyVaultDiagnostics.cs` and `TestKeyVaultAuth.cs` to your Function App
2. Call the diagnostic endpoint: `GET /diagnostics/keyvault`
3. Call the auth test endpoint: `GET /test/auth` (with your API key in `x-api-key` header)

### Step 2: Check Azure Portal

1. **Function App > Identity**: Verify system-assigned identity is "On"
2. **Key Vault > Access policies** or **Access control (IAM)**: Verify Function App has permissions
3. **Key Vault > Secrets**: Verify secret names match exactly
4. **Function App > Configuration**: Verify `KEY_VAULT_URI` is set

### Step 3: Check Application Insights

Look for specific error messages in Application Insights:

-   "Failed to retrieve secret"
-   "Authentication failed"
-   "ManagedIdentityCredential authentication unavailable"

## Environment Variables Checklist

Ensure these are set in your Function App configuration:

| Variable            | Example Value                         | Purpose                                       |
| ------------------- | ------------------------------------- | --------------------------------------------- |
| `KEY_VAULT_URI`     | `https://your-vault.vault.azure.net/` | Key Vault endpoint                            |
| `ENVIRONMENT`       | `production`                          | Explicit environment (optional)               |
| `WEBSITE_SITE_NAME` | `your-app-production`                 | Auto-set by Azure (for environment detection) |

## Quick Verification Commands

```bash
# Check if managed identity is enabled
az functionapp identity show --name <function-app-name> --resource-group <resource-group>

# Check Key Vault permissions
az role assignment list --assignee <function-app-principal-id> --scope "/subscriptions/<subscription-id>/resourceGroups/<resource-group>/providers/Microsoft.KeyVault/vaults/<key-vault-name>"

# Test Key Vault access (run from Azure Cloud Shell)
az keyvault secret show --vault-name <key-vault-name> --name DEV-X-API-ENVIRONMENT-KEY

# Check Function App settings
az functionapp config appsettings list --name <function-app-name> --resource-group <resource-group>
```

## Enhanced Error Logging

Use the `EnhancedKeyVaultApiKeyValidator` I created instead of the original one for better error diagnostics. It provides detailed error messages that will help identify the exact issue.

## Expected Resolution

After implementing these fixes, your 500 errors should be resolved. The most common issue is usually missing managed identity permissions or incorrect environment detection.

If you're still having issues after checking all these items, the diagnostic functions will provide specific details about what's failing in your environment.
