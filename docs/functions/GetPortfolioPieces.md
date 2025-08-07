## 📘 Function Documentation: `GetPortfolioPieces`

### 🧠 Overview
Retrieves a list of portfolio pieces with optional filtering by various criteria including category, author, and publication status.

---

### 🔗 Endpoint
```http
GET /portfolio
```

### 🔐 Authentication
| Header | Value |
| -- | -- |
| x-api-key | your-api-key |

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
curl -X GET \
  http://localhost:7071/portfolio \
  -H "x-api-key: your-api-key"
```

### 🧠 Common Issues
- Missing required fields will trigger 400 errors
- Ensure API key is valid and scoped correctly
- Check that referenced entities exist

### 🔗 Related Endpoints
| Method | Endpoint | Description |
| -- | -- | -- |
| PUT | /portfoliopiece/{slug} | Create or update a portfoliopiece |
| GET | /portfoliopiece/{slug} | Retrieve a specific portfoliopiece |
| DELETE | /portfoliopiece/{slug} | Delete a portfoliopiece |

#Function #Payload #ErrorHandling
