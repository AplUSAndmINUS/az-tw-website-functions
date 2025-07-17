# GitHub Azure Functions

This module provides Azure Functions for integrating with GitHub data, following the established patterns from the BlogPosts implementation.

## Functions

### 1. GetGitHubRepos (Timer Trigger)
- **Schedule**: Every 4 hours (`"0 0 */4 * * *"`)
- **Purpose**: Synchronizes GitHub repositories from the configured GitHub account
- **Environment Variables**:
  - `GITHUB_USERNAME`: GitHub username to sync (defaults to "AplUSAndmINUS")
  - `USE_MOCK_STORAGE`: Set to "true" for development/testing to use "mockgithubrepos" table

### 2. GetGitHubReposTable (HTTP GET)
- **Route**: `/github/repos`
- **Purpose**: Retrieve GitHub repositories with filtering options
- **Query Parameters**:
  - `category`: Filter by repository category (optional)
  - `limit`: Maximum number of results (optional)
  - `isPublished`: Filter by published status (default: true)
- **Authentication**: Requires valid API key

### 3. GetGitHubRepo (HTTP GET)
- **Route**: `/github/repos/{slug}`
- **Purpose**: Retrieve a specific GitHub repository by slug
- **Parameters**:
  - `slug`: Repository slug identifier
- **Query Parameters**:
  - `isPublished`: Filter by published status (default: true)
- **Authentication**: Requires valid API key

### 4. GetGitHubRepoByGitHubId (HTTP GET)
- **Route**: `/github/repos/githubid/{githubId}`
- **Purpose**: Retrieve a specific GitHub repository by GitHub ID
- **Parameters**:
  - `githubId`: GitHub repository ID (long)
- **Authentication**: Requires valid API key

### 5. GetGitHubActivityGrid (HTTP GET)
- **Route**: `/github/activity`
- **Purpose**: Retrieve GitHub activity grid data (contribution calendar)
- **Query Parameters**:
  - `username`: GitHub username (optional, defaults to environment variable)
- **Authentication**: Requires valid API key
- **Note**: Currently returns empty data - requires GitHub GraphQL API implementation

## Data Models

### GitHubRepoDTO
Represents a GitHub repository in API responses:
```json
{
  "id": "string",
  "gitHubId": 12345,
  "name": "repository-name",
  "fullName": "username/repository-name",
  "description": "Repository description",
  "htmlUrl": "https://github.com/username/repository-name",
  "language": "C#",
  "stargazersCount": 10,
  "forksCount": 5,
  "watchersCount": 3,
  "openIssuesCount": 2,
  "isPrivate": false,
  "isFork": false,
  "isArchived": false,
  "gitHubCreatedAt": "2023-01-01T00:00:00Z",
  "gitHubUpdatedAt": "2024-01-01T00:00:00Z",
  "gitHubPushedAt": "2024-01-01T00:00:00Z",
  "defaultBranch": "main",
  "topics": ["azure", "functions"],
  "lastModified": "2024-01-01T00:00:00Z",
  "slug": "repository-name",
  "category": "repository"
}
```

### GitHubActivityGridDTO
Represents GitHub activity data:
```json
{
  "date": "2024-01-01",
  "contributionCount": 5,
  "contributionLevel": "SECOND_QUARTILE"
}
```

## Storage

Data is stored in Azure Table Storage:
- **Development/Test**: `mockgithubrepos` table
- **Production**: `githubrepos` table

The table naming is controlled by the `USE_MOCK_STORAGE` environment variable.

## Services

### GitHubApiService
- Handles external GitHub REST API calls
- Fetches repository data from GitHub API
- Maps GitHub API responses to internal models

### GitHubRepoService
- Extends the base `ContentService` pattern
- Provides CRUD operations for GitHub repositories
- Handles synchronization from GitHub API
- Manages table storage operations

## Configuration

### Environment Variables
- `GITHUB_USERNAME`: GitHub account to sync repositories from
- `USE_MOCK_STORAGE`: Use mock storage tables for development
- `StorageAccountName` or `AZURE_STORAGE_ACCOUNT_NAME`: Azure Storage account
- `X_API_ENVIRONMENT_KEY`: API key for function authentication

### Logging
GitHub functions use the logging namespace `Functions.GitHub` with trace-level logging enabled.

## Dependencies

- Follows existing patterns from BlogPosts implementation
- Uses Managed Identity for Azure services
- Integrates with existing validation and storage infrastructure
- Timer trigger requires `Microsoft.Azure.Functions.Worker.Extensions.Timer` package