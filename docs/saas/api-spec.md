# API Specification

## Overview

RESTful API specification for the SharePoint External User Manager SaaS backend. This API enables tenant onboarding, external user management, collaboration policies, subscription management, and audit logging.

**Base URL**: `https://api.spexternal.com/v1`

**API Version**: 1.0.0

## Authentication

All API endpoints require authentication using JWT Bearer tokens issued by Microsoft Entra ID (Azure AD).

### Request Headers

```http
Authorization: Bearer {jwt_token}
X-Tenant-ID: {tenant_id}
Content-Type: application/json
```

### Token Acquisition

```typescript
// Client-side token acquisition (MSAL.js)
const tokenRequest = {
  scopes: ['api://spexternal/User.Read', 'api://spexternal/User.Manage']
};

const tokenResponse = await msalInstance.acquireTokenSilent(tokenRequest);
const accessToken = tokenResponse.accessToken;
```

## API Endpoints

### 1. Tenant Onboarding

#### POST /api/v1/tenants/onboard

Onboard a new tenant to the SaaS platform.

**Request**:
```http
POST /api/v1/tenants/onboard
Authorization: Bearer {token}
Content-Type: application/json

{
  "tenant_name": "Contoso Corporation",
  "azure_tenant_id": "12345678-1234-1234-1234-123456789012",
  "domain": "contoso.com",
  "primary_admin_email": "admin@contoso.com",
  "subscription_tier": "Trial",
  "admin_consent_granted": true
}
```

**Response** (201 Created):
```json
{
  "success": true,
  "data": {
    "tenant_id": "tenant-abc123",
    "tenant_name": "Contoso Corporation",
    "azure_tenant_id": "12345678-1234-1234-1234-123456789012",
    "domain": "contoso.com",
    "status": "Trial",
    "onboarding_date": "2024-01-15T10:00:00Z",
    "trial_end_date": "2024-02-14T10:00:00Z",
    "subscription": {
      "tier": "Trial",
      "status": "Active",
      "limits": {
        "max_external_users": 10,
        "audit_log_retention_days": 30,
        "api_rate_limit": 100
      }
    }
  }
}
```

**Error Responses**:
- `400 Bad Request`: Invalid input or missing required fields
- `409 Conflict`: Tenant already exists
- `403 Forbidden`: Admin consent not granted

---

#### GET /api/v1/tenants/{tenantId}

Get tenant information.

**Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "tenant_id": "tenant-abc123",
    "tenant_name": "Contoso Corporation",
    "domain": "contoso.com",
    "status": "Active",
    "subscription_tier": "Pro",
    "settings": {
      "external_sharing_enabled": true,
      "allow_anonymous_links": false,
      "default_link_permission": "View",
      "external_user_expiration_days": 90
    },
    "usage": {
      "external_users_count": 45,
      "active_policies_count": 3,
      "api_calls_last_30_days": 12500
    }
  }
}
```

---

#### PUT /api/v1/tenants/{tenantId}/settings

Update tenant settings.

**Request**:
```json
{
  "external_sharing_enabled": true,
  "allow_anonymous_links": false,
  "default_link_permission": "Contribute",
  "external_user_expiration_days": 60,
  "require_approval_for_external_sharing": true
}
```

**Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "tenant_id": "tenant-abc123",
    "settings": {
      "external_sharing_enabled": true,
      "allow_anonymous_links": false,
      "default_link_permission": "Contribute",
      "external_user_expiration_days": 60,
      "require_approval_for_external_sharing": true
    },
    "updated_at": "2024-01-15T11:30:00Z"
  }
}
```

---

### 2. External User Management

#### GET /api/v1/users

List all external users for the tenant.

**Query Parameters**:
- `page` (integer, default: 1): Page number
- `page_size` (integer, default: 50, max: 100): Items per page
- `status` (string): Filter by status (`Active`, `Invited`, `Suspended`, `Revoked`)
- `search` (string): Search by email or display name
- `sort_by` (string): Sort field (`email`, `invited_date`, `last_access`)
- `sort_order` (string): Sort order (`asc`, `desc`)

