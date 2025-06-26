# Comprehensive Testing Framework - Azure Functions Backend

## Overview

This document describes the comprehensive testing framework implemented for the Azure Functions backend, covering unit tests, integration tests, and testing strategies for all major components.

## Testing Structure

### 1. Unit Tests

#### BlogPostServiceTests (`Tests/BlogPosts/BlogPostServiceTests.cs`)

-   **Purpose**: Tests the BlogPostService business logic in isolation
-   **Framework**: XUnit with Moq for mocking dependencies
-   **Coverage**:
    -   `UpsertBlogPostAsync` - Valid DTO, null DTO, empty slug scenarios
    -   `GetBlogPostAsync` - Valid slug, empty slug scenarios
    -   `GetBlogPostsAsync` - No filters, filtered by published status, filtered by category
    -   `DeleteBlogPostAsync` - Valid slug, empty slug scenarios

#### MediaServiceTests (`Tests/Media/MediaServiceTests.cs`)

-   **Purpose**: Tests the MediaService business logic in isolation
-   **Framework**: XUnit with Moq for mocking dependencies
-   **Coverage**:
    -   `UploadMediaAsync` - Valid image, null file data, empty filename, unsupported content type
    -   `GetMediaAsync` - Valid ID, empty ID scenarios
    -   `GetMediaBatchAsync` - Valid IDs scenario
    -   `DeleteMediaAsync` - Valid ID, empty ID scenarios
    -   `DeleteMediaBatchAsync` - Valid IDs scenario

### 2. Integration Tests

#### AuthorIntegrationTest (`Tests/Authors/CreateAuthorIntegrationTest.cs`)

-   **Purpose**: Tests the complete Author workflow against real Azure storage
-   **Coverage**: Author creation, validation, and cleanup

#### BlogPostIntegrationTest (`Tests/BlogPosts/BlogPostIntegrationTest.cs`)

-   **Purpose**: Tests the complete BlogPost workflow against real Azure storage
-   **Coverage**:
    -   Create blog post with full DTO
    -   Retrieve and validate blog post data
    -   Update blog post content
    -   Verify update persistence
    -   List blog posts with filtering
    -   Delete blog post
    -   Verify deletion

#### MediaIntegrationTestV2 (`Tests/Media/MediaIntegrationTestV2.cs`)

-   **Purpose**: Tests the complete Media workflow against real Azure storage
-   **Coverage**:
    -   Upload test image (JPEG format)
    -   Retrieve and validate uploaded media
    -   Upload second image for batch operations
    -   Batch retrieve multiple media items
    -   Delete individual media item
    -   Batch delete remaining media items
    -   Verify all deletions

### 3. Comprehensive Test Runner

#### ComprehensiveIntegrationTestRunner (`Tests/ComprehensiveIntegrationTestRunner.cs`)

-   **Purpose**: Orchestrates all integration tests in a single execution
-   **Features**:
    -   Environment variable validation
    -   Sequential execution of all test suites
    -   Comprehensive reporting with pass/fail counts
    -   Detailed logging with emoji indicators
    -   Proper exit codes for CI/CD integration

## Test Execution

### Prerequisites

Set the following environment variables:

```bash
export AZURE_STORAGE_ACCOUNT_NAME="your-dev-storage-account"
export X_API_ENVIRONMENT_KEY="your-dev-api-key"
```

### Running Tests

#### All Integration Tests

```bash
cd Tests
dotnet run --project ComprehensiveIntegrationTestRunner.cs
```

#### Individual Unit Tests

```bash
cd Tests
dotnet test --filter "BlogPostServiceTests"
dotnet test --filter "MediaServiceTests"
```

#### Individual Integration Tests

```bash
cd Tests
dotnet run --project BlogPosts/BlogPostIntegrationTest.cs
dotnet run --project Media/MediaIntegrationTestV2.cs
```

## Test Data Management

### Naming Convention

-   All test data uses timestamp-based prefixes: `test-{yyyyMMdd-HHmmss}`
-   Ensures unique test data across concurrent executions
-   Facilitates easy identification and cleanup

### Cleanup Strategy

-   **Integration Tests**: Automatic cleanup after each test
-   **Unit Tests**: No cleanup needed (uses mocks)
-   **Failed Tests**: Manual cleanup may be required for integration tests

## Mocking Strategy

### MockAppInsightsLogger

-   **Location**: `Tests/Helpers/TestFactory.cs`
-   **Purpose**: Provides no-op logging for tests
-   **Usage**: Used across all services to prevent logging dependency issues

### Service Mocking

-   **Unit Tests**: Mock all external dependencies (storage, media handlers)
-   **Integration Tests**: Use real Azure services with test data
-   **Hybrid Approach**: Some tests mock conversion services for speed

## CI/CD Integration

### Exit Codes

-   **0**: All tests passed
-   **1**: One or more tests failed or environment issues

### Reporting

-   Console output with clear success/failure indicators
-   Structured logging for easy parsing in CI systems
-   Exception details for debugging failed tests

## Code Coverage Goals

### Current Coverage Areas

1. **Service Layer**: BlogPostService, MediaService, ContentService
2. **Data Flow**: DTO → Model → Entity mapping and vice versa
3. **Error Handling**: Validation, null checks, service failures
4. **Storage Operations**: CRUD operations for all entity types

### Future Coverage Extensions

1. **Azure Functions**: HTTP trigger testing with test server
2. **Validation Logic**: Extended input validation scenarios
3. **Media Processing**: Image conversion and thumbnail generation
4. **Performance Testing**: Load testing for high-volume scenarios

## Best Practices Implemented

### Test Independence

-   Each test creates its own test data
-   No shared state between tests
-   Parallel execution safe (with different timestamp prefixes)

### Error Resilience

-   Comprehensive exception handling
-   Graceful degradation for missing dependencies
-   Clear error messages for debugging

### Realistic Testing

-   Integration tests use real Azure services
-   Test data mimics production scenarios
-   Proper cleanup prevents storage bloat

### Maintainability

-   Clear test naming conventions
-   Comprehensive documentation
-   Modular test structure for easy extension

## Troubleshooting

### Common Issues

1. **Missing Environment Variables**

    - Error: "Missing AZURE_STORAGE_ACCOUNT_NAME"
    - Solution: Set required environment variables

2. **Azure Authentication Failures**

    - Error: Authentication errors during storage access
    - Solution: Ensure DefaultAzureCredential is properly configured

3. **Test Data Conflicts**

    - Error: Entity already exists
    - Solution: Tests use timestamp prefixes to avoid conflicts

4. **Cleanup Failures**
    - Error: Test data remains after test execution
    - Solution: Manual cleanup or check error logs for root cause

### Debugging Tips

1. **Enable Detailed Logging**: Modify mock loggers to output to console
2. **Run Tests Individually**: Isolate failures to specific test methods
3. **Check Azure Portal**: Verify test data creation and cleanup
4. **Review Error Messages**: Integration tests provide detailed error information

## Future Enhancements

### Planned Improvements

1. **Performance Tests**: Add load testing with realistic data volumes
2. **End-to-End Tests**: Full HTTP request/response testing
3. **Security Tests**: Authentication and authorization validation
4. **Chaos Testing**: Failure scenario simulation
5. **Contract Testing**: API contract validation with schema verification

### Testing Tools Integration

1. **Test Coverage Reports**: Integration with coverage analysis tools
2. **Performance Monitoring**: Response time and throughput metrics
3. **Visual Testing**: Screenshot comparison for media processing
4. **Database State Verification**: Advanced Azure Table Storage validation
