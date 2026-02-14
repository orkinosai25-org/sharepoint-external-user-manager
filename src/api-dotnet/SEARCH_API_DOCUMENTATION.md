# Search API Documentation

## Overview

The Search API provides comprehensive search functionality across client spaces, documents, users, and libraries. Search capabilities are tiered based on subscription level:

- **Free Tier**: Search within current client space only
- **Pro/Enterprise Tier**: Global search across all client spaces

## Endpoints

### 1. Global Search (Pro/Enterprise Only)

Search across all client spaces and resources.

**Endpoint**: `GET /api/v1/search`

**Query Parameters**:
- `q` or `query` (required): Search query string
- `scope`: Search scope (`current`, `all`, `global`). Default: `current`
- `clientId`: Filter by specific client space ID
- `userEmail`: Filter by user email
- `type`: Filter by result type (`document`, `user`, `clientspace`, `library`)
- `dateFrom`: Filter results modified after this date (ISO 8601)
- `dateTo`: Filter results modified before this date (ISO 8601)
- `page`: Page number (default: 1)
- `pageSize`: Results per page (default: 20, max: 100)

**Example Requests**:

```bash
# Search across all clients (Pro/Enterprise)
GET /api/v1/search?q=annual%20report&scope=all&page=1&pageSize=20

# Search with filters
GET /api/v1/search?q=contract&scope=all&type=document&dateFrom=2024-01-01

# Search by user
GET /api/v1/search?q=john&type=user&scope=all
```

**Response**:

```json
{
  "success": true,
  "data": [
    {
      "id": "doc-1",
      "type": "Document",
      "title": "Annual Report 2024",
      "description": "Comprehensive annual report for 2024",
      "url": "https://contoso.sharepoint.com/sites/client1/Documents/Annual-Report-2024.pdf",
      "clientId": 1,
      "clientName": "Acme Corporation",
      "ownerEmail": "john.doe@contoso.com",
      "ownerDisplayName": "John Doe",
      "createdDate": "2024-01-15T10:30:00Z",
      "modifiedDate": "2024-02-10T14:25:00Z",
      "score": 0.95,
      "metadata": {
        "fileType": "pdf",
        "size": "2048576"
      }
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "total": 45,
    "hasNext": true
  },
  "searchInfo": {
    "query": "annual report",
    "scope": "AllClients",
    "searchTimeMs": 125,
    "filters": {
      "clientId": null,
      "userEmail": null,
      "resultType": null,
      "dateFrom": null,
      "dateTo": null
    }
  }
}
```

### 2. Client Space Search (All Tiers)

Search within a specific client space.

**Endpoint**: `GET /api/v1/client-spaces/{clientId}/search`

**Path Parameters**:
- `clientId` (required): Client space ID

**Query Parameters**:
- `q` or `query` (required): Search query string
- `userEmail`: Filter by user email
- `type`: Filter by result type (`document`, `user`, `library`)
- `dateFrom`: Filter results modified after this date (ISO 8601)
- `dateTo`: Filter results modified before this date (ISO 8601)
- `page`: Page number (default: 1)
- `pageSize`: Results per page (default: 20, max: 100)

**Example Requests**:

```bash
# Search within a specific client space
GET /api/v1/client-spaces/1/search?q=project%20plan

# Search for documents only
GET /api/v1/client-spaces/1/search?q=contract&type=document

# Search with date filter
GET /api/v1/client-spaces/1/search?q=report&dateFrom=2024-01-01&dateTo=2024-12-31
```

**Response**: Same format as Global Search

### 3. Search Suggestions

Get autocomplete suggestions for search queries.

**Endpoint**: `GET /api/v1/search/suggestions`

**Query Parameters**:
- `q` or `query` (required): Partial search query
- `scope`: Search scope (`current`, `all`). Default: `current`

**Example Request**:

```bash
GET /api/v1/search/suggestions?q=ann&scope=all
```

**Response**:

```json
{
  "success": true,
  "data": [
    "Annual Report 2024",
    "Annual Budget Documents",
    "Annual Planning"
  ]
}
```

## Result Types

### Document Results

Documents include files, PDFs, Word documents, Excel spreadsheets, etc.

```json
{
  "id": "doc-1",
  "type": "Document",
  "title": "Project Plan Q1 2024",
  "description": "Quarterly project planning document",
  "url": "https://contoso.sharepoint.com/sites/client1/Documents/Project-Plan-Q1.docx",
  "clientId": 1,
  "clientName": "Acme Corporation",
  "ownerEmail": "jane.smith@contoso.com",
  "ownerDisplayName": "Jane Smith",
  "createdDate": "2024-01-10T09:00:00Z",
  "modifiedDate": "2024-02-08T16:30:00Z",
  "score": 0.87,
  "metadata": {
    "fileType": "docx",
    "size": "512000"
  }
}
```

### User Results

External users with access to client spaces.

```json
{
  "id": "external.user1@partner.com",
  "type": "User",
  "title": "Alice Johnson",
  "description": "external.user1@partner.com",
  "clientId": 1,
  "clientName": "Acme Corporation",
  "ownerEmail": "external.user1@partner.com",
  "ownerDisplayName": "Alice Johnson",
  "createdDate": "2024-01-05T10:00:00Z",
  "score": 0.92,
  "metadata": {
    "permissionLevel": "Read",
    "status": "Active"
  }
}
```

