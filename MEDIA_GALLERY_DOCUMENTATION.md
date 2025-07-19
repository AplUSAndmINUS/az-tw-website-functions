# Media Gallery Functions Documentation

This document provides comprehensive documentation for the Media Gallery Functions that enable integration with various social media platforms and blob storage.

## Overview

The Media Gallery Functions provide APIs to retrieve media content from multiple sources:
- **External Platforms**: TikTok, Instagram, YouTube, Facebook, LinkedIn, Pinterest
- **Blob Storage**: Local media stored in Azure Blob Storage

## Architecture

### Core Components

1. **HTTP Trigger Functions**
   - `GetAllMedia`: Returns all media items with pagination
   - `GetMediaByMedium`: Filters media by type (image, video, audio)
   - `GetMediaByPlatform`: Filters media by platform source

2. **Timer Trigger Function**
   - `WriteMediaTable`: Syncs external media metadata nightly at 1 AM MT (7 AM UTC)

3. **Data Models**
   - Extended `MediaEntity`, `MediaItemModel`, and `MediaItemDTO` with external platform support
   - Added fields: `Platform`, `ExternalId`, `ExternalUrl`, `IsExternal`

4. **Services**
   - `ExternalMediaSyncService`: Orchestrates syncing from all platforms
   - Platform-specific services: `ITikTokService`, `IInstagramService`, etc.
   - Mock implementations for demonstration and testing

## API Endpoints

### GetAllMedia

**Endpoint**: `GET /api/GetAllMedia`

**Parameters**:
- `limit` (optional, int): Maximum number of items to return (max 100)
- `offset` (optional, int): Number of items to skip for pagination

**Example Request**:
```
GET /api/GetAllMedia?limit=20&offset=10
```

**Response**:
```json
{
  "success": true,
  "data": [
    {
      "id": "guid",
      "platform": "Instagram",
      "mediaType": "image",
      "isExternal": true,
      "externalId": "instagram_123",
      "externalUrl": "https://instagram.com/p/example",
      "url": "https://instagram.com/p/example",
      "thumbnailUrl": "https://scontent.cdninstagram.com/thumb.jpg",
      "description": "Instagram post description",
      "width": 1080,
      "height": 1080,
      "uploadedAt": "2025-01-19T06:00:00Z"
    }
  ],
  "count": 1,
  "message": "Successfully retrieved all media items"
}
```

### GetMediaByMedium

**Endpoint**: `GET /api/GetMediaByMedium`

**Parameters**:
- `medium` (required, string): Medium type - "image", "video", or "audio"
- `limit` (optional, int): Maximum number of items to return
- `offset` (optional, int): Number of items to skip for pagination

**Example Request**:
```
GET /api/GetMediaByMedium?medium=video&limit=10
```

**Response**:
```json
{
  "success": true,
  "data": [
    {
      "id": "guid",
      "platform": "YouTube",
      "mediaType": "video",
      "isExternal": true,
      "externalId": "youtube_abc123",
      "externalUrl": "https://youtube.com/watch?v=abc123",
      "description": "YouTube video description",
      "duration": 300,
      "videoQuality": "Full HD",
      "width": 1920,
      "height": 1080
    }
  ],
  "count": 1,
  "medium": "video",
  "message": "Successfully retrieved 1 video media items"
}
```

### GetMediaByPlatform

**Endpoint**: `GET /api/GetMediaByPlatform`

**Parameters**:
- `platform` (required, string): Platform name - "TikTok", "Instagram", "YouTube", "Facebook", "LinkedIn", "Pinterest", "BlobStorage"
- `limit` (optional, int): Maximum number of items to return
- `offset` (optional, int): Number of items to skip for pagination

**Example Request**:
```
GET /api/GetMediaByPlatform?platform=tiktok&limit=5
```

**Response**:
```json
{
  "success": true,
  "data": [
    {
      "id": "guid",
      "platform": "TikTok",
      "mediaType": "video",
      "isExternal": true,
      "externalId": "tiktok_1_20250119",
      "externalUrl": "https://www.tiktok.com/@user/video/1234567890",
      "description": "TikTok video - Mock content for testing",
      "duration": 30,
      "videoQuality": "HD",
      "width": 720,
      "height": 1280
    }
  ],
  "count": 1,
  "platform": "TikTok",
  "message": "Successfully retrieved 1 media items from TikTok"
}
```

