# API Specification

## Base URL

```
Production:  https://api.spexternal.com/v1
Staging:     https://api-stage.spexternal.com/v1
Development: http://localhost:7071/api
```

## Authentication

All endpoints require an Azure AD Bearer token in the Authorization header:

```http
GET /tenants/me HTTP/1.1
Host: api.spexternal.com
Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGc...
Content-Type: application/json
X-Correlation-ID: 550e8400-e29b-41d4-a716-446655440000
```

### Headers

| Header | Required | Description |
|--------|----------|-------------|
| `Authorization` | Yes | Bearer token from Azure AD |
| `Content-Type` | Yes (POST/PUT) | `application/json` |
| `X-Correlation-ID` | No | Request tracking ID (auto-generated if not provided) |

## Response Format

### Success Response

```json
{
  "success": true,
  "data": { /* response data */ },
  "meta": {
    "correlationId": "550e8400-e29b-41d4-a716-446655440000",
    "timestamp": "2024-01-15T10:30:00Z"
  }
}
```

### Error Response

```json
{
  "success": false,
  "error": {
    "code": "TENANT_NOT_FOUND",
    "message": "Tenant not found or not onboarded",
    "details": "No tenant found for Entra ID tenant: 12345...",
    "correlationId": "550e8400-e29b-41d4-a716-446655440000"
  }
}
```

## Endpoints

### 1. Tenant Management

#### POST /tenants/onboard

Onboard a new tenant to the SaaS platform.

**Request:**
```http
POST /tenants/onboard HTTP/1.1
## Base Information

**Base URL**: `https://api.spexternal.com/v1`  
**Protocol**: HTTPS only  
**Authentication**: Azure AD Bearer Token  
**Content-Type**: `application/json`

## Authentication

All API requests require authentication using Azure AD Bearer tokens:

```http
Authorization: Bearer {azure_ad_token}
X-Tenant-ID: {tenant_id}
X-Correlation-ID: {optional_correlation_id}
```

### Error Responses

Standard error format:
```json
{
  "error": {
    "code": "ERROR_CODE",
    "message": "Human-readable error message",
    "details": "Additional context or technical details",
    "correlationId": "550e8400-e29b-41d4-a716-446655440000",
    "timestamp": "2024-01-20T14:35:22Z"
  }
}
```

### HTTP Status Codes

| Code | Meaning | Usage |
|------|---------|-------|
| 200 | OK | Successful GET/PUT request |
| 201 | Created | Successful POST request |
| 204 | No Content | Successful DELETE request |
| 400 | Bad Request | Invalid input parameters |
| 401 | Unauthorized | Missing or invalid authentication |
| 403 | Forbidden | Insufficient permissions or subscription tier |
| 404 | Not Found | Resource doesn't exist |
| 409 | Conflict | Resource already exists or state conflict |
| 429 | Too Many Requests | Rate limit exceeded |
| 500 | Internal Server Error | Server-side error |
| 503 | Service Unavailable | Temporary service disruption |

## API Endpoints

### Tenant Management

#### POST /tenants/onboard
Onboard a new tenant to the platform.

**Request:**
```http
POST /tenants/onboard
Authorization: Bearer {token}
Content-Type: application/json

{
  "organizationName": "Contoso Ltd",
  "primaryAdminEmail": "admin@contoso.com",
  "settings": {
    "timezone": "UTC",
    "locale": "en-US"
  }
}
```

**Response (201 Created):**
  "tenantId": "contoso.onmicrosoft.com",
  "adminEmail": "admin@contoso.com",
  "companyName": "Contoso Ltd",
  "subscriptionTier": "trial",
  "dataLocation": "eastus"
}
```

**Response (201):**
```json
{
  "success": true,
  "data": {
    "tenantId": 1,
    "entraIdTenantId": "12345678-1234-1234-1234-123456789abc",
    "organizationName": "Contoso Ltd",
    "primaryAdminEmail": "admin@contoso.com",
    "onboardedDate": "2024-01-15T10:00:00Z",
    "status": "Active",
    "subscription": {
      "tier": "Free",
      "status": "Trial",
      "trialExpiry": "2024-02-14T10:00:00Z",
      "maxUsers": 10
    }
  },
  "meta": {
    "correlationId": "550e8400-e29b-41d4-a716-446655440000",
    "timestamp": "2024-01-15T10:00:00Z"
  }
}
```

**Errors:**
- `400 Bad Request`: Invalid request data
- `401 Unauthorized`: Invalid or missing token
- `409 Conflict`: Tenant already onboarded

---

#### GET /tenants/me

    "tenantId": "contoso.onmicrosoft.com",
    "status": "active",
    "subscriptionTier": "trial",
    "trialEndDate": "2024-02-19T23:59:59Z",
    "apiEndpoint": "https://api.spexternal.com/v1",
    "onboardingCompleted": true,
    "createdDate": "2024-01-20T14:35:22Z"
  }
}
```

