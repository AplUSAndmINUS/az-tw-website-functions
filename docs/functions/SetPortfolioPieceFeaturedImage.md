## 📘 Function Documentation: `SetPortfolioPieceFeaturedImage`

### 🧠 Overview
Portfolio Media Functions using BaseMediaRelationshipFunctions

---

### 🔗 Endpoint
```http
POST /portfolio/{slug}/featured-image
```

### 🔐 Authentication
| Header | Value |
| -- | -- |
| x-api-key | your-api-key |

### URL Parameters
| Name | Type | Required | Description |
| -- | -- | -- | -- |
| slug | string | ✅ | Unique identifier for the portfoliopiece |

### 📦 Request Body
```json
{
  "title": "E-Commerce Platform",
  "slug": "ecommerce-platform",
  "description": "Modern e-commerce solution built with .NET",
  "content": "Full-stack e-commerce platform featuring...",
  "category": "Web Development",
  "status": "Published",
  "tagsList": ["dotnet", "ecommerce", "web"],
  "mediaReferencesJson": "[]"
}
```

### 📤 Response (200 OK)
```json
{
  "id": "portfolio-456",
  "slug": "ecommerce-platform",
  "title": "E-Commerce Platform",
  "category": "Web Development",
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
  http://localhost:7071/portfolio/sample-slug/featured-image \
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
