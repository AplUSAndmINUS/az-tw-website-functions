# Media Gallery API Documentation

## Overview
The Media Gallery API provides access to media content from various platforms including TikTok, Instagram, YouTube, Facebook, LinkedIn, Pinterest, and local blob storage.

## Base URL
All API endpoints are relative to the base URL of your Azure Functions deployment.

## Authentication
All endpoints require an API key to be provided in the `X-API-Key` header.

## Endpoints

### 1. Get All Media
**GET** `/api/media-gallery`

Returns all media from all platforms and blob storage.

**Query Parameters:**
- `authorId` (optional): Filter by author ID
- `limit` (optional): Maximum number of items to return (default: 100, max: 500)

**Response:**
```json
{
  "media": [
    {
      "id": "string",
      "authorId": "string",
      "title": "string",
      "description": "string",
      "mediaType": "image|video|audio",
      "contentType": "string",
      "url": "string",
      "thumbnailUrl": "string",
      "altText": "string",
      "width": 0,
      "height": 0,
      "platform": "string",
      "platformDisplayName": "string",
      "externalUrl": "string",
      "embedCode": "string",
      "likeCount": 0,
      "shareCount": 0,
      "viewCount": 0,
      "tags": ["string"],
      "createdAt": "2023-01-01T00:00:00Z",
      "lastUpdated": "2023-01-01T00:00:00Z",
      "duration": 0,
      "videoQuality": "string",
      "audioDuration": 0,
      "audioBitrate": "string",
      "purpose": "string",
      "isExternal": true,
      "isAvailable": true,
      "sortKey": "string",
      "category": "string"
    }
  ],
  "totalCount": 0,
  "pageSize": 100,
  "nextPageToken": null,
  "lastSyncTime": "2023-01-01T00:00:00Z",
  "availablePlatforms": ["tiktok", "instagram", "youtube", "facebook", "linkedin", "pinterest", "blob"],
  "availableMediaTypes": ["image", "video", "audio"]
}
```

### 2. Get Media by Medium
**GET** `/api/media-gallery/medium/{mediaType}`

Returns media filtered by media type.

**Path Parameters:**
- `mediaType`: The type of media (`image`, `video`, `audio`)

**Query Parameters:**
- `authorId` (optional): Filter by author ID
- `limit` (optional): Maximum number of items to return (default: 100, max: 500)

**Response:** Same structure as Get All Media

### 3. Get Media by Platform
**GET** `/api/media-gallery/platform/{platform}`

Returns media filtered by platform.

**Path Parameters:**
- `platform`: The platform name (`tiktok`, `instagram`, `youtube`, `facebook`, `linkedin`, `pinterest`, `blob`)

**Query Parameters:**
- `authorId` (optional): Filter by author ID
- `limit` (optional): Maximum number of items to return (default: 100, max: 500)

**Response:** Same structure as Get All Media

### 4. Manual Media Sync (Admin)
**POST** `/api/admin/sync-media`

Manually triggers media synchronization from external platforms.

**Authorization:** Function-level (requires function key)

**Query Parameters:**
- `authorId` (optional): Author ID to sync (default: from environment)
- `platform` (optional): Specific platform to sync (default: all platforms)

**Response:**
```json
{
  "success": true,
  "message": "Media sync completed successfully",
  "totalSynced": 0,
  "authorId": "string",
  "platform": "string",
  "syncTime": "2023-01-01T00:00:00Z"
}
```

## Timer Function

### WriteMediaTable
**Timer Trigger:** Runs nightly at 1:00 AM Mountain Time (`0 0 1 * * *`)

Automatically syncs media from all external platforms to the Azure Table Storage.

## Platform-Specific Details

### TikTok
- **Media Type:** Video
- **Mock Data:** Returns 5 sample videos with engagement metrics
- **External URLs:** `https://www.tiktok.com/@username/video/{id}`

### Instagram
- **Media Type:** Image and Video
- **Mock Data:** Returns 8 sample posts (mix of images and videos)
- **External URLs:** `https://www.instagram.com/p/{id}/`

### YouTube
- **Media Type:** Video
- **Mock Data:** Returns 6 sample videos with educational content
- **External URLs:** `https://www.youtube.com/watch?v={id}`

### Facebook
- **Media Type:** Image and Video
- **Mock Data:** Returns 4 sample posts
- **External URLs:** `https://www.facebook.com/username/posts/{id}`

### LinkedIn
- **Media Type:** Image
- **Mock Data:** Returns 3 sample professional posts
- **External URLs:** `https://www.linkedin.com/posts/username_{id}`

### Pinterest
- **Media Type:** Image
- **Mock Data:** Returns 10 sample pins
- **External URLs:** `https://www.pinterest.com/pin/{id}`

### Blob Storage
- **Media Type:** Image and Video
- **Data Source:** Azure Blob Storage with CDN URLs
- **URLs:** CDN-optimized URLs for fast delivery

## Error Handling

### Common Error Responses

**400 Bad Request**
```json
{
  "error": "Invalid parameter: {parameter_name}"
}
```

**401 Unauthorized**
```json
{
  "error": "Invalid API key"
}
```

**500 Internal Server Error**
```json
{
  "error": "Internal server error"
}
```

## Data Flow

1. **External Platform Sync:** Timer function runs nightly to fetch latest content from external platforms
2. **Metadata Storage:** External content metadata is stored in Azure Table Storage
3. **API Requests:** HTTP functions query the table storage and return formatted responses
4. **Frontend Integration:** Frontend applications consume the API to display media galleries

## Environment Variables

- `MEDIA_TABLE_NAME`: Name of the Azure Table Storage table (default: "media")
- `USE_MOCK_STORAGE`: Set to "true" to use mock table names (adds "mock" prefix)
- `DEFAULT_AUTHOR_ID`: Default author ID for sync operations (default: "terence-waters")
- `X_API_ENVIRONMENT_KEY`: API key for authentication

## Notes

- All external platform integrations are currently implemented as mock services
- In production, these would be replaced with actual API integrations
- The system supports both CDN-optimized blob storage content and external platform content
- Engagement metrics (likes, shares, views) are synced from external platforms
- Content is automatically categorized by platform and media type