#### GET /tenants/me
Get current tenant information.

**Request:**
```http
GET /tenants/me HTTP/1.1
Authorization: Bearer {token}
```

**Response (200 OK):**
GET /tenants/me
Authorization: Bearer {token}
X-Tenant-ID: contoso.onmicrosoft.com
```

**Response (200):**
```json
{
  "success": true,
  "data": {
    "tenantId": 1,
    "entraIdTenantId": "12345678-1234-1234-1234-123456789abc",
    "organizationName": "Contoso Ltd",
    "primaryAdminEmail": "admin@contoso.com",
    "onboardedDate": "2024-01-15T10:00:00Z",
    "status": "Active",
    "settings": {
      "timezone": "UTC",
      "locale": "en-US"
    }
  }
}
```

**Errors:**
- `401 Unauthorized`: Invalid token
- `404 Not Found`: Tenant not onboarded

---

#### GET /tenants/subscription

Get current subscription status and features.

**Request:**
```http
GET /tenants/subscription HTTP/1.1
Authorization: Bearer {token}
```

**Response (200 OK):**
    "tenantId": "contoso.onmicrosoft.com",
    "displayName": "Contoso Ltd",
    "status": "active",
    "subscriptionTier": "pro",
    "subscriptionStatus": "active",
    "subscriptionEndDate": "2025-01-20T00:00:00Z",
    "features": {
      "auditExport": true,
      "bulkOperations": true,
      "advancedReporting": true,
      "customPolicies": false
    },
    "limits": {
      "maxExternalUsers": 500,
      "maxLibraries": 100,
      "apiCallsPerMonth": 100000,
      "currentExternalUsers": 127,
      "currentLibraries": 23,
      "apiCallsThisMonth": 12450
    },
    "createdDate": "2024-01-20T14:35:22Z",
    "lastModifiedDate": "2024-01-25T10:20:15Z"
  }
}
```

#### PUT /tenants/me
Update tenant settings.

**Request:**
```http
PUT /tenants/me
Authorization: Bearer {token}
X-Tenant-ID: contoso.onmicrosoft.com
Content-Type: application/json

{
  "displayName": "Contoso Corporation",
  "settings": {
    "webhookUrl": "https://contoso.com/webhook",
    "notificationEmail": "notifications@contoso.com"
  }
}
```

**Response (200):**
```json
{
  "success": true,
  "data": {
    "tier": "Pro",
    "status": "Active",
    "startDate": "2024-01-15T10:00:00Z",
    "endDate": null,
    "trialExpiry": "2024-02-14T10:00:00Z",
    "gracePeriodEnd": null,
    "maxUsers": 100,
    "features": {
      "auditHistoryDays": 90,
      "exportEnabled": true,
      "scheduledReviews": false,
      "advancedPolicies": true
    },
    "usage": {
      "currentUsers": 25,
      "apiCallsThisMonth": 1523
    }
  }
}
```

**Errors:**
- `401 Unauthorized`: Invalid token
- `404 Not Found`: Tenant not onboarded

---

### 2. External User Management

#### GET /external-users

List external users with filtering and pagination.

**Request:**
```http
GET /external-users?library={libraryUrl}&status=Active&page=1&pageSize=50 HTTP/1.1
Authorization: Bearer {token}
```

**Query Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `library` | string | Filter by SharePoint library URL |
| `status` | string | Filter by status: `Active`, `Invited`, `Expired` |
| `email` | string | Search by user email |
| `company` | string | Filter by company name |
| `project` | string | Filter by project name |
| `page` | number | Page number (default: 1) |
| `pageSize` | number | Items per page (default: 50, max: 100) |

