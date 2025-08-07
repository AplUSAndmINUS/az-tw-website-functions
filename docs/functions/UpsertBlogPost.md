## 📘 Function Documentation: `UpsertBlogPost`

### 🧠 Overview
Creates or updates a blog post based on the provided parameters. Supports both new creation and modification of existing records.

---

### 🔗 Endpoint
```http
POST /posts/{slug?}
```

### 🔐 Authentication
| Header | Value |
| -- | -- |
| x-api-key | your-api-key |

### URL Parameters
| Name | Type | Required | Description |
| -- | -- | -- | -- |
| slug | string | ❌ | Unique identifier for the blogposts |

### 📦 Request Body
```json
{
  "title": "The Fluxline Philosophy: Structuring the Shift",
  "partitionKey": "resonance-philosophy",
  "slug": "resonance-philosophy",
  "description": "What do Ryan Reynolds, Pikachu, and your deepest drive have in common?",
  "content": "## 🧠 Resonance, Part 1: The Philosophy of Resonance\n\n...",
  "excerpt": "What do Ryan Reynolds, Pikachu, and your deepest drive have in common?",
  "authorSlug": "terence-waters",
  "publishDate": "2025-08-06T10:00:00Z",
  "isPublished": true,
  "status": "Published",
  "tagsList": ["Fluxline", "technology", "resonance"],
  "category": "Business",
  "mediaReferencesJson": "[\"https://.../uploaded-image.webp\"]",
  "featuredImageUrl": "https://.../uploaded-image.webp",
  "seoTitle": "Part 1: The Philosophy of Resonance",
  "seoDescription": "What do Ryan Reynolds, Pikachu, and your deepest drive have in common?",
  "readingTimeMinutes": 7
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
curl -X POST \
  http://localhost:7071/posts/sample-slug \
  -H "x-api-key: your-api-key" \
  -H "Content-Type: application/json" \
  -d '{ ... }'

```

### 🧠 Common Issues
- mediaReferencesJson must be a stringified array, not a raw array
- Missing required fields will trigger 400 errors
- Ensure API key is valid and scoped correctly
- Dates must be in ISO 8601 format

### 🔗 Related Endpoints
| Method | Endpoint | Description |
| -- | -- | -- |
| GET | /blogposts/{slug} | Retrieve a specific blogpost |
| GET | /blogposts | List all blogposts |
| DELETE | /blogposts/{slug} | Delete a blogpost |

#Function #Payload #ErrorHandling
