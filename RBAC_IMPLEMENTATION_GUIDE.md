# Tenant Role-Based Access Control (RBAC) - Implementation Guide

## Overview

This document describes the Role-Based Access Control (RBAC) system implemented for the SharePoint External User Manager SaaS platform. The system provides tenant-level user management with three distinct roles to control access to resources.

## Table of Contents

1. [Roles and Permissions](#roles-and-permissions)
2. [Architecture](#architecture)
3. [API Endpoints](#api-endpoints)
4. [Usage Examples](#usage-examples)
5. [Security Considerations](#security-considerations)
6. [Testing](#testing)

## Roles and Permissions

### Role Hierarchy

The system defines three roles with escalating privileges:

| Role | Value | Description | Access Level |
|------|-------|-------------|--------------|
| **TenantOwner** | 1 | Full control over tenant | All operations + subscription management |
| **TenantAdmin** | 2 | Manage client spaces and users | Create, read, update, delete resources |
| **Viewer** | 3 | Read-only access | View dashboard and client spaces only |

### Permission Matrix

| Operation | TenantOwner | TenantAdmin | Viewer |
|-----------|-------------|-------------|--------|
| View Dashboard | ✅ | ✅ | ✅ |
| View Client Spaces | ✅ | ✅ | ✅ |
| Create Client Space | ✅ | ✅ | ❌ |
| Invite External Users | ✅ | ✅ | ❌ |
| Remove External Users | ✅ | ✅ | ❌ |
| Create Libraries/Lists | ✅ | ✅ | ❌ |
| Manage Subscription | ✅ | ❌ | ❌ |
| Assign User Roles | ✅ | ❌ | ❌ |

### Primary Admin Privilege

The user whose email matches the `PrimaryAdminEmail` field in the `Tenants` table automatically receives **TenantOwner** privileges, even if not explicitly listed in the `TenantUsers` table.

## Architecture

### Database Schema

```sql
CREATE TABLE TenantUsers (
    Id INT PRIMARY KEY IDENTITY,
    TenantId INT NOT NULL,
    UserId NVARCHAR(100) NOT NULL,        -- Azure AD Object ID (oid)
    Email NVARCHAR(255) NOT NULL,
    DisplayName NVARCHAR(255) NULL,
    Role INT NOT NULL,                     -- 1=Owner, 2=Admin, 3=Viewer
    CreatedDate DATETIME2 NOT NULL,
    ModifiedDate DATETIME2 NOT NULL,
    CreatedBy NVARCHAR(100) NULL,
    
    CONSTRAINT FK_TenantUsers_Tenants FOREIGN KEY (TenantId) 
        REFERENCES Tenants(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_TenantUsers_TenantId_UserId UNIQUE (TenantId, UserId)
);

-- Indexes for performance
CREATE INDEX IX_TenantUsers_TenantId ON TenantUsers(TenantId);
CREATE INDEX IX_TenantUsers_UserId ON TenantUsers(UserId);
CREATE INDEX IX_TenantUsers_TenantId_Email ON TenantUsers(TenantId, Email);
CREATE INDEX IX_TenantUsers_TenantId_Role ON TenantUsers(TenantId, Role);
```

### Authorization Flow

```
┌─────────────────────────────────────────────────────────┐
│ 1. User makes API request with JWT token                │
│    - Contains tid (tenant ID) and oid (user ID) claims  │
└────────────────────────┬────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────┐
│ 2. RoleAuthorizationHandler executes                     │
│    - Extracts tid and oid from token                     │
│    - Looks up tenant in database                         │
└────────────────────────┬────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────┐
│ 3. Check if user is Primary Admin                        │
│    - Compare email with Tenants.PrimaryAdminEmail        │
│    - If match: Grant TenantOwner access                  │
└────────────────────────┬────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────┐
│ 4. Look up user role in TenantUsers table                │
│    - Query: WHERE TenantId = X AND UserId = Y            │
│    - Extract Role value                                   │
└────────────────────────┬────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────┐
│ 5. Validate role against policy requirement              │
│    - RequireAdmin: Allow Owner/Admin                     │
│    - RequireViewer: Allow Owner/Admin/Viewer             │
│    - RequireOwner: Allow Owner only                      │
└────────────────────────┬────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────┐
│ 6. Return authorization result                            │
│    - Success: Continue to controller                      │
│    - Failure: Return 403 Forbidden                        │
└─────────────────────────────────────────────────────────┘
```

### Authorization Policies

Three policies are defined in `Program.cs`:

```csharp
builder.Services.AddAuthorization(options =>
{
    // TenantOwner and TenantAdmin can create, update, delete resources
    options.AddPolicy("RequireAdmin", policy =>
        policy.Requirements.Add(new RoleRequirement("TenantOwner", "TenantAdmin")));

    // All authenticated users (including Viewer) can read resources
    options.AddPolicy("RequireViewer", policy =>
        policy.Requirements.Add(new RoleRequirement("TenantOwner", "TenantAdmin", "Viewer")));

    // Only TenantOwner can manage subscriptions and tenant settings
    options.AddPolicy("RequireOwner", policy =>
        policy.Requirements.Add(new RoleRequirement("TenantOwner")));
});
```

## API Endpoints

### Role Management Endpoints

#### Get All Tenant Users
```http
GET /tenant-users
Authorization: Bearer {jwt_token}
Policy: RequireAdmin
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "userId": "abc-123-def",
      "email": "admin@example.com",
      "displayName": "John Admin",
      "role": "TenantAdmin",
      "createdDate": "2026-01-15T10:30:00Z",
      "createdBy": "owner@example.com"
    }
  ]
}
```

#### Get Current User's Role
```http
GET /tenant-users/me
Authorization: Bearer {jwt_token}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "id": 1,
    "userId": "abc-123-def",
    "email": "user@example.com",
    "displayName": "Jane User",
    "role": "Viewer",
    "createdDate": "2026-01-15T10:30:00Z",
    "createdBy": "admin@example.com"
  }
}
```

#### Add User to Tenant
```http
POST /tenant-users
Authorization: Bearer {jwt_token}
Policy: RequireOwner
Content-Type: application/json

{
  "userId": "xyz-789-abc",
  "email": "newuser@example.com",
  "displayName": "New User",
  "role": "TenantAdmin"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "id": 5,
    "userId": "xyz-789-abc",
    "email": "newuser@example.com",
    "displayName": "New User",
    "role": "TenantAdmin",
    "createdDate": "2026-02-20T15:45:00Z",
    "createdBy": "owner@example.com"
  }
}
```

#### Update User Role
```http
PUT /tenant-users/{id}
Authorization: Bearer {jwt_token}
Policy: RequireOwner
Content-Type: application/json

{
  "role": "Viewer"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "id": 5,
    "userId": "xyz-789-abc",
    "email": "newuser@example.com",
    "displayName": "New User",
    "role": "Viewer",
    "createdDate": "2026-02-20T15:45:00Z",
    "createdBy": "owner@example.com"
  }
}
```

#### Remove User from Tenant
```http
DELETE /tenant-users/{id}
Authorization: Bearer {jwt_token}
Policy: RequireOwner
```

**Response:**
```json
{
  "success": true,
  "data": {
    "message": "User newuser@example.com removed from tenant"
  }
}
```

### Protected Client Endpoints

All client management endpoints in `ClientsController` are now protected:

| Endpoint | Method | Policy |
|----------|--------|--------|
| `/clients` | GET | RequireViewer |
| `/clients/{id}` | GET | RequireViewer |
| `/clients` | POST | RequireAdmin |
| `/clients/{id}/external-users` | GET | RequireViewer |
| `/clients/{id}/external-users` | POST | RequireAdmin |
| `/clients/{id}/external-users/{email}` | DELETE | RequireAdmin |
| `/clients/{id}/libraries` | GET | RequireViewer |
| `/clients/{id}/libraries` | POST | RequireAdmin |
| `/clients/{id}/lists` | GET | RequireViewer |
| `/clients/{id}/lists` | POST | RequireAdmin |

## Usage Examples

### Scenario 1: Onboarding New Tenant

When a new tenant is onboarded:

1. **Tenant record created** with `PrimaryAdminEmail` set to the admin's email
2. **Primary admin automatically has TenantOwner** privileges (no TenantUsers entry needed)
3. **Primary admin can add other users** to the tenant

```bash
# Primary admin adds a new TenantAdmin
curl -X POST https://api.example.com/tenant-users \
  -H "Authorization: Bearer {owner_token}" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "user-guid-from-azure-ad",
    "email": "admin2@company.com",
    "displayName": "Second Admin",
    "role": "TenantAdmin"
  }'
```

### Scenario 2: Adding Read-Only Users

A TenantOwner can add Viewer users who can see data but not modify anything:

```bash
curl -X POST https://api.example.com/tenant-users \
  -H "Authorization: Bearer {owner_token}" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "viewer-guid",
    "email": "viewer@company.com",
    "displayName": "Finance Viewer",
    "role": "Viewer"
  }'
```

### Scenario 3: Promoting a User

Upgrade a Viewer to TenantAdmin:

```bash
curl -X PUT https://api.example.com/tenant-users/5 \
  -H "Authorization: Bearer {owner_token}" \
  -H "Content-Type: application/json" \
  -d '{
    "role": "TenantAdmin"
  }'
```

### Scenario 4: Removing a User

Remove a user from the tenant (only TenantOwner can do this):

```bash
curl -X DELETE https://api.example.com/tenant-users/5 \
  -H "Authorization: Bearer {owner_token}"
```

### Scenario 5: Checking Your Role

Any authenticated user can check their own role:

```bash
curl https://api.example.com/tenant-users/me \
  -H "Authorization: Bearer {user_token}"
```

## Security Considerations

### 1. Tenant Isolation

- All queries filter by `TenantId` extracted from JWT token
- Users can only see and manage resources within their own tenant
- Cross-tenant access is impossible due to database-level filtering

### 2. JWT Token Validation

- All endpoints require valid Azure AD JWT tokens
- Tokens must contain `tid` (tenant ID) and `oid` (user ID) claims
- Token validation happens before authorization checks

### 3. Primary Admin Protection

- Primary admin email is immutable (set during tenant onboarding)
- Primary admin automatically has TenantOwner privileges
- Cannot be downgraded or removed through TenantUsers API

### 4. Self-Removal Prevention

```csharp
// Prevent user from removing themselves
if (tenantUser.UserId == currentUserId)
{
    return BadRequest(ApiResponse<object>.ErrorResponse(
        "CANNOT_REMOVE_SELF",
        "You cannot remove yourself from the tenant"));
}
```

### 5. Role Validation

Only valid roles can be assigned:
- TenantOwner
- TenantAdmin
- Viewer

Invalid role strings are rejected with a 400 Bad Request.

### 6. Audit Logging

All role management operations are logged with:
- Who performed the action (CreatedBy)
- When it was performed (CreatedDate)
- What was changed (Role updates tracked via ModifiedDate)

## Testing

### Unit Tests

The RBAC system includes 14 comprehensive unit tests:

**RoleAuthorizationHandlerTests** (6 tests):
- ✅ Primary admin automatic TenantOwner access
- ✅ TenantAdmin role enforcement
- ✅ Viewer role restrictions
- ✅ Missing JWT claims handling
- ✅ User not in tenant scenarios
- ✅ Null email safety

**TenantUsersControllerTests** (8 tests):
- ✅ Get current user role (primary admin)
- ✅ Get current user role (tenant user)
- ✅ User not in tenant returns 404
- ✅ Add new user to tenant
- ✅ Duplicate user returns conflict
- ✅ Update user role
- ✅ Remove user from tenant
- ✅ Self-removal prevention

### Running Tests

```bash
cd src/api-dotnet/WebApi/SharePointExternalUserManager.Api.Tests

# Run all tests
dotnet test

# Run only RBAC tests
dotnet test --filter "FullyQualifiedName~RoleAuthorizationHandlerTests"
dotnet test --filter "FullyQualifiedName~TenantUsersControllerTests"
```

**Test Results**: 96/96 tests passing ✅

### Security Scan

The implementation has been validated with CodeQL:

```bash
# CodeQL scan results
csharp: 0 alerts found ✅
```

## Error Responses

### 401 Unauthorized

Missing or invalid JWT token:

```json
{
  "success": false,
  "error": {
    "code": "AUTH_ERROR",
    "message": "Missing tenant claim"
  }
}
```

### 403 Forbidden

Insufficient permissions:

```json
{
  "success": false,
  "error": {
    "code": "FORBIDDEN",
    "message": "You do not have permission to perform this action"
  }
}
```

### 404 Not Found

User not in tenant:

```json
{
  "success": false,
  "error": {
    "code": "USER_NOT_FOUND",
    "message": "You are not assigned to this tenant. Please contact your tenant administrator."
  }
}
```

### 400 Bad Request

Invalid role or self-removal attempt:

```json
{
  "success": false,
  "error": {
    "code": "INVALID_ROLE",
    "message": "Invalid role: SuperAdmin. Valid roles are: TenantOwner, TenantAdmin, Viewer"
  }
}
```

```json
{
  "success": false,
  "error": {
    "code": "CANNOT_REMOVE_SELF",
    "message": "You cannot remove yourself from the tenant"
  }
}
```

### 409 Conflict

User already exists in tenant:

```json
{
  "success": false,
  "error": {
    "code": "USER_EXISTS",
    "message": "User admin@example.com is already a member of this tenant with role TenantAdmin"
  }
}
```

## Migration

To apply the RBAC database changes:

```bash
cd src/api-dotnet/WebApi/SharePointExternalUserManager.Api

# Update database
dotnet ef database update
```

This will create the `TenantUsers` table with all necessary indexes and constraints.

## Best Practices

1. **Assign Roles Carefully**
   - Only give TenantOwner to highly trusted users (can manage billing and roles)
   - Use TenantAdmin for day-to-day management tasks
   - Use Viewer for read-only access (auditors, executives)

2. **Regular Audits**
   - Periodically review user roles via `GET /tenant-users`
   - Remove users who no longer need access
   - Downgrade users who need less privilege

3. **Principle of Least Privilege**
   - Start users with Viewer role
   - Upgrade only when necessary
   - Document why each user needs elevated access

4. **Primary Admin Backup**
   - Ensure at least one other user has TenantOwner role
   - Document who the primary admin is
   - Have a process for transferring primary admin if needed

## Troubleshooting

### Issue: User can't access any resources

**Check:**
1. Is the JWT token valid?
2. Does the user exist in TenantUsers table?
3. Is the tenant active?
4. Does the role match the required policy?

**Solution:**
```sql
-- Check user's role
SELECT * FROM TenantUsers 
WHERE TenantId = {tenant_id} AND UserId = '{user_oid}';

-- Check if user is primary admin
SELECT PrimaryAdminEmail FROM Tenants WHERE Id = {tenant_id};
```

### Issue: Primary admin can't perform owner-only actions

**Check:**
- Does the user's email claim match `Tenants.PrimaryAdminEmail` exactly?
- Is the comparison case-sensitive? (It shouldn't be - uses OrdinalIgnoreCase)

### Issue: Changes not taking effect

**Check:**
- Are you using the correct tenant's JWT token?
- Have you restarted the application after database changes?
- Is caching interfering? (RBAC uses no caching)

## Support

For issues or questions about RBAC:

1. Check the test cases in `RoleAuthorizationHandlerTests.cs` for examples
2. Review the API documentation at `/swagger`
3. Contact the platform support team

---

**Document Version**: 1.0  
**Last Updated**: February 20, 2026  
**Authors**: SharePoint External User Manager Team
