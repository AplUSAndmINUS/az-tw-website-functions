# Azure Functions Website API

Azure Functions website API is a .NET 8.0 serverless application providing REST endpoints for managing website content including Authors, BlogPosts, Books, Portfolio pieces, GitHub integration, Contact forms, and Media management. The application uses Azure Storage (Tables, Blobs), Key Vault for secrets management, and Application Insights for logging.

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Project Architecture

This application follows a modular architecture with strict separation of concerns:

-   **Functions Layer** (`src/Functions/`): API endpoints and HTTP triggers only
-   **SharedStorage Layer** (`SharedStorage/`): Data access and storage services
-   **Utils Layer** (`Utils/`): Common utilities, logging, and cross-cutting concerns

**CRITICAL**: Functions must NOT modify the underlying structure of SharedStorage and Utils layers. These components are shared across multiple services and changing their interfaces will break other dependent functions.

## Working Effectively

-   Bootstrap, build, and test the repository:
    -   `dotnet restore` -- takes 2 minutes. NEVER CANCEL. Set timeout to 5+ minutes.
    -   `dotnet build src/Functions/Functions.csproj` -- takes 2 minutes. NEVER CANCEL. Set timeout to 5+ minutes.
    -   `dotnet publish src/Functions/Functions.csproj -c Release -o ./publish` -- takes 3 minutes. NEVER CANCEL. Set timeout to 10+ minutes.
-   Run the application locally:
    -   ALWAYS run the build steps first.
    -   Install Azure Functions Core Tools: `sudo apt-get update && sudo apt-get install -y azure-functions-core-tools-4`
    -   Server: Navigate to `./publish` directory, set environment variables, then `func start --port 7071` -- takes 2 minutes to start. NEVER CANCEL. Set timeout to 5+ minutes.
-   DO NOT attempt to build the root solution or Tests project -- they contain compilation errors. Only build the Functions project.
-   DO NOT run unit tests -- the test project has compilation errors and cannot be built successfully.

### Windows Environment

For Windows environments, use PowerShell commands:

```powershell
# Install Azure Functions Core Tools (if not already installed)
# Using Chocolatey
choco install azure-functions-core-tools --version=4.0.5198

# Set environment variables for local development
$env:USE_MOCK_STORAGE = "true"
$env:StorageAccountName = "mock-storage"
$env:X_API_ENVIRONMENT_KEY = "test-api-key"

# Navigate to publish directory and start
cd ./publish
func start --port 7071
```

## Validation

-   Always manually validate any new code by building and running the Functions application locally.
-   ALWAYS run through at least one complete API endpoint test after making changes.
-   Test API endpoints using curl with proper authentication headers: `curl -H "x-api-key: YOUR_API_KEY" http://localhost:7071/ENDPOINT`
-   Key endpoints to test: `/ping` (health check), `/authors`, `/books`, `/posts`, `/portfolio`, `/media/images`
-   You can build and run the application locally, but authentication will fail with mock credentials
-   _Note:_ This is expected behavior: credentials are stored in Azure Key Vault and are not integrated into the project. User will have to test these while running locally with proper credentials.
-   Always run the application startup validation to ensure no compilation errors were introduced.

## Pull Request Validation

When creating Pull Requests, ensure all of the following checks pass:

1. **Build Validation**:

    - All PRs must build successfully without errors
    - Zero compiler warnings in modified code
    - Nullable reference warnings must be addressed

2. **Test Results Documentation**:

    - Document any API test results in the PR description
    - Include screenshots of successful API calls where applicable
    - Provide curl commands used for testing

3. **PR Template Checklist**:
    - [ ] Fixed all compiler warnings
    - [ ] No changes to SharedStorage/Utils interfaces
    - [ ] Tested locally with mock storage
    - [ ] API endpoint tests pass
    - [ ] Code follows existing patterns and practices
    - [ ] Added logging for new failure cases

## Common Tasks

The following are outputs from frequently run commands. Reference them instead of viewing, searching, or running bash commands to save time.

### Repository Root

```
ls -la [repo-root]
.git
.github
.gitignore
.prettierrc
.vscode
Directory.Build.props
MediaTests
Properties
SharedStorage
Tests
Utils
az-tw-website-functions.csproj
az-tw-website-functions.sln
create-test-images.sh
generate-media-test-report.ps1
run-media-tests.ps1
src
test-api-endpoints.sh
test-image-upload.ps1
test-image-upload.sh
```

