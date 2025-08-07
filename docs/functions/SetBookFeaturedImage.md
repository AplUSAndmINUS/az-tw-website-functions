## 📘 Function Documentation: `SetBookFeaturedImage`

### 🧠 Overview
Book Media Functions using BaseMediaRelationshipFunctions

---

### 🔗 Endpoint
```http
POST /books/{slug}/featured-image
```

### 🔐 Authentication
| Header | Value |
| -- | -- |
| x-api-key | your-api-key |

### URL Parameters
| Name | Type | Required | Description |
| -- | -- | -- | -- |
| slug | string | ✅ | Unique identifier for the books |

### 📦 Request Body
```json
{
  "title": "Mastering Azure Functions",
  "slug": "mastering-azure-functions",
  "description": "Complete guide to serverless computing with Azure",
  "content": "Learn how to build and deploy serverless applications...",
  "authorSlug": "terence-waters",
  "category": "Technology",
  "status": "Published",
  "tagsList": ["azure", "serverless", "cloud"],
  "mediaReferencesJson": "[]"
}
```

### 📤 Response (200 OK)
```json
{
  "id": "book-789",
  "slug": "mastering-azure-functions",
  "title": "Mastering Azure Functions",
  "authorSlug": "terence-waters",
  "category": "Technology",
  "status": "Published",
  "lastModified": "2025-08-07T13:48:40.4333258Z"
}
```

### ❌ Error Responses
| Status Code | Message Example |
| -- | -- |
| 400 Bad Request | ["Title is required", "Author slug is required"] |
| 401 Unauthorized | "Invalid API key" |
| 500 Internal Server Error | "An unexpected error occurred" |

### 🧪 Testing Example (curl)
```bash
curl -X POST \
  http://localhost:7071/books/sample-slug/featured-image \
  -H "x-api-key: your-api-key" \
  -H "Content-Type: application/json" \
  -d '{ ... }'

```

### 🧠 Common Issues
- Missing required fields will trigger 400 errors
- Ensure API key is valid and scoped correctly
- Check that referenced entities exist

### 🔗 Related Endpoints
| Method | Endpoint | Description |
| -- | -- | -- |

#Function #Payload #ErrorHandling