**Request**:
```http
GET /api/v1/users?page=1&page_size=50&status=Active&sort_by=email
Authorization: Bearer {token}
X-Tenant-ID: tenant-abc123
```

**Response** (200 OK):
```json
{
  "success": true,
  "data": [
    {
      "user_id": "user-001",
      "email": "partner@external.com",
      "display_name": "Jane Partner",
      "user_type": "External",
      "status": "Active",
      "invited_by": "admin@contoso.com",
      "invited_date": "2024-01-10T09:15:00Z",
      "last_access_date": "2024-01-14T16:45:00Z",
      "access_expiration_date": "2024-04-10T09:15:00Z",
      "company_name": "Partner Corp",
      "permissions_count": 3
    }
  ],
  "pagination": {
    "page": 1,
    "page_size": 50,
    "total": 45,
    "total_pages": 1,
    "has_next": false,
    "has_prev": false
  }
}
```

---

#### POST /api/v1/users/invite

Invite an external user.

**Request**:
```json
{
  "email": "newpartner@external.com",
  "display_name": "John Doe",
  "company_name": "Partner Corp",
  "message": "Welcome to our collaboration workspace",
  "permissions": [
    {
      "resource_type": "Library",
      "resource_id": "lib-001",
      "resource_url": "https://contoso.sharepoint.com/sites/partners/documents",
      "permission_level": "Contribute"
    }
  ],
  "access_expiration_days": 90
}
```

**Response** (201 Created):
```json
{
  "success": true,
  "data": {
    "user_id": "user-002",
    "email": "newpartner@external.com",
    "display_name": "John Doe",
    "status": "Invited",
    "invited_by": "admin@contoso.com",
    "invited_date": "2024-01-15T11:30:00Z",
    "access_expiration_date": "2024-04-14T11:30:00Z",
    "invitation_id": "inv-xyz789",
    "invitation_link": "https://login.microsoftonline.com/..."
  }
}
```

**Licensing Check**:
```json
// If quota exceeded
{
  "success": false,
  "error": {
    "code": "QUOTA_EXCEEDED",
    "message": "External user limit reached for your subscription tier",
    "details": {
      "current_users": 10,
      "max_users": 10,
      "subscription_tier": "Free"
    },
    "upgrade_url": "/subscription/upgrade"
  }
}
```

---

#### GET /api/v1/users/{userId}

Get external user details.

**Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "user_id": "user-001",
    "email": "partner@external.com",
    "display_name": "Jane Partner",
    "user_type": "External",
    "status": "Active",
    "invited_by": "admin@contoso.com",
    "invited_date": "2024-01-10T09:15:00Z",
    "last_access_date": "2024-01-14T16:45:00Z",
    "access_expiration_date": "2024-04-10T09:15:00Z",
    "company_name": "Partner Corp",
    "job_title": "Project Manager",
    "permissions": [
      {
        "permission_id": "perm-001",
        "resource_type": "Library",
        "resource_id": "lib-001",
        "resource_url": "https://contoso.sharepoint.com/sites/partners/documents",
        "permission_level": "Contribute",
        "granted_date": "2024-01-10T09:15:00Z"
      }
    ]
  }
}
```

---

#### PUT /api/v1/users/{userId}

Update external user.

**Request**:
```json
{
  "display_name": "Jane Partner-Smith",
  "company_name": "Partner Corp International",
  "job_title": "Senior Project Manager",
  "access_expiration_date": "2024-06-10T00:00:00Z"
}
```

**Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "user_id": "user-001",
    "email": "partner@external.com",
    "display_name": "Jane Partner-Smith",
    "company_name": "Partner Corp International",
    "job_title": "Senior Project Manager",
    "access_expiration_date": "2024-06-10T00:00:00Z",
    "updated_at": "2024-01-15T12:00:00Z"
  }
}
```

