# Tenant Role-Based Access Control (RBAC) Implementation

## Overview
This document describes the tenant role-based access control (RBAC) system implemented for the SharePoint External User Manager API. The system provides fine-grained control over who can perform various operations within a tenant.

## Roles

### TenantRole Enum
The system defines three tenant roles with increasing levels of permissions:

#### Owner (0)
- **Full administrative access** to all tenant operations
- Can create, modify, and delete client spaces
- Can invite and remove external users
- Can manage all tenant settings
- **Auto-granted** to the primary admin email specified during tenant registration

#### Admin (1)
- **Full management capabilities** similar to Owner
- Can create, modify, and delete client spaces
- Can invite and remove external users
- Can manage tenant settings
- Must be explicitly assigned

#### Viewer (2)
- **Read-only access** to tenant data
- Can view dashboard and client information
- Can view external users
- Cannot create, modify, or delete resources
- **Default role** for authenticated users without explicit role assignment

## Database Schema

### TenantUsers Table
```sql
CREATE TABLE TenantUsers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    EntraIdUserId NVARCHAR(100) NOT NULL,
    Email NVARCHAR(255) NOT NULL,
    DisplayName NVARCHAR(255),
    Role INT NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME2 NOT NULL,
    ModifiedDate DATETIME2 NOT NULL,
    
    CONSTRAINT FK_TenantUsers_Tenants FOREIGN KEY (TenantId) 
        REFERENCES Tenants(Id) ON DELETE CASCADE,
    
    CONSTRAINT UQ_TenantUsers_TenantId_EntraIdUserId 
        UNIQUE (TenantId, EntraIdUserId)
);

-- Indexes for optimal query performance
CREATE INDEX IX_TenantUsers_TenantId ON TenantUsers(TenantId);
CREATE INDEX IX_TenantUsers_EntraIdUserId ON TenantUsers(EntraIdUserId);
CREATE INDEX IX_TenantUsers_TenantId_Email ON TenantUsers(TenantId, Email);
CREATE INDEX IX_TenantUsers_TenantId_Role ON TenantUsers(TenantId, Role);
```

## Authorization Attribute

### RequiresTenantRoleAttribute
This custom action filter enforces role-based authorization on controller endpoints.

**Usage:**
```csharp
[HttpPost]
[RequiresTenantRole("Create Client", TenantRole.Owner, TenantRole.Admin)]
public async Task<IActionResult> CreateClient([FromBody] CreateClientRequest request)
{
    // Only Owner and Admin roles can reach here
}
```

**Features:**
- Validates user authentication (JWT claims: `tid`, `oid`)
- Looks up user's role in the tenant
- Automatically grants Owner role to primary admin
- Defaults to Viewer role for users without explicit assignment
- Checks if user is active
- Returns appropriate HTTP status codes:
  - `401 Unauthorized`: Missing authentication claims
  - `403 Forbidden`: Insufficient role permissions
  - `404 Not Found`: Tenant not found

## Protected Endpoints

### ClientsController

#### Create Client Space
```csharp
POST /clients
Required Role: Owner or Admin
```

#### Invite External User
```csharp
POST /clients/{id}/external-users
Required Role: Owner or Admin
```

#### Remove External User
```csharp
DELETE /clients/{id}/external-users/{email}
Required Role: Owner or Admin
```

#### View Operations (All Roles)
```csharp
GET /clients
GET /clients/{id}
GET /clients/{id}/external-users
Required Role: Viewer, Admin, or Owner
```

## Role Assignment

### Primary Admin (Automatic)
When a tenant is registered, the `PrimaryAdminEmail` is automatically granted the **Owner** role. No explicit TenantUser record is required.

```csharp
// During tenant registration
var tenant = new TenantEntity
{
    EntraIdTenantId = tenantId,
    OrganizationName = orgName,
    PrimaryAdminEmail = "admin@company.com" // Auto-granted Owner role
};
```

### Explicit Role Assignment
Additional users can be assigned roles through the TenantUsers table:

```csharp
var tenantUser = new TenantUserEntity
{
    TenantId = tenant.Id,
    EntraIdUserId = userId,
    Email = "user@company.com",
    DisplayName = "John Doe",
    Role = TenantRole.Admin,
    IsActive = true
};
await dbContext.TenantUsers.AddAsync(tenantUser);
await dbContext.SaveChangesAsync();
```

