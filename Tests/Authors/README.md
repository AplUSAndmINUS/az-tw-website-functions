# CreateAuthor Integration Test

This directory contains integration tests for the CreateAuthor Azure Function that test against real DEV storage.

## Overview

The integration test validates the complete CreateAuthor function workflow:

1. **Input Validation** - Tests both valid and invalid author data
2. **API Key Authentication** - Uses real DEV API key for authentication
3. **Storage Integration** - Creates authors in real DEV Azure Table Storage
4. **Response Validation** - Verifies correct HTTP status codes and headers
5. **Data Persistence** - Confirms the author is actually saved in storage

## Setup

Before running the integration tests, you need to configure these environment variables:

### Required Environment Variables

```bash
# Your DEV Azure Storage Account name
export StorageAccountName="your-dev-storage-account"

# Your DEV API key for the functions
export X_API_ENVIRONMENT_KEY="your-dev-api-key"

# Optional: Table name (defaults to "authors" if not set)
export AUTHORS_TABLE_NAME="authors"
```

### Setting Up Environment Variables

**Option 1: Export in your shell**

```bash
export StorageAccountName="aztwwebsitestorage"
export X_API_ENVIRONMENT_KEY="your-secret-key-here"
```

**Option 2: Create a .env file (not tracked in git)**

```bash
# Create .env file in the Tests directory
echo "StorageAccountName=aztwwebsitestorage" > .env
echo "X_API_ENVIRONMENT_KEY=your-secret-key-here" >> .env
```

## Running the Tests

### Method 1: Console Application (Recommended for debugging)

The console application provides detailed output and is easy to debug:

```bash
cd /Users/terencewaters/source/repos/az-tw-website-functions/Tests
dotnet run
```

### Method 2: XUnit Test Runner

Run as a normal unit test (will be skipped if environment variables are not set):

```bash
cd /Users/terencewaters/source/repos/az-tw-website-functions/Tests
dotnet test --filter "CreateAuthor_IntegrationTest_ShouldPassAllTests"
```

### Method 3: Visual Studio Code

1. Open the test file `CreateAuthorIntegrationXunitTest.cs`
2. Ensure environment variables are set in your terminal
3. Use the "Run Test" button in VS Code

## What the Test Does

### Test 1: Valid Author Creation

-   Creates an author with valid data
-   Verifies HTTP 201 (Created) response
-   Checks that Location header is set correctly
-   Confirms the author exists in DEV storage
-   Automatically cleans up the test data

### Test 2: Invalid Author Validation

-   Attempts to create an author with invalid data (empty fields, invalid email, etc.)
-   Verifies HTTP 400 (Bad Request) response
-   Confirms validation is working correctly

## Test Data Management

The integration test:

-   Generates unique usernames for each test run to avoid conflicts
-   Automatically cleans up test data after completion
-   Uses the pattern: `testuser_{guid}_{timestamp}`

## Troubleshooting

### Common Issues

**"StorageAccountName not configured"**

-   Ensure the `StorageAccountName` environment variable is set
-   Check that your storage account name is correct

**"X_API_ENVIRONMENT_KEY not configured"**

-   Ensure the `X_API_ENVIRONMENT_KEY` environment variable is set
-   Verify your API key is valid for your DEV environment

**"Authentication failed"**

-   Double-check your API key
-   Ensure your DEV environment is configured to accept the API key

**"Table/Storage errors"**

-   Verify your storage account exists and is accessible
-   Check that the table storage service is running
-   Ensure your Azure credentials are configured correctly

### Debug Mode

The console application provides detailed logging. Look for:

-   `✅` Green checkmarks for successful operations
-   `❌` Red X marks for failures
-   Detailed error messages and stack traces

## Integration vs Unit Tests

This is an **integration test**, not a unit test because it:

-   Uses real Azure storage (not mocked)
-   Tests the complete function workflow
-   Requires actual environment configuration
-   Has external dependencies

For pure unit tests with mocked dependencies, see `CreateAuthorTest.cs`.

## Security Note

⚠️ **Never commit environment variables or API keys to source control!**

The environment variables contain sensitive information and should only be set locally or in secure CI/CD pipelines.
