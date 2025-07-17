# Tests Documentation

This folder contains unit tests and integration tests for the Azure Functions website application.

## Test Structure

- **Authors/**: Tests for Author-related functions
- **BlogPosts/**: Tests for Blog Post-related functions  
- **Books/**: Tests for Book-related functions
- **GitHub/**: Tests for GitHub API integration functions
- **PortfolioPiece/**: Tests for Portfolio Piece-related functions
- **Media/**: Tests for Media-related functionality
- **SharedStorage/**: Tests for shared storage services and utilities
- **Helpers/**: Utility classes and factories for testing

## Running Tests

### Prerequisites

Ensure you have the following environment variables set for integration tests:

```bash
# Azure Storage Configuration
StorageAccountName=<your-dev-storage-account>
X_API_ENVIRONMENT_KEY=<your-api-key>

# Optional table names (defaults provided)
AUTHORS_TABLE_NAME=authors
BLOG_POSTS_TABLE_NAME=blogposts
BOOKS_TABLE_NAME=books
PORTFOLIO_PIECES_TABLE_NAME=portfoliopieces
GITHUB_REPOS_TABLE_NAME=githubrepos
```

### Running Unit Tests

From the Tests directory:

```bash
dotnet test
```

### Running Specific Test Categories

Run only unit tests:
```bash
dotnet test --filter Category=Unit
```

Run only integration tests:
```bash
dotnet test --filter Category=Integration
```

## Test Types

### Unit Tests
- Test individual functions and services in isolation
- Use mock dependencies
- Fast execution
- No external dependencies required

### Integration Tests
- Test functions against real Azure storage
- Require valid Azure credentials
- Test end-to-end functionality
- Use dev/test environment

## API Testing

### Postman Collections

Each function area has associated Postman collections for manual testing:

- **Authors**: `MediaAPI.postman_collection.json` (Authors section)
- **BlogPosts**: `MediaAPI.postman_collection.json` (BlogPosts section)
- **Books**: `MediaAPI.postman_collection.json` (Books section)
- **PortfolioPiece**: `MediaAPI.postman_collection.json` (PortfolioPiece section)
- **GitHub**: `MediaAPI.postman_collection.json` (GitHub section)

### Swagger Documentation

The API endpoints are documented with Swagger/OpenAPI. When running locally:

1. Start the Functions app: `func start`
2. Navigate to: `http://localhost:7071/swagger/ui`

## Test Automation

### Continuous Integration

Tests are automatically run on:
- Pull requests
- Commits to main/develop branches
- Manual workflow triggers

### Test Reports

Test results and coverage reports are available in:
- GitHub Actions build logs
- Test result artifacts
- Coverage reports (if configured)

## Writing New Tests

### Unit Test Template

```csharp
using Xunit;
using Moq;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Tests.YourArea;

public class YourFunctionTests
{
    [Fact]
    public async Task YourFunction_ValidInput_ReturnsSuccess()
    {
        // Arrange
        var mockService = new Mock<IYourService>();
        var function = new YourFunction(mockService.Object);
        
        // Act
        var result = await function.Run(request);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }
}
```

### Integration Test Template

```csharp
using Xunit;
using Tests.Helpers;

namespace Tests.YourArea;

public class YourFunctionIntegrationTests : IDisposable
{
    private readonly string _testId = Guid.NewGuid().ToString();
    
    [Fact]
    public async Task YourFunction_IntegrationTest_WorksEndToEnd()
    {
        // Arrange
        var context = TestFactory.CreateFunctionContext();
        var request = TestFactory.CreateJsonRequestWithApiKey(
            context, testData, "test-key", "POST", "endpoint");
        
        // Act
        var response = await function.Run(request, context);
        
        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
    
    public void Dispose()
    {
        // Cleanup test data
    }
}
```

## Best Practices

1. **Isolation**: Each test should be independent and not rely on other tests
2. **Cleanup**: Always clean up test data, especially in integration tests
3. **Descriptive Names**: Use clear, descriptive test method names
4. **AAA Pattern**: Follow Arrange-Act-Assert pattern
5. **Mock Dependencies**: Use mocks for external dependencies in unit tests
6. **Test Data**: Use meaningful test data that reflects real scenarios
7. **Error Cases**: Test both success and failure scenarios
8. **Async/Await**: Properly handle async operations in tests

## Troubleshooting

### Common Issues

1. **Authentication Failures**: Ensure API keys are correctly set
2. **Storage Access**: Verify Azure Storage connection strings
3. **Package Conflicts**: Check for version mismatches in dependencies
4. **Build Errors**: Ensure all projects build successfully before running tests

### Debug Mode

Run tests with verbose output:
```bash
dotnet test --logger "console;verbosity=detailed"
```

Run specific test:
```bash
dotnet test --filter "FullyQualifiedName=Tests.Authors.AuthorFunctionsTests.UpsertAuthor_ValidRequest_ReturnsSuccess"
```

## Contributing

When adding new tests:

1. Follow the existing naming conventions
2. Add appropriate test categories
3. Include both unit and integration tests where applicable
4. Update this documentation for new test areas
5. Ensure tests pass in CI/CD pipeline

## Support

For questions about testing:
- Review existing test examples
- Check the main project documentation
- Refer to Azure Functions testing documentation
- Contact the development team