## Error Responses

### Insufficient Permissions (403 Forbidden)
```json
{
  "success": false,
  "error": "FORBIDDEN",
  "message": "Operation 'Create Client' requires one of the following roles: Owner, Admin. Your role: Viewer",
  "data": null
}
```

### Missing Authentication (401 Unauthorized)
```json
{
  "success": false,
  "error": "AUTH_ERROR",
  "message": "Missing tenant or user claim",
  "data": null
}
```

### Tenant Not Found (404 Not Found)
```json
{
  "success": false,
  "error": "TENANT_NOT_FOUND",
  "message": "Tenant not found",
  "data": null
}
```

## Security Considerations

### Principle of Least Privilege
- Users without explicit roles default to **Viewer** (read-only)
- Only Owner and Admin can perform destructive operations
- Each endpoint is explicitly protected with appropriate role requirements

### Tenant Isolation
- All role checks are scoped to the specific tenant
- Users can have different roles in different tenants
- TenantId is always validated from the JWT `tid` claim

### Inactive Users
- Users with `IsActive = false` are denied access
- This allows for temporary suspension without deletion

### Audit Trail
- Role information is stored in `HttpContext.Items` for logging
- Controllers can access `TenantUserId` and `TenantUserRole` from context
- All operations should be logged with user role information

## Testing

### Unit Tests
Comprehensive unit tests are provided in `RequiresTenantRoleAttributeTests.cs`:

- ✅ Owner role grants access to Owner-required endpoints
- ✅ Admin role grants access to Admin-required endpoints
- ✅ Viewer role is denied access to Admin-required endpoints
- ✅ Primary admin automatically receives Owner role
- ✅ Users without explicit roles default to Viewer
- ✅ Missing tenant claim returns 401 Unauthorized
- ✅ Non-existent tenant returns 404 Not Found
- ✅ Inactive users are denied access

**Test Results:** All 4 tests passed ✅

## Migration

### Applying the Migration
```bash
cd src/api-dotnet/WebApi/SharePointExternalUserManager.Api
dotnet ef database update
```

### Rolling Back
```bash
dotnet ef database update AddTenantAuth
```

## API DTOs

### TenantUserResponse
```csharp
public class TenantUserResponse
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string EntraIdUserId { get; set; }
    public string Email { get; set; }
    public string? DisplayName { get; set; }
    public TenantRole Role { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}
```

### AddTenantUserRequest
```csharp
public class AddTenantUserRequest
{
    public string Email { get; set; }
    public string? DisplayName { get; set; }
    public TenantRole Role { get; set; } = TenantRole.Viewer;
}
```

### UpdateTenantUserRoleRequest
```csharp
public class UpdateTenantUserRoleRequest
{
    public TenantRole Role { get; set; }
}
```

## Future Enhancements

### Recommended Additions
1. **Role Management API**: Endpoints to manage user roles
   - `POST /tenants/users` - Add user with role
   - `PUT /tenants/users/{userId}/role` - Update user role
   - `DELETE /tenants/users/{userId}` - Remove user
   - `GET /tenants/users` - List all users and roles

2. **Resource-Level Permissions**: Fine-grained permissions per client space
   - Allow different users to manage different client spaces
   - Implement ClientSpaceUser entity for per-resource access

3. **Role Groups/Teams**: Group users into teams with shared roles
   - Simplify management of large user bases
   - Inherit permissions from team membership

4. **Custom Roles**: Allow tenants to define custom roles
   - Create organization-specific roles
   - Map custom roles to base permissions

5. **Permission Caching**: Cache role lookups for performance
   - Reduce database queries
   - Invalidate cache on role changes

## Security Scan Results

**CodeQL Analysis:** ✅ 0 vulnerabilities found

The implementation has been scanned using CodeQL and found to be free of security vulnerabilities.

## Support

For questions or issues related to RBAC implementation, contact the development team or refer to:
- API Documentation: `/swagger`
- GitHub Issues: [Repository Issues](https://github.com/orkinosai25-org/sharepoint-external-user-manager/issues)
