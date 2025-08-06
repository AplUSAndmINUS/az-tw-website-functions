# Media Services Testing Guide

This guide explains how to run and create tests for the az-tw-website-functions media services.

## Overview

The Media Services in az-tw-website-functions are responsible for handling image and other media uploads, processing, and storage. The test suite ensures these services work correctly and remain reliable during development.

## Running Tests

To run the Media Services tests:

```powershell
# From the project root directory
.\run-media-tests.ps1
```

This script will:

1. Build the MediaTests project
2. Run all test cases
3. Display the results

## Test Coverage

The tests cover the following services:

### ImageConversionService

-   Basic initialization
-   Input validation
-   Image format conversion to WebP
-   Image resizing and optimization

### ThumbnailService

-   Basic initialization
-   Input validation
-   Thumbnail generation
-   Output validation

### MediaHandler Base Class

-   Abstract handler functionality
-   Upload and download operations
-   Media listing operations
-   Deletion operations

## Adding New Tests

To add new tests:

1. Create a new test class in the MediaTests project
2. Import the necessary namespaces
3. Create a test fixture class with the appropriate setup
4. Add [Fact] or [Theory] test methods
5. Run the tests to verify they pass

### Example Test Method

```csharp
[Fact]
public async Task ConvertToWebPAsync_ValidImage_ReturnsWebPResult()
{
    // Arrange - Create test data
    using var inputStream = new MemoryStream(testImageBytes);

    // Act - Call the method being tested
    var result = await _service.ConvertToWebPAsync(inputStream);

    // Assert - Verify the expected outcome
    Assert.NotNull(result);
    Assert.Equal("webp", result.Format);
    Assert.True(result.Content.Length > 0);
}
```

## Testing Patterns

1. **AAA Pattern**: Arrange, Act, Assert
2. **Naming Convention**: MethodName_Scenario_ExpectedBehavior
3. **Independence**: Each test should be independent and not rely on other tests
4. **Clean Up**: Dispose of any resources in the test

## Mock Services

For dependencies, use Moq to create mock objects:

```csharp
// Example of mocking a logger
var mockLogger = new Mock<IAppInsightsLogger<ImageConversionService>>();
```

## Test Data

Generate test images programmatically using ImageSharp:

```csharp
// Create a test image
using var image = new Image<Rgba32>(100, 100);
for (int x = 0; x < image.Width; x++)
{
    for (int y = 0; y < image.Height; y++)
    {
        image[x, y] = new Rgba32((byte)(x % 256), (byte)(y % 256), 100, 255);
    }
}

// Save to a memory stream
var inputStream = new MemoryStream();
await image.SaveAsPngAsync(inputStream);
inputStream.Position = 0;
```
