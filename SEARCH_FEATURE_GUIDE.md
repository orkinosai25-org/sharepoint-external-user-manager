# Search Feature - User Guide

## Overview

The Search feature allows you to find documents, users, client spaces, and libraries across your ClientSpace SaaS portal. Search is available in two modes:

- **Client Space Search**: Search within a specific client space (Available to all tiers)
- **Global Search**: Search across all your client spaces (Available to Pro/Enterprise tiers)

## Accessing Search

### Global Search Page

Navigate to the **Search** page from the main navigation menu. This provides a comprehensive search interface with:

- Large search bar for entering queries
- Search scope selector (Current Client / All Client Spaces)
- Result type filter (All Types, Documents, Users, Client Spaces, Libraries)
- Paginated results with detailed information
- Direct links to open results

### Client Space Search

When viewing a client space detail page, you'll find a dedicated search section that automatically scopes your search to that specific client space.

## Search Capabilities

### What You Can Search For

1. **Documents**
   - File names
   - Document descriptions
   - Metadata and properties
   - Results include file type, owner, and modification date

2. **Users**
   - External user emails
   - Display names
   - Permission levels
   - User status information

3. **Client Spaces**
   - Client names
   - Client references
   - Descriptions
   - Provisioning status

4. **Libraries**
   - Document library names
   - List names
   - Library descriptions
   - Item counts

### Search Features

- **Relevance Ranking**: Results are automatically ranked by relevance
- **Pagination**: Navigate through large result sets
- **Filters**: Narrow results by type
- **Scope Control**: Search current client or all clients
- **Quick Actions**: Direct links to open or view results

## API Endpoints

### Client Space Search

**Endpoint**: `GET /v1/client-spaces/{clientId}/search`

**Available to**: All subscription tiers

**Parameters**:
- `q` (required): Search query string
- `type` (optional): Filter by result type (Document, User, Library)
- `page` (optional): Page number (default: 1)
- `pageSize` (optional): Results per page (default: 20, max: 100)

**Example**:
```
GET /v1/client-spaces/123/search?q=contract&page=1&pageSize=20
```

### Global Search

**Endpoint**: `GET /v1/search`

**Available to**: Pro and Enterprise tiers

**Parameters**:
- `q` (required): Search query string
- `scope` (optional): Search scope - "current" or "all" (default: "current")
- `clientId` (optional): Filter by specific client space
- `type` (optional): Filter by result type
- `page` (optional): Page number (default: 1)
- `pageSize` (optional): Results per page (default: 20, max: 100)

**Example**:
```
GET /v1/search?q=annual+report&scope=all&type=Document&page=1
```

### Search Suggestions

**Endpoint**: `GET /v1/search/suggestions`

**Available to**: All subscription tiers

**Parameters**:
- `q` (required): Partial query string (minimum 2 characters)
- `scope` (optional): Search scope - "current" or "all"

**Example**:
```
GET /v1/search/suggestions?q=ann&scope=all
```

## Response Format

All search endpoints return responses in this format:

```json
{
  "success": true,
  "data": [
    {
      "id": "doc-1",
      "type": "Document",
      "title": "Annual Report 2024",
      "description": "Comprehensive annual report",
      "url": "https://...",
      "clientId": 1,
      "clientName": "Acme Corp",
      "ownerEmail": "user@example.com",
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
    "searchTimeMs": 125
  }
}
```

## Search Tips

1. **Use Specific Keywords**: More specific queries return better results
   - Good: "2024 annual financial report"
   - Less effective: "document"

2. **Filter by Type**: Narrow results to find what you need faster
   - Select "Documents" when looking for files
   - Select "Users" when looking for people

3. **Use Scope Wisely**: 
   - Use "Current Client Only" for faster, focused searches
   - Use "All Client Spaces" when you're not sure which client has the content

4. **Navigate Results**: Use pagination to browse through large result sets

5. **Review Metadata**: Check owner, dates, and other metadata to find the right result

## Subscription Tiers

### Free Tier
- ✅ Client space search
- ❌ Global search across all clients

### Pro Tier
- ✅ Client space search
- ✅ Global search across all clients
- ✅ Search suggestions
- Higher rate limits (60 requests/minute)

### Enterprise Tier
- ✅ All Pro features
- ✅ Highest rate limits (300 requests/minute)
- ✅ Priority support

## Rate Limits

To ensure fair usage, search has rate limits based on subscription tier:

| Tier | Rate Limit |
|------|------------|
| Free | 10 requests/minute |
| Pro | 60 requests/minute |
| Enterprise | 300 requests/minute |

When you exceed the rate limit, you'll receive a `429 Too Many Requests` response with a `Retry-After` header indicating when you can try again.

## Technical Implementation

### Current Implementation (MVP)

The search feature currently uses **mock data** for demonstration and testing purposes. This allows the UI and API to be fully functional while integration with live SharePoint and Microsoft Graph data sources is developed.

### Mock Data Includes:
- Sample documents (PDFs, Word docs, PowerPoint)
- Sample external users with various permission levels
- Sample client spaces in different states
- Sample document libraries

### Future Enhancements (Planned)

1. **Real Data Integration**
   - Microsoft Graph Search API
   - SharePoint Search API
   - Real-time indexing

2. **Advanced Search**
   - Azure AI Search integration
   - Semantic search with vector embeddings
   - Full-text content search
   - Document OCR for scanned files

3. **AI-Powered Features**
   - Natural language queries
   - Search result summarization
   - Smart suggestions

4. **Performance**
   - Redis caching
   - Distributed rate limiting
   - Search result pre-caching

5. **Security**
   - Fine-grained permission filtering
   - Audit logging
   - Data leak prevention

## Troubleshooting

### Search Returns No Results

**Possible Causes**:
1. No content matches your query
2. Insufficient permissions (client space not accessible)
3. Search scope is too narrow

**Solutions**:
- Try broader search terms
- Change scope to "All Client Spaces"
- Remove type filters
- Contact support if issue persists

### Rate Limit Exceeded

**Error**: `429 Too Many Requests`

**Solution**: 
- Wait for the time specified in the `Retry-After` header
- Consider upgrading to a higher tier for more requests
- Batch your searches to stay within limits

### Authentication Error

**Error**: `401 Unauthorized`

**Solution**:
- Ensure you're logged in
- Refresh your session
- Check Azure AD configuration

### Client Space Not Found

**Error**: `404 Not Found`

**Solution**:
- Verify the client space ID is correct
- Ensure the client space belongs to your tenant
- Check that the client space is active

## Support

For additional help with search:
- Review the [API Documentation](./SEARCH_API_DOCUMENTATION.md)
- Check the [Technical Documentation](./TECHNICAL_DOCUMENTATION.md)
- Contact support at support@clientspace.com

---

**Last Updated**: February 2026
**Version**: MVP (Mock Data)
**Status**: ✅ Fully Functional
