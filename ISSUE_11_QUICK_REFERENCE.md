# ISSUE 11 - RBAC Quick Reference

## What Was Implemented
Tenant Role-Based Access Control (RBAC) system for managing user permissions within tenants.

## Three Roles

| Role | Permissions | Notes |
|------|------------|-------|
| **Owner** | Full access to everything | Auto-granted to primary admin |
| **Admin** | Full management capabilities | Must be explicitly assigned |
| **Viewer** | Read-only access | Default for authenticated users |

## Protected Endpoints

### Create Client Space
```http
POST /clients
Authorization: Bearer {jwt_token}
Required Role: Owner or Admin
```

### Invite External User
```http
POST /clients/{id}/external-users
Authorization: Bearer {jwt_token}
Required Role: Owner or Admin
```

### Remove External User
```http
DELETE /clients/{id}/external-users/{email}
Authorization: Bearer {jwt_token}
Required Role: Owner or Admin
```

## Using the Attribute

```csharp
using SharePointExternalUserManager.Api.Attributes;
using SharePointExternalUserManager.Api.Models;

[HttpPost]
[RequiresTenantRole("Operation Name", TenantRole.Owner, TenantRole.Admin)]
public async Task<IActionResult> YourProtectedMethod()
{
    // Only Owner and Admin roles can execute this
}
```

## Adding a User with Role

```csharp
using SharePointExternalUserManager.Api.Data.Entities;
using SharePointExternalUserManager.Api.Models;

var tenantUser = new TenantUserEntity
{
    TenantId = tenant.Id,
    EntraIdUserId = "user-object-id-from-jwt",
    Email = "user@company.com",
    DisplayName = "John Doe",
    Role = TenantRole.Admin,
    IsActive = true
};

await dbContext.TenantUsers.AddAsync(tenantUser);
await dbContext.SaveChangesAsync();
```

## Applying the Database Migration

```bash
cd src/api-dotnet/WebApi/SharePointExternalUserManager.Api
dotnet ef database update
```

## Error Responses

### 401 Unauthorized
Missing or invalid authentication token.

### 403 Forbidden
```json
{
  "success": false,
  "error": "FORBIDDEN",
  "message": "Operation 'Create Client' requires one of the following roles: Owner, Admin. Your role: Viewer"
}
```

### 404 Not Found
Tenant not found in database.

## Testing

Run RBAC tests:
```bash
cd src/api-dotnet/WebApi
dotnet test --filter "FullyQualifiedName~RequiresTenantRoleAttributeTests"
```

All tests: ✅ 86/86 passed (including 4 RBAC tests)

## Security

✅ CodeQL scan: 0 vulnerabilities
✅ Tenant isolation enforced
✅ Principle of least privilege
✅ Primary admin protected

## Documentation

- **Full Implementation Guide**: [ISSUE_11_RBAC_IMPLEMENTATION.md](./ISSUE_11_RBAC_IMPLEMENTATION.md)
- **Security Analysis**: [ISSUE_11_SECURITY_SUMMARY.md](./ISSUE_11_SECURITY_SUMMARY.md)

## Status

✅ **COMPLETE** - Ready for production
