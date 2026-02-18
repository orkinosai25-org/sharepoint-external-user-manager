# SharePoint External User Manager - API Documentation

## Overview

This document provides comprehensive documentation for the SharePoint External User Manager SaaS API. The API is built with ASP.NET Core 8, uses JWT authentication via Microsoft Entra ID (Azure AD), and follows REST principles.

**Base URL**: `https://api.example.com` (replace with your deployment URL)  
**API Version**: v1  
**Authentication**: JWT Bearer Token (Azure AD)

## Table of Contents

- [Authentication](#authentication)
- [API Conventions](#api-conventions)
- [Endpoints](#endpoints)
  - [Health & Status](#health--status)
  - [Consent Flow](#consent-flow)
  - [Tenant Management](#tenant-management)
  - [Client Space Management](#client-space-management)
  - [External User Management](#external-user-management)
  - [Library Management](#library-management)
  - [List Management](#list-management)
- [Error Handling](#error-handling)
- [Rate Limiting](#rate-limiting)
- [Feature Gating](#feature-gating)

## Authentication

All authenticated endpoints require a JWT Bearer token obtained from Microsoft Entra ID (Azure AD).

### Getting an Access Token

1. Register your application in Azure AD
2. Configure the appropriate API permissions
3. Obtain a token using OAuth 2.0 authorization code flow or client credentials flow

### Using the Token

Include the token in the `Authorization` header of all requests:

```http
Authorization: Bearer <your-jwt-token>
```

### Required Token Claims

- `tid`: Tenant ID (Azure AD Tenant GUID)
- `oid`: Object ID (User GUID)
- `upn` or `email`: User's email address

## API Conventions

### Request Format

- **Content-Type**: `application/json`
- **Accept**: `application/json`

### Response Format

All API responses follow a consistent structure:

**Success Response**:
```json
{
  "success": true,
  "data": { ... },
  "pagination": { // Optional for paginated responses
    "page": 1,
    "pageSize": 30,
    "total": 100,
    "hasNext": true
  }
}
```

**Error Response**:
```json
{
  "success": false,
  "error": {
    "code": "ERROR_CODE",
    "message": "Human-readable error message",
    "details": "Optional detailed information"
  }
}
```

## Endpoints

### Health & Status

#### GET /health

Health check endpoint for monitoring and load balancers.

**Authentication**: None required

**Response**:
```json
{
  "status": "Healthy",
  "version": "1.0.0",
  "timestamp": "2026-02-18T20:00:00Z"
}
```

### Consent Flow

#### GET /consent/url

Generate the Azure AD admin consent URL that organization admins should visit to grant permissions.

**Authentication**: None required

**Query Parameters**:
- `redirectUri` (optional): Custom redirect URI after consent

**Response**:
```json
{
  "success": true,
  "data": {
    "consentUrl": "https://login.microsoftonline.com/common/v2.0/adminconsent?...",
    "instructions": [
      "Have a Global Administrator or Application Administrator visit the consent URL",
      "Review the requested permissions",
      "Grant consent for the organization",
      "You will be redirected back to complete the onboarding"
    ],
    "requiredPermissions": [
      "Sites.ReadWrite.All - Create and manage SharePoint sites",
      "User.ReadWrite.All - Invite external users",
      "Directory.Read.All - Read directory data"
    ],
    "redirectUri": "https://api.example.com/consent/callback"
  }
}
```

#### GET /consent/callback

Callback endpoint that receives the response from Azure AD after admin consent.

**Authentication**: None required

**Query Parameters**:
- `admin_consent`: Whether consent was granted ("True" or "False")
- `tenant`: Tenant ID that granted consent
- `error`: Error code if consent was denied
- `error_description`: Human-readable error description

**Success Response**:
```json
{
  "success": true,
  "data": {
    "success": true,
    "tenantId": "tenant-guid",
    "message": "Admin consent granted successfully",
    "nextSteps": [
      "Complete tenant registration at /tenants/register",
      "Configure your organization settings",
      "Start creating client spaces"
    ]
  }
}
```

#### GET /consent/status

Check the consent status for the authenticated tenant.

**Authentication**: Required

**Response**:
```json
{
  "success": true,
  "data": {
    "tenantId": "tenant-guid",
    "isRegistered": true,
    "consentGranted": true,
    "status": "Active",
    "requiresAction": false
  }
}
```

### Tenant Management

#### GET /tenants/me

Get information about the authenticated tenant.

**Authentication**: Required

**Response**:
```json
{
  "success": true,
  "data": {
    "tenantId": "tenant-guid",
    "userId": "user-guid",
    "userPrincipalName": "user@example.com",
    "isActive": true,
    "subscriptionTier": "Pro",
    "organizationName": "Acme Corporation"
  }
}
```

#### POST /tenants/register

Register/onboard a new tenant organization.

**Authentication**: Required

**Request Body**:
```json
{
  "organizationName": "Acme Corporation",
  "primaryAdminEmail": "admin@acme.com",
  "settings": {
    "sharePointTenantUrl": "acme.sharepoint.com",
    "enableExternalSharingDefault": true,
    "defaultExternalPermission": "Read"
  }
}
```

**Response** (201 Created):
```json
{
  "success": true,
  "data": {
    "tenantId": 123,
    "entraIdTenantId": "tenant-guid",
    "organizationName": "Acme Corporation",
    "subscriptionTier": "Free",
    "trialExpiryDate": "2026-03-20T00:00:00Z",
    "registeredDate": "2026-02-18T20:00:00Z",
    "nextSteps": [
      "Configure SharePoint tenant URL in settings",
      "Install SPFx web parts from App Catalog",
      "Create your first client space"
    ]
  }
}
```

### Client Space Management

#### GET /clients

List all client spaces for the authenticated tenant.

**Authentication**: Required

**Response**:
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "clientReference": "CLIENT-001",
      "clientName": "Acme Corporation",
      "description": "Client space for Acme Corp legal matters",
      "sharePointSiteId": "site-guid",
      "sharePointSiteUrl": "https://tenant.sharepoint.com/sites/client-001-acme",
      "provisioningStatus": "Provisioned",
      "provisionedDate": "2026-02-18T15:30:00Z",
      "provisioningError": null,
      "isActive": true,
      "createdDate": "2026-02-18T15:00:00Z",
      "createdBy": "admin@example.com"
    }
  ]
}
```

#### GET /clients/{id}

Get details of a specific client space.

**Authentication**: Required

**Path Parameters**:
- `id`: Client ID (integer)

**Response**: Same structure as individual client in the list above.

#### POST /clients

Create a new client space with automatic SharePoint site provisioning.

**Authentication**: Required

**Request Body**:
```json
{
  "clientReference": "CLIENT-002",
  "clientName": "Beta Industries",
  "description": "Collaboration space for Beta Industries project"
}
```

**Response** (201 Created):
```json
{
  "success": true,
  "data": {
    "id": 2,
    "clientReference": "CLIENT-002",
    "clientName": "Beta Industries",
    "description": "Collaboration space for Beta Industries project",
    "sharePointSiteId": "new-site-guid",
    "sharePointSiteUrl": "https://tenant.sharepoint.com/sites/client-002-beta",
    "provisioningStatus": "Provisioned",
    "provisionedDate": "2026-02-18T16:00:00Z",
    "provisioningError": null,
    "isActive": true,
    "createdDate": "2026-02-18T16:00:00Z",
    "createdBy": "admin@example.com"
  }
}
```

### External User Management

#### GET /clients/{id}/external-users

List all external users (guests) for a client site.

**Authentication**: Required

**Path Parameters**:
- `id`: Client ID (integer)

**Response**:
```json
{
  "success": true,
  "data": [
    {
      "id": "permission-guid",
      "email": "partner@external.com",
      "displayName": "John Partner",
      "permissionLevel": "Read",
      "invitedDate": "2026-02-15T10:00:00Z",
      "invitedBy": "admin@example.com",
      "lastAccessDate": null,
      "status": "Active"
    }
  ]
}
```

#### POST /clients/{id}/external-users

Invite an external user to a client site.

**Authentication**: Required

**Path Parameters**:
- `id`: Client ID (integer)

**Request Body**:
```json
{
  "email": "partner@external.com",
  "displayName": "John Partner",
  "permissionLevel": "Read",
  "message": "Welcome to our collaboration space for the Alpha project"
}
```

**Permission Levels**:
- `Read`: View-only access
- `Edit`: Can view and edit documents
- `Write`: Same as Edit
- `Contribute`: Can view, edit, and add documents

**Response**:
```json
{
  "success": true,
  "data": {
    "id": "new-permission-guid",
    "email": "partner@external.com",
    "displayName": "John Partner",
    "permissionLevel": "Read",
    "invitedDate": "2026-02-18T16:30:00Z",
    "invitedBy": "admin@example.com",
    "lastAccessDate": null,
    "status": "Invited"
  }
}
```

#### DELETE /clients/{id}/external-users/{email}

Remove an external user's access from a client site.

**Authentication**: Required

**Path Parameters**:
- `id`: Client ID (integer)
- `email`: Email address of the external user (URL encoded)

**Response**:
```json
{
  "success": true,
  "data": {
    "message": "External user partner@external.com removed successfully"
  }
}
```

### Library Management

#### GET /clients/{id}/libraries

Get all document libraries for a client site.

**Authentication**: Required

**Path Parameters**:
- `id`: Client ID (integer)

**Response**:
```json
{
  "success": true,
  "data": [
    {
      "id": "library-guid",
      "name": "Documents",
      "displayName": "Documents",
      "description": "Default document library",
      "webUrl": "https://tenant.sharepoint.com/sites/client/Documents",
      "createdDateTime": "2026-02-18T15:30:00Z",
      "lastModifiedDateTime": "2026-02-18T16:00:00Z",
      "itemCount": 0
    }
  ]
}
```

#### POST /clients/{id}/libraries

Create a new document library in a client site.

**Authentication**: Required

**Path Parameters**:
- `id`: Client ID (integer)

**Request Body**:
```json
{
  "name": "Legal Documents",
  "description": "Documents for legal review and contracts"
}
```

**Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "id": "new-library-guid",
    "name": "Legal Documents",
    "displayName": "Legal Documents",
    "description": "Documents for legal review and contracts",
    "webUrl": "https://tenant.sharepoint.com/sites/client/Legal%20Documents",
    "createdDateTime": "2026-02-18T17:00:00Z",
    "lastModifiedDateTime": "2026-02-18T17:00:00Z",
    "itemCount": 0
  }
}
```

### List Management

#### GET /clients/{id}/lists

Get all lists for a client site (excludes document libraries).

**Authentication**: Required

**Path Parameters**:
- `id`: Client ID (integer)

**Response**:
```json
{
  "success": true,
  "data": [
    {
      "id": "list-guid",
      "name": "Tasks",
      "displayName": "Project Tasks",
      "description": "Task tracking for the project",
      "webUrl": "https://tenant.sharepoint.com/sites/client/Lists/Tasks",
      "createdDateTime": "2026-02-18T15:30:00Z",
      "lastModifiedDateTime": "2026-02-18T16:00:00Z",
      "itemCount": 0,
      "listTemplate": "tasks"
    }
  ]
}
```

#### POST /clients/{id}/lists

Create a new list in a client site.

**Authentication**: Required

**Path Parameters**:
- `id`: Client ID (integer)

**Request Body**:
```json
{
  "name": "Project Milestones",
  "description": "Track project milestones and deliverables",
  "template": "tasks"
}
```

**Available Templates**:
- `genericList`: Generic custom list
- `tasks`: Task list with assignment and status
- `contacts`: Contact list
- `events`: Calendar/events list
- `links`: Links list
- `announcements`: Announcements list
- `survey`: Survey list
- `issueTracking`: Issue tracking list
- `customList`: Custom list

**Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "id": "new-list-guid",
    "name": "Project Milestones",
    "displayName": "Project Milestones",
    "description": "Track project milestones and deliverables",
    "webUrl": "https://tenant.sharepoint.com/sites/client/Lists/ProjectMilestones",
    "createdDateTime": "2026-02-18T17:15:00Z",
    "lastModifiedDateTime": "2026-02-18T17:15:00Z",
    "itemCount": 0,
    "listTemplate": "tasks"
  }
}
```

## Error Handling

### Common Error Codes

| Code | HTTP Status | Description |
|------|-------------|-------------|
| `AUTH_ERROR` | 401 | Missing or invalid authentication token |
| `TENANT_NOT_FOUND` | 404 | Tenant not found in database |
| `CLIENT_NOT_FOUND` | 404 | Client space not found |
| `TENANT_ALREADY_EXISTS` | 409 | Tenant already registered |
| `CLIENT_EXISTS` | 409 | Client reference already exists |
| `VALIDATION_ERROR` | 400 | Invalid request data |
| `SITE_NOT_PROVISIONED` | 400 | Client site not yet provisioned |
| `INVALID_PERMISSION` | 400 | Invalid permission level specified |
| `UPGRADE_REQUIRED` | 403 | Feature requires higher subscription tier |
| `SUBSCRIPTION_INACTIVE` | 403 | Subscription is not active |
| `TRIAL_EXPIRED` | 403 | Trial period has expired |
| `INVITE_FAILED` | 500 | Failed to invite external user |
| `REMOVE_FAILED` | 500 | Failed to remove external user |
| `CREATE_FAILED` | 500 | Failed to create resource |
| `INTERNAL_ERROR` | 500 | Internal server error |

### Example Error Response

```json
{
  "success": false,
  "error": {
    "code": "UPGRADE_REQUIRED",
    "message": "Feature 'AI Assistant' requires Pro plan or higher. Current plan: Free",
    "details": null
  }
}
```

## Rate Limiting

Currently, the API does not implement rate limiting. This may be added in future versions.

## Feature Gating

Some API features are gated by subscription tier:

| Feature | Free | Starter | Pro | Enterprise |
|---------|------|---------|-----|------------|
| Client Spaces | 3 | 10 | 50 | Unlimited |
| External Users per Site | 10 | 50 | 200 | Unlimited |
| Document Libraries | 5 | 20 | 100 | Unlimited |
| AI Assistant | ❌ | ❌ | ✅ | ✅ |
| Advanced Analytics | ❌ | ❌ | ✅ | ✅ |
| API Access | ✅ | ✅ | ✅ | ✅ |

When a feature is not available for your subscription tier, you'll receive a `403 Forbidden` response with error code `UPGRADE_REQUIRED`.

## Support

For API support, contact: support@example.com

## Changelog

### v1.0.0 (2026-02-18)
- Initial API release
- Multi-tenant support
- Azure AD authentication
- Client space provisioning
- External user management
- Library and list management
- Comprehensive error handling
- Feature gating by subscription tier
