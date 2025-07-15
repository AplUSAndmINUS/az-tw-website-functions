# Media to Content Relationship

## Overview

This document explains how media items (images, videos, etc.) can be linked to content items (blog posts, portfolio pieces, etc.) using the ContentId and RelatedContentType properties.

## Key Components

### 1. MediaReference and BlobReference Classes

The `MediaReference` and `BlobReference` classes now include optional ContentId and RelatedContentType properties:

```csharp
public class MediaReference
{
  public string BlobName { get; }
  public string ThumbnailBlobName { get; }
  public string CdnUrl { get; }
  public string ThumbnailCdnUrl { get; }
  public string? ContentId { get; } // NEW: ID of related content
  public string? RelatedContentType { get; } // NEW: Type of related content

  // Constructor includes contentId and relatedContentType parameters
}
```

### 2. MediaEntity Base Class

The `MediaEntity` class now includes ContentId and RelatedContentType properties to store the relationship:

```csharp
public class MediaEntity : ITableEntity
{
  // Existing properties...

  public string ContentId { get; set; } = string.Empty; // ID of related content
  public string RelatedContentType { get; set; } = string.Empty; // Type of related content
}
```

### 3. BlobStorageService Enhancements

The `BlobStorageService.UploadBlobAsync` method now accepts ContentId and RelatedContentType parameters:

```csharp
public async Task<MediaReference> UploadBlobAsync(
    string containerName,
    string blobName,
    Stream content,
    string? contentId = null,
    string? relatedContentType = null)
```

When contentId is provided:

-   The method adds this information as metadata on the blob
-   This metadata persists even if the table record is lost
-   The metadata can be retrieved later via GetBlobReferenceAsync

### 4. Media Handlers

Both `ImageHandler` and `VideoHandler` have been updated to:

-   Accept and pass along ContentId and RelatedContentType parameters
-   Set these values on the MediaEntity when creating new media items

### 5. MediaItemModel and DTO

The MediaItemModel and MediaItemDTO already had these properties:

```csharp
public string ContentId { get; set; } = string.Empty;
public string RelatedContentType { get; set; } = string.Empty;
```

The `MediaReferenceExtensions.MapFromMediaReference()` method has been updated to copy these values from the MediaReference to the MediaItemModel.

## Usage Guidelines

### 1. Uploading Media with Content Association

When uploading media that's associated with a specific content item:

```csharp
// Example: Uploading an image for a blog post
var mediaEntity = await _mediaService.UploadAsync(
    stream,
    fileName,
    contentType,
    authorId: "author123",
    contentId: "blog-post-123", // ID of the related blog post
    relatedContentType: "BlogPost" // Type of the related content
);
```

### 2. Retrieving Media for a Specific Content Item

To find all media associated with a specific content item:

```csharp
// Example: Finding all media for a blog post
var mediaItems = await _mediaService.GetMediaByContentIdAsync("blog-post-123", "BlogPost");
```

### 3. API Endpoints for Content-Media Relationship

The following API endpoints are available for working with content-media relationships:

#### Uploading Media with Content Association

```
POST /api/media/upload?fileName={fileName}&authorId={authorId}&contentId={contentId}&relatedContentType={relatedContentType}
```

-   `fileName`: Name of the uploaded file
-   `authorId`: (Optional) ID of the author who uploaded the media
-   `contentId`: (Optional) ID of the related content (e.g., blog slug)
-   `relatedContentType`: (Optional) Type of the related content (e.g., "BlogPost")

#### Retrieving Media for a Specific Content

```
GET /api/media/content/{contentId}?relatedContentType={relatedContentType}
```

-   `contentId`: ID of the content to retrieve media for
-   `relatedContentType`: (Optional) Type of the related content to filter by

### 3. Metadata Retrieval

Even if the table record is lost, the content relationship is preserved in blob metadata:

```csharp
// Example: Retrieving blob reference with metadata
var blobRef = await _blobStorageService.GetBlobReferenceAsync(containerName, blobName);
string? contentId = blobRef.ContentId;
string? relatedContentType = blobRef.RelatedContentType;
```

## Best Practices

1. **Consistency in RelatedContentType**: Use consistent string values for RelatedContentType, such as "BlogPost", "PortfolioPiece", "Author", etc.

2. **Content IDs**: Use the actual ID of the content item as ContentId (not slugs or other identifiers that might change)

3. **Orphaned Media**: Implement a process to clean up media items with ContentIds that no longer exist

4. **Validation**: When deleting content, consider checking if there are any media items associated with it

## Data Model

```
+-------------+          +----------------+
| ContentItem |1---------*| MediaItem     |
| ----------- |          | -------------- |
| Id          |<---------| ContentId      |
| Type        |<---------| RelatedContentType |
| ...         |          | ...            |
+-------------+          +----------------+
```

This relationship allows for:

-   One-to-many relationship between content and media
-   Flexible association of media with different content types
-   Cross-referencing between content and media