---

#### DELETE /api/v1/users/{userId}

Revoke external user access.

**Request**:
```json
{
  "reason": "Project completed",
  "revoke_all_permissions": true
}
```

**Response** (200 OK):
```json
{
  "success": true,
  "message": "User access revoked successfully",
  "data": {
    "user_id": "user-001",
    "status": "Revoked",
    "revoked_date": "2024-01-15T12:15:00Z",
    "permissions_revoked": 3
  }
}
```

---

### 3. Permissions Management

#### GET /api/v1/users/{userId}/permissions

Get user permissions.

**Response** (200 OK):
```json
{
  "success": true,
  "data": [
    {
      "permission_id": "perm-001",
      "resource_type": "Library",
      "resource_id": "lib-001",
      "resource_url": "https://contoso.sharepoint.com/sites/partners/documents",
      "resource_name": "Partner Documents",
      "permission_level": "Contribute",
      "granted_by": "admin@contoso.com",
      "granted_date": "2024-01-10T09:15:00Z",
      "expiration_date": "2024-04-10T09:15:00Z",
      "is_active": true
    }
  ]
}
```

---

#### POST /api/v1/users/{userId}/permissions

Grant permission to user.

**Request**:
```json
{
  "resource_type": "Site",
  "resource_id": "site-002",
  "resource_url": "https://contoso.sharepoint.com/sites/projects",
  "permission_level": "Read",
  "expiration_days": 60
}
```

**Response** (201 Created):
```json
{
  "success": true,
  "data": {
    "permission_id": "perm-002",
    "user_id": "user-001",
    "resource_type": "Site",
    "resource_id": "site-002",
    "resource_url": "https://contoso.sharepoint.com/sites/projects",
    "permission_level": "Read",
    "granted_by": "admin@contoso.com",
    "granted_date": "2024-01-15T12:30:00Z",
    "expiration_date": "2024-03-15T12:30:00Z"
  }
}
```

---

#### DELETE /api/v1/users/{userId}/permissions/{permissionId}

Revoke specific permission.

**Response** (200 OK):
```json
{
  "success": true,
  "message": "Permission revoked successfully",
  "data": {
    "permission_id": "perm-001",
    "revoked_date": "2024-01-15T13:00:00Z"
  }
}
```

---

### 4. Collaboration Policies

#### GET /api/v1/policies

List all policies.

**Response** (200 OK):
```json
{
  "success": true,
  "data": [
    {
      "policy_id": "policy-001",
      "policy_name": "External Access Expiration",
      "policy_type": "AccessExpiration",
      "is_enabled": true,
      "configuration": {
        "default_expiration_days": 90,
        "send_expiration_reminder": true,
        "reminder_days_before": [7, 1]
      },
      "applies_to": "All",
      "created_by": "admin@contoso.com",
      "created_at": "2024-01-01T00:00:00Z",
      "updated_at": "2024-01-15T10:00:00Z"
    },
    {
      "policy_id": "policy-002",
      "policy_name": "Restrict Anonymous Links",
      "policy_type": "ExternalSharing",
      "is_enabled": true,
      "configuration": {
        "allow_anonymous_links": false,
        "require_authentication": true,
        "allow_external_email_domains": ["partner.com", "vendor.com"]
      },
      "applies_to": "All",
      "created_by": "admin@contoso.com",
      "created_at": "2024-01-05T00:00:00Z"
    }
  ]
}
```

---

#### POST /api/v1/policies

Create new policy (Pro/Enterprise tier only).

**Request**:
```json
{
  "policy_name": "Auto-Revoke Inactive Users",
  "policy_type": "InactivityRevocation",
  "is_enabled": true,
  "configuration": {
    "inactive_days_threshold": 30,
    "send_warning_email": true,
    "warning_days_before": 7
  },
  "applies_to": "All"
}
```

