# ClientSpace MVP API Reference

**Complete REST API endpoint documentation for ClientSpace MVP**

This document provides comprehensive reference documentation for all API endpoints available in ClientSpace MVP, including authentication, request/response formats, and examples.

## Table of Contents

1. [Overview](#overview)
2. [Authentication](#authentication)
3. [Base URLs](#base-urls)
4. [Common Patterns](#common-patterns)
5. [Tenant Management](#tenant-management)
6. [Client Space Management](#client-space-management)
7. [External User Management](#external-user-management)
8. [Library Management](#library-management)
9. [List Management](#list-management)
10. [Search](#search)
11. [Subscription & Billing](#subscription--billing)
12. [Audit Logs](#audit-logs)
13. [Error Handling](#error-handling)
14. [Rate Limits](#rate-limits)

---

## Overview

ClientSpace provides a REST API for managing external users, client spaces, and documents in SharePoint Online. All endpoints return JSON and follow RESTful conventions.

**API Version**: v1  
**Protocol**: HTTPS only  
**Authentication**: OAuth 2.0 Bearer tokens  
**Content-Type**: `application/json`

---

## Authentication

### OAuth 2.0 Flow

ClientSpace uses Azure AD for authentication with multi-tenant support.

**Authorization Endpoint**:
```
https://login.microsoftonline.com/common/oauth2/v2.0/authorize
```

**Token Endpoint**:
```
https://login.microsoftonline.com/common/oauth2/v2.0/token
```

### Required Scopes

| Scope | Description |
|-------|-------------|
| `api://clientspace-api/user_impersonation` | Access API as signed-in user |
| `User.Read` | Read user profile |
| `Sites.ReadWrite.All` | Manage SharePoint sites |

### Getting a Token

```bash
curl -X POST \
  https://login.microsoftonline.com/common/oauth2/v2.0/token \
  -H 'Content-Type: application/x-www-form-urlencoded' \
  -d 'client_id=YOUR_CLIENT_ID' \
  -d 'scope=api://clientspace-api/user_impersonation' \
  -d 'code=AUTHORIZATION_CODE' \
  -d 'redirect_uri=YOUR_REDIRECT_URI' \
  -d 'grant_type=authorization_code' \
  -d 'client_secret=YOUR_CLIENT_SECRET'
```

**Response**:
```json
{
  "access_token": "eyJ0eXAiOiJKV1QiLCJhbGc...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "scope": "api://clientspace-api/user_impersonation User.Read Sites.ReadWrite.All"
}
```

### Using the Token

Include the token in all API requests:

```bash
curl -X GET \
  https://api.clientspace.app/api/v1/clients \
  -H 'Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGc...'
```

---

## Base URLs

| Environment | Base URL |
|-------------|----------|
| **Production** | `https://api.clientspace.app/api/v1` |
| **Development** | `https://api-dev.clientspace.app/api/v1` |
| **Local** | `https://localhost:7071/api/v1` |

---

## Common Patterns

### Pagination

All list endpoints support pagination:

**Query Parameters**:
- `page`: Page number (default: 1)
- `pageSize`: Items per page (default: 20, max: 100)
- `sortBy`: Field to sort by
- `sortOrder`: `asc` or `desc`

**Example**:
```bash
GET /api/v1/clients?page=2&pageSize=50&sortBy=name&sortOrder=asc
```

**Response Format**:
```json
{
  "items": [...],
  "pagination": {
    "currentPage": 2,
    "pageSize": 50,
    "totalItems": 150,
    "totalPages": 3,
    "hasNextPage": true,
    "hasPreviousPage": true
  }
}
```

### Filtering

Many endpoints support filtering:

**Query Parameters**:
- `filter`: OData-style filter expression
- `search`: Full-text search query

**Example**:
```bash
GET /api/v1/users?filter=status eq 'Active'&search=john
```

### Standard Response Format

**Success Response (200 OK)**:
```json
{
  "success": true,
  "data": { ... },
  "message": "Operation completed successfully"
}
```

**Error Response (4xx/5xx)**:
```json
{
  "success": false,
  "error": {
    "code": "RESOURCE_NOT_FOUND",
    "message": "Client not found",
    "details": {
      "clientId": "abc-123"
    }
  }
}
```

---

## Tenant Management

### Get Current Tenant

```http
GET /api/v1/tenant
```

**Response**:
```json
{
  "success": true,
  "data": {
    "id": "tenant-123",
    "name": "Acme Law Firm",
    "domain": "acmelaw.com",
    "azureAdTenantId": "00000000-0000-0000-0000-000000000000",
    "subscriptionTier": "Professional",
    "status": "Active",
    "createdAt": "2024-01-15T10:00:00Z",
    "updatedAt": "2024-02-01T15:30:00Z"
  }
}
```

### Update Tenant Settings

```http
PUT /api/v1/tenant/settings
```

**Request Body**:
```json
{
  "name": "Acme Legal Services",
  "settings": {
    "defaultSiteTemplate": "TeamSite",
    "defaultPermissionLevel": "Read",
    "enableExternalSharing": true,
    "requireMfaForAdmins": true
  }
}
```

**Response**: 200 OK with updated tenant object

### Get Tenant Usage

```http
GET /api/v1/tenant/usage
```

**Response**:
```json
{
  "success": true,
  "data": {
    "clientSpaces": {
      "current": 15,
      "limit": 50
    },
    "externalUsers": {
      "current": 120,
      "limit": 500
    },
    "storage": {
      "used": "25.5 GB",
      "limit": "100 GB"
    },
    "apiCalls": {
      "current": 15420,
      "limit": 50000,
      "resetDate": "2024-03-01T00:00:00Z"
    }
  }
}
```

---

## Client Space Management

### List Client Spaces

```http
GET /api/v1/clients
```

**Query Parameters**:
- `page`, `pageSize`: Pagination
- `status`: Filter by status (`Active`, `Inactive`, `Archived`)
- `search`: Search by name or site URL

**Example**:
```bash
GET /api/v1/clients?status=Active&search=corp&page=1&pageSize=20
```

**Response**:
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "client-123",
        "name": "ABC Corporation",
        "description": "Corporate transaction matter",
        "siteUrl": "https://contoso.sharepoint.com/sites/ABC-Corporation",
        "status": "Active",
        "externalUserCount": 5,
        "libraryCount": 3,
        "primaryContact": "john.smith@abccorp.com",
        "createdAt": "2024-01-20T10:00:00Z",
        "updatedAt": "2024-02-15T14:30:00Z"
      }
    ],
    "pagination": {
      "currentPage": 1,
      "pageSize": 20,
      "totalItems": 15,
      "totalPages": 1,
      "hasNextPage": false,
      "hasPreviousPage": false
    }
  }
}
```

### Get Client by ID

```http
GET /api/v1/clients/{clientId}
```

**Response**:
```json
{
  "success": true,
  "data": {
    "id": "client-123",
    "name": "ABC Corporation",
    "description": "Corporate transaction matter",
    "siteUrl": "https://contoso.sharepoint.com/sites/ABC-Corporation",
    "siteId": "00000000-0000-0000-0000-000000000000",
    "status": "Active",
    "primaryContact": "john.smith@abccorp.com",
    "metadata": {
      "matter": "M-2024-001",
      "department": "Corporate"
    },
    "externalUserCount": 5,
    "libraryCount": 3,
    "createdAt": "2024-01-20T10:00:00Z",
    "updatedAt": "2024-02-15T14:30:00Z",
    "createdBy": "admin@tenant.com"
  }
}
```

### Create Client Space

```http
POST /api/v1/clients
```

**Request Body**:
```json
{
  "name": "XYZ Industries",
  "description": "M&A Due Diligence",
  "primaryContact": "contact@xyzindustries.com",
  "siteTemplate": "TeamSite",
  "metadata": {
    "matter": "M-2024-005",
    "department": "Corporate"
  }
}
```

**Response**: 201 Created
```json
{
  "success": true,
  "data": {
    "id": "client-456",
    "name": "XYZ Industries",
    "siteUrl": "https://contoso.sharepoint.com/sites/XYZ-Industries",
    "status": "Provisioning",
    "createdAt": "2024-02-19T10:00:00Z"
  },
  "message": "Client space is being provisioned. This may take 1-2 minutes."
}
```

### Update Client Space

```http
PUT /api/v1/clients/{clientId}
```

**Request Body**:
```json
{
  "name": "XYZ Industries Ltd",
  "description": "M&A Due Diligence - Updated",
  "primaryContact": "newcontact@xyzindustries.com",
  "metadata": {
    "matter": "M-2024-005",
    "department": "Corporate",
    "status": "In Progress"
  }
}
```

**Response**: 200 OK with updated client object

### Archive Client Space

```http
POST /api/v1/clients/{clientId}/archive
```

**Request Body** (optional):
```json
{
  "reason": "Matter completed",
  "revokeExternalAccess": true
}
```

**Response**: 200 OK

### Delete Client Space

```http
DELETE /api/v1/clients/{clientId}
```

**Query Parameters**:
- `permanent`: Set to `true` for permanent deletion (default: false, archives instead)

**Response**: 204 No Content

---

## External User Management

### List External Users

```http
GET /api/v1/clients/{clientId}/users
```

**Query Parameters**:
- `page`, `pageSize`: Pagination
- `status`: Filter by status (`Active`, `Pending`, `Revoked`)
- `company`: Filter by company
- `search`: Search by name or email

**Response**:
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "user-789",
        "email": "jane.doe@example.com",
        "displayName": "Jane Doe",
        "company": "Example Consulting",
        "project": "Corporate Transaction",
        "status": "Active",
        "invitedDate": "2024-01-25T10:00:00Z",
        "acceptedDate": "2024-01-25T11:30:00Z",
        "lastAccessDate": "2024-02-18T15:45:00Z",
        "libraries": [
          {
            "id": "lib-123",
            "name": "Documents",
            "permissionLevel": "Read"
          }
        ]
      }
    ],
    "pagination": { ... }
  }
}
```

### Get External User by ID

```http
GET /api/v1/clients/{clientId}/users/{userId}
```

**Response**: User object with full details

### Invite External User

```http
POST /api/v1/clients/{clientId}/users/invite
```

**Request Body**:
```json
{
  "email": "john.external@example.com",
  "displayName": "John External",
  "company": "External Corp",
  "project": "Project Alpha",
  "libraries": [
    {
      "libraryId": "lib-123",
      "permissionLevel": "Read"
    },
    {
      "libraryId": "lib-456",
      "permissionLevel": "Edit"
    }
  ],
  "personalMessage": "Welcome to our client portal. You now have access to project documents.",
  "sendEmail": true
}
```

**Response**: 201 Created
```json
{
  "success": true,
  "data": {
    "id": "user-999",
    "email": "john.external@example.com",
    "status": "Pending",
    "invitedDate": "2024-02-19T10:00:00Z",
    "invitationUrl": "https://contoso.sharepoint.com/...",
    "expiresAt": "2024-03-20T10:00:00Z"
  },
  "message": "Invitation sent successfully"
}
```

### Bulk Invite External Users

```http
POST /api/v1/clients/{clientId}/users/invite-bulk
```

**Request Body**:
```json
{
  "users": [
    {
      "email": "user1@example.com",
      "displayName": "User One",
      "company": "Example Corp",
      "libraries": [{"libraryId": "lib-123", "permissionLevel": "Read"}]
    },
    {
      "email": "user2@example.com",
      "displayName": "User Two",
      "company": "Example Corp",
      "libraries": [{"libraryId": "lib-123", "permissionLevel": "Edit"}]
    }
  ],
  "sendEmails": true
}
```

**Response**: 202 Accepted
```json
{
  "success": true,
  "data": {
    "bulkOperationId": "bulk-001",
    "totalUsers": 2,
    "status": "Processing"
  },
  "message": "Bulk invitation is being processed"
}
```

### Update External User

```http
PUT /api/v1/clients/{clientId}/users/{userId}
```

**Request Body**:
```json
{
  "displayName": "Jane Doe Updated",
  "company": "New Company Name",
  "project": "Updated Project",
  "libraries": [
    {
      "libraryId": "lib-123",
      "permissionLevel": "Edit"
    }
  ]
}
```

**Response**: 200 OK with updated user object

### Revoke External User Access

```http
POST /api/v1/clients/{clientId}/users/{userId}/revoke
```

**Request Body** (optional):
```json
{
  "reason": "Project completed",
  "notifyUser": false
}
```

**Response**: 200 OK

### Resend Invitation

```http
POST /api/v1/clients/{clientId}/users/{userId}/resend-invitation
```

**Response**: 200 OK

---

## Library Management

### List Libraries

```http
GET /api/v1/clients/{clientId}/libraries
```

**Response**:
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "lib-123",
        "name": "Documents",
        "description": "General client documents",
        "url": "https://contoso.sharepoint.com/sites/ABC-Corp/Documents",
        "itemCount": 45,
        "sizeInBytes": 125000000,
        "externalUserCount": 3,
        "settings": {
          "versioningEnabled": true,
          "requireCheckOut": false,
          "requireApproval": false
        },
        "createdAt": "2024-01-20T10:00:00Z",
        "updatedAt": "2024-02-15T14:30:00Z"
      }
    ]
  }
}
```

### Get Library by ID

```http
GET /api/v1/clients/{clientId}/libraries/{libraryId}
```

**Response**: Library object with full details

### Create Library

```http
POST /api/v1/clients/{clientId}/libraries
```

**Request Body**:
```json
{
  "name": "Contracts",
  "description": "Contract documents and agreements",
  "template": "DocumentLibrary",
  "settings": {
    "versioningEnabled": true,
    "requireCheckOut": false,
    "requireApproval": true
  }
}
```

**Response**: 201 Created

### Update Library

```http
PUT /api/v1/clients/{clientId}/libraries/{libraryId}
```

**Request Body**:
```json
{
  "name": "Contracts Updated",
  "description": "Updated description",
  "settings": {
    "versioningEnabled": true,
    "requireCheckOut": true,
    "requireApproval": true
  }
}
```

**Response**: 200 OK

### Delete Library

```http
DELETE /api/v1/clients/{clientId}/libraries/{libraryId}
```

**Response**: 204 No Content

---

## List Management

### List SharePoint Lists

```http
GET /api/v1/clients/{clientId}/lists
```

**Response**: Similar structure to libraries endpoint

### Create List

```http
POST /api/v1/clients/{clientId}/lists
```

**Request Body**:
```json
{
  "name": "Tasks",
  "description": "Project tasks and milestones",
  "template": "GenericList",
  "columns": [
    {"name": "Title", "type": "Text"},
    {"name": "Status", "type": "Choice", "choices": ["Not Started", "In Progress", "Completed"]},
    {"name": "DueDate", "type": "DateTime"}
  ]
}
```

**Response**: 201 Created

---

## Search

### Search All Clients (Global Search)

```http
GET /api/v1/search
```

**Query Parameters**:
- `q`: Search query (required)
- `type`: Filter by type (`clients`, `users`, `documents`)
- `page`, `pageSize`: Pagination

**Example**:
```bash
GET /api/v1/search?q=contract&type=documents&page=1&pageSize=20
```

**Response**:
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "type": "document",
        "title": "Master Service Agreement.docx",
        "clientId": "client-123",
        "clientName": "ABC Corporation",
        "libraryId": "lib-123",
        "libraryName": "Contracts",
        "url": "https://contoso.sharepoint.com/sites/ABC-Corp/Contracts/MSA.docx",
        "snippet": "...terms of this agreement...",
        "modifiedDate": "2024-02-10T15:30:00Z",
        "modifiedBy": "jane.doe@example.com"
      }
    ],
    "pagination": { ... },
    "facets": {
      "clients": [
        {"name": "ABC Corporation", "count": 5},
        {"name": "XYZ Industries", "count": 3}
      ],
      "types": [
        {"name": "documents", "count": 15},
        {"name": "users", "count": 2}
      ]
    }
  }
}
```

### Search Within Client

```http
GET /api/v1/clients/{clientId}/search
```

**Query Parameters**:
- `q`: Search query (required)
- `library`: Filter by library ID
- `page`, `pageSize`: Pagination

**Response**: Similar to global search, scoped to client

---

## Subscription & Billing

### Get Subscription

```http
GET /api/v1/subscription
```

**Response**:
```json
{
  "success": true,
  "data": {
    "id": "sub-123",
    "tenantId": "tenant-123",
    "tier": "Professional",
    "status": "Active",
    "billingCycle": "Monthly",
    "amount": 99.00,
    "currency": "USD",
    "currentPeriodStart": "2024-02-01T00:00:00Z",
    "currentPeriodEnd": "2024-03-01T00:00:00Z",
    "cancelAtPeriodEnd": false,
    "stripeCustomerId": "cus_xxxxx",
    "stripeSubscriptionId": "sub_xxxxx",
    "limits": {
      "clientSpaces": 50,
      "externalUsers": 500,
      "storage": "100 GB",
      "apiCallsPerMonth": 50000
    }
  }
}
```

### Get Available Plans

```http
GET /api/v1/subscription/plans
```

**Response**:
```json
{
  "success": true,
  "data": [
    {
      "id": "plan-free",
      "name": "Free",
      "price": 0,
      "currency": "USD",
      "billingCycle": "Monthly",
      "features": {
        "clientSpaces": 3,
        "externalUsers": 10,
        "storage": "10 GB",
        "globalSearch": false,
        "aiAssistant": false
      }
    },
    {
      "id": "plan-pro",
      "name": "Professional",
      "price": 99,
      "currency": "USD",
      "billingCycle": "Monthly",
      "features": {
        "clientSpaces": 50,
        "externalUsers": 500,
        "storage": "100 GB",
        "globalSearch": true,
        "aiAssistant": true
      }
    }
  ]
}
```

### Create Checkout Session

```http
POST /api/v1/subscription/checkout
```

**Request Body**:
```json
{
  "planId": "plan-pro",
  "billingCycle": "Monthly",
  "successUrl": "https://portal.clientspace.app/subscription/success",
  "cancelUrl": "https://portal.clientspace.app/subscription/cancel"
}
```

**Response**:
```json
{
  "success": true,
  "data": {
    "sessionId": "cs_xxxxx",
    "checkoutUrl": "https://checkout.stripe.com/pay/cs_xxxxx"
  }
}
```

### Upgrade Subscription

```http
POST /api/v1/subscription/upgrade
```

**Request Body**:
```json
{
  "planId": "plan-enterprise",
  "billingCycle": "Annual"
}
```

**Response**: 200 OK with updated subscription

### Cancel Subscription

```http
POST /api/v1/subscription/cancel
```

**Request Body**:
```json
{
  "reason": "No longer needed",
  "feedback": "Service was great, but we're closing down",
  "cancelAtPeriodEnd": true
}
```

**Response**: 200 OK

---

## Audit Logs

### List Audit Logs

```http
GET /api/v1/audit-logs
```

**Query Parameters**:
- `startDate`: Filter by start date (ISO 8601)
- `endDate`: Filter by end date (ISO 8601)
- `userId`: Filter by user ID
- `action`: Filter by action type
- `resourceType`: Filter by resource type
- `page`, `pageSize`: Pagination

**Example**:
```bash
GET /api/v1/audit-logs?startDate=2024-02-01T00:00:00Z&action=UserInvited&page=1
```

**Response**:
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "log-123",
        "timestamp": "2024-02-19T10:15:30Z",
        "userId": "user@tenant.com",
        "action": "UserInvited",
        "resourceType": "ExternalUser",
        "resourceId": "user-789",
        "details": {
          "email": "external@example.com",
          "clientId": "client-123",
          "libraries": ["lib-123"]
        },
        "ipAddress": "203.0.113.1",
        "userAgent": "Mozilla/5.0..."
      }
    ],
    "pagination": { ... }
  }
}
```

### Export Audit Logs

```http
POST /api/v1/audit-logs/export
```

**Request Body**:
```json
{
  "format": "csv",
  "startDate": "2024-01-01T00:00:00Z",
  "endDate": "2024-02-19T23:59:59Z",
  "filters": {
    "action": ["UserInvited", "UserRevoked"]
  }
}
```

**Response**:
```json
{
  "success": true,
  "data": {
    "exportId": "export-123",
    "status": "Processing",
    "downloadUrl": null,
    "expiresAt": "2024-02-20T10:00:00Z"
  }
}
```

---

## Error Handling

### Error Response Format

All errors follow this structure:

```json
{
  "success": false,
  "error": {
    "code": "ERROR_CODE",
    "message": "Human-readable error message",
    "details": {
      "field": "additionalInfo"
    },
    "timestamp": "2024-02-19T10:00:00Z",
    "requestId": "req-12345"
  }
}
```

### Common Error Codes

| HTTP Status | Error Code | Description |
|-------------|------------|-------------|
| 400 | `INVALID_REQUEST` | Request validation failed |
| 401 | `UNAUTHORIZED` | Authentication required |
| 403 | `FORBIDDEN` | Insufficient permissions |
| 404 | `RESOURCE_NOT_FOUND` | Resource doesn't exist |
| 409 | `CONFLICT` | Resource conflict (e.g., duplicate) |
| 422 | `VALIDATION_ERROR` | Input validation failed |
| 429 | `RATE_LIMIT_EXCEEDED` | Too many requests |
| 500 | `INTERNAL_ERROR` | Server error |
| 503 | `SERVICE_UNAVAILABLE` | Service temporarily unavailable |

### Validation Errors

```json
{
  "success": false,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Validation failed",
    "details": {
      "errors": [
        {
          "field": "email",
          "message": "Invalid email format"
        },
        {
          "field": "name",
          "message": "Name is required"
        }
      ]
    }
  }
}
```

---

## Rate Limits

### Rate Limit Tiers

| Plan | Requests/Hour | Requests/Day | Burst Limit |
|------|---------------|--------------|-------------|
| **Free** | 100 | 1,000 | 10 req/sec |
| **Professional** | 1,000 | 10,000 | 50 req/sec |
| **Enterprise** | 10,000 | 100,000 | 100 req/sec |

### Rate Limit Headers

All responses include rate limit headers:

```
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 950
X-RateLimit-Reset: 1708344000
```

### Rate Limit Exceeded

**Response (429 Too Many Requests)**:
```json
{
  "success": false,
  "error": {
    "code": "RATE_LIMIT_EXCEEDED",
    "message": "Rate limit exceeded. Please retry after 3600 seconds.",
    "details": {
      "limit": 1000,
      "remaining": 0,
      "resetAt": "2024-02-19T11:00:00Z"
    }
  }
}
```

### Best Practices

1. **Implement exponential backoff** for retries
2. **Cache responses** when appropriate
3. **Use webhooks** instead of polling
4. **Monitor rate limit headers** and throttle requests
5. **Batch operations** when possible (e.g., bulk invite)

---

## Additional Resources

- **[User Guide](USER_GUIDE.md)**: Complete feature documentation
- **[Quick Start Guide](MVP_QUICK_START.md)**: Getting started in 5 minutes
- **[Deployment Runbook](MVP_DEPLOYMENT_RUNBOOK.md)**: Deployment procedures
- **[Support Runbook](MVP_SUPPORT_RUNBOOK.md)**: Troubleshooting guide
- **[OpenAPI Specification](../src/api-dotnet/openapi.yaml)**: Machine-readable API spec

---

## Changelog

### v1.0.0 (February 2026)
- Initial MVP release
- Tenant management endpoints
- Client space CRUD operations
- External user management
- Library and list management
- Search functionality
- Subscription and billing
- Audit logging

---

*Last Updated: February 2026*  
*Version: MVP 1.0*  
*API Version: v1*