### Key Project Structure

-   `src/Functions/` - Main Azure Functions application (.NET 8.0)
-   `SharedStorage/` - Shared storage services and models
-   `Utils/` - Utility classes and helpers
-   `Tests/` - Unit and integration tests (DO NOT BUILD - contains errors)
-   `MediaTests/` - Media processing tests (part of main project, causes build errors)

### Build Commands That Work

```bash
# Restore dependencies (2 minutes)
dotnet restore

# Build Functions project only (2 minutes)
dotnet build src/Functions/Functions.csproj

# Build in Release mode
dotnet build src/Functions/Functions.csproj -c Release

# Publish for deployment (3 minutes)
dotnet publish src/Functions/Functions.csproj -c Release -o ./publish
```

### Local Development Setup

```bash
# Install Azure Functions Core Tools (if not already installed)
sudo apt-get update
sudo apt-get install -y azure-functions-core-tools-4

# Set environment variables for local development
export USE_MOCK_STORAGE=true
export StorageAccountName=mock-storage
export X_API_ENVIRONMENT_KEY=test-api-key

# Navigate to publish directory and start
cd ./publish
func start --port 7071
```

### Expected Function Endpoints

When running locally, the application exposes these key endpoints:

-   Health: `GET /ping`
-   Authors: `GET /authors`, `PUT /authors/{slug}`, `DELETE /authors/{slug}`
-   Books: `GET /books`, `PUT /books/{slug}`, `DELETE /books/{slug}`
-   Blog Posts: `GET /posts`, `PUT /posts/{slug}`, `DELETE /posts/{slug}`
-   Portfolio: `GET /portfolio`, `PUT /portfolio/{slug}`, `DELETE /portfolio/{slug}`
-   Media: `GET /media`, `POST /media/images`, `DELETE /media/{mediaId}`
-   Contact: `POST /contact`
-   GitHub: `GET /github/repos`, `GET /github/activity`

### Test Scripts Available

-   `test-api-endpoints.sh` - Comprehensive API testing script
-   `create-test-images.sh` - Creates minimal test images
-   `test-image-upload.sh` - Tests media upload functionality

## Build and Deployment

### CI/CD Pipeline

The application uses GitHub Actions for automated deployment:

-   Builds: `.github/workflows/azure-functions-app-dotnet.yml`
-   Deploys to: Azure Functions (Flex Consumption plan)
-   Environments: develop, test, master branches

### Build Timing Expectations

-   `dotnet restore`: ~2 minutes (NEVER CANCEL, set 5+ minute timeout)
-   `dotnet build`: ~2 minutes (NEVER CANCEL, set 5+ minute timeout)
-   `dotnet publish`: ~3 minutes (NEVER CANCEL, set 10+ minute timeout)
-   Function app startup: ~2 minutes (NEVER CANCEL, set 5+ minute timeout)

### Build Failures and Workarounds

-   DO NOT build `az-tw-website-functions.csproj` (root project) -- contains test files with missing dependencies
-   DO NOT build `Tests/Function.Tests.csproj` -- contains compilation errors in media processing tests
-   DO NOT run `dotnet test` -- test project cannot be built
-   ONLY build `src/Functions/Functions.csproj` which builds successfully

### Common Build Errors and Fixes

#### Duplicate Assembly Attributes

If you encounter errors about duplicate assembly attributes:

1. Create a `Directory.Build.props` file at the solution root:

```xml
<Project>
  <PropertyGroup>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
    <GenerateAssemblyVersionAttribute>false</GenerateAssemblyVersionAttribute>
    <GenerateAssemblyFileVersionAttribute>false</GenerateAssemblyFileVersionAttribute>
    <GenerateAssemblyInformationalVersionAttribute>false</GenerateAssemblyInformationalVersionAttribute>
    <GenerateAssemblyTitleAttribute>false</GenerateAssemblyTitleAttribute>
    <GenerateAssemblyDescriptionAttribute>false</GenerateAssemblyDescriptionAttribute>
    <GenerateAssemblyConfigurationAttribute>false</GenerateAssemblyConfigurationAttribute>
    <GenerateAssemblyCompanyAttribute>false</GenerateAssemblyCompanyAttribute>
    <GenerateAssemblyProductAttribute>false</GenerateAssemblyProductAttribute>
    <GenerateAssemblyCopyrightAttribute>false</GenerateAssemblyCopyrightAttribute>
  </PropertyGroup>
</Project>
```

