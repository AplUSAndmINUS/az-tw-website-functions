## 📘 Function Documentation: `DeleteAuthor`

### 🧠 Overview
Azure Function to delete an author by slug

---

### 🔗 Endpoint
```http
DELETE /authors/{slug}
```

### 🔐 Authentication
| Header | Value |
| -- | -- |
| x-api-key | your-api-key |

### URL Parameters
| Name | Type | Required | Description |
| -- | -- | -- | -- |
| slug | string | ✅ | Unique identifier for the authors |

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
curl -X DELETE \
  http://localhost:7071/authors/sample-slug \
  -H "x-api-key: your-api-key"
```

### 🧠 Common Issues
- Missing required fields will trigger 400 errors
- Ensure API key is valid and scoped correctly
- Check that referenced entities exist

### 🔗 Related Endpoints
| Method | Endpoint | Description |
| -- | -- | -- |
| PUT | /authors/{slug} | Create or update a author |
| GET | /authors/{slug} | Retrieve a specific author |
| GET | /authors | List all authors |

#Function #Payload #ErrorHandling
