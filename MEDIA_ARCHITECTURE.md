# Media and Content Architecture

This document explains the clean architecture implemented for media and content management in the Azure Functions project.

## Architecture Overview

The architecture implements a clean separation between Azure Functions, business logic, and storage providers:

```
Azure Functions → Service Layer → Storage Services
     ↓              ↓                ↓
 HTTP Triggers → BlogPostService → ContentService + MediaService
                                     ↓              ↓
                               TableStorage   BlobStorage
```

## Key Components

### 1. MediaService (`SharedStorage.Services.Media.MediaService`)

The central service for all media operations:

-   **Upload media**: Handles different media types through handlers
-   **Metadata management**: Stores media metadata in Table Storage
-   **File storage**: Delegates actual file storage to BlobStorageService
-   **CRUD operations**: Get, delete, batch operations

**Key Features:**

-   Handler-based extensibility for different media types
-   Automatic thumbnail generation for images
-   CDN URL resolution
-   Media metadata stored separately from file content

### 2. Media Handlers

Type-specific handlers that implement `IMediaTypeHandler`:

#### ImageHandler (`SharedStorage.Services.Media.Handlers.ImageHandler`)

-   Processes image uploads
-   Generates thumbnails automatically
-   Supports WebP conversion
-   Integrates with ThumbnailService

#### VideoHandler (`SharedStorage.Services.Media.Handlers.VideoHandler`)

-   Processes video uploads
-   Sets appropriate metadata
-   Handles video-specific properties

### 3. ContentService (`SharedStorage.Services.ContentServices.ContentService`)

Generic base class for content management:

-   **Generic CRUD operations**: Get, upsert, delete content
-   **Publishing workflow**: Handles published/draft states
-   **Author and category filtering**
-   **Extensible**: Can be inherited for specific content types

### 4. BlogPostService (`Functions.BlogPosts.Services.BlogPostService`)

Extends ContentService for blog post management:

-   **Media integration**: Links blog posts with media by ID
-   **Featured media**: Set featured images and media
-   **Media references**: Manage collections of media items
-   **Content operations**: Full blog post CRUD with media support

## Data Model Changes

### Before (Direct URLs)

```csharp
public class BlogPost
{
    public string ImageUrl { get; set; }
    public string ImageDescription { get; set; }
    public string MediaUrl { get; set; }
    public string MediaDescription { get; set; }
}
```

### After (Media References)

```csharp
public class BlogPost
{
    public string? FeaturedImageId { get; set; }        // Reference to MediaEntity
    public string? FeaturedMediaId { get; set; }        // Reference to MediaEntity
    public string MediaReferencesJson { get; set; }     // Array of media IDs
}
```

## Usage Examples

### 1. Upload an Image

```csharp
// Through MediaService directly
var mediaService = serviceProvider.GetService<IMediaService>();
using var imageStream = File.OpenRead("image.jpg");
var mediaEntity = await mediaService.UploadImageAsync(
    imageStream,
    "image.jpg",
    authorId: "author-123",
    description: "Blog post cover image",
    purpose: "coverImage"
);

// The MediaEntity contains:
// - Id: Unique identifier
// - Url: CDN URL for the original image
// - ThumbnailUrl: CDN URL for the thumbnail
// - Metadata: Width, height, content type, etc.
```

### 2. Create a Blog Post with Media

```csharp
var blogPostService = serviceProvider.GetService<IBlogPostService>();

// First upload the media
var mediaEntity = await mediaService.UploadImageAsync(stream, "cover.jpg", "author-123");

// Create blog post model
var blogPost = new BlogPostModel
{
    Title = "My Blog Post",
    Content = "Blog content here...",
    Slug = "my-blog-post",
    AuthorSlug = "author-123",
    FeaturedImageId = mediaEntity.Id  // Reference by ID, not URL
};

// Save the blog post
var dto = await blogPostService.UpsertPostAsync("my-blog-post", blogPost);
```

### 3. Add Media to Existing Post

```csharp
// Upload additional media
var videoEntity = await mediaService.UploadVideoAsync(videoStream, "intro.mp4", "author-123");

// Link to blog post
await blogPostService.SetFeaturedMediaAsync("my-blog-post", videoEntity.Id);

// Or add to media references collection
await blogPostService.AddMediaReferenceAsync("my-blog-post", videoEntity.Id);
```

### 4. Retrieve Blog Post with Media URLs

```csharp
var blogPost = await blogPostService.GetPostAsync("my-blog-post");

// To get the actual media URLs, query the media service:
if (!string.IsNullOrEmpty(blogPost.FeaturedImageId))
{
    var featuredImage = await mediaService.GetMediaAsync(blogPost.FeaturedImageId);
    var imageUrl = featuredImage?.Url;
    var thumbnailUrl = featuredImage?.ThumbnailUrl;
}
```

## Dependency Injection Setup

### In your Function App's `Program.cs` or `Startup.cs`:

```csharp
using SharedStorage.Extensions;

// Register media services
services.AddMediaServices();

// Register specific content services
services.AddSingleton<IBlogPostService>(provider =>
{
    var tableStorageService = provider.GetRequiredService<ITableStorageService>();
    var mediaService = provider.GetRequiredService<IMediaService>();
    var logger = provider.GetRequiredService<IAppInsightsLogger<ContentService<BlogPostEntity, BlogPostModel, BlogPostDTO>>>();

    return new BlogPostService(tableStorageService, mediaService, logger);
});
```

## Environment Variables

The services require these environment variables:

```bash
AZURE_STORAGE_ACCOUNT_NAME=your_storage_account
MEDIA_TABLE_NAME=media                    # Optional, defaults to "media"
BLOGPOSTS_TABLE_NAME=blog                 # Optional, defaults to "blog"
```

## Benefits of This Architecture

1. **Clean Separation**: Business logic separated from storage implementation
2. **Extensibility**: Easy to add new media types via handlers
3. **Consistency**: Unified approach to media and content management
4. **Performance**: Media metadata separate from file storage
5. **Scalability**: CDN integration and proper blob organization
6. **Maintainability**: Clear interfaces and dependency injection

## Adding New Media Types

To add support for new media types (e.g., audio, documents):

1. **Create Entity**: Extend `MediaEntity`

```csharp
public class AudioEntity : MediaEntity
{
    public string Duration { get; set; }
    public string Bitrate { get; set; }
}
```

2. **Create Handler**: Implement `IMediaTypeHandler`

```csharp
public class AudioHandler : MediaHandler
{
    public override string SupportedType => "audio";
    // Implement upload, delete logic
}
```

3. **Register Handler**: Add to DI container

```csharp
services.AddSingleton<IMediaTypeHandler, AudioHandler>();
```

The MediaService will automatically pick up the new handler and route audio uploads accordingly.