2. Update `src/Functions/Functions.csproj` to add:

```xml
<PropertyGroup>
  <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
</PropertyGroup>
```

#### Nullable Reference Warnings

For nullable reference warnings:

1. Use the null-coalescing operator (`??`) or null conditional operators (`?.`)
2. Add appropriate null checks before operations
3. For interfaces that must return non-null values, throw exceptions rather than returning null:

```csharp
// INCORRECT - returns null which violates non-nullable return type
return fileExtension == ".unknown" ? null : Image.Load(imageData);

// CORRECT - throws exception for invalid cases
if (fileExtension == ".unknown")
    throw new InvalidOperationException($"Unsupported file extension: {fileExtension}");
return Image.Load(imageData);
```

#### Logger Parameter Order

Ensure correct parameter order in log methods:

```csharp
// INCORRECT
_logger.LogError(ex, "Error message with {0}", param1);

// CORRECT
_logger.LogError("Error message with {0}", param1, ex);
```

### Authentication and Configuration

-   Local development requires API key authentication via `x-api-key` header
-   Production uses Azure Key Vault for secret management
-   Storage authentication via Azure Managed Identity
-   Application Insights for telemetry and logging

## API Integration and GitHub Configuration

### GitHub API Integration

For GitHub integration features to work properly:

1. The GitHub token must have these permissions:
    - `read:user` - Required for accessing contribution data
    - `user:email` - Required for private contributions
2. Configure the token in one of two ways:

    - Local development: Add to `local.settings.json`

    ```json
    {
        "Values": {
            "GITHUB_TOKEN": "your_personal_access_token",
            "GITHUB_USERNAME": "your_github_username"
        }
    }
    ```

    - Production: Store in Azure Key Vault

    ```powershell
    # Using Azure CLI
    az keyvault secret set --vault-name <your-key-vault> --name "GITHUB-TOKEN" --value "your_personal_access_token"

    # Grant Function App access to Key Vault
    az keyvault set-policy --name <your-key-vault> --object-id <function-app-managed-identity-id> --secret-permissions get list

    # Configure Function App to use Key Vault reference
    az functionapp config appsettings set --name <function-app-name> --resource-group <resource-group> --settings "GITHUB_TOKEN=@Microsoft.KeyVault(SecretUri=https://<your-key-vault>.vault.azure.net/secrets/GITHUB-TOKEN)"
    ```

## Technology Stack

-   .NET 8.0 / C#
-   Azure Functions v4 (Isolated Worker Model)
-   Azure Storage (Tables, Blobs)
-   Azure Key Vault
-   Application Insights
-   SixLabors.ImageSharp (image processing)
-   xUnit (testing framework - not functional due to compilation errors)

## Important Notes

-   The application compiles and runs successfully when built correctly
-   Test infrastructure exists but has compilation errors - DO NOT attempt to run tests
-   All API endpoints require authentication with valid API key
-   Mock storage mode available for local development (`USE_MOCK_STORAGE=true`)
-   Media processing includes image conversion and thumbnail generation
-   Comprehensive error handling and logging throughout

## Code Quality Guidelines

### Error Handling

-   Always use structured exception handling (try/catch) for API endpoints
-   Log all exceptions using `_logger.LogError()` with correct parameter order
-   Return appropriate status codes (400 for invalid input, 404 for not found, 500 for server errors)
-   Do not expose internal exception details in API responses

### Logging

-   Use the appropriate log level for each situation:
    -   `LogTrace`: Detailed debugging information
    -   `LogDebug`: Development-time debugging
    -   `LogInformation`: Standard operational events
    -   `LogWarning`: Non-critical issues
    -   `LogError`: Errors that need investigation
    -   `LogCritical`: System failure requiring immediate attention
-   Include request identifiers in logs for correlation
-   Use structured logging with named parameters: `_logger.LogInformation("Processing {ItemType} with ID {ItemId}", type, id)`

### Code Maintainability

-   Keep Functions focused on HTTP handling and orchestration only
-   Defer business logic to SharedStorage services
-   Use dependency injection for all services
-   Do not modify interfaces in SharedStorage or Utils layers
-   Keep methods small and focused on a single responsibility
-   Use appropriate access modifiers (private, internal, public)
-   Write self-documenting code with descriptive naming
