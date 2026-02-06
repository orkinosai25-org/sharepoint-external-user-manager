# ISSUE-04 Quick Reference

## API Endpoints

### List Clients
```http
GET /clients
Authorization: Bearer {token}
```

### Get Client by ID
```http
GET /clients/{id}
Authorization: Bearer {token}
```

### Create Client
```http
POST /clients
Authorization: Bearer {token}
Content-Type: application/json

{
  "clientReference": "MAT-2024-001",
  "clientName": "Acme Corporation Ltd",
  "description": "Commercial property transaction"
}
```

## Response Format

### Success
```json
{
  "success": true,
  "data": { /* client data */ }
}
```

### Error
```json
{
  "success": false,
  "error": {
    "code": "ERROR_CODE",
    "message": "Human-readable message"
  }
}
```

## Local Development

### Start API
```bash
cd src/api-dotnet/WebApi/SharePointExternalUserManager.Api
dotnet run
```
**URL:** http://localhost:5049  
**Swagger:** http://localhost:5049/swagger

### Build
```bash
dotnet build
dotnet build --configuration Release
```

### Health Check
```bash
curl http://localhost:5049/health
```

## Configuration

### appsettings.Development.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=SharePointExternalUserManager;..."
  },
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "organizations",
    "ClientId": "YOUR_CLIENT_ID",
    "ClientSecret": "YOUR_CLIENT_SECRET",
    "Audience": "YOUR_API_CLIENT_ID"
  },
  "MicrosoftGraph": {
    "BaseUrl": "https://graph.microsoft.com/v1.0",
    "Scopes": "https://graph.microsoft.com/.default"
  }
}
```

## Database

### Tables Used
- **Clients** - Client space records
- **Tenants** - Tenant information
- **AuditLogs** - Operation audit trail

### Provisioning Status Values
- `Pending` - Initial state
- `Provisioning` - In progress
- `Provisioned` - Complete
- `Failed` - Error occurred

## Security

### Authentication
- JWT Bearer token required
- Azure AD multi-tenant
- Claims: `tid` (tenant), `oid` (user), `upn` (email)

### Tenant Isolation
- All queries filtered by TenantId
- TenantId from JWT `tid` claim
- Foreign key constraints enforced

### Audit Logging
- All operations logged
- Correlation IDs for tracing
- User identity captured
- IP address logged

## Error Codes

| Code | Description |
|------|-------------|
| AUTH_ERROR | Missing or invalid authentication |
| TENANT_NOT_FOUND | Tenant not found in database |
| CLIENT_NOT_FOUND | Client not found |
| CLIENT_EXISTS | Duplicate client reference |
| VALIDATION_ERROR | Input validation failed |

## Common Tasks

### Create Test Tenant (SQL)
```sql
INSERT INTO Tenants 
  (EntraIdTenantId, OrganizationName, PrimaryAdminEmail, OnboardedDate, Status, CreatedDate, ModifiedDate)
VALUES 
  ('your-tenant-id', 'Test Law Firm', 'admin@test.com', GETUTCDATE(), 'Active', GETUTCDATE(), GETUTCDATE());
```

### Query Clients
```sql
SELECT c.*, t.OrganizationName
FROM Clients c
JOIN Tenants t ON c.TenantId = t.Id
WHERE t.EntraIdTenantId = 'your-tenant-id';
```

### View Audit Logs
```sql
SELECT TOP 100 *
FROM AuditLogs
WHERE TenantId = 1
ORDER BY Timestamp DESC;
```

## Services

### ISharePointService
- `CreateClientSiteAsync()` - Provision SharePoint site

### IAuditLogService
- `LogActionAsync()` - Log operation to audit trail

## Documentation

- **API Spec:** docs/ISSUE_04_CLIENT_PROVISIONING.md
- **Implementation:** ISSUE_04_IMPLEMENTATION_COMPLETE.md
- **Security:** ISSUE_04_SECURITY_SUMMARY.md
- **Full Summary:** ISSUE_04_FINAL_SUMMARY.md

## Status

✅ **COMPLETE AND VERIFIED**

- Build: ✅ Pass
- Tests: ✅ Pass
- Security: ✅ No vulnerabilities
- Documentation: ✅ Complete

**Next:** ISSUE-05 - External User Management