**Response** (201 Created):
```json
{
  "success": true,
  "data": {
    "policy_id": "policy-003",
    "policy_name": "Auto-Revoke Inactive Users",
    "policy_type": "InactivityRevocation",
    "is_enabled": true,
    "configuration": {
      "inactive_days_threshold": 30,
      "send_warning_email": true,
      "warning_days_before": 7
    },
    "applies_to": "All",
    "created_by": "admin@contoso.com",
    "created_at": "2024-01-15T14:00:00Z"
  }
}
```

**Licensing Check**:
```json
// If feature not available in tier
{
  "success": false,
  "error": {
    "code": "FEATURE_NOT_AVAILABLE",
    "message": "Advanced policies are only available in Pro and Enterprise tiers",
    "details": {
      "required_tier": "Pro",
      "current_tier": "Free",
      "feature": "Custom Policies"
    },
    "upgrade_url": "/subscription/upgrade"
  }
}
```

---

#### PUT /api/v1/policies/{policyId}

Update policy.

**Request**:
```json
{
  "is_enabled": true,
  "configuration": {
    "default_expiration_days": 60,
    "send_expiration_reminder": true,
    "reminder_days_before": [14, 7, 1]
  }
}
```

**Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "policy_id": "policy-001",
    "policy_name": "External Access Expiration",
    "is_enabled": true,
    "configuration": {
      "default_expiration_days": 60,
      "send_expiration_reminder": true,
      "reminder_days_before": [14, 7, 1]
    },
    "updated_at": "2024-01-15T14:30:00Z"
  }
}
```

---

### 5. Subscription Management

#### GET /api/v1/subscription

Get current subscription details.

**Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "subscription_id": "sub-abc123",
    "tenant_id": "tenant-abc123",
    "tier": "Pro",
    "status": "Active",
    "billing_cycle": "Monthly",
    "price_per_month": 49.00,
    "currency": "USD",
    "start_date": "2024-01-01T00:00:00Z",
    "renewal_date": "2024-02-01T00:00:00Z",
    "auto_renew": true,
    "limits": {
      "max_external_users": 100,
      "audit_log_retention_days": 365,
      "api_rate_limit": 200,
      "advanced_policies": true,
      "support_level": "priority"
    },
    "usage": {
      "external_users_count": 45,
      "external_users_percentage": 45,
      "api_calls_this_month": 12500,
      "storage_used_gb": 5.2
    }
  }
}
```

---

#### POST /api/v1/subscription/upgrade

Upgrade subscription tier.

**Request**:
```json
{
  "new_tier": "Enterprise",
  "billing_cycle": "Annual",
  "payment_method": "credit_card"
}
```

**Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "subscription_id": "sub-abc123",
    "tier": "Enterprise",
    "status": "Active",
    "billing_cycle": "Annual",
    "price_per_year": 1990.00,
    "currency": "USD",
    "upgrade_date": "2024-01-15T15:00:00Z",
    "next_billing_date": "2025-01-15T00:00:00Z",
    "prorated_credit": 35.00
  }
}
```

---

#### POST /api/v1/subscription/cancel

Cancel subscription.

**Request**:
```json
{
  "reason": "No longer needed",
  "feedback": "Great product, but our needs have changed",
  "cancel_immediately": false
}
```

**Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "subscription_id": "sub-abc123",
    "status": "Cancelled",
    "cancellation_date": "2024-01-15T15:30:00Z",
    "service_end_date": "2024-02-01T00:00:00Z",
    "message": "Your subscription will remain active until 2024-02-01"
  }
}
```

---

### 6. Audit Logs

#### GET /api/v1/audit-logs

Query audit logs.

**Query Parameters**:
- `start_date` (ISO 8601): Start of date range
- `end_date` (ISO 8601): End of date range
- `event_type` (string): Filter by event type
- `event_category` (string): Filter by category
- `user_id` (string): Filter by actor user ID
- `severity` (string): Filter by severity
- `page` (integer): Page number
- `page_size` (integer): Items per page

