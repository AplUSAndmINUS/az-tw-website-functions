# Azure Function Authorization Guide

## Understanding Authentication in Azure Functions

Azure Functions has two layers of authentication that can be confusing:

1. **Function Authorization Level**: This is built into the Azure Functions platform and controlled by the `AuthorizationLevel` enum in your HTTP trigger attribute.
2. **Custom API Key Validation**: This is your custom implementation in the `ApiKeyValidator.cs` class that checks for the `x-api-key` header.

## Function Authorization Levels

In your function declaration, you use:
```csharp
[HttpTrigger(AuthorizationLevel.Function, "put", Route = "authors/{slug}")]
```

This means the Azure Functions runtime requires a valid function key before your custom code even runs.

### Available Authorization Levels

- **Anonymous**: No key required
- **Function**: Requires a function-specific key or master key
- **Admin**: Requires a master key
- **User**: Requires user authentication through Easy Auth
- **System**: Used for internal Azure Functions system calls

## How to Get Your Function Keys

### Using Azure Portal

1. Go to your Function App (e.g., az-tw-website-develop)
2. Click on the specific function (e.g., UpsertAuthorAsync)
3. Go to "Function Keys" in the left menu
4. Copy the default key or create a new one

### Using Azure CLI

```bash
# Get function-specific keys
az functionapp function keys list --name <function-app-name> --resource-group <resource-group> --function-name <function-name>

# Get host keys (work for all functions)
az functionapp keys list --name <function-app-name> --resource-group <resource-group>
```

## How to Use Function Keys in Requests

### As a Query Parameter

```
https://<function-app>.azurewebsites.net/authors/john-doe?code=YOUR_FUNCTION_KEY
```

### As a Header

```
x-functions-key: YOUR_FUNCTION_KEY
```

## Sending Both Authentication Keys

For your current setup, you need to send:

1. `x-functions-key: YOUR_FUNCTION_KEY` - For Azure Functions authorization
2. `x-api-key: YOUR_API_KEY` - For your custom API key validation

## Best Practices

- For production: Use `AuthorizationLevel.Function` and proper key management
- For development: Use `AuthorizationLevel.Anonymous` locally, and rely on your custom API key validation
- For testing: Create a specific function key for automated tests
- Always keep your keys secure and never commit them to source control

## Postman Settings

When testing with Postman, add both headers:

```
x-functions-key: YOUR_FUNCTION_KEY
x-api-key: YOUR_API_KEY
```