**Response (200 OK):**
    "tenantId": "contoso.onmicrosoft.com",
    "displayName": "Contoso Corporation",
    "lastModifiedDate": "2024-01-20T15:00:00Z"
  }
}
```

### External User Management

#### GET /external-users
List external users for the tenant.

**Request:**
```http
GET /external-users?page=1&pageSize=50&status=active&company=Acme&project=ProjectX
Authorization: Bearer {token}
X-Tenant-ID: contoso.onmicrosoft.com
```

**Query Parameters:**
- `page` (optional, default: 1): Page number
- `pageSize` (optional, default: 50, max: 100): Items per page
- `status` (optional): Filter by status (invited, active, expired, revoked)
- `company` (optional): Filter by company name
- `project` (optional): Filter by project name
- `search` (optional): Search in email, name, company, project

**Response (200):**
```json
{
  "success": true,
  "data": [
    {
      "id": "user-001",
      "email": "partner@external.com",
      "displayName": "Jane Partner",
      "library": "https://contoso.sharepoint.com/sites/project1/docs",
      "permissions": "Read",
      "invitedBy": "admin@contoso.com",
      "invitedDate": "2024-01-10T09:15:00Z",
      "lastAccess": "2024-01-14T16:45:00Z",
      "status": "Active",
      "metadata": {
        "company": "Partner Corp",
        "project": "Q1 Campaign"
      }
      "userId": "user-guid-123",
      "email": "partner@acme.com",
      "displayName": "Jane Partner",
      "firstName": "Jane",
      "lastName": "Partner",
      "company": "Acme Corp",
      "project": "ProjectX",
      "department": "Engineering",
      "status": "active",
      "invitedBy": "admin@contoso.com",
      "invitedDate": "2024-01-10T09:15:00Z",
      "lastAccessDate": "2024-01-19T16:45:00Z",
      "expirationDate": "2024-04-10T23:59:59Z",
      "libraries": [
        {
          "libraryId": "lib-guid-456",
          "libraryName": "Partner Documents",
          "permissions": "read"
        }
      ]
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 50,
    "total": 125,
    "totalPages": 3,
    "hasNext": true,
    "hasPrev": false
  }
}
```

**Errors:**
- `401 Unauthorized`: Invalid token
- `402 Payment Required`: Subscription expired or quota exceeded
- `403 Forbidden`: Insufficient permissions

---

#### POST /external-users/invite

Invite an external user to a library (MVP optional).

**Request:**
```http
POST /external-users/invite HTTP/1.1
Authorization: Bearer {token}
Content-Type: application/json

{
  "email": "newpartner@external.com",
  "displayName": "New Partner",
  "library": "https://contoso.sharepoint.com/sites/project1/docs",
  "permissions": "Contribute",
  "message": "Welcome to our collaboration workspace",
  "metadata": {
    "company": "New Partner Inc",
    "project": "Q2 Initiative"
  }
}
```

**Response (201 Created):**
    "total": 127,
    "totalPages": 3,
    "hasNext": true,
    "hasPrevious": false
  }
}
```

#### POST /external-users/invite
Invite an external user.

**Request:**
```http
POST /external-users/invite
Authorization: Bearer {token}
X-Tenant-ID: contoso.onmicrosoft.com
Content-Type: application/json

{
  "email": "newpartner@acme.com",
  "displayName": "John Partner",
  "firstName": "John",
  "lastName": "Partner",
  "company": "Acme Corp",
  "project": "ProjectX",
  "libraryId": "lib-guid-456",
  "permissions": "contribute",
  "message": "Welcome to our collaboration workspace!",
  "expirationDays": 90
}
```

**Response (201):**
```json
{
  "success": true,
  "data": {
    "id": "user-002",
    "email": "newpartner@external.com",
    "displayName": "New Partner",
    "library": "https://contoso.sharepoint.com/sites/project1/docs",
    "permissions": "Contribute",
    "invitedBy": "admin@contoso.com",
    "invitedDate": "2024-01-15T11:30:00Z",
    "status": "Invited",
    "metadata": {
      "company": "New Partner Inc",
      "project": "Q2 Initiative"
    }
  }
}
```

**Errors:**
- `400 Bad Request`: Invalid email or library URL
- `402 Payment Required`: User quota exceeded
- `409 Conflict`: User already invited

---

#### POST /external-users/remove

Remove external user access (MVP optional).

**Request:**
```http
POST /external-users/remove HTTP/1.1
Authorization: Bearer {token}
Content-Type: application/json

{
  "email": "partner@external.com",
  "library": "https://contoso.sharepoint.com/sites/project1/docs"
}
```

**Response (200 OK):**
    "userId": "user-guid-789",
    "email": "newpartner@acme.com",
    "displayName": "John Partner",
    "company": "Acme Corp",
    "project": "ProjectX",
    "status": "invited",
    "invitedBy": "admin@contoso.com",
    "invitedDate": "2024-01-20T15:30:00Z",
    "expirationDate": "2024-04-20T23:59:59Z",
    "invitationId": "inv-guid-012",
    "invitationUrl": "https://..."
  }
}
```

#### POST /external-users/remove
Remove an external user (revoke access).

**Request:**
```http
POST /external-users/remove
Authorization: Bearer {token}
X-Tenant-ID: contoso.onmicrosoft.com
Content-Type: application/json

