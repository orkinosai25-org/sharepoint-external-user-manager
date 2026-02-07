# External User Management API Documentation

## Overview

The External User Management API provides endpoints for managing external users (guests) in SharePoint client sites. All operations are tenant-isolated and fully audited.

## Authentication

All endpoints require authentication via Azure AD JWT bearer token:

```
Authorization: Bearer <YOUR_JWT_TOKEN>
```

## Endpoints

### 1. List External Users

Get all external users for a specific client site.

**Endpoint**: `GET /clients/{id}/external-users`

**Path Parameters**:
- `id` (integer, required): The client ID

**Response**:
```json
{
  "success": true,
  "data": [
    {
      "id": "permission-id-123",
      "email": "partner@external.com",
      "displayName": "John Partner",
      "permissionLevel": "Read",
      "invitedDate": "2026-02-06T10:00:00Z",
      "invitedBy": "admin@company.com",
      "lastAccessDate": null,
      "status": "Active"
    }
  ]
}
```

**Possible Errors**:
- `401 Unauthorized`: Missing or invalid authentication token
- `404 Not Found`: Client not found or not accessible to your tenant
- `400 Bad Request`: Client site not provisioned yet

**Example cURL**:
```bash
curl -X GET "https://api.example.com/clients/1/external-users" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

---

### 2. Invite External User

Invite an external user to a client site with specified permissions.

**Endpoint**: `POST /clients/{id}/external-users`

**Path Parameters**:
- `id` (integer, required): The client ID

**Request Body**:
```json
{
  "email": "partner@external.com",
  "displayName": "John Partner",
  "permissionLevel": "Read",
  "message": "Welcome to our client space"
}
```

**Request Fields**:
- `email` (string, required): Email address of the external user
- `displayName` (string, optional): Display name for the user
- `permissionLevel` (string, required): Permission level - must be one of: "Read", "Edit", "Write", "Contribute"
- `message` (string, optional): Custom welcome message in invitation email

**Response**:
```json
{
  "success": true,
  "data": {
    "id": "permission-id-456",
    "email": "partner@external.com",
    "displayName": "John Partner",
    "permissionLevel": "Read",
    "invitedDate": "2026-02-06T12:00:00Z",
    "invitedBy": "admin@company.com",
    "lastAccessDate": null,
    "status": "Invited"
  }
}
```

**Possible Errors**:
- `401 Unauthorized`: Missing or invalid authentication token
- `404 Not Found`: Client not found or not accessible to your tenant
- `400 Bad Request`: Invalid permission level or client site not provisioned
- `500 Internal Server Error`: Failed to invite user (e.g., Graph API error)

**Permission Levels**:
- `Read`: View-only access to documents and lists
- `Edit` / `Write` / `Contribute`: Can view, add, update, and delete items
- `Owner` / `FullControl`: Full control over the site (typically not used for external users)

**Example cURL**:
```bash
curl -X POST "https://api.example.com/clients/1/external-users" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "partner@external.com",
    "displayName": "John Partner",
    "permissionLevel": "Read",
    "message": "Welcome to our collaboration space"
  }'
```

---

### 3. Remove External User

Remove an external user's access from a client site.

**Endpoint**: `DELETE /clients/{id}/external-users/{email}`

**Path Parameters**:
- `id` (integer, required): The client ID
- `email` (string, required): Email address of the external user to remove (URL-encoded)

**Response**:
```json
{
  "success": true,
  "data": {
    "message": "External user partner@external.com removed successfully"
  }
}
```

**Possible Errors**:
- `401 Unauthorized`: Missing or invalid authentication token
- `404 Not Found`: Client not found or not accessible to your tenant
- `400 Bad Request`: Client site not provisioned yet
- `500 Internal Server Error`: Failed to remove user (e.g., user not found in permissions)

**Example cURL**:
```bash
curl -X DELETE "https://api.example.com/clients/1/external-users/partner%40external.com" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

---

## Security & Tenant Isolation

### Tenant Isolation
- All operations are automatically scoped to the authenticated tenant
- Tenants can only manage external users in their own client sites
- Cross-tenant access is prevented at the database and API level

### Audit Logging
All external user operations are logged to the AuditLogs table with:
- Action type (e.g., "EXTERNAL_USER_INVITED", "EXTERNAL_USER_REMOVED")
- Timestamp
- User who performed the action
- Success/failure status
- Correlation ID for tracing

### Permissions
External user operations require appropriate permissions:
- **List**: Available to all authenticated users within the tenant
- **Invite**: Requires write permissions (FirmAdmin, Admin, Owner roles)
- **Remove**: Requires delete permissions (FirmAdmin, Admin, Owner roles)

---

## Implementation Details

### Microsoft Graph Integration
The API uses Microsoft Graph API to manage SharePoint site permissions:
- **List Users**: `GET /sites/{siteId}/permissions`
- **Invite User**: `POST /sites/{siteId}/permissions`
- **Remove User**: `DELETE /sites/{siteId}/permissions/{permissionId}`

### External User Identification
External users (guests) are identified by:
- User IDs containing `#EXT#` (Azure AD external user format)
- Display names containing "(Guest)"

### Email Extraction
Azure AD encodes external user emails in the format:
```
original.email_domain.com#EXT#@tenant.onmicrosoft.com
```

The API automatically extracts and decodes the original email address for display.

---

## Error Handling

All errors follow a consistent structure:

```json
{
  "success": false,
  "error": {
    "code": "ERROR_CODE",
    "message": "Human-readable error message"
  }
}
```

Common error codes:
- `AUTH_ERROR`: Authentication failure
- `TENANT_NOT_FOUND`: Tenant not found in database
- `CLIENT_NOT_FOUND`: Client not found or not accessible
- `SITE_NOT_PROVISIONED`: Client site not yet provisioned
- `INVALID_PERMISSION`: Invalid permission level specified
- `INVITE_FAILED`: Failed to invite external user
- `REMOVE_FAILED`: Failed to remove external user

---

## Best Practices

### Inviting External Users
1. Always verify the email address before inviting
2. Use meaningful display names to help identify users
3. Include a custom message explaining the purpose of the invitation
4. Start with "Read" permissions and escalate only if needed
5. Monitor the audit logs for successful invitations

### Managing Permissions
1. Review external users regularly and remove those no longer needed
2. Use the lowest privilege level necessary for the user's role
3. Keep track of when users were invited and their last access date
4. Document the business reason for each external user invitation

### Error Recovery
1. Check audit logs if an operation fails
2. Verify the client site is provisioned before inviting users
3. Ensure the email address is valid and properly formatted
4. Use correlation IDs from error responses when reporting issues

---

## Examples

### Complete Workflow: Invite and Manage External User

```bash
# 1. List current external users
curl -X GET "https://api.example.com/clients/1/external-users" \
  -H "Authorization: Bearer $TOKEN"

# 2. Invite a new external user
curl -X POST "https://api.example.com/clients/1/external-users" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "consultant@partner.com",
    "displayName": "Jane Consultant",
    "permissionLevel": "Read",
    "message": "Welcome! You have been granted read access to our client documents."
  }'

# 3. Later, remove the external user
curl -X DELETE "https://api.example.com/clients/1/external-users/consultant%40partner.com" \
  -H "Authorization: Bearer $TOKEN"

# 4. Verify removal
curl -X GET "https://api.example.com/clients/1/external-users" \
  -H "Authorization: Bearer $TOKEN"
```

---

## Support

For issues or questions:
1. Check the audit logs for detailed operation history
2. Review error messages and correlation IDs
3. Verify Azure AD and Microsoft Graph permissions
4. Contact support with correlation IDs for failed operations

