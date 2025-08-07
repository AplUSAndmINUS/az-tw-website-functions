## 📘 Function Documentation: `GetBlogPosts`

### 🧠 Overview
Retrieves a list of blog posts with optional filtering by various criteria including category, author, and publication status.

---

### 🔗 Endpoint
```http
GET /posts
```

### 🔐 Authentication
| Header | Value |
| -- | -- |
| x-api-key | your-api-key |

### 📤 Response (200 OK)
```json
{
  "id": "c681fb3d-213d-415a-b931-b4e0f7c70492",
  "partitionKey": "resonance-philosophy",
  "rowKey": "post",
  "title": "The Fluxline Philosophy: Structuring the Shift",
  "authorSlug": "terence-waters",
  "slug": "resonance-philosophy",
  "category": "Business",
  "status": "Published",
  "mediaReferencesJson": "[\"https://..."]",
  "publishDate": "2025-08-06T10:00:00Z",
  "lastModified": "2025-08-07T13:48:40.4333258Z",
  "tagsList": ["Fluxline", "technology", "resonance"]
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
curl -X GET \
  http://localhost:7071/posts \
  -H "x-api-key: your-api-key"
```

### 🧠 Common Issues
- mediaReferencesJson must be a stringified array, not a raw array
- Missing required fields will trigger 400 errors
- Ensure API key is valid and scoped correctly
- Dates must be in ISO 8601 format

### 🔗 Related Endpoints
| Method | Endpoint | Description |
| -- | -- | -- |
| PUT | /blogposts/{slug} | Create or update a blogpost |
| GET | /blogposts/{slug} | Retrieve a specific blogpost |
| DELETE | /blogposts/{slug} | Delete a blogpost |

#Function #Payload #ErrorHandling
