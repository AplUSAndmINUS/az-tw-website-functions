## 📘 Function Documentation: `AppInsightsDiagnostics`

### 🧠 Overview
Diagnostic function to help identify AppInsights and environment configuration issues

---

### 🔗 Endpoint
```http
GET /diagnostics/appinsights
```

### 🔐 Authentication
| Header | Value |
| -- | -- |
| x-api-key | your-api-key |

### 📤 Response (200 OK)
```json
{
  "id": "sample-id",
  "slug": "sample-slug",
  "title": "Sample Title",
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
curl -X GET \
  http://localhost:7071/diagnostics/appinsights \
  -H "x-api-key: your-api-key"
```

### 🧠 Common Issues
- Missing required fields will trigger 400 errors
- Ensure API key is valid and scoped correctly
- Check that referenced entities exist

### 🔗 Related Endpoints
| Method | Endpoint | Description |
| -- | -- | -- |

#Function #Payload #ErrorHandling