{
  "userId": "user-guid-123",
  "reason": "Contract ended",
  "removeFromAllLibraries": true
}
```

**Response (200):**
```json
{
  "success": true,
  "data": {
    "email": "partner@external.com",
    "library": "https://contoso.sharepoint.com/sites/project1/docs",
    "removedBy": "admin@contoso.com",
    "removedDate": "2024-01-15T12:00:00Z"
  }
}
```

**Errors:**
- `404 Not Found`: User or library not found

---

### 3. Policy Management

#### GET /policies

Get collaboration policies for the tenant.

**Request:**
```http
GET /policies HTTP/1.1
Authorization: Bearer {token}
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "policyType": "GuestExpiration",
      "enabled": true,
      "configuration": {
        "expirationDays": 90,
        "notifyBeforeDays": 7
      },
      "modifiedDate": "2024-01-15T10:00:00Z"
    },
    {
      "id": 2,
      "policyType": "RequireApproval",
      "enabled": false,
      "configuration": {
        "approvers": ["admin@contoso.com"]
      },
      "modifiedDate": "2024-01-15T10:00:00Z"
    }
  ]
}
```

---

#### PUT /policies

Update collaboration policies.

**Request:**
```http
PUT /policies HTTP/1.1
Authorization: Bearer {token}
Content-Type: application/json

{
  "policyType": "GuestExpiration",
  "enabled": true,
  "configuration": {
    "expirationDays": 60,
    "notifyBeforeDays": 14
  }
}
```

**Response (200 OK):**
    "userId": "user-guid-123",
    "email": "partner@acme.com",
    "status": "revoked",
    "revokedDate": "2024-01-20T16:00:00Z",
    "revokedBy": "admin@contoso.com"
  }
}
```

#### GET /external-users/{userId}
Get details for a specific external user.

**Request:**
```http
GET /external-users/user-guid-123
Authorization: Bearer {token}
X-Tenant-ID: contoso.onmicrosoft.com
```

**Response (200):**
```json
{
  "success": true,
  "data": {
    "userId": "user-guid-123",
    "email": "partner@acme.com",
    "displayName": "Jane Partner",
    "firstName": "Jane",
    "lastName": "Partner",
    "company": "Acme Corp",
    "project": "ProjectX",
    "department": "Engineering",
    "status": "active",
    "invitedBy": "admin@contoso.com",
    "invitedDate": "2024-01-10T09:15:00Z",
    "lastAccessDate": "2024-01-19T16:45:00Z",
    "expirationDate": "2024-04-10T23:59:59Z",
    "libraries": [
      {
        "libraryId": "lib-guid-456",
        "libraryName": "Partner Documents",
        "siteUrl": "https://contoso.sharepoint.com/sites/partners",
        "permissions": "read",
        "grantedDate": "2024-01-10T09:20:00Z",
        "grantedBy": "admin@contoso.com"
      }
    ],
    "auditSummary": {
      "totalAccesses": 45,
      "lastActivity": "2024-01-19T16:45:00Z",
      "filesAccessed": 12,
      "filesDownloaded": 3
    }
  }
}
```

### Policy Management

#### GET /policies
Get tenant policies.

**Request:**
```http
GET /policies
Authorization: Bearer {token}
X-Tenant-ID: contoso.onmicrosoft.com
```