**Request**:
```http
GET /api/v1/audit-logs?start_date=2024-01-01T00:00:00Z&end_date=2024-01-15T23:59:59Z&event_category=UserManagement
Authorization: Bearer {token}
X-Tenant-ID: tenant-abc123
```

**Response** (200 OK):
```json
{
  "success": true,
  "data": [
    {
      "id": "audit-001",
      "timestamp": "2024-01-15T10:30:00Z",
      "event_type": "UserInvited",
      "event_category": "UserManagement",
      "severity": "Info",
      "actor": {
        "user_id": "user-admin-001",
        "email": "admin@contoso.com",
        "ip_address": "203.0.113.1"
      },
      "target": {
        "resource_type": "User",
        "resource_id": "user-002",
        "resource_name": "newpartner@external.com"
      },
      "action": {
        "name": "InviteUser",
        "result": "Success",
        "details": {
          "permission_level": "Contribute",
          "site_url": "https://contoso.sharepoint.com/sites/partners"
        }
      }
    }
  ],
  "pagination": {
    "page": 1,
    "page_size": 50,
    "total": 1250,
    "total_pages": 25
  }
}
```

---

#### GET /api/v1/audit-logs/export

Export audit logs (Enterprise tier only).

**Query Parameters**: Same as GET /audit-logs
**Additional**: `format` (string): `json`, `csv`, or `xlsx`

**Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "export_id": "export-xyz789",
    "format": "csv",
    "status": "Processing",
    "created_at": "2024-01-15T16:00:00Z",
    "estimated_completion": "2024-01-15T16:05:00Z",
    "download_url": null
  }
}
```

**Check export status**:
```http
GET /api/v1/audit-logs/export/{exportId}
```

---

### 7. Usage Analytics

#### GET /api/v1/analytics/usage

Get usage analytics (Pro/Enterprise tier only).

**Query Parameters**:
- `metric_type` (string): Metric to retrieve
- `start_date` (ISO 8601): Start date
- `end_date` (ISO 8601): End date
- `granularity` (string): `day`, `week`, `month`

**Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "metric_type": "ExternalUsersCount",
    "period": {
      "start_date": "2024-01-01T00:00:00Z",
      "end_date": "2024-01-15T23:59:59Z"
    },
    "granularity": "day",
    "data_points": [
      {
        "date": "2024-01-01",
        "value": 35
      },
      {
        "date": "2024-01-02",
        "value": 37
      },
      {
        "date": "2024-01-15",
        "value": 45
      }
    ],
    "summary": {
      "current_value": 45,
      "change_from_start": 10,
      "percent_change": 28.57
    }
  }
}
```

---

## Error Handling

### Standard Error Response

```json
{
  "success": false,
  "error": {
    "code": "ERROR_CODE",
    "message": "Human-readable error message",
    "details": "Additional error details or context",
    "request_id": "req-xyz789",
    "timestamp": "2024-01-15T10:00:00Z"
  }
}
```

### Error Codes

| Code | HTTP Status | Description |
|------|-------------|-------------|
| `UNAUTHORIZED` | 401 | Invalid or missing authentication token |
| `FORBIDDEN` | 403 | Insufficient permissions |
| `TENANT_NOT_FOUND` | 404 | Tenant does not exist |
| `USER_NOT_FOUND` | 404 | User does not exist |
| `RESOURCE_NOT_FOUND` | 404 | Requested resource not found |
| `VALIDATION_ERROR` | 400 | Request validation failed |
| `QUOTA_EXCEEDED` | 402 | Subscription limit reached |
| `FEATURE_NOT_AVAILABLE` | 402 | Feature not available in current tier |
| `RATE_LIMIT_EXCEEDED` | 429 | Too many requests |
| `TENANT_NOT_ACTIVE` | 403 | Tenant is suspended or inactive |
| `SUBSCRIPTION_EXPIRED` | 402 | Subscription has expired |
| `INTERNAL_ERROR` | 500 | Internal server error |
| `SERVICE_UNAVAILABLE` | 503 | Service temporarily unavailable |