### Client Space Results

Client spaces/sites.

```json
{
  "id": "1",
  "type": "ClientSpace",
  "title": "Acme Corporation",
  "description": "Technology consulting project",
  "url": "https://contoso.sharepoint.com/sites/acme",
  "clientId": 1,
  "clientName": "Acme Corporation",
  "createdDate": "2023-08-15T12:00:00Z",
  "score": 1.0,
  "metadata": {
    "clientReference": "CLI-001",
    "provisioningStatus": "Completed",
    "isActive": "True"
  }
}
```

### Library Results

Document libraries and lists.

```json
{
  "id": "guid-here",
  "type": "Library",
  "title": "Project Documents",
  "description": "Main project documentation library",
  "url": "https://contoso.sharepoint.com/sites/acme/ProjectDocs",
  "clientId": 1,
  "clientName": "Acme Corporation",
  "ownerEmail": "john.doe@contoso.com",
  "ownerDisplayName": "John Doe",
  "createdDate": "2023-09-01T10:00:00Z",
  "score": 0.85,
  "metadata": {
    "externalUserCount": "3",
    "externalSharingEnabled": "True"
  }
}
```

## Rate Limiting

Search endpoints are rate-limited to prevent abuse:

| Subscription Tier | Rate Limit (per minute) |
|------------------|------------------------|
| Free             | 10 requests           |
| Pro              | 60 requests           |
| Enterprise       | 300 requests          |

**Rate Limit Headers**:

When rate limits are applied, the following headers are included in responses:

- `X-RateLimit-Limit`: Total requests allowed per window
- `X-RateLimit-Remaining`: Remaining requests in current window
- `X-RateLimit-Reset`: Seconds until rate limit resets
- `Retry-After`: Seconds to wait before retrying (when rate limit exceeded)

**Rate Limit Exceeded Response** (HTTP 429):

```json
{
  "success": false,
  "error": {
    "code": "RATE_LIMIT_EXCEEDED",
    "message": "Too many requests. Please try again later.",
    "details": "Rate limit of 10 requests per minute exceeded. Reset in 45 seconds."
  }
}
```

## Error Responses

### 400 Bad Request

Invalid query parameters.

```json
{
  "success": false,
  "error": {
    "code": "INVALID_QUERY",
    "message": "Search query parameter 'q' is required"
  }
}
```

### 401 Unauthorized

Missing or invalid authentication.

```json
{
  "success": false,
  "error": {
    "code": "UNAUTHORIZED",
    "message": "Tenant ID not found"
  }
}
```

### 403 Forbidden

Feature not available in current subscription tier.

```json
{
  "success": false,
  "error": {
    "code": "FORBIDDEN",
    "message": "Global search across all clients requires Pro or Enterprise subscription",
    "details": "Upgrade your subscription to access cross-client search"
  }
}
```

### 429 Too Many Requests

Rate limit exceeded (see Rate Limiting section above).

### 500 Internal Server Error

Server error during search execution.

```json
{
  "success": false,
  "error": {
    "code": "INTERNAL_ERROR",
    "message": "An error occurred while executing the search"
  }
}
```

## Authentication

All search endpoints require authentication via Azure AD bearer token in the Authorization header:

```
Authorization: Bearer <token>
```

The token must include:
- `tenantId` claim
- Valid subscription status

## Feature Gates

Search features are gated by subscription tier:

| Feature | Free | Pro | Enterprise |
|---------|------|-----|------------|
| Client Space Search | ✅ | ✅ | ✅ |
| Global Search | ❌ | ✅ | ✅ |
| Search Suggestions | ✅ | ✅ | ✅ |

## Implementation Notes

### Current Implementation (MVP)

The current implementation uses in-memory mock data for demonstration purposes:
- Mock documents, users, client spaces, and libraries
- In-memory rate limiting (does not persist across restarts)
- Simple relevance scoring algorithm

### Phase 2 Enhancements

Future enhancements will include:
- Integration with SharePoint Search API or Azure AI Search
- Redis-based distributed rate limiting
- Vector search for semantic matching
- Advanced ranking algorithms
- Full-text content indexing
- AI-powered search result summarization

## Testing

### Using curl

```bash
# Global search
curl -X GET "https://your-function-app.azurewebsites.net/api/v1/search?q=report&scope=all" \
  -H "Authorization: Bearer YOUR_TOKEN"

# Client space search
curl -X GET "https://your-function-app.azurewebsites.net/api/v1/client-spaces/1/search?q=contract" \
  -H "Authorization: Bearer YOUR_TOKEN"

# Search suggestions
curl -X GET "https://your-function-app.azurewebsites.net/api/v1/search/suggestions?q=ann" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### Using Postman

1. Create a new GET request
2. Add Authorization header with Bearer token
3. Set query parameters as needed
4. Send request

## Best Practices

1. **Use specific queries**: More specific queries return better results
2. **Leverage filters**: Use type, date, and user filters to narrow results
3. **Implement pagination**: Always paginate results for better UX
4. **Handle rate limits**: Implement exponential backoff when rate limited
5. **Cache suggestions**: Cache search suggestions to reduce API calls
6. **Monitor search time**: Use the `searchTimeMs` field to monitor performance

## Support

For issues or questions about the Search API, please contact support or refer to the main API documentation.
