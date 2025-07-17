# Books Azure Functions Documentation

## Overview

The Books Azure Functions provide a complete CRUD (Create, Read, Update, Delete) API for managing book content, following the same architectural patterns established by BlogPosts and PortfolioPieces. The implementation includes full media integration capabilities for featured images, videos, and additional media references.

## Architecture

The Books implementation follows the established three-layer architecture:

### 1. Models Layer (`Functions.Books.Models`)
- **BookModel**: Inherits from `BaseContentModel`, represents the business object
- **BookEntity**: Inherits from `BaseContentEntity`, handles Azure Table Storage persistence  
- **BookDTO**: Data Transfer Object for API responses
- **BookMapper**: Handles conversions between Model, Entity, and DTO
- **BookWithMediaDTO**: Enhanced DTO that includes associated media content

### 2. Services Layer (`Functions.Books.Services`)
- **IBookService**: Service interface defining all book operations
- **BookService**: Implementation that inherits from `ContentService<BookEntity, BookModel, BookDTO>`

### 3. Functions Layer (`Functions.Books.Functions`)
- **GetBooksFunction**: GET operations for retrieving books
- **UpsertBook**: POST/PUT operations for creating/updating books
- **DeleteBook**: DELETE operations for removing books
- **BookMediaFunctions**: Media relationship operations

## Data Structure

### Book Properties

Books inherit all properties from `BaseContentModel`:

```csharp
public class BookModel : BaseContentModel
{
    // Storage identifiers
    public string Id { get; set; }
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    // Core content properties
    public string Title { get; set; }           // Book title
    public string AuthorSlug { get; set; }      // Author identifier
    public string Description { get; set; }     // Book description/summary
    public string Content { get; set; }         // Book content/details
    public string Slug { get; set; }           // URL-friendly identifier
    public string Category { get; set; }       // Book category/genre
    public string Status { get; set; }         // "Draft" or "Published"

    // Media references
    public string? FeaturedImageId { get; set; }     // Cover image
    public string? FeaturedMediaId { get; set; }     // Primary media
    public string? FeaturedVideoId { get; set; }     // Trailer/promotional video
    public string MediaReferencesJson { get; set; }  // Additional media (JSON array)

    // Date properties
    public DateTime PublishDate { get; set; }
    public DateTime LastModified { get; set; }

    // Tags and metadata
    public string[] TagsList { get; set; }
    
    // Computed properties
    public bool IsPublished => Status == "Published";
}
```

### Table Storage Structure

Books are stored in Azure Table Storage with the following schema:

- **Table Name**: Determined by `ContentNameResolver.GetTableName(ContentSections.Books, null, useMock)`
- **Partition Key**: Book slug (for efficient queries by book)
- **Row Key**: "book" (consistent identifier)

## API Endpoints

### 1. Get Books
**GET** `/books`

Retrieves a collection of books with optional filtering.

**Query Parameters:**
- `authorSlug` (optional): Filter by author
- `category` (optional): Filter by category/genre
- `isPublished` (optional): Filter by publication status (true/false/null)
- `limit` (optional): Maximum number of results (default: 50, max: 100)
- `includeMedia` (optional): Include associated media content

**Example Requests:**
```http
GET /books
GET /books?category=Fiction&isPublished=true&limit=10
GET /books?authorSlug=john-doe&includeMedia=true
```

**Response:** Array of `BookDTO` or `BookWithMediaDTO` objects

### 2. Get Single Book
**GET** `/books/{slug}`

Retrieves a specific book by its slug.

**Path Parameters:**
- `slug`: Book identifier (URL-friendly)

**Query Parameters:**
- `isPublished` (optional): Filter by publication status
- `includeMedia` (optional): Include associated media content

**Example Requests:**
```http
GET /books/my-amazing-book
GET /books/sci-fi-novel?includeMedia=true
```

**Response:** `BookDTO` or `BookWithMediaDTO` object

### 3. Create/Update Book
**POST** `/books/{slug}` (Create)
**PUT** `/books/{slug}` (Update)

Creates a new book or updates an existing one.

**Path Parameters:**
- `slug`: Book identifier (URL-friendly)

**Request Body:** `BookModel` object (JSON)

**Required Fields:**
- `title`: Book title
- `authorSlug`: Author identifier
- `content`: Book content/details
- `category`: Book category/genre
- `tagsList`: Array of tags (can be empty)

**Optional Fields:**
- `description`: Book description
- `status`: "Draft" or "Published" (default: "Draft")
- `featuredImageId`: Cover image ID
- `featuredVideoId`: Promotional video ID
- `featuredMediaId`: Primary media ID
- `publishDate`: Publication date (default: current time)

