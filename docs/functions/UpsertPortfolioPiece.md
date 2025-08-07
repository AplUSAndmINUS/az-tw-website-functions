## 📘 Function Documentation: `UpsertPortfolioPiece`

### 🧠 Overview
Creates or updates a portfolio piece based on the provided parameters. Supports both new creation and modification of existing records.

---

### 🔗 Endpoint
```http
POST /portfolio/{slug}
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

### ✅ Required Fields
| Field | Type | Notes |
| -- | -- | -- |
| Title | string | Title of the content |
| Slug | string | Unique identifier |
| AuthorSlug | string | Must match an existing author |
| Content | string | Markdown supported |
| Category | string | Category classification |

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
  http://localhost:7071/portfolio/sample-slug \
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
| GET | /portfoliopiece/{slug} | Retrieve a specific portfoliopiece |
| GET | /portfoliopiece | List all portfoliopiece |
| DELETE | /portfoliopiece/{slug} | Delete a portfoliopiece |

#Function #Payload #ErrorHandling
