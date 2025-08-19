# Azure Website Functions API

A .NET 8.0 serverless application providing REST endpoints for managing website content including Authors, BlogPosts, Books, Portfolio pieces, GitHub integration, Contact forms, and Media management.

## Features

-   Content Management API (Authors, Blog Posts, Books, Portfolio)
-   Media Management (Upload, Associate, Delete)
-   GitHub Integration (Repos, Activity Grid)
-   Contact Form Handling
-   Azure Storage Integration (Tables, Blobs)
-   Key Vault Secrets Management
-   Application Insights Telemetry

## Project Structure

-   `src/Functions/` - Main Azure Functions application (.NET 8.0)
-   `SharedStorage/` - Shared storage services and models
-   `Utils/` - Utility classes and helpers
-   `docs/functions/` - API documentation

## Local Development Setup

### Prerequisites

-   .NET 8.0 SDK
-   Azure Functions Core Tools v4
-   Azure Storage Emulator or Azurite (optional for mock storage)

### Configuration

1. Clone the repository
2. Create a `local.settings.json` file in `src/Functions/` based on `local.example.settings.json`
3. Update required settings:

```json
{
    "IsEncrypted": false,
    "Values": {
        "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
        "USE_MOCK_STORAGE": "true",
        "X_API_ENVIRONMENT_KEY": "test-api-key",
        "PROXY_ALLOWED_ORIGINS": "http://localhost:3000,http://localhost:7071",
        "PROXY_ALLOWED_IP_RANGES": "*",
        "PROXY_ENABLE_DETAILED_LOGGING": "true"
    }
}
```

### Build and Run

```powershell
# Restore dependencies
dotnet restore

# Build Functions project
dotnet build src/Functions/Functions.csproj

# Run Functions locally
cd src/Functions
func start
```

## API Authentication

All endpoints require API key authentication via `x-api-key` header:

-   Local development: Use value from `X_API_ENVIRONMENT_KEY` setting
-   Production: API keys are retrieved from Azure Key Vault

## GitHub Integration

To enable GitHub activity grid with real contribution data:

1. Add to `local.settings.json` or Azure Application Settings:

```json
{
    "GITHUB_TOKEN": "your_personal_access_token",
    "GITHUB_USERNAME": "your_github_username"
}
```

1. Token requires minimum permissions:
    - `read:user` - Access to user profile and contribution data
    - `user:email` - Required for private contributions

## Contact Form Setup

Requires SMTP configuration:

```json
{
    "FROM_EMAIL": "your_email@example.com",
    "FROM_NAME": "Website Contact Form",
    "SMTP_PORT": "587",
    "SMTP_SERVER": "your.smtp.server",
    "SMTP_USERNAME": "username",
    "SMTP_PASSWORD": "password",
    "TO_EMAIL": "recipient@example.com"
}
```

## Proxy Configuration

The application includes an API proxy for enhanced security:

-   Front-end adds `/proxy/` to API calls
-   Proxy handles API key authentication
-   Set these environment variables:

```json
{
    "PROXY_ALLOWED_ORIGINS": "your-website-domains-comma-separated",
    "PROXY_ALLOWED_IP_RANGES": "*",
    "PROXY_ENABLE_DETAILED_LOGGING": "true",
    "PROXY_BASE_URL": "https://your-function-app-url"
}
```

## Deployment

Deployed as Azure Functions App with:

-   Authentication via Key Vault
-   Storage via Azure Storage
-   Logging via Application Insights

For detailed API documentation, see the [Functions Documentation](./docs/functions/README.md).
