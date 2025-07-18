# Frontend API Mapping for Azure Functions Backend

This document provides a comprehensive mapping of API endpoints required for the frontend implementation, including function names, endpoints, request/response structures, data models (DTOs), and implementation guidelines for proper integration.

**Key Features:**

-   Azure Key Vault integration for secure API key management
-   No `/api` route prefix (routePrefix set to empty string)
-   Environment-specific API key handling (dev/staging/production)
-   Comprehensive media upload and management support

## Table of Contents

-   [Authentication & Authorization](#authentication--authorization)
-   [Books API](#books-api)
    -   [Endpoints](#books-endpoints)
    -   [Data Models](#books-data-models)
    -   [Integration Guidelines](#books-integration-guidelines)
-   [Blog Posts API](#blog-posts-api)
    -   [Endpoints](#blog-posts-endpoints)
    -   [Data Models](#blog-posts-data-models)
    -   [Integration Guidelines](#blog-posts-integration-guidelines)
-   [Portfolio Pieces API](#portfolio-pieces-api)
    -   [Endpoints](#portfolio-pieces-endpoints)
    -   [Data Models](#portfolio-pieces-data-models)
    -   [Integration Guidelines](#portfolio-pieces-integration-guidelines)
-   [Authors API](#authors-api)
    -   [Endpoints](#authors-endpoints)
    -   [Data Models](#authors-data-models)
    -   [Integration Guidelines](#authors-integration-guidelines)
-   [Media API](#media-api)
    -   [Endpoints](#media-endpoints)
    -   [Data Models](#media-data-models)
    -   [Integration Guidelines](#media-integration-guidelines)
-   [Contact Me API](#contact-me-api)
    -   [Endpoints](#contact-me-endpoints)
    -   [Data Models](#contact-me-data-models)
    -   [Integration Guidelines](#contact-me-integration-guidelines)
-   [GitHub Repositories API](#github-repositories-api)
    -   [Endpoints](#github-repositories-endpoints)
    -   [Data Models](#github-repositories-data-models)
    -   [Integration Guidelines](#github-repositories-integration-guidelines)

## Authentication & Authorization

All API endpoints require proper authentication via the `x-api-key` header. This API key validation is implemented at the function level through the `IAPIKeyValidator` interface with Azure Key Vault integration.

### Authentication Implementation

```typescript
// Frontend API client configuration
import axios from "axios";

// Create axios instance with base configuration
const apiClient = axios.create({
    baseURL: process.env.REACT_APP_API_BASE_URL || "http://localhost:7071",
    headers: {
        "Content-Type": "application/json",
        "x-api-key": process.env.REACT_APP_API_KEY || ""
    }
});

// Note: No /api prefix is used as the Function App has routePrefix set to empty string

// Add response interceptor for error handling
apiClient.interceptors.response.use(
    (response) => response,
    (error) => {
        // Handle authentication errors
        if (error.response && error.response.status === 401) {
            console.error("API Authentication failed. Check your API key.");
        }
        return Promise.reject(error);
    }
);

export default apiClient;
```

### Environment Configuration

The API supports different environments with corresponding API keys managed through Azure Key Vault:

-   **Development**: `DEV-X-API-ENVIRONMENT-KEY` secret
-   **Staging**: `STAGING-X-API-ENVIRONMENT-KEY` secret
-   **Production**: `PROD-X-API-ENVIRONMENT-KEY` secret

Set your environment variables accordingly:

```typescript
// .env.local
REACT_APP_API_BASE_URL=http://localhost:7071
REACT_APP_API_KEY=your-dev-key-here

// .env.development
REACT_APP_API_BASE_URL=https://mock-dev-api.terencewaters.com
REACT_APP_API_KEY=your-dev-key-here

// .env.staging
REACT_APP_API_BASE_URL=https://mock-tst-api.terencewaters.com
REACT_APP_API_KEY=your-staging-key-here

// .env.production
REACT_APP_API_BASE_URL=https://api.terencewaters.com
REACT_APP_API_KEY=your-production-key-here
```

### Important Notes on Authentication

1. **API Key Storage**: Store the API key in environment variables, never in source code
2. **Key Vault Integration**: API keys are managed through Azure Key Vault with environment-specific secrets
3. **Header Name**: Use `x-api-key` (lowercase) for the authentication header
4. **CORS**: The backend is configured with CORS settings to allow requests from the frontend domain

## Books API

### Books Endpoints

| Function Name             | HTTP Method | Endpoint                       | Description                       | Query Parameters                                                                                                                                                                                                                                      | Request Body            | Response                                 |
| ------------------------- | ----------- | ------------------------------ | --------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------- | ---------------------------------------- |
| `GetBooks`                | GET         | `/books`                       | Retrieves a collection of books   | `authorSlug` (optional): Filter by author<br>`category` (optional): Filter by category<br>`isPublished` (optional): Filter by publication status<br>`limit` (optional): Max results (default: 50)<br>`includeMedia` (optional): Include media content | None                    | Array of `BookDTO` or `BookWithMediaDTO` |
| `GetBook`                 | GET         | `/books/{slug}`                | Retrieves a specific book by slug | `isPublished` (optional): Filter by publication status<br>`includeMedia` (optional): Include media content                                                                                                                                            | None                    | `BookDTO` or `BookWithMediaDTO`          |
| `UpsertBook`              | POST/PUT    | `/books/{slug}`                | Creates or updates a book         | None                                                                                                                                                                                                                                                  | `BookModel` (see below) | `BookDTO`                                |
| `DeleteBook`              | DELETE      | `/books/{slug}`                | Removes a book                    | None                                                                                                                                                                                                                                                  | None                    | 200 OK or 404 Not Found                  |
| `UploadBookFeaturedImage` | POST        | `/books/{slug}/featured-image` | Upload book cover/featured image  | None                                                                                                                                                                                                                                                  | Form-data file upload   | `BookDTO`                                |

### Books Data Models

#### BookDTO (Response Model)

```typescript
interface BookDTO {
    id: string;
    partitionKey: string;
    rowKey: string;
    timestamp?: string;
    title: string;
    authorSlug: string;
    description: string;
    content: string;
    slug: string;
    category: string;
    status: string;
    featuredImageId?: string;
    featuredMediaId?: string;
    featuredVideoId?: string;
    mediaReferencesJson: string;
    publishDate: string; // ISO date string
    lastModified: string; // ISO date string
    tagsList: string[];
}
```

#### BookWithMediaDTO (Response Model with Media)

```typescript
interface BookWithMediaDTO {
    book: BookDTO;
    mediaItems: MediaItemModel[];
    featuredImage?: MediaItemModel;
    featuredVideo?: MediaItemModel;
    featuredMedia?: MediaItemModel;
}

interface MediaItemModel {
    id: string;
    name: string;
    description?: string;
    mediaType: string; // "image", "video", etc.
    url: string;
    thumbnailUrl?: string;
    contentType: string;
    size: number;
    width?: number;
    height?: number;
    purpose?: string;
    uploadedAt: string; // ISO date string
}
```

#### Book Request Model (for creating/updating books)

```typescript
interface BookRequest {
    title: string; // Required
    authorSlug: string; // Required
    description?: string;
    content: string; // Required
    category: string; // Required
    status?: string; // Default: "Draft"
    featuredImageId?: string;
    featuredVideoId?: string;
    featuredMediaId?: string;
    publishDate?: string; // ISO date string, optional
    tagsList: string[]; // Can be empty array, but required
}
```

### Books Integration Guidelines

#### Setting Up Book API Service

```typescript
// src/services/booksService.ts
import apiClient from "../utils/apiClient";
import {BookDTO, BookWithMediaDTO, BookRequest} from "../types/BookTypes";

export const BooksService = {
    // Get all books with optional filtering
    async getBooks(params?: {
        authorSlug?: string;
        category?: string;
        isPublished?: boolean;
        limit?: number;
        includeMedia?: boolean;
    }): Promise<BookDTO[] | BookWithMediaDTO[]> {
        const response = await apiClient.get("/books", {params});
        return response.data;
    },

    // Get a single book by slug
    async getBook(
        slug: string,
        params?: {
            isPublished?: boolean;
            includeMedia?: boolean;
        }
    ): Promise<BookDTO | BookWithMediaDTO> {
        const response = await apiClient.get(`/books/${slug}`, {params});
        return response.data;
    },

    // Create or update a book
    async upsertBook(slug: string, book: BookRequest): Promise<BookDTO> {
        const response = await apiClient.post(`/books/${slug}`, book);
        return response.data;
    },

    // Delete a book
    async deleteBook(slug: string): Promise<void> {
        await apiClient.delete(`/books/${slug}`);
    },

    // Set featured image
    async setFeaturedImage(slug: string, mediaId: string): Promise<BookDTO> {
        const response = await apiClient.post(`/books/${slug}/featured-image`, {
            mediaId
        });
        return response.data;
    },

    // Set featured video
    async setFeaturedVideo(slug: string, mediaId: string): Promise<BookDTO> {
        const response = await apiClient.post(`/books/${slug}/featured-video`, {
            mediaId
        });
        return response.data;
    },

    // Set featured media
    async setFeaturedMedia(slug: string, mediaId: string): Promise<BookDTO> {
        const response = await apiClient.post(`/books/${slug}/featured-media`, {
            mediaId
        });
        return response.data;
    },

    // Add media reference to a book
    async addMediaReference(slug: string, mediaId: string): Promise<BookDTO> {
        const response = await apiClient.post(`/books/${slug}/media`, {
            mediaId
        });
        return response.data;
    },

    // Remove media reference from a book
    async removeMediaReference(
        slug: string,
        mediaId: string
    ): Promise<BookDTO> {
        const response = await apiClient.delete(
            `/books/${slug}/media/${mediaId}`
        );
        return response.data;
    }
};
```

#### Zustand Store Integration

```typescript
// src/store/booksStore.ts
import create from "zustand";
import {BooksService} from "../services/booksService";
import {BookDTO, BookWithMediaDTO, BookRequest} from "../types/BookTypes";

interface BooksState {
    books: BookDTO[];
    currentBook: BookDTO | BookWithMediaDTO | null;
    loading: boolean;
    error: string | null;
    fetchBooks: (params?: any) => Promise<void>;
    fetchBook: (slug: string, params?: any) => Promise<void>;
    createBook: (slug: string, book: BookRequest) => Promise<void>;
    updateBook: (slug: string, book: BookRequest) => Promise<void>;
    deleteBook: (slug: string) => Promise<void>;
    setFeaturedImage: (slug: string, mediaId: string) => Promise<void>;
    setFeaturedVideo: (slug: string, mediaId: string) => Promise<void>;
    setFeaturedMedia: (slug: string, mediaId: string) => Promise<void>;
    addMediaReference: (slug: string, mediaId: string) => Promise<void>;
    removeMediaReference: (slug: string, mediaId: string) => Promise<void>;
}

export const useBooksStore = create<BooksState>((set, get) => ({
    books: [],
    currentBook: null,
    loading: false,
    error: null,

    fetchBooks: async (params) => {
        set({loading: true, error: null});
        try {
            const books = await BooksService.getBooks(params);
            set({books: books as BookDTO[], loading: false});
        } catch (error) {
            console.error("Error fetching books:", error);
            set({error: "Failed to fetch books", loading: false});
        }
    },

    fetchBook: async (slug, params) => {
        set({loading: true, error: null});
        try {
            const book = await BooksService.getBook(slug, params);
            set({currentBook: book, loading: false});
        } catch (error) {
            console.error(`Error fetching book ${slug}:`, error);
            set({error: `Failed to fetch book ${slug}`, loading: false});
        }
    },

    createBook: async (slug, book) => {
        set({loading: true, error: null});
        try {
            const newBook = await BooksService.upsertBook(slug, book);
            set((state) => ({
                books: [...state.books, newBook],
                currentBook: newBook,
                loading: false
            }));
        } catch (error) {
            console.error("Error creating book:", error);
            set({error: "Failed to create book", loading: false});
        }
    },

    updateBook: async (slug, book) => {
        set({loading: true, error: null});
        try {
            const updatedBook = await BooksService.upsertBook(slug, book);
            set((state) => ({
                books: state.books.map((b) =>
                    b.slug === slug ? updatedBook : b
                ),
                currentBook: updatedBook,
                loading: false
            }));
        } catch (error) {
            console.error(`Error updating book ${slug}:`, error);
            set({error: `Failed to update book ${slug}`, loading: false});
        }
    },

    deleteBook: async (slug) => {
        set({loading: true, error: null});
        try {
            await BooksService.deleteBook(slug);
            set((state) => ({
                books: state.books.filter((b) => b.slug !== slug),
                currentBook:
                    state.currentBook?.slug === slug ? null : state.currentBook,
                loading: false
            }));
        } catch (error) {
            console.error(`Error deleting book ${slug}:`, error);
            set({error: `Failed to delete book ${slug}`, loading: false});
        }
    },

    setFeaturedImage: async (slug, mediaId) => {
        set({loading: true, error: null});
        try {
            const updatedBook = await BooksService.setFeaturedImage(
                slug,
                mediaId
            );
            set((state) => ({
                books: state.books.map((b) =>
                    b.slug === slug ? updatedBook : b
                ),
                currentBook:
                    state.currentBook?.slug === slug
                        ? updatedBook
                        : state.currentBook,
                loading: false
            }));
        } catch (error) {
            console.error(
                `Error setting featured image for book ${slug}:`,
                error
            );
            set({
                error: `Failed to set featured image for book ${slug}`,
                loading: false
            });
        }
    },

    setFeaturedVideo: async (slug, mediaId) => {
        set({loading: true, error: null});
        try {
            const updatedBook = await BooksService.setFeaturedVideo(
                slug,
                mediaId
            );
            set((state) => ({
                books: state.books.map((b) =>
                    b.slug === slug ? updatedBook : b
                ),
                currentBook:
                    state.currentBook?.slug === slug
                        ? updatedBook
                        : state.currentBook,
                loading: false
            }));
        } catch (error) {
            console.error(
                `Error setting featured video for book ${slug}:`,
                error
            );
            set({
                error: `Failed to set featured video for book ${slug}`,
                loading: false
            });
        }
    },

    setFeaturedMedia: async (slug, mediaId) => {
        set({loading: true, error: null});
        try {
            const updatedBook = await BooksService.setFeaturedMedia(
                slug,
                mediaId
            );
            set((state) => ({
                books: state.books.map((b) =>
                    b.slug === slug ? updatedBook : b
                ),
                currentBook:
                    state.currentBook?.slug === slug
                        ? updatedBook
                        : state.currentBook,
                loading: false
            }));
        } catch (error) {
            console.error(
                `Error setting featured media for book ${slug}:`,
                error
            );
            set({
                error: `Failed to set featured media for book ${slug}`,
                loading: false
            });
        }
    },

    addMediaReference: async (slug, mediaId) => {
        set({loading: true, error: null});
        try {
            const updatedBook = await BooksService.addMediaReference(
                slug,
                mediaId
            );
            set((state) => ({
                books: state.books.map((b) =>
                    b.slug === slug ? updatedBook : b
                ),
                currentBook:
                    state.currentBook?.slug === slug
                        ? updatedBook
                        : state.currentBook,
                loading: false
            }));
        } catch (error) {
            console.error(
                `Error adding media reference to book ${slug}:`,
                error
            );
            set({
                error: `Failed to add media reference to book ${slug}`,
                loading: false
            });
        }
    },

    removeMediaReference: async (slug, mediaId) => {
        set({loading: true, error: null});
        try {
            const updatedBook = await BooksService.removeMediaReference(
                slug,
                mediaId
            );
            set((state) => ({
                books: state.books.map((b) =>
                    b.slug === slug ? updatedBook : b
                ),
                currentBook:
                    state.currentBook?.slug === slug
                        ? updatedBook
                        : state.currentBook,
                loading: false
            }));
        } catch (error) {
            console.error(
                `Error removing media reference from book ${slug}:`,
                error
            );
            set({
                error: `Failed to remove media reference from book ${slug}`,
                loading: false
            });
        }
    }
}));
```

#### React Component Example

```typescript
// src/components/books/BookDetails.tsx
import React, {useEffect} from "react";
import {useParams} from "react-router-dom";
import {useBooksStore} from "../../store/booksStore";

interface BookDetailsProps {
    includeMedia?: boolean;
}

export const BookDetails: React.FC<BookDetailsProps> = ({
    includeMedia = true
}) => {
    const {slug} = useParams<{slug: string}>();
    const {currentBook, loading, error, fetchBook} = useBooksStore();

    useEffect(() => {
        if (slug) {
            fetchBook(slug, {includeMedia});
        }
    }, [slug, includeMedia, fetchBook]);

    if (loading) return <div>Loading...</div>;
    if (error) return <div>Error: {error}</div>;
    if (!currentBook) return <div>Book not found</div>;

    // Type guard to check if currentBook has mediaItems property
    const hasMedia = (book: any): book is BookWithMediaDTO => {
        return "mediaItems" in book;
    };

    return (
        <div className="book-details">
            <h1>{currentBook.title}</h1>
            <p>{currentBook.description}</p>

            {hasMedia(currentBook) && currentBook.featuredImage && (
                <div className="featured-image">
                    <img
                        src={currentBook.featuredImage.url}
                        alt={
                            currentBook.featuredImage.description ||
                            currentBook.title
                        }
                    />
                </div>
            )}

            <div
                className="content"
                dangerouslySetInnerHTML={{__html: currentBook.content}}
            />

            <div className="metadata">
                <p>Category: {currentBook.category}</p>
                <p>Status: {currentBook.status}</p>
                <p>
                    Published:{" "}
                    {new Date(currentBook.publishDate).toLocaleDateString()}
                </p>
                <p>
                    Last Modified:{" "}
                    {new Date(currentBook.lastModified).toLocaleDateString()}
                </p>
            </div>

            {hasMedia(currentBook) && currentBook.mediaItems.length > 0 && (
                <div className="media-gallery">
                    <h2>Related Media</h2>
                    <div className="media-grid">
                        {currentBook.mediaItems.map((media) => (
                            <div key={media.id} className="media-item">
                                {media.mediaType === "image" ? (
                                    <img
                                        src={media.thumbnailUrl || media.url}
                                        alt={media.description || media.name}
                                    />
                                ) : (
                                    <div className="media-placeholder">
                                        {media.mediaType}
                                    </div>
                                )}
                                <p>{media.name}</p>
                            </div>
                        ))}
                    </div>
                </div>
            )}
        </div>
    );
};
```

#### Important Implementation Notes for Books API

1. **Media References Parsing**: The `mediaReferencesJson` field contains a JSON string array that needs to be parsed:

    ```typescript
    const mediaIds = JSON.parse(book.mediaReferencesJson || "[]");
    ```

2. **Date Handling**: Always convert date strings to JavaScript Date objects for manipulation, then back to ISO strings for API requests:

    ```typescript
    // For display
    const displayDate = new Date(book.publishDate).toLocaleDateString();

    // For form input
    const inputDateValue = new Date(book.publishDate)
        .toISOString()
        .split("T")[0];

    // Before sending to API
    const bookData = {
        ...formValues,
        publishDate: new Date(formValues.publishDate).toISOString()
    };
    ```

3. **Error Handling**: Implement proper error handling for all API calls, especially for validation errors:

    ```typescript
    try {
        await booksService.upsertBook(slug, bookData);
    } catch (error) {
        if (error.response?.status === 400) {
            // Handle validation errors
            const validationErrors = error.response.data.errors;
            setErrors(validationErrors);
        } else {
            // Handle other errors
            setError("An unexpected error occurred. Please try again.");
        }
    }
    ```

4. **Content Security**: If displaying HTML content, consider using a sanitization library:

    ```typescript
    import DOMPurify from "dompurify";

    // In your component
    <div
        dangerouslySetInnerHTML={{__html: DOMPurify.sanitize(book.content)}}
    />;
    ```

5. **Optimistic Updates**: For better UX, implement optimistic updates in the store:

    ```typescript
    // Before API call
    const previousBooks = [...get().books];

    // Optimistically update the UI
    set((state) => ({
        books: state.books.filter((b) => b.slug !== slug)
    }));

    try {
        await BooksService.deleteBook(slug);
    } catch (error) {
        // Revert on failure
        set({books: previousBooks, error: "Failed to delete book"});
    }
    ```

## Blog Posts API

### Blog Posts Endpoints

| Function Name                  | HTTP Method | Endpoint                             | Description                          | Query Parameters                                                                                                                                                                                                                                                                         | Request Body                | Response                                         |
| ------------------------------ | ----------- | ------------------------------------ | ------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------- | ------------------------------------------------ |
| `GetBlogPosts`                 | GET         | `/blog-posts`                        | Retrieves a collection of blog posts | `authorSlug` (optional): Filter by author<br>`category` (optional): Filter by category<br>`tag` (optional): Filter by tag<br>`isPublished` (optional): Filter by publication status<br>`limit` (optional): Max results (default: 50)<br>`includeMedia` (optional): Include media content | None                        | Array of `BlogPostDTO` or `BlogPostWithMediaDTO` |
| `GetBlogPost`                  | GET         | `/blog-posts/{slug}`                 | Retrieves a specific blog post       | `isPublished` (optional): Filter by publication status<br>`includeMedia` (optional): Include media content                                                                                                                                                                               | None                        | `BlogPostDTO` or `BlogPostWithMediaDTO`          |
| `UpsertBlogPost`               | POST/PUT    | `/blog-posts/{slug}`                 | Creates or updates a blog post       | None                                                                                                                                                                                                                                                                                     | `BlogPostModel` (see below) | `BlogPostDTO`                                    |
| `DeleteBlogPost`               | DELETE      | `/blog-posts/{slug}`                 | Removes a blog post                  | None                                                                                                                                                                                                                                                                                     | None                        | 200 OK or 404 Not Found                          |
| `SetBlogPostFeaturedImage`     | POST        | `/blog-posts/{slug}/featured-image`  | Sets blog post featured image        | None                                                                                                                                                                                                                                                                                     | `{ "mediaId": "guid" }`     | `BlogPostDTO`                                    |
| `SetBlogPostFeaturedVideo`     | POST        | `/blog-posts/{slug}/featured-video`  | Sets blog post featured video        | None                                                                                                                                                                                                                                                                                     | `{ "mediaId": "guid" }`     | `BlogPostDTO`                                    |
| `AddBlogPostMediaReference`    | POST        | `/blog-posts/{slug}/media`           | Adds media to a blog post            | None                                                                                                                                                                                                                                                                                     | `{ "mediaId": "guid" }`     | `BlogPostDTO`                                    |
| `RemoveBlogPostMediaReference` | DELETE      | `/blog-posts/{slug}/media/{mediaId}` | Removes media from a blog post       | None                                                                                                                                                                                                                                                                                     | None                        | `BlogPostDTO`                                    |

### Blog Posts Data Models

#### BlogPostDTO (Response Model)

```typescript
interface BlogPostDTO {
    id: string;
    partitionKey: string;
    rowKey: string;
    timestamp?: string;
    title: string;
    authorSlug: string;
    description: string;
    content: string;
    slug: string;
    category: string;
    status: string;
    isPublished: boolean;
    featuredImageId?: string;
    mediaReferencesJson: string;
    publishDate: string; // ISO date string
    lastModified: string; // ISO date string
    tagsList: string[];
    readingTime?: number; // In minutes, calculated on the server
    disableComments: boolean;
    priority?: number;
}
```

#### BlogPostWithMediaDTO (Response Model with Media)

```typescript
interface BlogPostWithMediaDTO {
    blogPost: BlogPostDTO;
    mediaItems: MediaItemModel[];
    featuredImage?: MediaItemModel;
}

interface MediaItemModel {
    id: string;
    name: string;
    description?: string;
    mediaType: string; // "image", "video", etc.
    url: string;
    thumbnailUrl?: string;
    contentType: string;
    size: number;
    width?: number;
    height?: number;
    purpose?: string;
    uploadedAt: string; // ISO date string
}
```

#### BlogPost Request Model (for creating/updating blog posts)

```typescript
interface BlogPostRequest {
    title: string; // Required
    authorSlug: string; // Required
    description?: string;
    content: string; // Required
    category: string; // Required
    status?: string; // Default: "Draft"
    featuredImageId?: string;
    publishDate?: string; // ISO date string, optional
    tagsList: string[]; // Can be empty array, but required
    disableComments?: boolean; // Default: false
    priority?: number;
}
```

### Blog Posts Integration Guidelines

#### Setting Up Blog Posts API Service

```typescript
// src/services/blogPostsService.ts
import apiClient from "../utils/apiClient";
import {
    BlogPostDTO,
    BlogPostWithMediaDTO,
    BlogPostRequest
} from "../types/BlogPostTypes";

export const BlogPostsService = {
    // Get all blog posts with optional filtering
    async getBlogPosts(params?: {
        authorSlug?: string;
        category?: string;
        tag?: string;
        isPublished?: boolean;
        limit?: number;
        includeMedia?: boolean;
    }): Promise<BlogPostDTO[] | BlogPostWithMediaDTO[]> {
        const response = await apiClient.get("/blog-posts", {params});
        return response.data;
    },

    // Get a single blog post by slug
    async getBlogPost(
        slug: string,
        params?: {
            isPublished?: boolean;
            includeMedia?: boolean;
        }
    ): Promise<BlogPostDTO | BlogPostWithMediaDTO> {
        const response = await apiClient.get(`/blog-posts/${slug}`, {params});
        return response.data;
    },

    // Create or update a blog post
    async upsertBlogPost(
        slug: string,
        blogPost: BlogPostRequest
    ): Promise<BlogPostDTO> {
        const response = await apiClient.post(`/blog-posts/${slug}`, blogPost);
        return response.data;
    },

    // Delete a blog post
    async deleteBlogPost(slug: string): Promise<void> {
        await apiClient.delete(`/blog-posts/${slug}`);
    },

    // Set featured image
    async setFeaturedImage(
        slug: string,
        mediaId: string
    ): Promise<BlogPostDTO> {
        const response = await apiClient.post(
            `/blog-posts/${slug}/featured-image`,
            {mediaId}
        );
        return response.data;
    },

    // Set featured video
    async setFeaturedVideo(
        slug: string,
        mediaId: string
    ): Promise<BlogPostDTO> {
        const response = await apiClient.post(
            `/blog-posts/${slug}/featured-video`,
            {mediaId}
        );
        return response.data;
    },

    // Add media reference to a blog post
    async addMediaReference(
        slug: string,
        mediaId: string
    ): Promise<BlogPostDTO> {
        const response = await apiClient.post(`/blog-posts/${slug}/media`, {
            mediaId
        });
        return response.data;
    },

    // Remove media reference from a blog post
    async removeMediaReference(
        slug: string,
        mediaId: string
    ): Promise<BlogPostDTO> {
        const response = await apiClient.delete(
            `/blog-posts/${slug}/media/${mediaId}`
        );
        return response.data;
    }
};
```

#### Zustand Store Integration

```typescript
// src/store/blogPostsStore.ts
import create from "zustand";
import {BlogPostsService} from "../services/blogPostsService";
import {
    BlogPostDTO,
    BlogPostWithMediaDTO,
    BlogPostRequest
} from "../types/BlogPostTypes";

interface BlogPostsState {
    blogPosts: BlogPostDTO[];
    currentBlogPost: BlogPostDTO | BlogPostWithMediaDTO | null;
    loading: boolean;
    error: string | null;
    fetchBlogPosts: (params?: any) => Promise<void>;
    fetchBlogPost: (slug: string, params?: any) => Promise<void>;
    createBlogPost: (slug: string, blogPost: BlogPostRequest) => Promise<void>;
    updateBlogPost: (slug: string, blogPost: BlogPostRequest) => Promise<void>;
    deleteBlogPost: (slug: string) => Promise<void>;
    setFeaturedImage: (slug: string, mediaId: string) => Promise<void>;
    setFeaturedVideo: (slug: string, mediaId: string) => Promise<void>;
    addMediaReference: (slug: string, mediaId: string) => Promise<void>;
    removeMediaReference: (slug: string, mediaId: string) => Promise<void>;
    // Additional helper functions
    getBlogPostsByCategory: (category: string) => BlogPostDTO[];
    getBlogPostsByTag: (tag: string) => BlogPostDTO[];
    getPublishedBlogPosts: () => BlogPostDTO[];
}

export const useBlogPostsStore = create<BlogPostsState>((set, get) => ({
    blogPosts: [],
    currentBlogPost: null,
    loading: false,
    error: null,

    fetchBlogPosts: async (params) => {
        set({loading: true, error: null});
        try {
            const blogPosts = await BlogPostsService.getBlogPosts(params);
            set({blogPosts: blogPosts as BlogPostDTO[], loading: false});
        } catch (error) {
            console.error("Error fetching blog posts:", error);
            set({error: "Failed to fetch blog posts", loading: false});
        }
    },

    fetchBlogPost: async (slug, params) => {
        set({loading: true, error: null});
        try {
            const blogPost = await BlogPostsService.getBlogPost(slug, params);
            set({currentBlogPost: blogPost, loading: false});
        } catch (error) {
            console.error(`Error fetching blog post ${slug}:`, error);
            set({error: `Failed to fetch blog post ${slug}`, loading: false});
        }
    },

    createBlogPost: async (slug, blogPost) => {
        set({loading: true, error: null});
        try {
            const newBlogPost = await BlogPostsService.upsertBlogPost(
                slug,
                blogPost
            );
            set((state) => ({
                blogPosts: [...state.blogPosts, newBlogPost],
                currentBlogPost: newBlogPost,
                loading: false
            }));
        } catch (error) {
            console.error("Error creating blog post:", error);
            set({error: "Failed to create blog post", loading: false});
        }
    },

    updateBlogPost: async (slug, blogPost) => {
        set({loading: true, error: null});
        try {
            const updatedBlogPost = await BlogPostsService.upsertBlogPost(
                slug,
                blogPost
            );
            set((state) => ({
                blogPosts: state.blogPosts.map((bp) =>
                    bp.slug === slug ? updatedBlogPost : bp
                ),
                currentBlogPost: updatedBlogPost,
                loading: false
            }));
        } catch (error) {
            console.error(`Error updating blog post ${slug}:`, error);
            set({error: `Failed to update blog post ${slug}`, loading: false});
        }
    },

    deleteBlogPost: async (slug) => {
        set({loading: true, error: null});
        try {
            await BlogPostsService.deleteBlogPost(slug);
            set((state) => ({
                blogPosts: state.blogPosts.filter((bp) => bp.slug !== slug),
                currentBlogPost:
                    state.currentBlogPost?.slug === slug
                        ? null
                        : state.currentBlogPost,
                loading: false
            }));
        } catch (error) {
            console.error(`Error deleting blog post ${slug}:`, error);
            set({error: `Failed to delete blog post ${slug}`, loading: false});
        }
    },

    setFeaturedImage: async (slug, mediaId) => {
        set({loading: true, error: null});
        try {
            const updatedBlogPost = await BlogPostsService.setFeaturedImage(
                slug,
                mediaId
            );
            set((state) => ({
                blogPosts: state.blogPosts.map((bp) =>
                    bp.slug === slug ? updatedBlogPost : bp
                ),
                currentBlogPost:
                    state.currentBlogPost?.slug === slug
                        ? updatedBlogPost
                        : state.currentBlogPost,
                loading: false
            }));
        } catch (error) {
            console.error(
                `Error setting featured image for blog post ${slug}:`,
                error
            );
            set({
                error: `Failed to set featured image for blog post ${slug}`,
                loading: false
            });
        }
    },

    setFeaturedVideo: async (slug, mediaId) => {
        set({loading: true, error: null});
        try {
            const updatedBlogPost = await BlogPostsService.setFeaturedVideo(
                slug,
                mediaId
            );
            set((state) => ({
                blogPosts: state.blogPosts.map((bp) =>
                    bp.slug === slug ? updatedBlogPost : bp
                ),
                currentBlogPost:
                    state.currentBlogPost?.slug === slug
                        ? updatedBlogPost
                        : state.currentBlogPost,
                loading: false
            }));
        } catch (error) {
            console.error(
                `Error setting featured video for blog post ${slug}:`,
                error
            );
            set({
                error: `Failed to set featured video for blog post ${slug}`,
                loading: false
            });
        }
    },

    addMediaReference: async (slug, mediaId) => {
        set({loading: true, error: null});
        try {
            const updatedBlogPost = await BlogPostsService.addMediaReference(
                slug,
                mediaId
            );
            set((state) => ({
                blogPosts: state.blogPosts.map((bp) =>
                    bp.slug === slug ? updatedBlogPost : bp
                ),
                currentBlogPost:
                    state.currentBlogPost?.slug === slug
                        ? updatedBlogPost
                        : state.currentBlogPost,
                loading: false
            }));
        } catch (error) {
            console.error(
                `Error adding media reference to blog post ${slug}:`,
                error
            );
            set({
                error: `Failed to add media reference to blog post ${slug}`,
                loading: false
            });
        }
    },

    removeMediaReference: async (slug, mediaId) => {
        set({loading: true, error: null});
        try {
            const updatedBlogPost = await BlogPostsService.removeMediaReference(
                slug,
                mediaId
            );
            set((state) => ({
                blogPosts: state.blogPosts.map((bp) =>
                    bp.slug === slug ? updatedBlogPost : bp
                ),
                currentBlogPost:
                    state.currentBlogPost?.slug === slug
                        ? updatedBlogPost
                        : state.currentBlogPost,
                loading: false
            }));
        } catch (error) {
            console.error(
                `Error removing media reference from blog post ${slug}:`,
                error
            );
            set({
                error: `Failed to remove media reference from blog post ${slug}`,
                loading: false
            });
        }
    },

    // Helper functions for filtering
    getBlogPostsByCategory: (category) => {
        return get().blogPosts.filter((post) => post.category === category);
    },

    getBlogPostsByTag: (tag) => {
        return get().blogPosts.filter((post) => post.tagsList.includes(tag));
    },

    getPublishedBlogPosts: () => {
        return get().blogPosts.filter(
            (post) =>
                post.isPublished && new Date(post.publishDate) <= new Date()
        );
    }
}));
```

#### React Component Example

```tsx
// src/components/blog/BlogPostDetails.tsx
import React, {useEffect} from "react";
import {useParams} from "react-router-dom";
import {useBlogPostsStore} from "../../store/blogPostsStore";
import {formatDate} from "../../utils/dateUtils";
import DOMPurify from "dompurify";

interface BlogPostDetailsProps {
    includeMedia?: boolean;
}

export const BlogPostDetails: React.FC<BlogPostDetailsProps> = ({
    includeMedia = true
}) => {
    const {slug} = useParams<{slug: string}>();
    const {currentBlogPost, loading, error, fetchBlogPost} =
        useBlogPostsStore();

    useEffect(() => {
        if (slug) {
            fetchBlogPost(slug, {includeMedia});
        }
    }, [slug, includeMedia, fetchBlogPost]);

    if (loading) return <div className="loading">Loading...</div>;
    if (error) return <div className="error">Error: {error}</div>;
    if (!currentBlogPost)
        return <div className="not-found">Blog post not found</div>;

    // Type guard to check if currentBlogPost has mediaItems property
    const hasMedia = (post: any): post is BlogPostWithMediaDTO => {
        return "mediaItems" in post;
    };

    // Check if the blog post has the actual BlogPostDTO properties or the wrapped version
    const post =
        "blogPost" in currentBlogPost
            ? currentBlogPost.blogPost
            : currentBlogPost;

    return (
        <article className="blog-post-details">
            <header>
                <h1>{post.title}</h1>
                <div className="meta">
                    <span className="date">{formatDate(post.publishDate)}</span>
                    <span className="reading-time">
                        {post.readingTime || 0} min read
                    </span>
                </div>

                {hasMedia(currentBlogPost) && currentBlogPost.featuredImage && (
                    <div className="featured-image">
                        <img
                            src={currentBlogPost.featuredImage.url}
                            alt={
                                currentBlogPost.featuredImage.description ||
                                post.title
                            }
                        />
                    </div>
                )}

                <p className="description">{post.description}</p>
            </header>

            <div
                className="content"
                dangerouslySetInnerHTML={{
                    __html: DOMPurify.sanitize(post.content)
                }}
            />

            <footer>
                <div className="tags">
                    {post.tagsList.map((tag) => (
                        <span key={tag} className="tag">
                            #{tag}
                        </span>
                    ))}
                </div>

                <div className="category">
                    Category: <span>{post.category}</span>
                </div>

                {!post.disableComments && (
                    <div className="comments-section">
                        {/* Comments component would go here */}
                        <h3>Comments</h3>
                        {/* Comments implementation */}
                    </div>
                )}
            </footer>

            {hasMedia(currentBlogPost) &&
                currentBlogPost.mediaItems &&
                currentBlogPost.mediaItems.length > 0 && (
                    <div className="related-media">
                        <h3>Related Media</h3>
                        <div className="media-grid">
                            {currentBlogPost.mediaItems.map((media) => (
                                <div key={media.id} className="media-item">
                                    {media.mediaType === "image" ? (
                                        <img
                                            src={
                                                media.thumbnailUrl || media.url
                                            }
                                            alt={
                                                media.description || media.name
                                            }
                                        />
                                    ) : media.mediaType === "video" ? (
                                        <video
                                            controls
                                            src={media.url}
                                            poster={media.thumbnailUrl}
                                        >
                                            Your browser does not support the
                                            video tag.
                                        </video>
                                    ) : (
                                        <div className="media-placeholder">
                                            {media.mediaType}
                                        </div>
                                    )}
                                    <p>{media.name}</p>
                                </div>
                            ))}
                        </div>
                    </div>
                )}
        </article>
    );
};
```

#### Important Implementation Notes for Blog Posts API

1. **Date Formatting Utility**:

    ```typescript
    // src/utils/dateUtils.ts
    export const formatDate = (dateString: string): string => {
        if (!dateString) return "";

        const date = new Date(dateString);
        return new Intl.DateTimeFormat("en-US", {
            year: "numeric",
            month: "long",
            day: "numeric"
        }).format(date);
    };

    export const isValidDate = (dateString: string): boolean => {
        if (!dateString) return false;
        const date = new Date(dateString);
        return !isNaN(date.getTime());
    };

    export const formatForInput = (dateString: string): string => {
        if (!dateString) return "";
        const date = new Date(dateString);
        if (isNaN(date.getTime())) return "";

        return date.toISOString().split("T")[0]; // YYYY-MM-DD format for input[type="date"]
    };
    ```

2. **Media References Handling**:

    ```typescript
    // Parse JSON media references
    const parseMediaReferences = (jsonString?: string): string[] => {
        try {
            return JSON.parse(jsonString || "[]");
        } catch (e) {
            console.error("Error parsing media references:", e);
            return [];
        }
    };

    // In your component
    const mediaIds = parseMediaReferences(post.mediaReferencesJson);
    ```

3. **Form Validation for Blog Posts**:

    ```tsx
    // src/components/blog/BlogPostForm.tsx
    import React, {useState} from "react";
    import {useBlogPostsStore} from "../../store/blogPostsStore";
    import {formatForInput} from "../../utils/dateUtils";

    interface BlogPostFormProps {
        slug?: string; // If provided, we're editing; otherwise creating
        onSuccess?: () => void;
    }

    export const BlogPostForm: React.FC<BlogPostFormProps> = ({
        slug,
        onSuccess
    }) => {
        const {
            currentBlogPost,
            createBlogPost,
            updateBlogPost,
            loading,
            error
        } = useBlogPostsStore();
        const isEditing = !!slug;

        const [form, setForm] = useState({
            title: currentBlogPost?.title || "",
            authorSlug: currentBlogPost?.authorSlug || "",
            description: currentBlogPost?.description || "",
            content: currentBlogPost?.content || "",
            category: currentBlogPost?.category || "",
            status: currentBlogPost?.status || "Draft",
            publishDate: formatForInput(currentBlogPost?.publishDate || ""),
            tagsList: currentBlogPost?.tagsList || [],
            disableComments: currentBlogPost?.disableComments || false
        });

        const [validationErrors, setValidationErrors] = useState<
            Record<string, string>
        >({});

        const validate = () => {
            const errors: Record<string, string> = {};

            if (!form.title.trim()) errors.title = "Title is required";
            if (!form.authorSlug.trim())
                errors.authorSlug = "Author is required";
            if (!form.content.trim()) errors.content = "Content is required";
            if (!form.category.trim()) errors.category = "Category is required";

            setValidationErrors(errors);
            return Object.keys(errors).length === 0;
        };

        const handleSubmit = async (e: React.FormEvent) => {
            e.preventDefault();

            if (!validate()) return;

            const blogPostData = {
                ...form,
                publishDate: form.publishDate
                    ? new Date(form.publishDate).toISOString()
                    : undefined
            };

            try {
                if (isEditing) {
                    await updateBlogPost(slug!, blogPostData);
                } else {
                    // Generate a slug from the title if not editing
                    const newSlug = form.title
                        .toLowerCase()
                        .replace(/[^a-z0-9]+/g, "-")
                        .replace(/^-|-$/g, "");
                    await createBlogPost(newSlug, blogPostData);
                }

                if (onSuccess) onSuccess();
            } catch (err) {
                console.error("Error saving blog post:", err);
            }
        };

        const handleChange = (
            e: React.ChangeEvent<
                HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement
            >
        ) => {
            const {name, value, type} = e.target;
            const checked =
                type === "checkbox"
                    ? (e.target as HTMLInputElement).checked
                    : undefined;

            setForm((prev) => ({
                ...prev,
                [name]: type === "checkbox" ? checked : value
            }));

            // Clear validation error when field is edited
            if (validationErrors[name]) {
                setValidationErrors((prev) => ({...prev, [name]: ""}));
            }
        };

        const handleTagsChange = (e: React.ChangeEvent<HTMLInputElement>) => {
            const tags = e.target.value
                .split(",")
                .map((tag) => tag.trim())
                .filter((tag) => tag);
            setForm((prev) => ({...prev, tagsList: tags}));
        };

        return (
            <form onSubmit={handleSubmit} className="blog-post-form">
                <div className="form-group">
                    <label htmlFor="title">Title *</label>
                    <input
                        id="title"
                        name="title"
                        type="text"
                        value={form.title}
                        onChange={handleChange}
                        className={validationErrors.title ? "error" : ""}
                    />
                    {validationErrors.title && (
                        <p className="error-message">
                            {validationErrors.title}
                        </p>
                    )}
                </div>

                {/* Other form fields... */}

                <div className="form-actions">
                    <button type="submit" disabled={loading}>
                        {loading
                            ? "Saving..."
                            : isEditing
                            ? "Update Blog Post"
                            : "Create Blog Post"}
                    </button>
                    {error && <p className="form-error">{error}</p>}
                </div>
            </form>
        );
    };
    ```

4. **Handling Draft vs Published Status**:

    ```typescript
    // Utility for determining if a post should be visible
    const isPostVisible = (post: BlogPostDTO, userIsAdmin = false): boolean => {
        if (userIsAdmin) return true; // Admins see everything

        if (post.status !== "Published") return false;
        if (!post.isPublished) return false;

        const publishDate = new Date(post.publishDate);
        const now = new Date();
        return publishDate <= now;
    };

    // Filter visible posts
    const visiblePosts = posts.filter((post) =>
        isPostVisible(post, userHasAdminRole)
    );
    ```

5. **Rich Text Editor Integration**:

    ```tsx
    // src/components/common/RichTextEditor.tsx (simplified example)
    import React from "react";
    import {CKEditor} from "@ckeditor/ckeditor5-react";
    import ClassicEditor from "@ckeditor/ckeditor5-build-classic";

    interface RichTextEditorProps {
        value: string;
        onChange: (data: string) => void;
        placeholder?: string;
    }

    export const RichTextEditor: React.FC<RichTextEditorProps> = ({
        value,
        onChange,
        placeholder
    }) => {
        return (
            <CKEditor
                editor={ClassicEditor}
                data={value}
                config={{
                    placeholder: placeholder,
                    mediaEmbed: {
                        previewsInData: true
                    },
                    toolbar: [
                        "heading",
                        "|",
                        "bold",
                        "italic",
                        "link",
                        "|",
                        "bulletedList",
                        "numberedList",
                        "|",
                        "imageUpload",
                        "blockQuote",
                        "insertTable",
                        "|",
                        "undo",
                        "redo"
                    ]
                }}
                onChange={(event, editor) => {
                    const data = editor.getData();
                    onChange(data);
                }}
            />
        );
    };

    // Usage in BlogPostForm
    <div className="form-group">
        <label htmlFor="content">Content *</label>
        <RichTextEditor
            value={form.content}
            onChange={(data) => setForm((prev) => ({...prev, content: data}))}
            placeholder="Write your blog post content here..."
        />
        {validationErrors.content && (
            <p className="error-message">{validationErrors.content}</p>
        )}
    </div>;
    ```

## Contact Me API

### Contact Me Endpoints

| Function Name | HTTP Method | Endpoint   | Description            | Request Body               | Response                                                                |
| ------------- | ----------- | ---------- | ---------------------- | -------------------------- | ----------------------------------------------------------------------- |
| `ContactMe`   | POST        | `/contact` | Submits a contact form | `ContactMeDTO` (see below) | `{ "success": true, "message": "Contact form submitted successfully" }` |

### Contact Me Data Models

#### ContactMeDTO (Request Model)

```typescript
interface ContactMeDTO {
    name: string; // Required, min 2 chars
    email: string; // Required, valid email format
    message: string; // Required, min 10 chars
    // Note: Additional fields like phone, company, website, subject
    // can be included but are not processed by the current DTO
}
```

#### Contact Response Model

```typescript
interface ContactResponse {
    success: boolean;
    message: string;
}
```

#### Contact Error Response

```typescript
interface ContactErrorResponse {
    errors: string[];
}
```

## Portfolio Pieces API

### Portfolio Pieces Endpoints

| Function Name                        | HTTP Method | Endpoint                                   | Description                                | Query Parameters                                                                                                                                                                                                                                      | Request Body                      | Response                                                     |
| ------------------------------------ | ----------- | ------------------------------------------ | ------------------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------- | ------------------------------------------------------------ |
| `GetPortfolioPieces`                 | GET         | `/portfolio-pieces`                        | Retrieves a collection of portfolio pieces | `authorSlug` (optional): Filter by author<br>`category` (optional): Filter by category<br>`isPublished` (optional): Filter by publication status<br>`limit` (optional): Max results (default: 50)<br>`includeMedia` (optional): Include media content | None                              | Array of `PortfolioPieceDTO` or `PortfolioPieceWithMediaDTO` |
| `GetPortfolioPiece`                  | GET         | `/portfolio-pieces/{slug}`                 | Retrieves a specific portfolio piece       | `isPublished` (optional): Filter by publication status<br>`includeMedia` (optional): Include media content                                                                                                                                            | None                              | `PortfolioPieceDTO` or `PortfolioPieceWithMediaDTO`          |
| `UpsertPortfolioPiece`               | POST/PUT    | `/portfolio-pieces/{slug}`                 | Creates or updates a portfolio piece       | None                                                                                                                                                                                                                                                  | `PortfolioPieceModel` (see below) | `PortfolioPieceDTO`                                          |
| `DeletePortfolioPiece`               | DELETE      | `/portfolio-pieces/{slug}`                 | Removes a portfolio piece                  | None                                                                                                                                                                                                                                                  | None                              | 200 OK or 404 Not Found                                      |
| `SetPortfolioPieceFeaturedImage`     | POST        | `/portfolio-pieces/{slug}/featured-image`  | Sets portfolio piece featured image        | None                                                                                                                                                                                                                                                  | `{ "mediaId": "guid" }`           | `PortfolioPieceDTO`                                          |
| `SetPortfolioPieceFeaturedVideo`     | POST        | `/portfolio-pieces/{slug}/featured-video`  | Sets portfolio piece featured video        | None                                                                                                                                                                                                                                                  | `{ "mediaId": "guid" }`           | `PortfolioPieceDTO`                                          |
| `AddPortfolioPieceMediaReference`    | POST        | `/portfolio-pieces/{slug}/media`           | Adds media to a portfolio piece            | None                                                                                                                                                                                                                                                  | `{ "mediaId": "guid" }`           | `PortfolioPieceDTO`                                          |
| `RemovePortfolioPieceMediaReference` | DELETE      | `/portfolio-pieces/{slug}/media/{mediaId}` | Removes media from a portfolio piece       | None                                                                                                                                                                                                                                                  | None                              | `PortfolioPieceDTO`                                          |

### Portfolio Pieces Data Models

#### PortfolioPieceDTO (Response Model)

```typescript
interface PortfolioPieceDTO {
    id: string;
    partitionKey: string;
    rowKey: string;
    timestamp?: string;
    title: string;
    authorSlug: string;
    description: string;
    content: string;
    slug: string;
    category: string;
    status: string;
    isPublished: boolean;
    featuredImageId?: string;
    mediaReferencesJson: string;
    projectUrl?: string;
    sourceCodeUrl?: string;
    publishDate: string; // ISO date string
    lastModified: string; // ISO date string
    tagsList: string[];
    completionDate?: string; // ISO date string
    clientName?: string;
    priority?: number;
}
```

#### PortfolioPieceWithMediaDTO (Response Model with Media)

```typescript
interface PortfolioPieceWithMediaDTO {
    portfolioPiece: PortfolioPieceDTO;
    mediaItems: MediaItemModel[];
    featuredImage?: MediaItemModel;
}

// Using the same MediaItemModel from other sections
```

#### PortfolioPiece Request Model (for creating/updating portfolio pieces)

```typescript
interface PortfolioPieceRequest {
    title: string; // Required
    authorSlug: string; // Required
    description?: string;
    content: string; // Required
    category: string; // Required
    status?: string; // Default: "Draft"
    featuredImageId?: string;
    publishDate?: string; // ISO date string, optional
    tagsList: string[]; // Can be empty array, but required
    projectUrl?: string;
    sourceCodeUrl?: string;
    completionDate?: string; // ISO date string, optional
    clientName?: string;
    priority?: number;
}
```

### Portfolio Pieces Integration Guidelines

#### Setting Up Portfolio Pieces API Service

```typescript
// src/services/portfolioPiecesService.ts
import apiClient from "../utils/apiClient";
import {
    PortfolioPieceDTO,
    PortfolioPieceWithMediaDTO,
    PortfolioPieceRequest
} from "../types/PortfolioPieceTypes";

export const PortfolioPiecesService = {
    // Get all portfolio pieces with optional filtering
    async getPortfolioPieces(params?: {
        authorSlug?: string;
        category?: string;
        isPublished?: boolean;
        limit?: number;
        includeMedia?: boolean;
    }): Promise<PortfolioPieceDTO[] | PortfolioPieceWithMediaDTO[]> {
        const response = await apiClient.get("/portfolio-pieces", {params});
        return response.data;
    },

    // Get a single portfolio piece by slug
    async getPortfolioPiece(
        slug: string,
        params?: {
            isPublished?: boolean;
            includeMedia?: boolean;
        }
    ): Promise<PortfolioPieceDTO | PortfolioPieceWithMediaDTO> {
        const response = await apiClient.get(`/portfolio-pieces/${slug}`, {
            params
        });
        return response.data;
    },

    // Create or update a portfolio piece
    async upsertPortfolioPiece(
        slug: string,
        portfolioPiece: PortfolioPieceRequest
    ): Promise<PortfolioPieceDTO> {
        const response = await apiClient.post(
            `/portfolio-pieces/${slug}`,
            portfolioPiece
        );
        return response.data;
    },

    // Delete a portfolio piece
    async deletePortfolioPiece(slug: string): Promise<void> {
        await apiClient.delete(`/portfolio-pieces/${slug}`);
    },

    // Set featured image
    async setFeaturedImage(
        slug: string,
        mediaId: string
    ): Promise<PortfolioPieceDTO> {
        const response = await apiClient.post(
            `/portfolio-pieces/${slug}/featured-image`,
            {mediaId}
        );
        return response.data;
    },

    // Set featured video
    async setFeaturedVideo(
        slug: string,
        mediaId: string
    ): Promise<PortfolioPieceDTO> {
        const response = await apiClient.post(
            `/portfolio-pieces/${slug}/featured-video`,
            {mediaId}
        );
        return response.data;
    },

    // Add media reference to a portfolio piece
    async addMediaReference(
        slug: string,
        mediaId: string
    ): Promise<PortfolioPieceDTO> {
        const response = await apiClient.post(
            `/portfolio-pieces/${slug}/media`,
            {mediaId}
        );
        return response.data;
    },

    // Remove media reference from a portfolio piece
    async removeMediaReference(
        slug: string,
        mediaId: string
    ): Promise<PortfolioPieceDTO> {
        const response = await apiClient.delete(
            `/portfolio-pieces/${slug}/media/${mediaId}`
        );
        return response.data;
    }
};
```

#### Zustand Store Integration

```typescript
// src/store/portfolioPiecesStore.ts
import create from "zustand";
import {PortfolioPiecesService} from "../services/portfolioPiecesService";
import {
    PortfolioPieceDTO,
    PortfolioPieceWithMediaDTO,
    PortfolioPieceRequest
} from "../types/PortfolioPieceTypes";

interface PortfolioPiecesState {
    portfolioPieces: PortfolioPieceDTO[];
    currentPortfolioPiece:
        | PortfolioPieceDTO
        | PortfolioPieceWithMediaDTO
        | null;
    loading: boolean;
    error: string | null;
    fetchPortfolioPieces: (params?: any) => Promise<void>;
    fetchPortfolioPiece: (slug: string, params?: any) => Promise<void>;
    createPortfolioPiece: (
        slug: string,
        portfolioPiece: PortfolioPieceRequest
    ) => Promise<void>;
    updatePortfolioPiece: (
        slug: string,
        portfolioPiece: PortfolioPieceRequest
    ) => Promise<void>;
    deletePortfolioPiece: (slug: string) => Promise<void>;
    setFeaturedImage: (slug: string, mediaId: string) => Promise<void>;
    setFeaturedVideo: (slug: string, mediaId: string) => Promise<void>;
    addMediaReference: (slug: string, mediaId: string) => Promise<void>;
    removeMediaReference: (slug: string, mediaId: string) => Promise<void>;
    // Additional helper functions
    getPortfolioPiecesByCategory: (category: string) => PortfolioPieceDTO[];
    getPublishedPortfolioPieces: () => PortfolioPieceDTO[];
    getFeaturedPortfolioPieces: () => PortfolioPieceDTO[];
}

export const usePortfolioPiecesStore = create<PortfolioPiecesState>(
    (set, get) => ({
        portfolioPieces: [],
        currentPortfolioPiece: null,
        loading: false,
        error: null,

        fetchPortfolioPieces: async (params) => {
            set({loading: true, error: null});
            try {
                const portfolioPieces =
                    await PortfolioPiecesService.getPortfolioPieces(params);
                set({
                    portfolioPieces: portfolioPieces as PortfolioPieceDTO[],
                    loading: false
                });
            } catch (error) {
                console.error("Error fetching portfolio pieces:", error);
                set({
                    error: "Failed to fetch portfolio pieces",
                    loading: false
                });
            }
        },

        fetchPortfolioPiece: async (slug, params) => {
            set({loading: true, error: null});
            try {
                const portfolioPiece =
                    await PortfolioPiecesService.getPortfolioPiece(
                        slug,
                        params
                    );
                set({currentPortfolioPiece: portfolioPiece, loading: false});
            } catch (error) {
                console.error(`Error fetching portfolio piece ${slug}:`, error);
                set({
                    error: `Failed to fetch portfolio piece ${slug}`,
                    loading: false
                });
            }
        },

        createPortfolioPiece: async (slug, portfolioPiece) => {
            set({loading: true, error: null});
            try {
                const newPortfolioPiece =
                    await PortfolioPiecesService.upsertPortfolioPiece(
                        slug,
                        portfolioPiece
                    );
                set((state) => ({
                    portfolioPieces: [
                        ...state.portfolioPieces,
                        newPortfolioPiece
                    ],
                    currentPortfolioPiece: newPortfolioPiece,
                    loading: false
                }));
            } catch (error) {
                console.error("Error creating portfolio piece:", error);
                set({
                    error: "Failed to create portfolio piece",
                    loading: false
                });
            }
        },

        updatePortfolioPiece: async (slug, portfolioPiece) => {
            set({loading: true, error: null});
            try {
                const updatedPortfolioPiece =
                    await PortfolioPiecesService.upsertPortfolioPiece(
                        slug,
                        portfolioPiece
                    );
                set((state) => ({
                    portfolioPieces: state.portfolioPieces.map((pp) =>
                        pp.slug === slug ? updatedPortfolioPiece : pp
                    ),
                    currentPortfolioPiece: updatedPortfolioPiece,
                    loading: false
                }));
            } catch (error) {
                console.error(`Error updating portfolio piece ${slug}:`, error);
                set({
                    error: `Failed to update portfolio piece ${slug}`,
                    loading: false
                });
            }
        },

        deletePortfolioPiece: async (slug) => {
            set({loading: true, error: null});
            try {
                await PortfolioPiecesService.deletePortfolioPiece(slug);
                set((state) => ({
                    portfolioPieces: state.portfolioPieces.filter(
                        (pp) => pp.slug !== slug
                    ),
                    currentPortfolioPiece:
                        state.currentPortfolioPiece?.slug === slug
                            ? null
                            : state.currentPortfolioPiece,
                    loading: false
                }));
            } catch (error) {
                console.error(`Error deleting portfolio piece ${slug}:`, error);
                set({
                    error: `Failed to delete portfolio piece ${slug}`,
                    loading: false
                });
            }
        },

        setFeaturedImage: async (slug, mediaId) => {
            set({loading: true, error: null});
            try {
                const updatedPortfolioPiece =
                    await PortfolioPiecesService.setFeaturedImage(
                        slug,
                        mediaId
                    );
                set((state) => ({
                    portfolioPieces: state.portfolioPieces.map((pp) =>
                        pp.slug === slug ? updatedPortfolioPiece : pp
                    ),
                    currentPortfolioPiece:
                        state.currentPortfolioPiece?.slug === slug
                            ? updatedPortfolioPiece
                            : state.currentPortfolioPiece,
                    loading: false
                }));
            } catch (error) {
                console.error(
                    `Error setting featured image for portfolio piece ${slug}:`,
                    error
                );
                set({
                    error: `Failed to set featured image for portfolio piece ${slug}`,
                    loading: false
                });
            }
        },

        setFeaturedVideo: async (slug, mediaId) => {
            set({loading: true, error: null});
            try {
                const updatedPortfolioPiece =
                    await PortfolioPiecesService.setFeaturedVideo(
                        slug,
                        mediaId
                    );
                set((state) => ({
                    portfolioPieces: state.portfolioPieces.map((pp) =>
                        pp.slug === slug ? updatedPortfolioPiece : pp
                    ),
                    currentPortfolioPiece:
                        state.currentPortfolioPiece?.slug === slug
                            ? updatedPortfolioPiece
                            : state.currentPortfolioPiece,
                    loading: false
                }));
            } catch (error) {
                console.error(
                    `Error setting featured video for portfolio piece ${slug}:`,
                    error
                );
                set({
                    error: `Failed to set featured video for portfolio piece ${slug}`,
                    loading: false
                });
            }
        },

        addMediaReference: async (slug, mediaId) => {
            set({loading: true, error: null});
            try {
                const updatedPortfolioPiece =
                    await PortfolioPiecesService.addMediaReference(
                        slug,
                        mediaId
                    );
                set((state) => ({
                    portfolioPieces: state.portfolioPieces.map((pp) =>
                        pp.slug === slug ? updatedPortfolioPiece : pp
                    ),
                    currentPortfolioPiece:
                        state.currentPortfolioPiece?.slug === slug
                            ? updatedPortfolioPiece
                            : state.currentPortfolioPiece,
                    loading: false
                }));
            } catch (error) {
                console.error(
                    `Error adding media reference to portfolio piece ${slug}:`,
                    error
                );
                set({
                    error: `Failed to add media reference to portfolio piece ${slug}`,
                    loading: false
                });
            }
        },

        removeMediaReference: async (slug, mediaId) => {
            set({loading: true, error: null});
            try {
                const updatedPortfolioPiece =
                    await PortfolioPiecesService.removeMediaReference(
                        slug,
                        mediaId
                    );
                set((state) => ({
                    portfolioPieces: state.portfolioPieces.map((pp) =>
                        pp.slug === slug ? updatedPortfolioPiece : pp
                    ),
                    currentPortfolioPiece:
                        state.currentPortfolioPiece?.slug === slug
                            ? updatedPortfolioPiece
                            : state.currentPortfolioPiece,
                    loading: false
                }));
            } catch (error) {
                console.error(
                    `Error removing media reference from portfolio piece ${slug}:`,
                    error
                );
                set({
                    error: `Failed to remove media reference from portfolio piece ${slug}`,
                    loading: false
                });
            }
        },

        // Helper functions for filtering
        getPortfolioPiecesByCategory: (category) => {
            return get().portfolioPieces.filter(
                (piece) => piece.category === category
            );
        },

        getPublishedPortfolioPieces: () => {
            return get().portfolioPieces.filter(
                (piece) =>
                    piece.isPublished &&
                    new Date(piece.publishDate) <= new Date()
            );
        },

        getFeaturedPortfolioPieces: () => {
            return get()
                .portfolioPieces.filter(
                    (piece) =>
                        piece.isPublished &&
                        piece.priority &&
                        piece.priority > 0
                )
                .sort((a, b) => (b.priority || 0) - (a.priority || 0));
        }
    })
);
```

#### React Component Example - Portfolio Grid

```tsx
// src/components/portfolio/PortfolioGrid.tsx
import React, {useEffect} from "react";
import {Link} from "react-router-dom";
import {usePortfolioPiecesStore} from "../../store/portfolioPiecesStore";
import {formatDate} from "../../utils/dateUtils";

interface PortfolioGridProps {
    category?: string;
    featuredOnly?: boolean;
    limit?: number;
}

export const PortfolioGrid: React.FC<PortfolioGridProps> = ({
    category,
    featuredOnly = false,
    limit
}) => {
    const {portfolioPieces, loading, error, fetchPortfolioPieces} =
        usePortfolioPiecesStore();

    useEffect(() => {
        fetchPortfolioPieces({
            category,
            isPublished: true,
            includeMedia: true,
            limit
        });
    }, [category, limit, fetchPortfolioPieces]);

    const displayPieces = featuredOnly
        ? usePortfolioPiecesStore
              .getState()
              .getFeaturedPortfolioPieces()
              .slice(0, limit)
        : portfolioPieces;

    if (loading) return <div className="loading">Loading...</div>;
    if (error) return <div className="error">Error: {error}</div>;
    if (!displayPieces.length)
        return <div className="no-results">No portfolio pieces found.</div>;

    return (
        <div className="portfolio-grid">
            {displayPieces.map((piece) => (
                <Link
                    to={`/portfolio/${piece.slug}`}
                    key={piece.id}
                    className="portfolio-card"
                >
                    {piece.featuredImageId && (
                        <div className="portfolio-image">
                            <img
                                src={`/api/media/${piece.featuredImageId}/thumbnail`}
                                alt={piece.title}
                            />
                        </div>
                    )}
                    <div className="portfolio-content">
                        <h3>{piece.title}</h3>
                        <p className="description">{piece.description}</p>
                        <div className="meta">
                            <span className="category">{piece.category}</span>
                            {piece.completionDate && (
                                <span className="date">
                                    {formatDate(piece.completionDate)}
                                </span>
                            )}
                        </div>
                    </div>
                </Link>
            ))}
        </div>
    );
};
```

#### Important Implementation Notes for Portfolio Pieces API

1. **Project Links**:

    The Portfolio Pieces API includes unique fields for project URLs and source code links that should be rendered as buttons in the UI:

    ```tsx
    <div className="project-links">
        {portfolioPiece.projectUrl && (
            <a
                href={portfolioPiece.projectUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="btn btn-primary"
            >
                View Project
            </a>
        )}

        {portfolioPiece.sourceCodeUrl && (
            <a
                href={portfolioPiece.sourceCodeUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="btn btn-secondary"
            >
                View Source Code
            </a>
        )}
    </div>
    ```

2. **Portfolio Filtering**:

    Create components for filtering portfolio pieces by category:

    ```tsx
    // src/components/portfolio/PortfolioFilters.tsx
    import React from "react";

    interface PortfolioFiltersProps {
        categories: string[];
        selectedCategory: string | null;
        onCategoryChange: (category: string | null) => void;
    }

    export const PortfolioFilters: React.FC<PortfolioFiltersProps> = ({
        categories,
        selectedCategory,
        onCategoryChange
    }) => {
        return (
            <div className="portfolio-filters">
                <button
                    className={`filter-btn ${
                        selectedCategory === null ? "active" : ""
                    }`}
                    onClick={() => onCategoryChange(null)}
                >
                    All
                </button>

                {categories.map((category) => (
                    <button
                        key={category}
                        className={`filter-btn ${
                            selectedCategory === category ? "active" : ""
                        }`}
                        onClick={() => onCategoryChange(category)}
                    >
                        {category}
                    </button>
                ))}
            </div>
        );
    };
    ```

3. **Portfolio Feature Section**:

    Create a component to display featured portfolio pieces on the homepage:

    ```tsx
    // src/components/home/FeaturedPortfolio.tsx
    import React, {useEffect} from "react";
    import {Link} from "react-router-dom";
    import {usePortfolioPiecesStore} from "../../store/portfolioPiecesStore";

    export const FeaturedPortfolio: React.FC = () => {
        const {loading, error, fetchPortfolioPieces} =
            usePortfolioPiecesStore();
        const featuredPieces = usePortfolioPiecesStore
            .getState()
            .getFeaturedPortfolioPieces()
            .slice(0, 3);

        useEffect(() => {
            fetchPortfolioPieces({
                isPublished: true,
                includeMedia: true
            });
        }, [fetchPortfolioPieces]);

        if (loading)
            return <div className="loading">Loading featured projects...</div>;
        if (error) return null; // Don't show errors on homepage sections
        if (!featuredPieces.length) return null;

        return (
            <section className="featured-portfolio">
                <h2>Featured Projects</h2>
                <div className="featured-grid">
                    {featuredPieces.map((piece) => (
                        <Link
                            to={`/portfolio/${piece.slug}`}
                            key={piece.id}
                            className="featured-card"
                        >
                            {/* Card content */}
                        </Link>
                    ))}
                </div>
                <div className="view-all">
                    <Link to="/portfolio" className="btn btn-primary">
                        View All Projects
                    </Link>
                </div>
            </section>
        );
    };
    ```

## Authentication

All endpoints require API key authentication via the `x-api-key` header. The API key value should be obtained from environment configuration and is managed through Azure Key Vault.

## Error Handling

Error responses include descriptive messages:

```typescript
interface ErrorResponse {
    error?: string;
    errors?: string[];
    statusCode?: number;
}
```

Common error status codes:

-   **400 Bad Request**: Invalid input data
-   **401 Unauthorized**: Missing or invalid API key
-   **404 Not Found**: Resource not found
-   **500 Internal Server Error**: Server error

## Implementation Notes

1. **TypeScript Models**: The interfaces above are TypeScript representations of the C# DTOs.

2. **Date Handling**: All date fields are ISO format strings in the TypeScript interfaces, but Date objects in C#.

3. **Media References**: The `mediaReferencesJson` field in BookDTO contains a JSON string array of media IDs, which needs to be parsed on the frontend.

4. **Authentication**: Implement an API interceptor to include the API key in all requests.

5. **Request/Response Transformation**: Create adapter functions to transform between frontend and API data models.
