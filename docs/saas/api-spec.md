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

Get current tenant information.

**Request:**
```http
GET /tenants/me HTTP/1.1
Authorization: Bearer {token}
```

**Response (200 OK):**
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

**Rate Limit Headers:**
```http
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1705320000
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
