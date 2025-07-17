# API Data Models Documentation

This document provides a comprehensive overview of all data models used in the az-tw-website-functions API.

## Base Models

These models serve as the foundation for other content-specific models.

### BaseContentModel

Base model for all content types (Blog Posts, Portfolio Pieces, Videos, Books, etc.)

| Property            | Type            | Description                                          | Required | Default |
| ------------------- | --------------- | ---------------------------------------------------- | -------- | ------- |
| Id                  | string          | Unique identifier                                    | No       |         |
| PartitionKey        | string          | Table storage partition key                          | No       |         |
| RowKey              | string          | Table storage row key                                | No       |         |
| Timestamp           | DateTimeOffset? | Record timestamp                                     | No       |         |
| ETag                | ETag            | Entity tag for concurrency control                   | No       |         |
| Title               | string          | Content title                                        | Yes      |         |
| AuthorSlug          | string          | Author's slug identifier                             | Yes      |         |
| Description         | string          | Brief content description                            | No       |         |
| Content             | string          | Main content body                                    | Yes      |         |
| Slug                | string          | URL-friendly identifier                              | Yes      |         |
| Category            | string          | Content category                                     | Yes      |         |
| Status              | string          | Publication status (Draft, Published, Archived)      | No       | Draft   |
| FeaturedImageId     | string          | ID of featured image                                 | No       |         |
| FeaturedMediaId     | string          | ID of featured media                                 | No       |         |
| FeaturedVideoId     | string          | ID of featured video                                 | No       |         |
| MediaReferencesJson | string          | JSON array of media IDs                              | No       | []      |
| PublishDate         | DateTime        | Date when content was published                      | No       |         |
| LastModified        | DateTime        | Date when content was last modified                  | No       |         |
| TagsList            | string[]        | Array of content tags                                | No       | []      |
| IsPublished         | bool            | Computed property indicating if content is published | No       |         |

### MediaItemModel

A shared model for media items that can be used across different content types.

| Property           | Type     | Description                              | Required | Default |
| ------------------ | -------- | ---------------------------------------- | -------- | ------- |
| Id                 | string   | Unique identifier                        | No       |         |
| AuthorId           | string   | ID of the author who uploaded the media  | No       |         |
| Filename           | string   | Original filename                        | No       |         |
| MediaType          | string   | Type of media (image, video, audio)      | No       |         |
| Purpose            | string   | Purpose of the media item                | No       |         |
| ContentType        | string   | MIME type of the media                   | No       |         |
| Url                | string   | Full-size media URL                      | No       |         |
| ThumbnailUrl       | string   | Thumbnail URL                            | No       |         |
| Description        | string   | Media description                        | No       |         |
| AltText            | string   | Alternative text for accessibility       | No       |         |
| Width              | int      | Width in pixels                          | No       |         |
| Height             | int      | Height in pixels                         | No       |         |
| SizeBytes          | long     | File size in bytes                       | No       |         |
| Resolution         | string   | Media resolution                         | No       |         |
| UploadedAt         | DateTime | Upload timestamp                         | No       |         |
| LastModified       | DateTime | Last modification timestamp              | No       |         |
| ContentId          | string   | ID of the related content                | No       |         |
| RelatedContentType | string   | Type of content this media is related to | No       |         |
| ImagePurpose       | string   | More specific image purposes             | No       |         |
| Duration           | int      | Duration in seconds (for videos)         | No       |         |
| VideoQuality       | string   | Video quality (SD, HD, 4K)               | No       |         |
| AudioDuration      | int      | Duration in seconds (for audio)          | No       |         |
| AudioBitrate       | string   | Audio bitrate                            | No       |         |
| MetadataJson       | string   | Additional metadata as JSON              | No       | {}      |

## Author Models

### AuthorModel

Model for author information.

| Property                  | Type   | Description                         | Required | Default |
| ------------------------- | ------ | ----------------------------------- | -------- | ------- |
| AuthorSlug                | string | URL-friendly identifier             | Yes      |         |
| FirstName                 | string | Author's first name                 | Yes      |         |
| LastName                  | string | Author's last name                  | Yes      |         |
| Email                     | string | Author's email address              | Yes      |         |
| Username                  | string | Username for authentication         | Yes      |         |
| DisplayName               | string | Author's display name               | Yes      |         |
| Location                  | string | Author's location                   | No       |         |
| Bio                       | string | Author's biography                  | No       |         |
| Website                   | string | Author's website URL                | No       |         |
| TwitterHandle             | string | Author's Twitter handle             | No       |         |
| InstagramHandle           | string | Author's Instagram handle           | No       |         |
| LinkedInHandle            | string | Author's LinkedIn URL               | No       |         |
| BlueskyHandle             | string | Author's Bluesky handle             | No       |         |
| ProfileImageId            | string | ID of profile image                 | No       |         |
| ProfileImageBlobContainer | string | Blob container for profile image    | No       |         |
| ProfileImageFileName      | string | Filename of profile image           | No       |         |
| ProfileImageCdnUrl        | string | CDN URL for profile image           | No       |         |
| ThumbnailCdnUrl           | string | CDN URL for thumbnail               | No       |         |
| MediaReferencesJson       | string | JSON array of media IDs             | No       | []      |
| HasValidProfileImage      | bool   | Indicates if profile image is valid | No       | false   |
| ImageContentType          | string | MIME type of profile image          | No       |         |
| ImageSizeBytes            | long   | Size of profile image in bytes      | No       |         |
| ImageWidth                | int    | Width of profile image in pixels    | No       |         |
| ImageHeight               | int    | Height of profile image in pixels   | No       |         |

