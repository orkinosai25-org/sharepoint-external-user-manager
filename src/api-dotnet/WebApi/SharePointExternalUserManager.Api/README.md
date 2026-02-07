# SharePoint External User Manager - ASP.NET Core Web API

## Overview

This is the ASP.NET Core .NET 8 Web API implementation for the SharePoint External User Manager SaaS platform. It provides multi-tenant authentication, tenant context management, and core API endpoints with Azure SQL database support.

## Architecture

- **Framework**: ASP.NET Core .NET 8
- **Authentication**: Microsoft Identity Web (Entra ID JWT tokens)
- **Database**: Azure SQL with Entity Framework Core 8
- **Multi-Tenancy**: Tenant ID extracted from JWT claims and enforced at database level
- **Models**: Entity models with proper tenant isolation

## Prerequisites

- .NET 8 SDK
- Azure AD App Registration (for authentication)
- SQL Server (LocalDB, Express, Docker, or Azure SQL)
- EF Core CLI tools: `dotnet tool install --global dotnet-ef`

## Database Setup

### Connection String Configuration

#### Local Development (appsettings.Development.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=SharePointExternalUserManager;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

#### Production (appsettings.json)
Store connection string in Azure Key Vault or App Service Configuration. Do not commit production connection strings to source control.

### Database Schema

The database includes four tables with full tenant isolation:

1. **Tenants** - Organization/tenant records
2. **Clients** - Client spaces (with TenantId FK)
3. **Subscriptions** - Billing and plan tracking (with TenantId FK)
4. **AuditLogs** - Audit trail for all operations (with TenantId FK)

All child tables include `TenantId` foreign key for tenant isolation.

### Apply Migrations

#### Option 1: SQL Server LocalDB (Windows)
```bash
cd src/api-dotnet/WebApi/SharePointExternalUserManager.Api
dotnet ef database update
```

#### Option 2: Docker SQL Server (Cross-platform)
Start SQL Server:
```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourPassword123!" \
  -p 1433:1433 --name sqlserver \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

Update connection string in `appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=SharePointExternalUserManager;User Id=sa;Password=YourPassword123!;TrustServerCertificate=True"
  }
}
```

Apply migrations:
```bash
dotnet ef database update
```

#### Option 3: Azure SQL Database
Update connection string in Azure App Service Configuration, then:
```bash
dotnet ef database update
```

Or generate SQL script for Azure deployment:
```bash
dotnet ef migrations script --output migration.sql --idempotent
```

## Configuration

### appsettings.json

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "ClientId": "YOUR_CLIENT_ID_HERE",
    "TenantId": "common",
    "Audience": "YOUR_CLIENT_ID_HERE"
  },
  "ConnectionStrings": {
    "DefaultConnection": "PLACEHOLDER - Set via Azure App Service Configuration or Key Vault"
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

# Restore packages
dotnet restore

# Apply database migrations
dotnet ef database update

# Run the API
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
  "timestamp": "2026-02-06T12:15:32Z"
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

#### GET /clients
Returns all client spaces for the authenticated tenant.

#### POST /clients
Create a new client space with SharePoint site provisioning.

**Request Body**:
```json
{
  "clientReference": "CLIENT-001",
  "clientName": "Acme Corporation",
  "description": "Client space for Acme Corp legal matters"
}
```

#### GET /clients/{id}
Get details of a specific client.

#### GET /clients/{id}/external-users
List all external users (guests) for a client site.

#### POST /clients/{id}/external-users
Invite an external user to a client site with specified permissions.

**Request Body**:
```json
{
  "email": "partner@external.com",
  "displayName": "John Partner",
  "permissionLevel": "Read",
  "message": "Welcome to our collaboration space"
}
```

**Permission Levels**: "Read", "Edit", "Write", "Contribute"

#### DELETE /clients/{id}/external-users/{email}
Remove an external user's access from a client site.

For detailed API documentation, see [EXTERNAL_USER_API_DOCS.md](../../EXTERNAL_USER_API_DOCS.md).

## Multi-Tenant Isolation

The API enforces tenant isolation by:

1. Validating JWT tokens from Azure AD
2. Extracting `tid` (tenant ID) and `oid` (user ID) claims
3. Ensuring all database queries include TenantId filter
4. Foreign key constraints prevent cross-tenant data access
5. Cascade delete ensures proper tenant data cleanup

### Database Tenant Isolation
- All child tables have `TenantId` foreign key
- Indexes on `TenantId` for query performance
- Composite indexes include `TenantId` as first column

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

### View Swagger Documentation
Open browser: `http://localhost:5000/swagger`

