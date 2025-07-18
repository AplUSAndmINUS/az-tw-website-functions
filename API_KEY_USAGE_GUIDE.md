# API Key Usage Guide

Due to issues with function keys in the development and test environments, all authenticated API calls should use the custom API key validation mechanism instead of relying on function keys.

## API Key Authentication

The application uses a custom API key validation mechanism that checks for the `x-api-key` header against a configured environment variable.

### How to Authenticate API Calls

1. Include the `x-api-key` header in your HTTP requests
2. Use the appropriate API key for each environment:
    - Development: Check environment variable `X_API_ENVIRONMENT_KEY` in the develop environment
    - Test: Check environment variable `X_API_ENVIRONMENT_KEY` in the test environment
    - Production: Check environment variable `X_API_ENVIRONMENT_KEY` in the production environment

## Security Considerations

### API Key Best Practices

1. **Key Strength**: API keys should be at least 32 characters long and include a mix of uppercase letters, lowercase letters, numbers, and special characters
2. **Key Rotation**: Rotate API keys periodically (every 30-90 days) to limit exposure in case of leakage
3. **Environmental Isolation**: Use different API keys for each environment (development, test, production)
4. **Secure Storage**: Never commit API keys to source control; always use environment variables or Azure Key Vault
5. **Access Control**: Limit who has access to API keys, especially production keys

### Generating Secure API Keys

For improved security, use a secure random generator to create API keys. For example:

```bash
# Generate a secure API key using OpenSSL (recommended for production)
openssl rand -base64 24

# Alternative using PowerShell
[Convert]::ToBase64String((New-Object Security.Cryptography.RNGCryptoServiceProvider).GetBytes(32))
```

### Example API Call

```bash
# Example using curl to call an API function with the API key header
curl -X POST https://{{AzureWebLink}}.azurewebsites.net/authors/some-author-slug \
  -H "Content-Type: application/json" \
  -H "x-api-key: X_API_ENVIRONMENT_KEY" \
  -d '{"name": "Author Name", "bio": "Author Bio"}'
```

### Bypassing Function Keys Issue

This approach bypasses the issue with function keys in the development and test environments. The custom API key validation is implemented in `ApiKeyValidator.cs` and is used by all functions that require authentication.

### Function Authorization Levels

All functions are now configured with `AuthorizationLevel.Anonymous` to avoid issues with function keys. However, the application's custom API key validation is applied consistently to functions that require authentication, providing a uniform security model.

The API key validation is performed in the function code for all protected endpoints, so you must provide the `x-api-key` header for these functions to authenticate your requests.

## Troubleshooting

If you receive a 401 Unauthorized response with a message about an invalid API key, check:

1. That you're including the `x-api-key` header in your request
2. That the API key value matches the `X_API_ENVIRONMENT_KEY` environment variable for the target environment
3. That you're calling the correct function endpoint for the environment you're targeting