**Response (200):**
```json
{
  "success": true,
  "data": {
    "expirationPolicy": {
      "enabled": true,
      "defaultExpirationDays": 90,
      "sendReminderDays": 7,
      "autoRevoke": false
    },
    "approvalPolicy": {
      "enabled": true,
      "requireApprovalForInvites": true,
      "approvers": ["admin@contoso.com", "manager@contoso.com"],
      "autoApproveInternalRequests": false
    },
    "restrictionPolicy": {
      "enabled": true,
      "allowedDomains": ["acme.com", "partner.com"],
      "blockedDomains": ["competitor.com"],
      "requireCompanyField": true,
      "requireProjectField": true
    },
    "notificationPolicy": {
      "enabled": true,
      "notifyOnInvite": true,
      "notifyOnRemoval": true,
      "notifyOnExpiration": true,
      "notificationEmail": "notifications@contoso.com"
    },
    "lastModifiedDate": "2024-01-15T10:00:00Z",
    "lastModifiedBy": "admin@contoso.com"
  }
}
```

#### PUT /policies
Update tenant policies.

**Request:**
```http
PUT /policies
Authorization: Bearer {token}
X-Tenant-ID: contoso.onmicrosoft.com
Content-Type: application/json

{
  "expirationPolicy": {
    "enabled": true,
    "defaultExpirationDays": 120,
    "sendReminderDays": 14,
    "autoRevoke": true
  },
  "approvalPolicy": {
    "enabled": true,
    "requireApprovalForInvites": true,
    "approvers": ["admin@contoso.com"]
  }
}
```

**Response (200):**
```json
{
  "success": true,
  "data": {
    "id": 1,
    "policyType": "GuestExpiration",
    "enabled": true,
    "configuration": {
      "expirationDays": 60,
      "notifyBeforeDays": 14
    },
    "modifiedDate": "2024-01-15T14:00:00Z"
  }
}
```

**Errors:**
- `400 Bad Request`: Invalid policy configuration
- `402 Payment Required`: Feature not available in current tier
- `403 Forbidden`: Insufficient permissions

---

### 4. Audit Logs

#### GET /audit

Get audit logs with filtering.

**Request:**
```http
GET /audit?action=UserInvited&startDate=2024-01-01&endDate=2024-01-31&page=1 HTTP/1.1
Authorization: Bearer {token}
```

**Query Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `action` | string | Filter by action type |
| `userId` | string | Filter by user ID |
| `startDate` | string | Start date (ISO 8601) |
| `endDate` | string | End date (ISO 8601) |
| `page` | number | Page number |
| `pageSize` | number | Items per page (max: 100) |