---

## Rate Limiting

Rate limits vary by subscription tier:

| Tier | Requests/Minute | Burst |
|------|-----------------|-------|
| Free | 50 | 75 |
| Pro | 200 | 300 |
| Enterprise | 1000 | 1500 |

**Response Headers**:
```http
X-RateLimit-Limit: 200
X-RateLimit-Remaining: 195
X-RateLimit-Reset: 1705320060
```

**Rate Limit Exceeded Response**:
```json
{
  "success": false,
  "error": {
    "code": "RATE_LIMIT_EXCEEDED",
    "message": "Rate limit exceeded",
    "details": "You have exceeded your API rate limit. Please try again later.",
    "retry_after": 60
  }
}
```

---

## Pagination

All list endpoints support cursor-based pagination:

**Request**:
```http
GET /api/v1/users?page=2&page_size=50
```

**Response**:
```json
{
  "success": true,
  "data": [...],
  "pagination": {
    "page": 2,
    "page_size": 50,
    "total": 150,
    "total_pages": 3,
    "has_next": true,
    "has_prev": true,
    "next_page": "/api/v1/users?page=3&page_size=50",
    "prev_page": "/api/v1/users?page=1&page_size=50"
  }
}
```

---

## Webhooks

### Register Webhook

```http
POST /api/v1/webhooks
```

**Request**:
```json
{
  "url": "https://your-app.com/webhooks/spexternal",
  "events": ["user.invited", "user.revoked", "subscription.updated"],
  "secret": "webhook-signing-secret"
}
```

### Webhook Events

- `user.invited`: External user was invited
- `user.revoked`: External user access was revoked
- `permission.granted`: Permission was granted
- `permission.revoked`: Permission was revoked
- `policy.created`: Policy was created
- `policy.updated`: Policy was updated
- `subscription.updated`: Subscription tier changed
- `subscription.expired`: Subscription expired

---

## OpenAPI/Swagger

Full OpenAPI 3.0 specification available at:
```
https://api.spexternal.com/swagger.json
```

Interactive API documentation:
```
https://api.spexternal.com/docs
```

---

## SDK & Client Libraries

### JavaScript/TypeScript

```typescript
import { SPExternalClient } from '@spexternal/sdk';

const client = new SPExternalClient({
  baseURL: 'https://api.spexternal.com/v1',
  tenantId: 'tenant-abc123',
  getAccessToken: async () => {
    // Your token acquisition logic
    return accessToken;
  }
});

// Invite user
const user = await client.users.invite({
  email: 'partner@external.com',
  displayName: 'Partner User',
  permissions: [...]
});
```

### .NET

```csharp
using SPExternal.SDK;

var client = new SPExternalClient(new SPExternalClientOptions
{
    BaseUrl = "https://api.spexternal.com/v1",
    TenantId = "tenant-abc123",
    GetAccessToken = async () => {
        // Your token acquisition logic
        return accessToken;
    }
});

// Invite user
var user = await client.Users.InviteAsync(new InviteUserRequest
{
    Email = "partner@external.com",
    DisplayName = "Partner User",
    Permissions = new[] { ... }
});
```

---

## API Versioning

- Current version: `v1`
- Version is part of the URL: `/api/v1/...`
- Backward compatibility maintained within major versions
- Breaking changes will result in new major version (v2)
- Deprecation notices provided 6 months in advance

---

## Best Practices

1. **Always validate JWT tokens** before making API calls
2. **Cache tenant information** to reduce API calls
3. **Implement retry logic** with exponential backoff
4. **Handle rate limits gracefully**
5. **Use pagination** for large result sets
6. **Subscribe to webhooks** instead of polling
7. **Log request IDs** for troubleshooting
8. **Implement proper error handling**

---

## Next Steps

1. Generate OpenAPI specification file
2. Create SDK client libraries
3. Set up API documentation portal
4. Implement API versioning strategy
5. Create API usage examples and tutorials
