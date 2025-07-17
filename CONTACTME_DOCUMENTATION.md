# ContactMe Azure Function Documentation

This document provides comprehensive information about the ContactMe Azure Function implementation for handling contact form submissions.

## Overview

The ContactMe Azure Function is a POST-only HTTP trigger that accepts form data from a front-end contact form, validates the input, stores the submission in Azure Table Storage, and sends a formatted email notification concurrently.

## Architecture

### Components

1. **ContactMeFunction** - The main Azure Function HTTP trigger
2. **ContactMeService** - Service layer for handling business logic
3. **EmailService** - Service for sending formatted emails
4. **ContactMeModel** - Data model for contact submissions
5. **ContactMeEntity** - Azure Table Storage entity
6. **ContactMeDTO** - Data transfer object for API input

### Data Flow

1. HTTP POST request received at `/contact`
2. API key validation
3. Request body validation and deserialization
4. Contact form data validation
5. Concurrent operations:
   - Store submission in Azure Table Storage
   - Send formatted email notification
6. Return success response

## API Specification

### Endpoint

```
POST /contact
```

### Headers

- `X-API-Key`: Required API key for authentication
- `Content-Type`: application/json

### Request Body

```json
{
  "name": "John Doe",
  "email": "john@example.com",
  "message": "Hello, I would like to get in touch..."
}
```

### Response

#### Success (200 OK)

```json
{
  "success": true,
  "message": "Contact form submitted successfully"
}
```

#### Validation Errors (400 Bad Request)

```json
{
  "errors": [
    "Name is required",
    "Please provide a valid email address",
    "Message must be at least 10 characters long"
  ]
}
```

#### Unauthorized (401 Unauthorized)

```
Invalid API key
```

## Validation Rules

### Name
- Required field
- Must be at least 2 characters long
- Trimmed automatically

### Email
- Required field
- Must be a valid email address format
- Validated using regex pattern
- Trimmed automatically

### Message
- Required field
- Must be at least 10 characters long
- Trimmed automatically

## Storage

### Table Storage

Contact submissions are stored in Azure Table Storage with environment-based table naming:

- **Production**: `contact` table
- **Development/Test**: `mockcontact` table

#### Entity Structure

```csharp
public class ContactMeEntity : ITableEntity
{
    public string PartitionKey { get; set; } // Format: "yyyy-MM"
    public string RowKey { get; set; } // Format: "yyyyMMddHHmmss_{Id}"
    public string Id { get; set; } // Unique identifier
    public string Name { get; set; }
    public string Email { get; set; }
    public string Message { get; set; }
    public DateTime SubmittedAt { get; set; }
    public string UserAgent { get; set; }
    public string IpAddress { get; set; }
    // Standard ITableEntity properties
}
```

## Email Service

### Email Configuration

The EmailService requires the following environment variables:

- `SMTP_HOST`: SMTP server hostname (default: smtp.gmail.com)
- `SMTP_PORT`: SMTP server port (default: 587)
- `SMTP_USERNAME`: SMTP username (required)
- `SMTP_PASSWORD`: SMTP password (required)
- `FROM_EMAIL`: Sender email address (default: SMTP_USERNAME)
- `FROM_NAME`: Sender display name (default: TerenceWaters.com)
- `TO_EMAIL`: Recipient email address (default: SMTP_USERNAME)

### Email Format

The email service formats contact submissions into a professional, structured format:

#### Subject Line
```
TerenceWaters.com Email Submission - {Name}
```

#### Body Format
```
============================================================
CONTACT FORM SUBMISSION
============================================================

From: John Doe
Email: john@example.com
Submitted: 2024-01-15 14:30:25 UTC

MESSAGE:
------------------------------------------------------------
Hello, I would like to get in touch...
------------------------------------------------------------

TECHNICAL DETAILS:
User Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36
IP Address: 192.168.1.100

============================================================
End of submission
============================================================
```

## Environment Configuration

### Required Environment Variables

- `X_API_ENVIRONMENT_KEY`: API key for function authentication
- `SMTP_USERNAME`: SMTP server username
- `SMTP_PASSWORD`: SMTP server password
- `StorageAccountName` or `AZURE_STORAGE_ACCOUNT_NAME`: Azure Storage account name
- `AzureWebJobsStorage`: Azure Storage connection string (optional, falls back to managed identity)

### Optional Environment Variables

- `SMTP_HOST`: SMTP server hostname (default: smtp.gmail.com)
- `SMTP_PORT`: SMTP server port (default: 587)
- `FROM_EMAIL`: Sender email address (default: SMTP_USERNAME)
- `FROM_NAME`: Sender display name (default: TerenceWaters.com)
- `TO_EMAIL`: Recipient email address (default: SMTP_USERNAME)
- `USE_MOCK_STORAGE`: Set to "true" to use mock storage table names

## Service Registration

Services are registered in the dependency injection container:

```csharp
// In SharedStorage/Extensions/ServiceCollectionExtensions.cs
services.AddScoped<IEmailService, EmailService>();
services.AddScoped<IAppMode, DefaultAppMode>();

// In Functions/Extensions/FunctionServiceExtensions.cs
services.AddScoped<IContactMeService, ContactMeService>();
```

## Error Handling

The function includes comprehensive error handling:

1. **API Key Validation**: Returns 401 Unauthorized for invalid API keys
2. **Request Body Validation**: Returns 400 Bad Request for empty or invalid JSON
3. **Field Validation**: Returns 400 Bad Request with detailed error messages
4. **Service Errors**: Returns 500 Internal Server Error for unexpected failures
5. **Logging**: All errors are logged with appropriate context

## Security Considerations

1. **API Key Authentication**: All requests must include a valid API key
2. **Input Validation**: All input fields are validated and sanitized
3. **Rate Limiting**: Consider implementing rate limiting in production
4. **Email Security**: SMTP credentials should be securely stored in Azure Key Vault
5. **IP Logging**: Client IP addresses are logged for security monitoring

## Testing

### Manual Testing

You can test the function using curl:

```bash
curl -X POST "https://your-function-app.azurewebsites.net/contact" \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-api-key" \
  -d '{
    "name": "Test User",
    "email": "test@example.com",
    "message": "This is a test message from the contact form."
  }'
```

### Expected Behavior

1. Successful submission should return 200 OK with success message
2. Email should be sent to the configured recipient
3. Submission should be stored in the appropriate table storage
4. All operations should be logged for monitoring

## Monitoring and Logging

The function uses Application Insights for monitoring:

- Request/response logging
- Error tracking
- Performance metrics
- Custom telemetry for email sending and storage operations

## Deployment

The function is deployed as part of the main Functions app. Ensure all required environment variables are configured in the Azure Function App settings.

## Troubleshooting

### Common Issues

1. **Invalid API Key**: Ensure the `X_API_ENVIRONMENT_KEY` is correctly set
2. **SMTP Errors**: Verify SMTP credentials and server configuration
3. **Storage Errors**: Check Azure Storage account permissions and connection
4. **Validation Errors**: Ensure all required fields are provided and meet validation criteria

### Debug Logging

Enable debug logging by setting the logging level to Information or Debug in the Function App configuration.