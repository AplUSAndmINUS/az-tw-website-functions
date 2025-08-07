## 📘 Function Documentation: `DeleteBook`

### 🧠 Overview
Azure Function for deleting books (DELETE operations)

---

### 🔗 Endpoint
```http
DELETE /books/{slug}
```

### 🔐 Authentication
| Header | Value |
| -- | -- |
| x-api-key | your-api-key |

### URL Parameters
| Name | Type | Required | Description |
| -- | -- | -- | -- |
| slug | string | ✅ | Unique identifier for the books |

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
curl -X DELETE \
  http://localhost:7071/books/sample-slug \
  -H "x-api-key: your-api-key"
```

### 🧠 Common Issues
- Missing required fields will trigger 400 errors
- Ensure API key is valid and scoped correctly
- Check that referenced entities exist

### 🔗 Related Endpoints
| Method | Endpoint | Description |
| -- | -- | -- |
| PUT | /books/{slug} | Create or update a book |
| GET | /books/{slug} | Retrieve a specific book |
| GET | /books | List all books |

#Function #Payload #ErrorHandling