**Example Request:**
```json
{
    "title": "The Great Adventure",
    "authorSlug": "jane-smith",
    "description": "An epic tale of courage and discovery",
    "content": "Full book content here...",
    "category": "Adventure",
    "status": "Published",
    "tagsList": ["adventure", "epic", "fantasy"],
    "featuredImageId": "{{your-media-id-guid}}"
}
```

**Response:** `BookDTO` object

### 4. Delete Book
**DELETE** `/books/{slug}`

Removes a book from the system.

**Path Parameters:**
- `slug`: Book identifier

**Response:** HTTP 200 (success) or 404 (not found)

## Media Operations

### 1. Set Featured Image
**POST** `/books/{slug}/featured-image`

Sets the cover image for a book.

**Request Body:**
```json
{
    "mediaId": "{{your-media-id-guid}}"
}
```

### 2. Set Featured Video
**POST** `/books/{slug}/featured-video`

Sets a promotional/trailer video for a book.

**Request Body:**
```json
{
    "mediaId": "{{REDACTED-GUID}}"
}
```

### 3. Set Featured Media
**POST** `/books/{slug}/featured-media`

Sets primary media content for a book.

**Request Body:**
```json
{
    "mediaId": "{{REDACTED-GUID}}"
}
```

### 4. Add Media Reference
**POST** `/books/{slug}/media`

Adds additional media content to a book.

**Request Body:**
```json
{
    "mediaId": "{{REDACTED-GUID}}"
}
```

### 5. Remove Media Reference
**DELETE** `/books/{slug}/media/{mediaId}`

Removes a media reference from a book.

**Path Parameters:**
- `slug`: Book identifier
- `mediaId`: Media identifier to remove

## Error Handling

All endpoints return appropriate HTTP status codes:

- **200 OK**: Successful operation
- **201 Created**: Resource created successfully
- **400 Bad Request**: Invalid input data
- **401 Unauthorized**: Missing or invalid API key
- **404 Not Found**: Resource not found
- **500 Internal Server Error**: Server error

Error responses include descriptive messages:

```json
{
    "error": "Title is required",
    "statusCode": 400
}
```

## Authentication

All endpoints require API key authentication via the `X-API-Key` header. The API key is validated using the `IAPIKeyValidator` service.

## Validation

### Required Fields
- `title`: Must not be empty
- `slug`: Must not be empty (auto-set from route)
- `authorSlug`: Must not be empty
- `content`: Must not be empty
- `category`: Must not be empty
- `tagsList`: Must not be null (can be empty array)

### Media ID Validation
- All media IDs must be valid GUIDs
- Media references are validated against the MediaService

### Date Validation
- `publishDate`: Automatically set to UTC
- `lastModified`: Automatically updated on each operation
- Draft books get future dates to avoid Azure Table Storage issues

## Integration with Shared Services

The Books implementation leverages existing shared infrastructure:

### Storage Services
- **TableStorageService**: For entity persistence
- **MediaService**: For media content management
- **BlobStorageService**: For file storage (via MediaService)

### Utilities
- **ContentNameResolver**: For consistent table naming
- **DataValidation**: For input validation and sanitization
- **ApiKeyValidator**: For authentication
- **AppInsightsLogger**: For telemetry and monitoring

### Azure Managed Identity
The service uses Azure Managed Identity for secure access to storage resources, following the same patterns as BlogPosts and PortfolioPieces.

## Monitoring and Logging

Comprehensive logging is implemented throughout:

- **Function Entry/Exit**: Track all function calls
- **Validation Errors**: Log invalid input data
- **Service Operations**: Track CRUD operations
- **Media Operations**: Log media relationship changes
- **Error Conditions**: Detailed error logging with context

All logs are sent to Application Insights for monitoring and alerting.

## Consistency with Existing Patterns

The Books implementation maintains complete consistency with BlogPosts and PortfolioPieces:

- Same inheritance hierarchy and base classes
- Same service layer patterns and interfaces
- Same validation and error handling approaches
- Same media integration capabilities
- Same Azure Function routing and parameter patterns
- Same dependency injection configuration
- Same authentication and authorization model

This ensures maintainability and reduces the learning curve for developers familiar with the existing codebase.

## Testing Recommendations

For comprehensive testing, consider:

1. **Unit Tests**: Test individual service methods
2. **Integration Tests**: Test complete CRUD workflows
3. **Media Integration Tests**: Test media attachment operations
4. **Validation Tests**: Test input validation scenarios
5. **Error Handling Tests**: Test error conditions and responses
6. **Performance Tests**: Test with large datasets and concurrent operations

The existing test patterns for BlogPosts can serve as templates for Books testing.