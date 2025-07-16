# Function Key Issue and Workaround

## Issue Description

There is currently an issue with retrieving and managing function keys in the develop and test environments of the Azure Functions app. This appears to be related to the specific configuration of these environments using the Flex Consumption plan with a user-assigned managed identity for storage access.

When attempting to access or manage function keys, either through the Azure Portal or Azure CLI, the following errors occur:
- Azure Portal: InternalServerError when accessing App Keys or Host Keys
- Azure CLI: "Bad Request" when using `az functionapp keys list` or `az functionapp keys set` commands

This issue does not affect the production environment, which uses a different storage configuration.

## Root Causes

The issue appears to be related to one or more of the following factors:
1. Flex Consumption plan configuration
2. User-assigned managed identity used for storage access
3. Custom storage configuration with `AzureWebJobsStorage__clientId` and related settings
4. Possible issues with the key storage mechanism in the Azure Functions infrastructure

## Workaround

Since all authenticated functions in the application already implement custom API key validation using the `ApiKeyValidator` class, the recommended workaround is to:

1. Use the custom API key validation via the `x-api-key` header instead of function keys
2. Set the appropriate API key value for each environment in the `X_API_ENVIRONMENT_KEY` environment variable
3. Include the `x-api-key` header in all API calls that require authentication

For detailed instructions on using the API key authentication, see the [API_KEY_USAGE_GUIDE.md](./API_KEY_USAGE_GUIDE.md) file.

## Long-term Solutions

If function key management is required in the future, the following solutions could be explored:

1. Change the storage configuration to use connection strings instead of managed identities for the develop and test environments (similar to production)
2. Open a support ticket with Microsoft to investigate the function key management issue with user-assigned managed identity storage configuration
3. Consider alternative authentication methods such as Azure AD authentication if appropriate for the application's requirements

## Testing

A test script (`***REMOVED***-auth.sh`) has been created to demonstrate and verify the custom API key authentication approach. This script can be used to test API calls in the develop environment without relying on function keys.