## Project Structure

```
SharePointExternalUserManager.Api/
├── Controllers/
│   ├── HealthController.cs          # Health check endpoint
│   └── TenantsController.cs         # Tenant management endpoints
├── Data/
│   ├── Entities/
│   │   ├── TenantEntity.cs          # Tenant entity model
│   │   ├── ClientEntity.cs          # Client entity model
│   │   ├── SubscriptionEntity.cs    # Subscription entity model
│   │   └── AuditLogEntity.cs        # Audit log entity model
│   ├── Migrations/                  # EF Core migrations
│   │   └── 20260206121956_InitialCreate.cs
│   └── ApplicationDbContext.cs      # EF Core DbContext
├── Program.cs                        # Application startup and configuration
├── appsettings.json                 # Production configuration
├── appsettings.Development.json     # Development configuration
└── SharePointExternalUserManager.Api.csproj
```

## Entity Framework Core Commands

### View Database Info
```bash
dotnet ef dbcontext info
```

### Create New Migration
```bash
dotnet ef migrations add <MigrationName> --output-dir Data/Migrations
```

### Apply Migrations
```bash
dotnet ef database update
```

### Remove Last Migration (if not applied)
```bash
dotnet ef migrations remove
```

### Generate SQL Script
```bash
dotnet ef migrations script --output migration.sql --idempotent
```

## Development Notes

### Entity Models
Located in `/Data/Entities/`:
- **TenantEntity**: Organization/tenant records
- **ClientEntity**: Client spaces with TenantId FK
- **SubscriptionEntity**: Billing/plan tracking with TenantId FK
- **AuditLogEntity**: Audit trail with TenantId FK

### ApplicationDbContext
- Automatic timestamp management (CreatedDate, ModifiedDate)
- Comprehensive indexes for query performance
- Entity relationships with cascade delete
- Supports tenant-scoped queries

### Authentication Flow
1. Client obtains JWT token from Azure AD
2. Client sends request with `Authorization: Bearer <token>` header
3. Microsoft.Identity.Web middleware validates token
4. Controller extracts tenant/user claims from `User.Claims`
5. Response includes tenant-specific data

## Acceptance Criteria ✅

### ISSUE-02 (API Skeleton)
- [x] API runs locally
- [x] Entra ID JWT authentication configured
- [x] Multi-tenant support (common endpoint)
- [x] Health check endpoint (GET /health)
- [x] Tenant context endpoint (GET /tenants/me)
- [x] Tenant ID extracted from JWT claims
- [x] Tenant isolation enforced

### ISSUE-03 (Database)
- [x] Entity Framework Core integrated
- [x] Entity models created with proper annotations
- [x] ApplicationDbContext with DbSets and indexes
- [x] Migrations generated successfully
- [x] TenantId on all child tables
- [x] Indexes on TenantId + timestamps
- [x] Connection string configuration

## Security Considerations

### Connection Strings
- ❌ Never commit connection strings to source control
- ✅ Use Azure Key Vault for production
- ✅ Use App Service Configuration for secrets
- ✅ Use User Secrets for local development

### Tenant Isolation
- ✅ All queries must filter by TenantId
- ✅ Foreign key constraints enforce data integrity
- ✅ EF Core prevents SQL injection
- ✅ Cascade delete prevents orphaned records

## Troubleshooting

### Authentication Fails
- Verify Client ID in `appsettings.json`
- Ensure token audience matches Client ID
- Check token is not expired
- Verify `tid` and `oid` claims exist in token

### Database Connection Fails
- Check SQL Server is running
- Verify connection string is correct
- Ensure database exists (run `dotnet ef database update`)
- Check firewall allows connection to SQL Server

### Build Warnings
- `NU1902`: Microsoft.Identity.Web 3.6.0 vulnerability - inherited from Functions project reference

## Next Steps (ISSUE-04)

- Implement client space provisioning endpoints
- Add SharePoint site creation via Microsoft Graph
- Create ClientsController with CRUD operations
- Store provisioned client data in database

## Additional Resources

- [Entity Framework Core Documentation](https://docs.microsoft.com/en-us/ef/core/)
- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)
- [Microsoft Identity Web](https://docs.microsoft.com/en-us/azure/active-directory/develop/microsoft-identity-web)
- [Azure SQL Database](https://docs.microsoft.com/en-us/azure/azure-sql/)

