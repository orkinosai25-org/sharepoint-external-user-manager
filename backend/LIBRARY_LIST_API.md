# Library & List Management API

This document describes the new API endpoints for creating and managing document libraries and lists in client spaces.

## Overview

The Library & List Management feature enables solicitors to create document libraries and lists directly within client spaces without needing access to SharePoint admin UI. These assets are created in the SharePoint site associated with each client and appear immediately in the client space view.

## Endpoints

### Create Document Library

Create a new document library in a client's SharePoint site.

**Endpoint:** `POST /clients/{id}/libraries`

**Authentication:** Required (Azure AD JWT token)

**Permissions:** `CLIENTS_WRITE` (FirmAdmin or FirmUser with write permissions)

**Request Body:**
```json
{
  "name": "Client Documents",
  "description": "Documents for the client project" // optional
}
```

**Response:** `200 OK`
```json
{
  "id": "library-guid-12345",
  "name": "Client Documents",
  "displayName": "Client Documents",
  "description": "Documents for the client project",
  "webUrl": "https://contoso.sharepoint.com/sites/client/Client%20Documents",
  "createdDateTime": "2024-01-15T10:30:00Z",
  "lastModifiedDateTime": "2024-01-15T10:30:00Z",
  "itemCount": 0
}
```

**Error Responses:**
- `400 Bad Request` - Invalid request body or validation error
- `401 Unauthorized` - Missing or invalid authentication token
- `403 Forbidden` - Insufficient permissions
- `404 Not Found` - Client not found or not accessible
- `500 Internal Server Error` - SharePoint API error or server error

**Validation Rules:**
- `name`: Required, 1-255 characters
- `description`: Optional, max 1000 characters

---

### Create List

Create a new list in a client's SharePoint site.

**Endpoint:** `POST /clients/{id}/lists`

**Authentication:** Required (Azure AD JWT token)

**Permissions:** `CLIENTS_WRITE` (FirmAdmin or FirmUser with write permissions)

**Request Body:**
```json
{
  "name": "Project Tasks",
  "description": "Task list for the client project", // optional
  "template": "tasks" // optional, defaults to "genericList"
}
```

**Response:** `200 OK`
```json
{
  "id": "list-guid-67890",
  "name": "Project Tasks",
  "displayName": "Project Tasks",
  "description": "Task list for the client project",
  "webUrl": "https://contoso.sharepoint.com/sites/client/Lists/ProjectTasks",
  "createdDateTime": "2024-01-15T10:35:00Z",
  "lastModifiedDateTime": "2024-01-15T10:35:00Z",
  "itemCount": 0,
  "listTemplate": "tasks"
}
```

**Error Responses:**
- `400 Bad Request` - Invalid request body or validation error
- `401 Unauthorized` - Missing or invalid authentication token
- `403 Forbidden` - Insufficient permissions
- `404 Not Found` - Client not found or not accessible
- `500 Internal Server Error` - SharePoint API error or server error

**Validation Rules:**
- `name`: Required, 1-255 characters
- `description`: Optional, max 1000 characters
- `template`: Optional, must be one of:
  - `genericList` (default)
  - `documentLibrary`
  - `survey`
  - `links`
  - `announcements`
  - `contacts`
  - `events`
  - `tasks`
  - `issueTracking`
  - `customList`

---

## Existing Endpoints

### Get Client Libraries

Fetch all document libraries for a specific client.

**Endpoint:** `GET /clients/{id}/libraries`

**Authentication:** Required (Azure AD JWT token)

**Permissions:** `CLIENTS_READ` (FirmAdmin or FirmUser)

**Response:** `200 OK`
```json
[
  {
    "id": "library-guid-12345",
    "name": "Documents",
    "displayName": "Documents",
    "description": "Default document library",
    "webUrl": "https://contoso.sharepoint.com/sites/client/Shared%20Documents",
    "createdDateTime": "2024-01-01T10:00:00Z",
    "lastModifiedDateTime": "2024-01-15T14:30:00Z",
    "itemCount": 0
  }
]
```

