# Azure Functions Website Backend - Project Overview

## 📋 Table of Contents

-   [🏗️ Project Architecture](#️-project-architecture)
-   [🔐 Authentication & Security](#-authentication--security)
-   [📚 API Endpoints Summary](#-api-endpoints-summary)
-   [🗄️ Data Architecture](#️-data-architecture)
-   [🖼️ Media Processing](#️-media-processing)
-   [⚙️ Configuration & Environment](#️-configuration--environment)
-   [🚀 Deployment & Infrastructure](#-deployment--infrastructure)
-   [🧪 Testing Framework](#-testing-framework)
-   [📖 Development Guidelines](#-development-guidelines)
-   [🔧 Common Operations](#-common-operations)

---

## 🏗️ Project Architecture

### Overview

This Azure Functions project provides the backend API for TerenceWaters.com using .NET 8 with an isolated worker model. The architecture follows clean architecture principles with clear separation between functions, services, and storage.

### Project Structure

```
src/
├── Functions/
│   ├── Authors/        # Author management endpoints
│   ├── BlogPosts/      # Blog content management
│   ├── Books/          # Book catalog management
│   ├── PortfolioPiece/ # Portfolio project management
│   ├── GitHub/         # GitHub integration
│   ├── Shared/         # Media and shared functions
│   └── Contact/        # Contact form handling
├── SharedStorage/      # Storage services layer
│   ├── Services/       # Business logic services
│   └── Models/         # Data models and DTOs
└── Utils/             # Utilities, validation, helpers
```

### Architecture Layers

```
┌─────────────────────────────────────────┐
│           Azure Functions               │
│      (HTTP Triggers & Timers)           │
├─────────────────────────────────────────┤
│            Service Layer                │
│   (BlogPostService, MediaService, etc) │
├─────────────────────────────────────────┤
│           Storage Services              │
│  (BlobStorageService, TableStorage)    │
├─────────────────────────────────────────┤
│          Azure Storage                  │
│     (Blob + Table + Key Vault)         │
└─────────────────────────────────────────┘
```

### Key Principles

-   **Clean Architecture**: Clear separation of concerns between layers
-   **Dependency Injection**: All services are registered and injected
-   **Content-Agnostic Design**: Base classes support multiple content types
-   **Handler Pattern**: Extensible media type handling
-   **Environment Flexibility**: Support for dev, test, and production environments

---

## 🔐 Authentication & Security

### API Key Authentication

All endpoints use custom API key validation via the `x-api-key` header.

#### Key Management

-   **Development**: Keys stored in Azure Key Vault
-   **Production**: Keys retrieved via managed identity
-   **Local**: Keys in local.settings.json (development only)

#### Authentication Flow

1. Request includes `x-api-key` header
2. `KeyVaultApiKeyValidator` retrieves correct key from Key Vault
3. Environment detection determines which secret to use
4. Request validated against retrieved key

#### Environment Mapping

| Environment  | Key Vault Secret           | Usage                   |
| ------------ | -------------------------- | ----------------------- |
| Development  | Dev environment key        | Development and testing |
| Test/Staging | Staging environment key    | Pre-production testing  |
| Production   | Production environment key | Live environment        |

### Azure Function Authorization

Functions use `AuthorizationLevel.Function` requiring both:

-   Azure Functions key (function-level or master)
-   Custom API key for business logic validation

### Security Best Practices

-   ✅ No hardcoded secrets in code or documentation
-   ✅ Managed identity for Azure services
-   ✅ Key Vault for secret management
-   ✅ Environment-specific configurations
-   ✅ Comprehensive request validation
-   ✅ Secure error handling (no sensitive data in errors)

---

## 📚 API Endpoints Summary

### Content Management APIs

#### Authors API (7 endpoints)

-   **CRUD Operations**: Get, Create/Update, Delete authors
-   **Media Management**: Profile images, background images, media references
-   **Features**: Social links, bio, contact information

#### Blog Posts API (10 endpoints)

-   **CRUD Operations**: Get all, Get single, Create, Update, Delete
-   **Media Support**: Featured images, videos, media collections
-   **Filtering**: By author, category, publication status
-   **Features**: Rich content, excerpts, tag management

#### Books API (11 endpoints)

-   **CRUD Operations**: Complete book catalog management
-   **Media Support**: Book covers, promotional videos, supplementary media
-   **Filtering**: By author, genre, publication status
-   **Features**: ISBN support, publication dates, category management

#### Portfolio API (9 endpoints)

-   **CRUD Operations**: Project showcase management
-   **Media Support**: Project screenshots, demo videos
-   **Features**: Technology tags, project URLs, GitHub links

### Utility APIs

#### Media API (8 endpoints)

-   **File Upload**: Images, videos, documents via multipart form
-   **Management**: Get, delete, batch operations
-   **Processing**: Automatic image optimization, thumbnail generation
-   **Organization**: Content-based organization and CDN integration

#### GitHub API (4 endpoints)

-   **Repository Data**: Sync and retrieve repository information
-   **Activity Grid**: Contribution calendar visualization
-   **Auto-sync**: Timer-triggered updates every 4 hours

#### Contact API (1 endpoint)

-   **Form Submission**: Secure contact form processing
-   **Validation**: Input sanitization and validation

### Common Query Parameters

-   `includeMedia`: Include associated media content
-   `limit`: Pagination control (default: 50, max: 100)
-   `authorSlug`: Filter by specific author
-   `category`: Filter by content category
-   `isPublished`: Filter by publication status

### Response Formats

All APIs return consistent JSON responses:

-   **Standard DTO**: Basic content information
-   **WithMedia DTO**: Includes associated media assets
-   **Error Responses**: Structured error information with appropriate HTTP codes

---

## 🗄️ Data Architecture

### Storage Strategy

#### Azure Table Storage

-   **Content Storage**: All content types (blogs, books, portfolio, authors)
-   **Metadata Storage**: Media metadata and relationships
-   **Partitioning**: Slug-based partitioning for efficient queries
-   **Indexing**: Row key strategies for optimal performance

#### Azure Blob Storage

-   **File Storage**: Images, videos, documents
-   **Organization**: Container-based organization by content type
-   **CDN Integration**: Automatic CDN URL generation
-   **Optimization**: Image compression and format conversion

### Data Models

#### Base Content Model

```typescript
interface BaseContent {
    id: string;
    slug: string;
    title: string;
    content: string;
    authorSlug: string;
    category: string;
    tags: string[];
    status: "Draft" | "Published";
    createdAt: string;
    lastModified: string;
    featuredImageId?: string;
    featuredVideoId?: string;
    mediaReferences?: string[];
}
```

#### Media Model

```typescript
interface MediaItem {
    id: string;
    fileName: string;
    contentType: string;
    size: number;
    url: string;
    cdnUrl: string;
    description?: string;
    uploadedAt: string;
    assetType: "Images" | "Videos" | "Documents";
    contentSection: string;
}
```

### Content Relationships

-   **Author → Content**: One-to-many relationship via authorSlug
-   **Content → Media**: Many-to-many via media ID references
-   **Content → Categories**: Flexible category system per content type
-   **Media → Content**: Bidirectional linking with content ID

---

## 🖼️ Media Processing

### Upload Pipeline

```
File Upload → Type Detection → Handler Selection → Processing → Storage → Metadata
```

### Processing Capabilities

#### Image Processing

-   **Format Support**: JPEG, PNG, GIF, WebP
-   **Optimization**: Automatic WebP conversion for web delivery
-   **Thumbnails**: Automatic thumbnail generation
-   **Metadata**: Dimension extraction, file size calculation

#### Video Processing

-   **Format Support**: MP4, AVI, MOV, and other common formats
-   **Metadata**: Duration, bitrate, codec information
-   **Storage**: Direct blob storage with CDN delivery

#### Document Processing

-   **Format Support**: PDF, DOC, DOCX, TXT, and other document types
-   **Metadata**: File size, type detection
-   **Security**: File type validation and sanitization

### Handler Architecture

-   **Extensible Design**: Plugin-based handler system
-   **Type-Specific Logic**: Handlers for images, videos, documents
-   **Future Ready**: Easy to add new media types

### CDN Integration

-   **Automatic URLs**: CDN URLs generated for all media
-   **Performance**: Global content distribution
-   **Caching**: Optimized caching strategies

---

## ⚙️ Configuration & Environment

### Environment Detection

The system automatically detects environments based on:

-   Function App names
-   Environment variables
-   Hostname patterns

### Configuration Sources

1. **Azure Key Vault**: Secure secrets (API keys, connection strings)
2. **Environment Variables**: Non-sensitive configuration
3. **App Settings**: Azure-specific settings

### Environment-Specific Settings

#### Development

-   **Storage**: Development storage accounts
-   **Keys**: Development API keys from Key Vault
-   **Logging**: Verbose logging enabled
-   **CDN**: Development CDN endpoints

#### Test/Staging

-   **Storage**: Isolated test storage
-   **Keys**: Staging-specific API keys
-   **Testing**: Integration test configurations
-   **Validation**: Pre-production validation rules

#### Production

-   **Storage**: Production storage with redundancy
-   **Keys**: Production API keys with rotation
-   **Monitoring**: Full Application Insights integration
-   **Performance**: Optimized for scale

### Required Settings

-   `X_API_ENVIRONMENT_KEY`: API authentication key (Key Vault reference)
-   `KEY_VAULT_URI`: Azure Key Vault endpoint
-   `USE_MOCK_STORAGE`: Development/testing toggle
-   Storage account configuration via managed identity

---

## 🚀 Deployment & Infrastructure

### Azure Resources Required

#### Core Resources

-   **Azure Functions App**: .NET 8 isolated worker
-   **Azure Storage Account**: Blob and Table storage
-   **Azure Key Vault**: Secret management
-   **Application Insights**: Monitoring and logging

#### Optional Resources

-   **Azure CDN**: Content delivery optimization
-   **Azure Front Door**: Global load balancing
-   **Log Analytics**: Advanced log analysis

### Deployment Strategy

#### GitHub Actions Integration

-   **Environment-specific**: Separate workflows for dev/test/prod
-   **Key Vault References**: Secure secret management
-   **Managed Identity**: No stored credentials needed

#### Infrastructure as Code

-   **Bicep Templates**: Available for consistent deployments
-   **Resource Naming**: Environment-specific naming conventions
-   **Tagging Strategy**: Proper resource organization

### Deployment Checklist

-   ✅ Managed identity configured
-   ✅ Key Vault permissions assigned
-   ✅ Storage account connections validated
-   ✅ API keys configured in Key Vault
-   ✅ Application Insights connected
-   ✅ Function authorization levels set
-   ✅ CDN endpoints configured (if used)

---

## 🧪 Testing Framework

### Test Structure

```
Tests/
├── Unit/              # Service layer unit tests
├── Integration/       # End-to-end integration tests
├── Media/            # Media processing tests
└── Utilities/        # Testing utilities and helpers
```

### Testing Categories

#### Unit Tests

-   **Service Logic**: Business logic validation
-   **Model Mapping**: DTO/Entity conversion testing
-   **Validation**: Input validation testing
-   **Mocking**: Isolated component testing

#### Integration Tests

-   **End-to-End**: Full workflow testing
-   **Storage**: Azure Storage integration
-   **Media Processing**: File upload and processing
-   **API Endpoints**: HTTP request/response validation

#### Comprehensive Test Runner

-   **Orchestrated Execution**: All tests in sequence
-   **Environment Setup**: Test data management
-   **Cleanup**: Automatic test data removal
-   **Reporting**: Detailed test results

### Test Data Management

-   **Unique Naming**: Timestamp-based test identifiers
-   **Isolation**: Independent test execution
-   **Cleanup**: Automatic resource cleanup
-   **Validation**: Data integrity verification

---

## 📖 Development Guidelines

### Code Organization

#### Naming Conventions

-   **Files**: PascalCase for C# files
-   **Methods**: Async suffix for async methods
-   **Variables**: camelCase for local variables
-   **Constants**: PascalCase for constants

#### Project Structure

-   **Domain Separation**: Each content type in separate folders
-   **Shared Code**: Common functionality in SharedStorage
-   **Utilities**: Cross-cutting concerns in Utils
-   **Tests**: Mirror main project structure

### Development Practices

#### Error Handling

-   **Comprehensive**: All error scenarios covered
-   **Logging**: Structured logging with Application Insights
-   **User-Friendly**: Clear error messages without sensitive data
-   **HTTP Codes**: Appropriate status codes for all scenarios

#### Performance Considerations

-   **Async/Await**: All I/O operations are async
-   **Caching**: Appropriate caching strategies
-   **Batch Operations**: Efficient bulk operations
-   **Resource Management**: Proper disposal patterns

#### Security Guidelines

-   **Input Validation**: All inputs validated and sanitized
-   **Output Encoding**: Safe output encoding
-   **Authentication**: Consistent authentication patterns
-   **Secrets Management**: No hardcoded secrets

### Adding New Features

#### New Content Type

1. Create Models (Entity, DTO, Model)
2. Implement Service (inherit from ContentService)
3. Create Azure Functions
4. Register in dependency injection
5. Add tests and documentation

#### New Media Type

1. Create Handler (implement IMediaTypeHandler)
2. Add processing logic
3. Register handler in DI
4. Add validation rules
5. Create tests

---

## 🔧 Common Operations

### Local Development Setup

1. **Prerequisites**: .NET 8 SDK, Azure Functions Core Tools
2. **Configuration**: Set up local.settings.json
3. **Storage**: Use Azurite for local development
4. **Testing**: Run integration tests to verify setup

### API Testing with Postman

1. **Import Collection**: Use provided postman-collection.json
2. **Environment Setup**: Configure environment variables
3. **Authentication**: Set API key in collection variables
4. **Testing**: Use organized folder structure for endpoint testing

### Monitoring & Debugging

#### Application Insights

-   **Real-time Monitoring**: Live metrics and performance
-   **Error Tracking**: Exception tracking and analysis
-   **Custom Events**: Business logic tracking
-   **Performance**: Request timing and dependencies

#### Logging Strategy

-   **Structured Logging**: JSON-formatted logs
-   **Context Information**: Request correlation IDs
-   **Error Details**: Stack traces and context
-   **Performance Metrics**: Operation timing

### Troubleshooting Guide

#### Common Issues

-   **Authentication Failures**: Check Key Vault permissions and managed identity
-   **Storage Errors**: Verify storage account connection and permissions
-   **Media Upload Issues**: Check file size limits and format support
-   **Performance Issues**: Review Application Insights for bottlenecks

#### Diagnostic Steps

1. Check Application Insights for errors
2. Verify configuration and environment variables
3. Test authentication with simple endpoints
4. Validate storage connectivity
5. Review recent changes and deployments

### Maintenance Tasks

#### Regular Operations

-   **Key Rotation**: Rotate API keys in Key Vault
-   **Storage Cleanup**: Remove orphaned media files
-   **Performance Review**: Analyze Application Insights metrics
-   **Security Audit**: Review access logs and permissions

#### Monitoring Alerts

-   **Error Rate**: High error rate alerts
-   **Performance**: Slow response time alerts
-   **Availability**: Uptime monitoring
-   **Storage**: Storage quota alerts

---

## 📊 Environment URLs & Testing

### Environment Endpoints

-   **Development**: `https://mock-dev-api.terencewaters.com`
-   **Test/Staging**: `https://mock-tst-api.terencewaters.com`
-   **Production**: `https://api.terencewaters.com`
-   **Local Development**: `http://localhost:7071`

### Testing Tools

-   **Postman Collection**: Complete endpoint testing suite
-   **Environment Variables**: Easy environment switching
-   **Test Scripts**: Automated request/response validation

---

This comprehensive overview provides all the essential information for understanding, developing, and maintaining the Azure Functions backend without exposing any sensitive configuration details or security information.