### AuthorDTO

Data transfer object for author information.

| Property             | Type   | Description                         | Required | Default |
| -------------------- | ------ | ----------------------------------- | -------- | ------- |
| AuthorSlug           | string | URL-friendly identifier             | Yes      |         |
| DisplayName          | string | Author's display name               | Yes      |         |
| FirstName            | string | Author's first name                 | Yes      |         |
| LastName             | string | Author's last name                  | Yes      |         |
| Email                | string | Author's email address              | Yes      |         |
| Username             | string | Username for authentication         | Yes      |         |
| Location             | string | Author's location                   | No       |         |
| Bio                  | string | Author's biography                  | No       |         |
| Website              | string | Author's website URL                | No       |         |
| TwitterHandle        | string | Author's Twitter handle             | No       |         |
| InstagramHandle      | string | Author's Instagram handle           | No       |         |
| LinkedInHandle       | string | Author's LinkedIn URL               | No       |         |
| BlueskyHandle        | string | Author's Bluesky handle             | No       |         |
| HasValidProfileImage | bool   | Indicates if profile image is valid | No       | false   |
| ProfileImageId       | string | ID of profile image                 | No       |         |
| ProfileImageFileName | string | Filename of profile image           | No       |         |
| ProfileImageCdnUrl   | string | CDN URL for profile image           | No       |         |
| ThumbnailCdnUrl      | string | CDN URL for thumbnail               | No       |         |
| MediaReferencesJson  | string | JSON array of media IDs             | No       | []      |

### AuthorWithMediaDTO

DTO for combining an author with their associated media items.

| Property            | Type                 | Description                                 | Required | Default |
| ------------------- | -------------------- | ------------------------------------------- | -------- | ------- |
| Author              | AuthorModel          | The author model                            | Yes      |         |
| MediaItems          | List<MediaItemModel> | All media items associated with this author | Yes      |         |
| ProfileImage        | MediaItemModel?      | Convenience property for profile image      | No       |         |
| BackgroundImage     | MediaItemModel?      | Convenience property for background image   | No       |         |
| MediaReferencesJson | string               | JSON serialized list of media references    | No       |         |
| AuthorSlug          | string               | URL-friendly identifier                     | No       |         |
| FirstName           | string               | Author's first name                         | No       |         |
| LastName            | string               | Author's last name                          | No       |         |
| Email               | string               | Author's email address                      | No       |         |
| Username            | string               | Username for authentication                 | No       |         |
| DisplayName         | string               | Author's display name                       | No       |         |
| Location            | string               | Author's location                           | No       |         |
| Bio                 | string               | Author's biography                          | No       |         |
| Website             | string               | Author's website URL                        | No       |         |
| TwitterHandle       | string               | Author's Twitter handle                     | No       |         |
| InstagramHandle     | string               | Author's Instagram handle                   | No       |         |
| LinkedInHandle      | string               | Author's LinkedIn URL                       | No       |         |
| BlueskyHandle       | string               | Author's Bluesky handle                     | No       |         |
| ProfileImageCdnUrl  | string               | CDN URL for profile image                   | No       |         |
| ThumbnailCdnUrl     | string               | CDN URL for thumbnail                       | No       |         |

## Blog Post Models

### BlogPostModel

Model class for blog posts, inherits from BaseContentModel with all its properties.

### BlogPostDTO

Data transfer object for blog posts.

| Property            | Type            | Description                           | Required | Default |
| ------------------- | --------------- | ------------------------------------- | -------- | ------- |
| Id                  | string          | Unique identifier                     | No       |         |
| PartitionKey        | string          | Table storage partition key           | No       |         |
| RowKey              | string          | Table storage row key                 | No       |         |
| Timestamp           | DateTimeOffset? | Record timestamp                      | No       |         |
| Title               | string          | Blog post title                       | Yes      |         |
| AuthorSlug          | string          | Author's slug identifier              | Yes      |         |
| Description         | string          | Brief blog post description           | No       |         |
| Content             | string          | Main blog post content                | Yes      |         |
| Slug                | string          | URL-friendly identifier               | Yes      |         |
| Category            | string          | Blog post category                    | Yes      |         |
| Status              | string          | Publication status                    | No       | Draft   |
| FeaturedImageId     | string          | ID of featured image                  | No       |         |
| FeaturedMediaId     | string          | ID of featured media                  | No       |         |
| FeaturedVideoId     | string          | ID of featured video                  | No       |         |
| MediaReferencesJson | string          | JSON array of media IDs               | No       | []      |
| PublishDate         | DateTime        | Date when blog post was published     | No       |         |
| LastModified        | DateTime        | Date when blog post was last modified | No       |         |
| TagsList            | string[]        | Array of blog post tags               | No       | []      |