---

### Get Client Lists

Fetch all lists for a specific client.

**Endpoint:** `GET /clients/{id}/lists`

**Authentication:** Required (Azure AD JWT token)

**Permissions:** `CLIENTS_READ` (FirmAdmin or FirmUser)

**Response:** `200 OK`
```json
[
  {
    "id": "list-guid-67890",
    "name": "Tasks",
    "displayName": "Tasks",
    "description": "Task tracking list",
    "webUrl": "https://contoso.sharepoint.com/sites/client/Lists/Tasks",
    "createdDateTime": "2024-01-01T10:00:00Z",
    "lastModifiedDateTime": "2024-01-15T16:45:00Z",
    "itemCount": 0,
    "listTemplate": "genericList"
  }
]
```

---

## Usage Examples

### cURL Examples

**Create a Document Library:**
```bash
curl -X POST https://api.example.com/clients/123/libraries \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Legal Documents",
    "description": "Legal documents for the client case"
  }'
```

**Create a Task List:**
```bash
curl -X POST https://api.example.com/clients/123/lists \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Case Tasks",
    "description": "Tasks for the legal case",
    "template": "tasks"
  }'
```

**Get All Libraries for a Client:**
```bash
curl -X GET https://api.example.com/clients/123/libraries \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

**Get All Lists for a Client:**
```bash
curl -X GET https://api.example.com/clients/123/lists \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

---

## Implementation Details

### SharePoint Integration

The endpoints use Microsoft Graph API to interact with SharePoint:

- **Document Libraries:** Created using the `/sites/{siteId}/lists` endpoint with `template: 'documentLibrary'`
- **Lists:** Created using the `/sites/{siteId}/lists` endpoint with the specified template type

### Mock Mode

When Graph integration is disabled (development/testing), the service returns mock data:
- Mock libraries and lists are generated with timestamps
- No actual SharePoint operations are performed
- Useful for development without SharePoint access

### Permissions

- **Read Operations** (`GET`): Require `CLIENTS_READ` permission
- **Write Operations** (`POST`): Require `CLIENTS_WRITE` permission

Permissions are checked at the tenant level, ensuring multi-tenant isolation.

### Client Status

Libraries and lists can only be created for clients with:
- Status: `Active`
- Valid `siteId` (SharePoint site provisioned)

If a client is in `Provisioning` or `Error` state, the API will return an error.

---

## Testing

Comprehensive test coverage is provided in `src/functions/clients/createLibraryAndList.spec.ts`:

- ✅ Valid request validation
- ✅ Missing required fields
- ✅ Empty field validation
- ✅ Max length validation
- ✅ Template type validation
- ✅ Default values

Run tests with:
```bash
npm test
```

Run specific tests:
```bash
npm test -- --testPathPattern="createLibraryAndList"
```

---

## Acceptance Criteria

✅ **Solicitor can add a library or list without SharePoint admin UI**
- RESTful API endpoints enable creation via HTTP requests
- No direct SharePoint admin access required
- Validation ensures data quality

✅ **Created assets appear immediately in client space view**
- Libraries/lists are created directly in SharePoint
- Immediate availability after successful creation
- Assets returned in GET requests to libraries/lists endpoints

---

## Future Enhancements

Potential improvements for future releases:

1. **Batch Creation:** Create multiple libraries/lists in a single request
2. **Custom Columns:** Support for adding custom columns during list creation
3. **Templates:** Pre-defined templates for common use cases (e.g., "Legal Case", "Project")
4. **Permissions:** Set specific permissions during creation
5. **Deletion:** Endpoints to delete libraries and lists
6. **Update:** Endpoints to update library/list metadata
7. **Item Management:** CRUD operations for list items
8. **Versioning:** Enable/configure versioning for libraries

---

## Support

For issues or questions:
- Review the API documentation above
- Check validation error messages in 400 responses
- Verify client status is `Active` before creating assets
- Ensure proper authentication and permissions
- Check SharePoint site provisioning status
