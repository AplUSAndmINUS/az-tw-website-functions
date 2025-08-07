## 📘 Function Documentation: `SetAuthorProfileImage`

### 🧠 Overview
Author Media Functions using BaseMediaRelationshipFunctions

---

### 🔗 Endpoint
```http
POST /authors/{slug}/profile-image
```

### 🔐 Authentication
| Header | Value |
| -- | -- |
| x-api-key | your-api-key |

### URL Parameters
| Name | Type | Required | Description |
| -- | -- | -- | -- |
| slug | string | ✅ | Unique identifier for the authors |

### 📦 Request Body
```json
{
  "title": "Terence Waters",
  "slug": "terence-waters",
  "description": "Software architect and tech entrepreneur",
  "content": "Passionate about technology and innovation...",
  "status": "Published",
  "category": "Author",
  "tagsList": ["technology", "entrepreneur"],
  "mediaReferencesJson": "[]"
}
```

### 📤 Response (200 OK)
```json
{
  "id": "author-123",
  "slug": "terence-waters",
  "title": "Terence Waters",
  "description": "Software architect and tech entrepreneur",
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
  http://localhost:7071/authors/sample-slug/profile-image \
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
