# Authorization Level Change Documentation

## Change Summary

All Azure Functions within this project have been updated from `AuthorizationLevel.Function` to `AuthorizationLevel.Anonymous` to address issues with function key management in development and test environments.

## Security Model

This change does NOT compromise security for the following reasons:

1. **Custom API Key Validation**: All functions that previously required authentication continue to require authentication via the custom `x-api-key` header validation implemented in `ApiKeyValidator.cs`.

2. **Consistent Security Model**: The application now uses a single, consistent authentication mechanism across all environments.

3. **Simplified Development**: Eliminates issues with function key management in development and test environments.

## Implementation Details

### Before Change

Previously, functions used a dual authentication approach:

-   Azure Functions built-in authorization using function keys
-   Custom API key validation via `x-api-key` header

This resulted in:

-   Development/test environments having issues with function key management
-   Inconsistent authorization requirements between environments
-   The need to maintain two separate authentication mechanisms

### After Change

Functions now use a single authentication approach:

-   All functions set to `AuthorizationLevel.Anonymous` at the Azure Functions level
-   Authentication handled exclusively through custom `x-api-key` header validation

Benefits:

-   Consistent authorization model across all environments
-   Simplified authentication flow
-   No function key management issues
-   Same security level maintained through custom API key validation

## Code Changes

The following change was made to all function definitions:

```csharp
// Before
[HttpTrigger(AuthorizationLevel.Function, "method", Route = "route")] HttpRequestData req

// After
[HttpTrigger(AuthorizationLevel.Anonymous, "method", Route = "route")] HttpRequestData req
```

All functions continue to validate the `x-api-key` header through code like:

```csharp
var apiValidationResult = await _apiKeyValidator.ValidateApiKeyAsync(req, _appLogger, "FunctionName");
if (apiValidationResult != null)
{
  return apiValidationResult;
}
```

## Security Recommendations

To maintain strong security with this approach:

1. **Use Strong API Keys**: Generate cryptographically strong API keys for each environment (32+ characters with mixed case, numbers, and symbols).

2. **Rotate API Keys Regularly**: Change keys periodically (every 30-90 days).

3. **Secure API Key Storage**: Store API keys in Azure Key Vault or environment variables, never in source code.

4. **Monitor API Usage**: Implement monitoring and alerting for unauthorized access attempts.

5. **Update CI/CD**: Ensure all deployment pipelines are updated to include the `X_API_ENVIRONMENT_KEY` environment variable.

## Additional Notes

This approach aligns with modern API security practices where authentication is handled at the application level rather than relying on infrastructure-level controls.
