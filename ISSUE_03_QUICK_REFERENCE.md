# ISSUE-03 Quick Reference

## Database Setup Commands

### Apply Migrations (Local Development)
```bash
cd src/api-dotnet/WebApi/SharePointExternalUserManager.Api
dotnet ef database update
```

### Create New Migration
```bash
cd src/api-dotnet/WebApi/SharePointExternalUserManager.Api
dotnet ef migrations add <MigrationName> --output-dir Data/Migrations
```

### Generate SQL Script (for Azure deployment)
```bash
cd src/api-dotnet/WebApi/SharePointExternalUserManager.Api
dotnet ef migrations script --output migration.sql --idempotent
```

## Docker SQL Server Setup

### Start SQL Server Container
```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourPassword123!" \
  -p 1433:1433 --name sqlserver \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

### Connection String (appsettings.Development.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=SharePointExternalUserManager;User Id=sa;Password=YourPassword123!;TrustServerCertificate=True"
  }
}
```

## Database Schema

### Tables
- **Tenants** - Organization records (primary)
- **Clients** - Client spaces (TenantId FK)
- **Subscriptions** - Billing tracking (TenantId FK)
- **AuditLogs** - Audit trail (TenantId FK)

### Key Indexes
- All child tables have indexes on `TenantId`
- Composite indexes: `TenantId + CreatedDate`, `TenantId + Status`
- Filtered indexes on nullable columns

## Build Commands

### Build
```bash
cd src/api-dotnet/WebApi/SharePointExternalUserManager.Api
dotnet build
```

### Run API
```bash
cd src/api-dotnet/WebApi/SharePointExternalUserManager.Api
dotnet run
```

API available at: `http://localhost:5000`

## Testing Endpoints

### Health Check
```bash
curl http://localhost:5000/health
```

### Tenant Info (requires auth token)
```bash
curl -H "Authorization: Bearer $TOKEN" http://localhost:5000/tenants/me
```

## Documentation Files

- **ISSUE_03_IMPLEMENTATION_COMPLETE.md** - Full implementation guide
- **API README.md** - Database setup and API documentation
- **.github/ISSUE_03_SECURITY_SUMMARY.md** - Security scan results

## Next Steps

Ready for **ISSUE-04: Client Space Provisioning**
- Database infrastructure complete
- Tenant isolation enforced
- Audit logging ready
