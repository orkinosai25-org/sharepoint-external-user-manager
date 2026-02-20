# Swagger Security Configuration Guide

## Overview

The SharePoint External User Manager API includes comprehensive Swagger/OpenAPI documentation. For security reasons, Swagger access is controlled based on the environment and configuration settings.

## Security Modes

### 1. Development Environment (Default)
- **Swagger Status**: Always enabled
- **Authentication**: Not required
- **Use Case**: Local development and debugging

### 2. Production Environment (Secure by Default)
- **Swagger Status**: Disabled by default
- **Authentication**: Required if enabled
- **Use Case**: Production deployment with strict access control

## Configuration

Swagger security is configured in `appsettings.json` or environment-specific configuration files:

```json
{
  "Swagger": {
    "Enabled": true,
    "RequireAuthentication": false,
    "AllowedRoles": []
  }
}
```

### Configuration Options

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `Enabled` | boolean | `true` | Controls whether Swagger is accessible |
| `RequireAuthentication` | boolean | `false` | Requires JWT authentication to access Swagger |
| `AllowedRoles` | string[] | `[]` | List of roles that can access Swagger (empty = all authenticated users) |

## Deployment Scenarios

### Scenario 1: Production - Swagger Disabled (Recommended)

This is the most secure configuration for production environments.

**appsettings.Production.json:**
```json
{
  "Swagger": {
    "Enabled": false
  }
}
```

**Behavior:**
- Swagger UI is completely inaccessible
- `/swagger` endpoints return 404
- No documentation exposed to external users

### Scenario 2: Production - Swagger with Authentication

Enable Swagger in production but require authentication.

**appsettings.Production.json:**
```json
{
  "Swagger": {
    "Enabled": true,
    "RequireAuthentication": true,
    "AllowedRoles": []
  }
}
```

**Behavior:**
- Swagger UI requires valid JWT token
- Any authenticated user can access
- Users must sign in with Microsoft Entra ID

### Scenario 3: Production - Swagger with Role-Based Access

Restrict Swagger access to specific roles (e.g., administrators only).

**appsettings.Production.json:**
```json
{
  "Swagger": {
    "Enabled": true,
    "RequireAuthentication": true,
    "AllowedRoles": ["Admin", "TenantOwner"]
  }
}
```

**Behavior:**
- Swagger UI requires valid JWT token
- User must have one of the specified roles
- Returns 403 Forbidden for users without required role

### Scenario 4: Staging/Testing - Swagger Enabled

For staging or testing environments where authentication might not be required.

**appsettings.Staging.json:**
```json
{
  "Swagger": {
    "Enabled": true,
    "RequireAuthentication": false
  }
}
```

**Behavior:**
- Swagger UI accessible without authentication
- Suitable for internal testing environments
- Should not be used in public-facing deployments

## How It Works

### Request Flow

1. **Request to `/swagger` endpoint**
2. **Environment Check**
   - Development: Allow access (skip authentication)
   - Production: Check configuration
3. **Configuration Check**
   - If `Enabled: false`: Return 404 (endpoint not registered)
   - If `Enabled: true` and `RequireAuthentication: true`: Proceed to auth check
4. **Authentication Check**
   - Verify JWT token is present and valid
   - If not authenticated: Return 401 Unauthorized
5. **Authorization Check** (if roles configured)
   - Verify user has at least one of the allowed roles
   - If not authorized: Return 403 Forbidden
6. **Grant Access**
   - Serve Swagger UI

### Error Responses

**401 Unauthorized (No Authentication):**
```json
{
  "error": "UNAUTHORIZED",
  "message": "Authentication required to access Swagger documentation"
}
```

**403 Forbidden (Insufficient Permissions):**
```json
{
  "error": "FORBIDDEN",
  "message": "Insufficient permissions to access Swagger documentation"
}
```

## Security Best Practices

### Production Environments

1. **Disable Swagger Entirely** (Recommended)
   - Set `Enabled: false` in production configuration
   - Generates and maintains API documentation separately
   - Use tools like Postman, Azure API Management, or exported OpenAPI specs

2. **Enable with Strict Role-Based Access** (Alternative)
   - Set `RequireAuthentication: true`
   - Specify `AllowedRoles` with admin-only roles
   - Regularly audit who has access
   - Monitor Swagger endpoint access in logs

3. **Never Allow Anonymous Access**
   - Never set `RequireAuthentication: false` in production
   - Always require authentication if Swagger is enabled

### Role Configuration

Roles are checked in two ways:
1. Standard ASP.NET Core role claims (`context.User.IsInRole(role)`)
2. Azure AD roles in JWT token (`roles` claim)

Ensure your Azure AD app registration includes:
- App roles defined in the manifest
- Users/groups assigned to appropriate roles
- Roles included in JWT tokens

### Monitoring

Consider logging Swagger access attempts:
- Log successful access (user, timestamp, tenant)
- Log failed authentication/authorization attempts
- Set up alerts for unusual access patterns

## Environment Variables Override

You can override Swagger settings using environment variables:

```bash
# Disable Swagger
export Swagger__Enabled=false

# Enable with authentication
export Swagger__Enabled=true
export Swagger__RequireAuthentication=true

# Set allowed roles
export Swagger__AllowedRoles__0=Admin
export Swagger__AllowedRoles__1=TenantOwner
```

## Testing

### Local Development

Swagger is always accessible at `https://localhost:7071/swagger` during development.

### Production Testing

To test Swagger authentication in production-like environments:

1. Configure authentication as shown in Scenario 2 or 3
2. Obtain a valid JWT token from Azure AD
3. Access Swagger UI
4. Click "Authorize" button
5. Enter: `Bearer YOUR_JWT_TOKEN`
6. Test API endpoints through Swagger UI

## Troubleshooting

### Swagger Returns 404

**Cause:** Swagger is disabled in configuration
**Solution:** Set `Swagger:Enabled` to `true` in your environment's configuration

### Swagger Returns 401

**Cause:** Authentication required but no valid JWT token provided
**Solution:** 
1. Sign in with Microsoft Entra ID
2. Obtain JWT token
3. Add `Authorization: Bearer TOKEN` header to requests

### Swagger Returns 403

**Cause:** User is authenticated but doesn't have required role
**Solution:**
1. Verify `AllowedRoles` configuration
2. Check user's roles in Azure AD
3. Ensure roles are included in JWT token claims
4. Assign appropriate role to user in Azure AD

### Swagger Shows But APIs Fail

**Cause:** Swagger is accessible but individual API endpoints require authentication
**Solution:** This is expected behavior. Each API endpoint has its own authorization requirements defined by `[Authorize]` attributes and `RequiresPlanAttribute`.

## Related Documentation

- [Global Exception Middleware Guide](./GLOBAL_EXCEPTION_MIDDLEWARE_GUIDE.md)
- [Rate Limiting Configuration](./RATE_LIMITING_CONFIGURATION.md)
- [Security Summary](./SECURITY_SUMMARY.md)
- [Azure AD App Setup](./AZURE_AD_APP_SETUP.md)

## Summary

The Swagger security implementation follows defense-in-depth principles:

✅ **Disabled by default in production** - Most secure option
✅ **Authentication required when enabled** - Protects sensitive API documentation  
✅ **Role-based access control** - Restricts access to authorized administrators  
✅ **Flexible configuration** - Adapts to different deployment scenarios  
✅ **Environment-aware** - Different behavior for dev vs. production  

This approach satisfies ISSUE-08 requirements to "Disable in Production OR Protect behind admin role" while providing flexibility for different operational needs.
