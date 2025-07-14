# Unified Media Model

This document outlines the unified media model approach implemented across the entire application to ensure consistent media handling across all content types (blog posts, portfolio pieces, authors, etc.).

## Core Components

### 1. MediaItemModel

The `MediaItemModel` serves as a shared DTO model for all media items across the application. It includes:

-   Core identification properties (Id, AuthorId)
-   Media content properties (Filename, MediaType, ContentType)
-   URLs and presentation (Url, ThumbnailUrl)
-   Metadata (Description, AltText, Width, Height)
-   Type-specific properties for images, videos, and audio
-   Relationship tracking to link media with content

### 2. MediaEntity and Specialized Entities

The database storage model includes:

-   `MediaEntity`: Base entity for all media types
-   `ImageEntity`: Specialized entity for images
-   `VideoEntity`: Specialized entity for videos
-   Future specialized entities can be added as needed

### 3. MediaItemMapper

Maps between `MediaEntity` and `MediaItemModel` objects, handling conversions between storage and API representations.

### 4. BaseContentWithMediaDTO

A generic base class for DTOs that combine content with media items, which can be extended by:

-   BlogPostWithMediaDTO
-   PortfolioPostWithMediaDTO
-   AuthorWithMediaDTO
-   Future content types

### 5. MediaItemService

A service for handling the unified media model across all content types, providing methods for:

-   Getting media items for specific content
-   Retrieving media by IDs
-   Uploading media and associating it with content
-   Deleting media and removing associations

### 6. Content Reference Tracking

Tracks relationships between media items and content through:

-   `MediaContentReferenceEntity`: Entity for tracking associations
-   Content-specific metadata tables (blogmediametadata, portfoliomediametadata, etc.)

## Usage Examples

### Retrieving Content with Media

```csharp
// Get a blog post with its media
var blogPost = await _blogPostService.GetBlogPostAsync(slug);
var mediaItems = await _mediaItemService.GetMediaForContentAsync(blogPost.Id, "blog");

var blogPostWithMedia = new BlogPostWithMediaDTO(blogPost, mediaItems);

// Access specialized media
var featuredImage = blogPostWithMedia.FeaturedImage;
var featuredVideo = blogPostWithMedia.FeaturedVideo;
```

### Uploading Media for Content

```csharp
// Upload an image for a blog post
var mediaItem = await _mediaItemService.UploadMediaAsync(
    stream,
    "cover-image.jpg",
    "image/jpeg",
    "image",
    "featured",
    authorId,
    blogPostId,
    "Blog post cover image",
    "Cover image for the blog post about unified media models"
);

// Update the blog post with the new media ID
blogPost.FeaturedImageId = mediaItem.Id;
await _blogPostService.UpdateBlogPostAsync(blogPost);
```

## Benefits

1. **Consistency**: Media handling is consistent across all content types
2. **Reusability**: Shared components reduce code duplication
3. **Extensibility**: Easy to add new media types or content types
4. **Separation of Concerns**: Clear distinction between storage, business logic, and presentation
5. **Forward Compatibility**: New media features can be added centrally

## Implementation Notes

1. The `MediaItemModel` can be extended with additional properties as needed
2. New specialized entity types can be added for new media types
3. Content-specific DTOs can customize how they present media
4. The unified model supports various media types (images, videos, audio, documents, etc.)

## Migration Path

For existing code:

1. Add `MediaItemModel` properties to existing DTOs
2. Use `MediaItemMapper` to convert existing `MediaEntity` objects
3. Update service methods to use the new model
4. Gradually replace direct `MediaEntity` usage with `MediaItemModel`
