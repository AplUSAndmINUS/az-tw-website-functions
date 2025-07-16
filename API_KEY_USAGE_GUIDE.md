# API Key Usage Guide

Due to issues with function keys in the development and test environments, all authenticated API calls should use the custom API key validation mechanism instead of relying on function keys.

## API Key Authentication

The application uses a custom API key validation mechanism that checks for the `x-api-key` header against a configured environment variable.

### How to Authenticate API Calls

1. Include the `x-api-key` header in your HTTP requests
2. Use the appropriate API key for each environment:
   - Development: `az-tw-DEV-website-api-key-9874`
   - Test: Check environment variable `X_API_ENVIRONMENT_KEY` in the test environment
   - Production: Check environment variable `X_API_ENVIRONMENT_KEY` in the production environment

### Example API Call

```bash
# Example using curl to call an API function with the API key header
curl -X POST https://az-tw-website-develop.azurewebsites.net/authors/some-author-slug \
  -H "Content-Type: application/json" \
  -H "x-api-key: YOUR-API-KEY" \
  -d '{"name": "Author Name", "bio": "Author Bio"}'
```

### Bypassing Function Keys Issue

This approach bypasses the issue with function keys in the development and test environments. The custom API key validation is implemented in `ApiKeyValidator.cs` and is used by all functions that require authentication.

### Function Authorization Levels

While some functions are configured with `AuthorizationLevel.Function` and others with `AuthorizationLevel.Anonymous`, the application's custom API key validation is applied consistently to functions that require authentication, regardless of their authorization level.

For functions with `AuthorizationLevel.Anonymous`, the API key validation is still performed in the function code, so you still need to provide the `x-api-key` header for these functions if they implement the validation.

## Troubleshooting

If you receive a 401 Unauthorized response with a message about an invalid API key, check:

1. That you're including the `x-api-key` header in your request
2. That the API key value matches the `X_API_ENVIRONMENT_KEY` environment variable for the target environment
3. That you're calling the correct function endpoint for the environment you're targeting
