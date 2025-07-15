# CDN URL Handling Strategy

## Overview

This document outlines the strategy for handling CDN URLs in the media upload and retrieval process for the TerenceWaters.com website functions.

## Key Components

### 1. MediaReference and BlobReference Classes

Two new classes have been introduced to encapsulate CDN URLs for media items:

-   `MediaReference`: Contains properties for both the original blob and its thumbnail, including CDN URLs.
-   `BlobReference`: A simpler version containing just the blob name and its CDN URL.

### 2. BlobStorageService Enhancements

The `BlobStorageService` has been enhanced to:

-   Always return CDN URLs via `MediaReference` or `BlobReference` objects
-   Use `CdnUrlBuilder.ResolveCdnUrl()` to generate proper CDN URLs based on content section and asset type
-   Handle both production and mock environments appropriately

### 3. CDN URL Validation

Two extension methods have been added to validate CDN URLs:

-   `MediaItemModel.EnsureValidCdnUrls()`: Validates that the model's URLs are proper CDN URLs
-   `MediaItemDTO.EnsureValidCdnUrls()`: Validates that the DTO's URLs are proper CDN URLs

### 4. Unified CDN Endpoints

All CDN endpoints now use the Azure Edge URL pattern:

```csharp
public const string CdnEndpointBase = "https://twmedia-cdn.azureedge.net";
```

This ensures consistent URL patterns across all media types.

### 5. Media Handler Updates

The `ImageHandler` and `VideoHandler` have been updated to:

-   Use the CDN URLs returned from `BlobStorageService.UploadBlobAsync()`
-   Ensure these URLs are properly propagated to the `MediaEntity` objects

### 6. MediaItemMapper Enhancements

The `MediaItemMapper` has been enhanced to:

-   Always validate CDN URLs during model/DTO mapping
-   Log warnings for invalid or missing CDN URLs
-   Ensure CDN URLs are consistently used in all models and DTOs

### 7. MediaReferenceExtensions

A new extension method `MapFromMediaReference()` has been added to create `MediaItemModel` instances from `MediaReference` objects, ensuring CDN URLs are properly set.

## Usage Guidelines

1. Always use `BlobStorageService.UploadBlobAsync()` to upload media, which returns a `MediaReference` with CDN URLs.
2. Use the `MediaReference.CdnUrl` and `MediaReference.ThumbnailCdnUrl` properties when creating media entities.
3. When mapping between models and DTOs, the `EnsureValidCdnUrls()` extension methods will validate URLs.
4. For new uploads, use `MediaReferenceExtensions.MapFromMediaReference()` to create models from references.

## Testing

The `MediaCdnUrlTests` class includes tests to verify that:

1. Invalid URLs generate appropriate warnings
2. Valid CDN URLs pass validation without warnings
3. Both models and DTOs are properly validated

## Future Enhancements

1. Add CDN URL validation to the API layer to catch invalid URLs before they reach the client
2. Implement automatic URL repair for legacy media items with invalid CDN URLs
3. Add support for signed/secure CDN URLs for protected content
4. Implement Front Door URLs when available