**Response (200 OK):**
    "message": "Policies updated successfully",
    "lastModifiedDate": "2024-01-20T16:30:00Z",
    "lastModifiedBy": "admin@contoso.com"
  }
}
```

### Audit Management

#### GET /audit
Get audit logs for the tenant.

**Request:**
```http
GET /audit?page=1&pageSize=50&eventType=user.invite&startDate=2024-01-01&endDate=2024-01-31
Authorization: Bearer {token}
X-Tenant-ID: contoso.onmicrosoft.com
```

**Query Parameters:**
- `page` (optional, default: 1): Page number
- `pageSize` (optional, default: 50, max: 100): Items per page
- `eventType` (optional): Filter by event type
- `actor` (optional): Filter by user email
- `startDate` (optional): Start date (ISO 8601)
- `endDate` (optional): End date (ISO 8601)
- `resourceType` (optional): Filter by resource type
- `status` (optional): Filter by status (success, failure)

**Response (200):**
```json
{
  "success": true,
  "data": [
    {
      "id": 1001,
      "timestamp": "2024-01-15T14:30:00Z",
      "userId": "user-obj-id-123",
      "userEmail": "admin@contoso.com",
      "action": "UserInvited",
      "resourceType": "ExternalUser",
      "resourceId": "partner@external.com",
      "details": {
        "library": "Marketing Docs",
        "permissions": "Read"
      },
      "ipAddress": "203.0.113.42",
      "status": "Success"
      "auditId": 12345,
      "timestamp": "2024-01-20T14:35:22Z",
      "correlationId": "550e8400-e29b-41d4-a716-446655440000",
      "eventType": "user.invite",
      "actor": {
        "email": "admin@contoso.com",
        "displayName": "Admin User",
        "ipAddress": "203.0.113.45"
      },
      "action": "POST /external-users/invite",
      "status": "success",
      "target": {
        "resourceType": "externalUser",
        "resourceId": "user-guid-123",
        "email": "partner@acme.com"
      },
      "metadata": {
        "libraryId": "lib-guid-456",
        "permissions": "read",
        "company": "Acme Corp",
        "project": "ProjectX"
      },
      "changes": {
        "before": null,
        "after": {
          "status": "invited",
          "expirationDate": "2024-04-20T23:59:59Z"
        }
      }
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 50,
    "total": 523,
    "totalPages": 11
  }
}
```

**Errors:**
- `402 Payment Required`: Audit history retention exceeded for tier
- `403 Forbidden`: Read-only role required

---

## Error Codes

| Code | HTTP Status | Description |
|------|-------------|-------------|
| `UNAUTHORIZED` | 401 | Invalid or missing authentication token |
| `FORBIDDEN` | 403 | Insufficient permissions |
| `TENANT_NOT_FOUND` | 404 | Tenant not onboarded |
| `USER_NOT_FOUND` | 404 | External user not found |
| `LIBRARY_NOT_FOUND` | 404 | SharePoint library not found |
| `VALIDATION_ERROR` | 400 | Request validation failed |
| `CONFLICT` | 409 | Resource already exists |
| `SUBSCRIPTION_EXPIRED` | 402 | Subscription expired or quota exceeded |
| `FEATURE_NOT_AVAILABLE` | 402 | Feature not available in current tier |
| `RATE_LIMIT_EXCEEDED` | 429 | Too many requests |
| `INTERNAL_ERROR` | 500 | Internal server error |

## Rate Limiting

Rates vary by subscription tier:

| Tier | Requests/Minute | Burst |
|------|-----------------|-------|
| Free | 10 | 20 |
| Pro | 100 | 150 |
| Enterprise | 500 | 1000 |
    "total": 1523,
    "totalPages": 31,
    "hasNext": true,
    "hasPrevious": false
  },
  "exportUrl": null
}
```

#### POST /audit/export
Export audit logs (Premium feature).

**Request:**
```http
POST /audit/export
Authorization: Bearer {token}
X-Tenant-ID: contoso.onmicrosoft.com
Content-Type: application/json

{
  "format": "csv",
  "startDate": "2024-01-01",
  "endDate": "2024-01-31",
  "eventTypes": ["user.invite", "user.remove"]
}
```

**Response (202 Accepted):**
```json
{
  "success": true,
  "data": {
    "exportId": "export-guid-123",
    "status": "processing",
    "estimatedCompletionTime": "2024-01-20T16:45:00Z",
    "statusUrl": "/audit/export/export-guid-123"
  }
}
```

## Rate Limiting

**Limits per Tenant:**
- **Standard Operations**: 100 requests/minute
- **Bulk Operations**: 10 requests/minute
- **User Invitations**: 50 invitations/hour
- **Export Operations**: 5 exports/hour

**Rate Limit Headers:**
```http
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1705320000
X-RateLimit-Reset: 1705851600
X-RateLimit-Retry-After: 45
```

## Pagination

All list endpoints support pagination:

```json
{
  "pagination": {
    "page": 1,
    "pageSize": 50,
    "total": 125,
    "totalPages": 3,
    "hasNext": true,
    "hasPrev": false
  }
}
```

## Versioning

API uses URL-based versioning:
- Current version: `/v1/`
- Future versions: `/v2/`, `/v3/`, etc.
- Breaking changes increment major version
- Backward-compatible changes don't require version change

## OpenAPI Specification

Full OpenAPI 3.0 spec available at:
```
GET https://api.spexternal.com/openapi.json
```

View interactive API docs at:
```
https://api.spexternal.com/docs
```

(Generated from this spec + code annotations)
- Default page size: 50
- Maximum page size: 100
- Response includes pagination metadata

## Versioning

- Current version: v1
- Version in URL path: `/v1/`
- Breaking changes will increment major version
- Deprecation notices: 6 months advance notice

## Webhooks (Future)

Webhook events (planned for future release):
- `user.invited`
- `user.removed`
- `user.expired`
- `policy.updated`
- `subscription.changed`

## SDKs (Future)

Planned client libraries:
- JavaScript/TypeScript (npm)
- .NET (NuGet)
- Python (pip)
- PowerShell (PowerShell Gallery)

---

**API Version**: 1.0  
**Last Updated**: 2024-02-03  
**OpenAPI Spec**: Available at `/openapi.json`
