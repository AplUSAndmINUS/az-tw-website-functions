# Azure Functions Website API

Azure Functions website API is a .NET 8.0 serverless application providing REST endpoints for managing website content including Authors, BlogPosts, Books, Portfolio pieces, GitHub integration, Contact forms, and Media management. The application uses Azure Storage (Tables, Blobs), Key Vault for secrets management, and Application Insights for logging.

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Working Effectively

- Bootstrap, build, and test the repository:
  - `dotnet restore` -- takes 2 minutes. NEVER CANCEL. Set timeout to 5+ minutes.
  - `dotnet build src/Functions/Functions.csproj` -- takes 2 minutes. NEVER CANCEL. Set timeout to 5+ minutes.
  - `dotnet publish src/Functions/Functions.csproj -c Release -o ./publish` -- takes 3 minutes. NEVER CANCEL. Set timeout to 10+ minutes.
- Run the application locally:
  - ALWAYS run the build steps first.
  - Install Azure Functions Core Tools: `sudo apt-get update && sudo apt-get install -y azure-functions-core-tools-4`
  - Server: Navigate to `./publish` directory, set environment variables, then `func start --port 7071` -- takes 2 minutes to start. NEVER CANCEL. Set timeout to 5+ minutes.
- DO NOT attempt to build the root solution or Tests project -- they contain compilation errors. Only build the Functions project.
- DO NOT run unit tests -- the test project has compilation errors and cannot be built successfully.

## Validation

- Always manually validate any new code by building and running the Functions application locally.
- ALWAYS run through at least one complete API endpoint test after making changes.
- Test API endpoints using curl with proper authentication headers: `curl -H "x-api-key: YOUR_API_KEY" http://localhost:7071/ENDPOINT`
- Key endpoints to test: `/ping` (health check), `/authors`, `/books`, `/posts`, `/portfolio`, `/media/images`
- You can build and run the application locally, but authentication will fail with mock credentials (expected behavior).
- Always run the application startup validation to ensure no compilation errors were introduced.

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
- `src/Functions/` - Main Azure Functions application (.NET 8.0)
- `SharedStorage/` - Shared storage services and models
- `Utils/` - Utility classes and helpers
- `Tests/` - Unit and integration tests (DO NOT BUILD - contains errors)
- `MediaTests/` - Media processing tests (part of main project, causes build errors)

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
- Health: `GET /ping`
- Authors: `GET /authors`, `PUT /authors/{slug}`, `DELETE /authors/{slug}`
- Books: `GET /books`, `PUT /books/{slug}`, `DELETE /books/{slug}`
- Blog Posts: `GET /posts`, `PUT /posts/{slug}`, `DELETE /posts/{slug}`
- Portfolio: `GET /portfolio`, `PUT /portfolio/{slug}`, `DELETE /portfolio/{slug}`
- Media: `GET /media`, `POST /media/images`, `DELETE /media/{mediaId}`
- Contact: `POST /contact`
- GitHub: `GET /github/repos`, `GET /github/activity`

### Test Scripts Available
- `test-api-endpoints.sh` - Comprehensive API testing script
- `create-test-images.sh` - Creates minimal test images
- `test-image-upload.sh` - Tests media upload functionality

## Build and Deployment

### CI/CD Pipeline
The application uses GitHub Actions for automated deployment:
- Builds: `.github/workflows/azure-functions-app-dotnet.yml`
- Deploys to: Azure Functions (Flex Consumption plan)
- Environments: develop, test, master branches

### Build Timing Expectations
- `dotnet restore`: ~2 minutes (NEVER CANCEL, set 5+ minute timeout)
- `dotnet build`: ~2 minutes (NEVER CANCEL, set 5+ minute timeout)  
- `dotnet publish`: ~3 minutes (NEVER CANCEL, set 10+ minute timeout)
- Function app startup: ~2 minutes (NEVER CANCEL, set 5+ minute timeout)

### Build Failures and Workarounds
- DO NOT build `az-tw-website-functions.csproj` (root project) -- contains test files with missing dependencies
- DO NOT build `Tests/Function.Tests.csproj` -- contains compilation errors in media processing tests
- DO NOT run `dotnet test` -- test project cannot be built
- ONLY build `src/Functions/Functions.csproj` which builds successfully

### Authentication and Configuration
- Local development requires API key authentication via `x-api-key` header
- Production uses Azure Key Vault for secret management
- Storage authentication via Azure Managed Identity
- Application Insights for telemetry and logging

## Technology Stack
- .NET 8.0 / C#
- Azure Functions v4 (Isolated Worker Model)
- Azure Storage (Tables, Blobs)
- Azure Key Vault
- Application Insights
- SixLabors.ImageSharp (image processing)
- xUnit (testing framework - not functional due to compilation errors)

## Important Notes
- The application compiles and runs successfully when built correctly
- Test infrastructure exists but has compilation errors - DO NOT attempt to run tests
- All API endpoints require authentication with valid API key
- Mock storage mode available for local development (`USE_MOCK_STORAGE=true`)
- Media processing includes image conversion and thumbnail generation
- Comprehensive error handling and logging throughout