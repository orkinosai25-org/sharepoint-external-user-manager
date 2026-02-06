# SharePoint External User Manager - ASP.NET Core Web API

## Overview

This is the ASP.NET Core .NET 8 Web API implementation for the SharePoint External User Manager SaaS platform (ISSUE-02). It provides multi-tenant authentication, tenant context management, and core API endpoints.

## Architecture

- **Framework**: ASP.NET Core .NET 8
- **Authentication**: Microsoft Identity Web (Entra ID JWT tokens)
- **Multi-Tenancy**: Tenant ID extracted from JWT claims
- **Models**: Shared with Azure Functions project (transitional)

## Prerequisites

- .NET 8 SDK
- Azure AD App Registration (for authentication)

## Configuration

### appsettings.json

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "ClientId": "YOUR_CLIENT_ID_HERE",
    "TenantId": "common",
    "Audience": "YOUR_CLIENT_ID_HERE"
  }
}
```

### Azure AD Setup

1. Register an application in Azure AD
2. Set `api://YOUR_CLIENT_ID` as the Application ID URI
3. Add the Client ID to `appsettings.json`
4. For multi-tenant: use `"TenantId": "common"`

## Running Locally

```bash
cd src/api-dotnet/WebApi/SharePointExternalUserManager.Api
dotnet run
```

The API will start on `http://localhost:5000` (or `https://localhost:5001`).

## Endpoints

### Public Endpoints

#### GET /health
Health check endpoint for monitoring.

**Response**:
```json
{
  "status": "Healthy",
  "version": "1.0.0",
  "timestamp": "2026-02-05T23:58:32Z"
}
```

### Authenticated Endpoints

#### GET /tenants/me
Returns the authenticated tenant's information extracted from JWT claims.

**Headers**:
```
Authorization: Bearer <JWT_TOKEN>
```

**Response**:
```json
{
  "success": true,
  "data": {
    "tenantId": "tenant-guid",
    "userId": "user-guid",
    "userPrincipalName": "user@domain.com",
    "isActive": true,
    "subscriptionTier": "Free"
  }
}
```

## Multi-Tenant Isolation

The API enforces tenant isolation by:

1. Validating JWT tokens from Azure AD
2. Extracting `tid` (tenant ID) and `oid` (user ID) claims
3. Ensuring all operations are scoped to the authenticated tenant

## Testing

### Test Health Endpoint
```bash
curl http://localhost:5000/health
```

### Test Tenant Endpoint (requires token)
```bash
# Get a token from Azure AD first
TOKEN="your-jwt-token-here"
curl -H "Authorization: Bearer $TOKEN" http://localhost:5000/tenants/me
```

## Project Structure

```
SharePointExternalUserManager.Api/
├── Controllers/
│   ├── HealthController.cs      # Health check endpoint
│   └── TenantsController.cs     # Tenant management endpoints
├── Program.cs                    # Application startup and configuration
├── appsettings.json             # Configuration
└── SharePointExternalUserManager.Api.csproj
```

## Development Notes

### Models
Currently references models from the Azure Functions project (`SharePointExternalUserManager.Functions`). These will be moved to `/src/shared` in ISSUE-03.

### Authentication Flow
1. Client obtains JWT token from Azure AD
2. Client sends request with `Authorization: Bearer <token>` header
3. Microsoft.Identity.Web middleware validates token
4. Controller extracts tenant/user claims from `User.Claims`
5. Response includes tenant-specific data

## Next Steps (ISSUE-03)

- Add Entity Framework Core
- Add Azure SQL Database support
- Create database migrations
- Implement tenant data persistence
- Move models to shared project

## Acceptance Criteria for ISSUE-02 ✅

- [x] API runs locally
- [x] Entra ID JWT authentication configured
- [x] Multi-tenant support (common endpoint)
- [x] Health check endpoint (GET /health)
- [x] Tenant context endpoint (GET /tenants/me)
- [x] Tenant ID extracted from JWT claims
- [x] Tenant isolation enforced
- [x] Build succeeds with no errors

## Troubleshooting

### Authentication Fails
- Verify Client ID in `appsettings.json`
- Ensure token audience matches Client ID
- Check token is not expired
- Verify `tid` and `oid` claims exist in token

### Build Warnings
- `NU1902`: Microsoft.Identity.Web 3.6.0 vulnerability - inherited from Functions project, will be updated in ISSUE-03

##Security Note

The referenced Functions project uses Microsoft.Identity.Web 3.6.0 which has a known vulnerability. This will be addressed when models are moved to the shared project in ISSUE-03.
