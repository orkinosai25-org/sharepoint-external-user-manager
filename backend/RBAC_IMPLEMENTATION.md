# Role-Based Access Control (RBAC) Implementation

## Overview

This implementation provides role-based access control for the SharePoint External User Manager backend APIs, ensuring only authorized users can manage client spaces.

## Roles

The system supports the following roles:

### FirmAdmin
- **Full access** to client management operations
- Can **create**, **read**, **update**, and **delete** clients
- Can manage all client resources (libraries, lists, users)
- Permissions:
  - `clients:read`
  - `clients:write`
  - `clients:delete`

### FirmUser (Read-Only)
- **Read-only** access to client information
- Can **view** clients but cannot create or modify them
- Can view client libraries and lists
- Permissions:
  - `clients:read`

### Owner
- Highest privilege level (tenant owner)
- Has all FirmAdmin permissions plus additional tenant management capabilities
- Automatically assigned to the user who onboards a tenant

### Admin
- Similar to FirmAdmin
- Full access to client management

### User
- Basic user role with read-only access
- Same as FirmUser

### ReadOnly
- Explicit read-only role
- Same as FirmUser

## Role Assignment

Roles are assigned through two mechanisms:

1. **JWT Token Claims**: Roles can be included in the Azure AD JWT token's `roles` claim array
2. **Default Assignment**: If no roles are found in the token, users are assigned the `FirmUser` role by default
3. **Primary Admin**: Users matching the tenant's `PrimaryAdminEmail` automatically receive `Owner` and `FirmAdmin` roles

### Role Mapping

Azure AD app roles are mapped to application roles as follows:

| Azure AD Role | Application Role(s) | Notes |
|--------------|---------------------|-------|
| `FirmAdmin` | `FirmAdmin` | Specific firm admin role |
| `admin` | `Admin`, `FirmAdmin` | Generic admin gets both roles for compatibility |
| `FirmUser` | `FirmUser` | Specific firm user role |
| `user` | `User`, `FirmUser` | Generic user gets both roles for compatibility |
| `owner` | `Owner`, `FirmAdmin` | Owner includes admin capabilities |
| `readonly` | `ReadOnly` | Explicit read-only role |

**Note**: Generic roles (`admin`, `user`) are mapped to both their specific application role and the corresponding Firm* role to ensure compatibility and proper permission inheritance.

## Protected Endpoints

### Client Management APIs

All client-related endpoints now enforce permissions:

#### Create Client
- **Endpoint**: `POST /clients`
- **Required Permission**: `clients:write`
- **Allowed Roles**: FirmAdmin, Admin, Owner

#### Get Client
- **Endpoint**: `GET /clients/:id`
- **Required Permission**: `clients:read`
- **Allowed Roles**: All roles (FirmAdmin, FirmUser, Admin, User, ReadOnly, Owner)

#### List Clients
- **Endpoint**: `GET /clients`
- **Required Permission**: `clients:read`
- **Allowed Roles**: All roles

#### Get Client Libraries
- **Endpoint**: `GET /clients/:id/libraries`
- **Required Permission**: `clients:read`
- **Allowed Roles**: All roles

#### Get Client Lists
- **Endpoint**: `GET /clients/:id/lists`
- **Required Permission**: `clients:read`
- **Allowed Roles**: All roles

## Error Handling

When a user lacks the required permission, the API returns:

- **Status Code**: `403 Forbidden`
- **Error Code**: `FORBIDDEN`
- **Error Message**: Descriptive message indicating the required permission
- **Details**: Information about:
  - The required permission
  - The user's current roles

Example error response:
```json
{
  "success": false,
  "error": {
    "code": "FORBIDDEN",
    "message": "You do not have permission to create clients",
    "details": "Required permission: clients:write. Your roles: FirmUser",
    "correlationId": "abc-123-def"
  }
}
```

## Implementation Details

### Permission Middleware (`src/middleware/permissions.ts`)

The permission middleware provides several utility functions:

- `hasPermission(context, permission)`: Check if user has a specific permission
- `hasRole(context, role)`: Check if user has a specific role
- `hasAnyRole(context, roles)`: Check if user has any of the specified roles
- `requirePermission(context, permission, action?)`: Throw error if permission is not met
- `requireRole(context, role, action?)`: Throw error if role is not met
- `requireAnyRole(context, roles, action?)`: Throw error if none of the roles are met

### Authentication Middleware (`src/middleware/auth.ts`)

Updated to:
1. Extract roles from JWT token claims
2. Map Azure AD roles to application roles
3. Assign default `FirmUser` role if no roles found
4. Populate `roles` array in `TenantContext`

### Usage Example

```typescript
import { requirePermission, Permissions } from '../../middleware/permissions';

async function createClient(req: HttpRequest, context: InvocationContext) {
  // Authenticate request
  const tenantContext = await authenticateRequest(req, context);
  
  // Check permissions
  requirePermission(tenantContext, Permissions.CLIENTS_WRITE, 'create clients');
  
  // ... rest of the function
}
```

## Testing

Comprehensive unit tests are provided in `src/middleware/permissions.spec.ts` covering:
- Permission checks for different roles
- Role validation
- Error handling
- Various user scenarios

Run tests with:
```bash
npm test -- permissions.spec.ts
```

## Security Considerations

1. **Principle of Least Privilege**: Users are assigned the minimum permissions needed
2. **Default Read-Only**: Users without explicit roles default to read-only access
3. **Clear Error Messages**: Permission errors provide helpful feedback without exposing security details
4. **Audit Logging**: All API calls are logged with user and role information
5. **Token Validation**: All requests require valid JWT tokens from Azure AD

## Future Enhancements

Potential improvements include:
- User role management UI
- Database-backed role assignments
- Fine-grained permissions at the resource level
- Role-based rate limiting
- Permission caching for improved performance