### BlogPostWithMediaDTO

DTO for combining a blog post with its associated media items.

| Property      | Type                 | Description                                    | Required | Default |
| ------------- | -------------------- | ---------------------------------------------- | -------- | ------- |
| BlogPost      | BlogPostDTO          | The blog post DTO                              | Yes      |         |
| MediaItems    | List<MediaItemModel> | All media items associated with this blog post | Yes      |         |
| FeaturedImage | MediaItemModel?      | Convenience property for featured image        | No       |         |
| FeaturedVideo | MediaItemModel?      | Convenience property for featured video        | No       |         |
| FeaturedMedia | MediaItemModel?      | Convenience property for featured media        | No       |         |

## Portfolio Piece Models

### PortfolioPieceModel

Model class for portfolio pieces, inherits from BaseContentModel with all its properties.

### PortfolioPieceDTO

Data transfer object for portfolio pieces.

| Property            | Type            | Description                                                  | Required | Default |
| ------------------- | --------------- | ------------------------------------------------------------ | -------- | ------- |
| Id                  | string          | Unique identifier                                            | No       |         |
| PartitionKey        | string          | Table storage partition key                                  | No       |         |
| RowKey              | string          | Table storage row key                                        | No       |         |
| Timestamp           | DateTimeOffset? | Record timestamp                                             | No       |         |
| Title               | string          | Portfolio piece title                                        | Yes      |         |
| AuthorSlug          | string          | Author's slug identifier                                     | Yes      |         |
| Description         | string          | Brief portfolio piece description                            | No       |         |
| Content             | string          | Main portfolio piece content                                 | Yes      |         |
| Slug                | string          | URL-friendly identifier                                      | Yes      |         |
| Category            | string          | Portfolio piece category                                     | Yes      |         |
| Status              | string          | Publication status                                           | No       | Draft   |
| FeaturedImageId     | string          | ID of featured image                                         | No       |         |
| FeaturedMediaId     | string          | ID of featured media                                         | No       |         |
| FeaturedVideoId     | string          | ID of featured video                                         | No       |         |
| MediaReferencesJson | string          | JSON array of media IDs                                      | No       | []      |
| PublishDate         | DateTime        | Date when portfolio piece was published                      | No       |         |
| LastModified        | DateTime        | Date when portfolio piece was last modified                  | No       |         |
| TagsList            | string[]        | Array of portfolio piece tags                                | No       | []      |
| IsPublished         | bool            | Computed property indicating if portfolio piece is published | No       |         |

### PortfolioPieceWithMediaDTO

DTO for combining a portfolio piece with its associated media items.

| Property       | Type                 | Description                                          | Required | Default |
| -------------- | -------------------- | ---------------------------------------------------- | -------- | ------- |
| PortfolioPiece | PortfolioPieceDTO    | The portfolio piece DTO                              | Yes      |         |
| MediaItems     | List<MediaItemModel> | All media items associated with this portfolio piece | Yes      |         |
| FeaturedImage  | MediaItemModel?      | Convenience property for featured image              | No       |         |
| FeaturedVideo  | MediaItemModel?      | Convenience property for featured video              | No       |         |
| FeaturedMedia  | MediaItemModel?      | Convenience property for featured media              | No       |         |

## API Endpoints

### Author Endpoints

-   **GET /GetAuthors** - Retrieves all authors
-   **GET /GetAuthor/{authorSlug}** - Retrieves an author by slug
-   **POST /UpsertAuthor** - Creates or updates an author

### Blog Post Endpoints

-   **GET /GetBlogPosts** - Retrieves all blog posts
-   **GET /GetBlogPost/{slug}** - Retrieves a blog post by slug
-   **POST /UpsertBlogPost** - Creates or updates a blog post

### Portfolio Piece Endpoints

-   **GET /GetPortfolioPieces** - Retrieves all portfolio pieces
-   **GET /GetPortfolioPiece/{slug}** - Retrieves a portfolio piece by slug
-   **POST /UpsertPortfolioPiece** - Creates or updates a portfolio piece

### Media Endpoints

-   **POST /UploadMedia** - Uploads a media file
-   **GET /GetMedia/{id}** - Retrieves a media item by ID