## Timer Function

### WriteMediaTable

**Schedule**: Runs daily at 1 AM MT (7 AM UTC)
**CRON Expression**: `"0 0 7 * * *"`

**Functionality**:
- Syncs media metadata from all external platforms
- Only stores metadata, not actual media files
- Handles platform failures gracefully (continues with other platforms)
- Logs sync results and errors

**Platforms Synced**:
1. TikTok
2. Instagram
3. YouTube
4. Facebook
5. LinkedIn
6. Pinterest

## Data Model

### MediaEntity (Extended)

```csharp
public class MediaEntity : ITableEntity
{
    // Existing fields...
    
    // New external platform fields
    public bool IsExternal { get; set; } = false;
    public string Platform { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public string ExternalUrl { get; set; } = string.Empty;
}
```

### Platform Values

- `"TikTok"`: TikTok videos
- `"Instagram"`: Instagram photos and videos
- `"YouTube"`: YouTube videos
- `"Facebook"`: Facebook photos and videos
- `"LinkedIn"`: LinkedIn professional content
- `"Pinterest"`: Pinterest pins (images)
- `"BlobStorage"`: Local Azure Blob Storage content

## Error Handling

### HTTP Functions

All HTTP functions return standardized error responses:

```json
{
  "success": false,
  "message": "Error description",
  "error": "Technical error details"
}
```

**HTTP Status Codes**:
- `200 OK`: Successful request
- `400 Bad Request`: Invalid parameters
- `500 Internal Server Error`: Server error

### Timer Function

- Individual platform failures don't stop the entire sync process
- Errors are logged but sync continues with remaining platforms
- Critical errors are re-thrown to mark function execution as failed

## Configuration

### Environment Variables

- `MEDIA_TABLE_NAME`: Table name for media storage (default: "media")
- `USE_MOCK_STORAGE`: Use mock storage for development (default: false)

### Table Storage

Media metadata is stored in Azure Table Storage with:
- **PartitionKey**: AuthorId or platform identifier
- **RowKey**: Media ID
- **Platform filtering**: Enabled through Platform field

## Mock Data

For development and testing, mock services generate realistic data:

- **TikTok**: 5 sample videos with typical TikTok dimensions (720x1280)
- **Instagram**: 6 mixed images and videos (1080x1080)
- **YouTube**: 4 longer videos with HD quality (1920x1080)
- **Facebook**: 3 mixed content items
- **LinkedIn**: 2 professional images
- **Pinterest**: 4 pins with typical Pinterest dimensions (736x1104)

## Testing

### Unit Tests

Comprehensive unit tests cover:
- HTTP function parameter validation
- Error handling scenarios
- Service integrations
- Timer function execution
- Mock service behavior

**Test Categories**:
- `GetAllMediaTests`: Tests for GetAllMedia function
- `GetMediaByMediumTests`: Tests for GetMediaByMedium function
- `GetMediaByPlatformTests`: Tests for GetMediaByPlatform function
- `WriteMediaTableTests`: Tests for timer function
- `ExternalMediaSyncServiceTests`: Tests for sync service

### Running Tests

```bash
cd Tests
dotnet test --filter "Category!=Integration"
```

## Deployment

### Function App Settings

Configure the following in your Azure Function App:
- Connection strings for Azure Storage
- API keys for external platforms (when implementing real APIs)
- Environment-specific table names

### Security

- All HTTP functions use `AuthorizationLevel.Function`
- Function keys required for API access
- External platform APIs should use secure authentication when implemented

## Future Enhancements

1. **Real API Integration**: Replace mock services with actual platform APIs
2. **Authentication**: Implement OAuth flows for platform access
3. **Caching**: Add Redis caching for frequently accessed media
4. **Content Filtering**: Add content moderation and filtering capabilities
5. **Analytics**: Track media engagement and popularity metrics
6. **Webhooks**: Real-time updates from platforms via webhooks

## Troubleshooting

### Common Issues

1. **Build Errors**: Ensure all package references are correct
2. **Timer Not Running**: Check CRON expression and timezone settings
3. **Empty Results**: Verify table storage connection and data existence
4. **API Errors**: Check function keys and parameter formats

### Logging

All functions include comprehensive logging:
- Information level: Successful operations and counts
- Warning level: Non-critical issues
- Error level: Failures and exceptions

Check Application Insights for detailed execution logs and metrics.