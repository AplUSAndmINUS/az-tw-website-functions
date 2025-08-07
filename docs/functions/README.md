# Azure Functions API Documentation

This directory contains automatically generated documentation for all Azure Functions in the application.

## Functions by Category

### Authors

- [DeleteAuthor](./DeleteAuthor.md) - DELETE - Azure Function to delete an author by slug
- [GetAuthorAsync](./GetAuthorAsync.md) - GET - Retrieves a specific author by its unique identifier (slug) with optional media information.
- [SetAuthorProfileImage](./SetAuthorProfileImage.md) - POST - Author Media Functions using BaseMediaRelationshipFunctions
- [UpsertAuthorAsync](./UpsertAuthorAsync.md) - PUT - Creates or updates a author based on the provided parameters. Supports both new creation and modification of existing records.

### BlogPosts

- [DeleteBlogPost](./DeleteBlogPost.md) - DELETE - Permanently deletes a specific blog post by its unique identifier (slug).
- [GetBlogPosts](./GetBlogPosts.md) - GET - Retrieves a list of blog posts with optional filtering by various criteria including category, author, and publication status.
- [SetBlogPostFeaturedImage](./SetBlogPostFeaturedImage.md) - POST - BlogPost Media Functions using BaseMediaRelationshipFunctions
- [UpsertBlogPost](./UpsertBlogPost.md) - POST, PUT - Creates or updates a blog post based on the provided parameters. Supports both new creation and modification of existing records.

### Books

- [DeleteBook](./DeleteBook.md) - DELETE - Azure Function for deleting books (DELETE operations)
- [GetBooks](./GetBooks.md) - GET - Azure Function for retrieving books (GET operations)
- [SetBookFeaturedImage](./SetBookFeaturedImage.md) - POST - Book Media Functions using BaseMediaRelationshipFunctions
- [UpsertBook](./UpsertBook.md) - POST, PUT - Azure Function for creating and updating books (POST/PUT operations)

### ContactMe

- [ContactMe](./ContactMe.md) - POST - Azure Function for handling contact form submissions

### GitHub

- [GetGitHubActivityGrid](./GetGitHubActivityGrid.md) - GET - Retrieves a specific githubactivitygrid by its unique identifier (slug) with optional media information.
- [GetGitHubReposTable](./GetGitHubReposTable.md) - GET - Retrieves a specific githubrepostable by its unique identifier (slug) with optional media information.

### PortfolioPiece

- [DeletePortfolioPiece](./DeletePortfolioPiece.md) - DELETE - Permanently deletes a specific portfolio piece by its unique identifier (slug).
- [GetPortfolioPieces](./GetPortfolioPieces.md) - GET - Retrieves a list of portfolio pieces with optional filtering by various criteria including category, author, and publication status.
- [SetPortfolioPieceFeaturedImage](./SetPortfolioPieceFeaturedImage.md) - POST - Portfolio Media Functions using BaseMediaRelationshipFunctions
- [UpsertPortfolioPiece](./UpsertPortfolioPiece.md) - POST, PUT - Creates or updates a portfolio piece based on the provided parameters. Supports both new creation and modification of existing records.

### Shared

- [AppInsightsDiagnostics](./AppInsightsDiagnostics.md) - GET - Diagnostic function to help identify AppInsights and environment configuration issues
- [UploadDocument](./UploadDocument.md) - POST - Azure Function endpoint for UploadDocument operations
- [UploadImage](./UploadImage.md) - POST - Shared media functions for handling global media operations across all content types.
 These functions provide centralized upload, retrieval, and deletion of media assets
 that can be used by blog posts, portfolio pieces, authors, and future content types.

### TestAppInsightsLogging.cs

- [TestAppInsightsLogging](./TestAppInsightsLogging.md) - GET - Azure Function endpoint for TestAppInsightsLogging operations

## Usage Notes

- All endpoints require API key authentication via `x-api-key` header
- Base URL for local development: `http://localhost:7071`
- All dates should be in ISO 8601 format
- Media references must be stringified JSON arrays

#Function #Documentation #API
