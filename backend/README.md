# Backend - SharePoint External User Manager

## Overview

This directory contains the Azure Functions backend for the SharePoint External User Manager SaaS solution. The backend provides a RESTful API for managing external users, libraries, policies, and licensing.

## Technology Stack

- **Runtime:** .NET 8 (LTS)
- **Framework:** Azure Functions v4 (Isolated Worker)
- **Language:** C# 12
- **Authentication:** Microsoft Identity Web (Entra ID)
- **Database:** Azure SQL Database (multi-tenant) + Azure Cosmos DB
- **Key Management:** Azure Key Vault
- **Monitoring:** Application Insights

## Project Structure

```
backend/
├── src/
│   ├── Functions/
│   │   ├── TenantOnboarding/
│   │   │   └── RegisterTenantFunction.cs
│   │   ├── UserManagement/
│   │   │   └── GetLibrariesFunction.cs
│   │   ├── PolicyManagement/
│   │   ├── AuditLog/
│   │   └── Licensing/
│   ├── Middleware/
│   │   ├── AuthenticationMiddleware.cs
│   │   └── LicenseEnforcementMiddleware.cs
│   ├── Services/
│   │   └── LicensingService.cs
│   ├── Models/
│   │   ├── Tenant.cs
│   │   ├── ExternalUser.cs
│   │   ├── Library.cs
│   │   └── ApiResponse.cs
│   ├── Program.cs
│   └── SharePointExternalUserManager.Functions.csproj
├── tests/
└── README.md
```

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Azure Functions Core Tools v4](https://learn.microsoft.com/azure/azure-functions/functions-run-local)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/)
- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli)
- Azure subscription for deployment

### Local Development

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd sharepoint-external-user-manager/backend/src
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Configure local settings**
   
   Create a `local.settings.json` file (not committed to git):
   ```json
   {
     "IsEncrypted": false,
     "Values": {
       "AzureWebJobsStorage": "UseDevelopmentStorage=true",
       "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
       "EntraId__ClientId": "<your-entra-id-client-id>",
       "EntraId__TenantId": "<your-entra-id-tenant-id>",
       "KeyVaultUri": "https://<your-keyvault>.vault.azure.net/",
       "SqlServer__MasterConnection": "Server=localhost;Database=master-db;Integrated Security=true;",
       "CosmosDb__Endpoint": "https://<your-cosmos-account>.documents.azure.com:443/",
       "CosmosDb__DatabaseName": "SharedMetadata"
     }
   }
   ```

4. **Run locally**
   ```bash
   func start
   ```
   
   Or using Visual Studio:
   - Open `SharePointExternalUserManager.Functions.csproj`
   - Press F5 to start debugging

5. **Test endpoints**
   ```bash
   # Register a new tenant
   curl -X POST http://localhost:7071/api/v1/tenants/register \
     -H "Content-Type: application/json" \
     -d '{
       "tenantDomain": "contoso.com",
       "displayName": "Contoso Corporation",
       "adminEmail": "admin@contoso.com"
     }'
   
   # Get libraries (requires auth token)
   curl -X GET http://localhost:7071/api/v1/libraries \
     -H "Authorization: Bearer <token>"
   ```

## API Endpoints

See [API Specification](/docs/saas/api-spec.md) for detailed endpoint documentation.

### Key Endpoints

- `POST /api/v1/tenants/register` - Register new tenant
- `POST /api/v1/tenants/verify` - Verify admin identity
- `GET /api/v1/libraries` - List libraries
- `POST /api/v1/libraries` - Create library
- `GET /api/v1/libraries/{id}/users` - List external users
- `POST /api/v1/libraries/{id}/users` - Invite external user
- `GET /api/v1/subscription` - Get subscription status

## Authentication

All endpoints (except tenant registration) require authentication via Azure AD Bearer tokens.

**Token Format:**
```
Authorization: Bearer <jwt_token>
X-Tenant-ID: <tenant_guid>
```

The authentication middleware validates:
- Token signature (Azure AD)
- Token expiration
- Required claims (tid, oid, upn)
- Tenant context

## Licensing Enforcement

The licensing middleware enforces subscription tiers and feature gates:

### Subscription Tiers

| Tier | Max Users | Max Libraries | Features |
|------|-----------|---------------|----------|
| Free | 5 | 3 | Basic management |
| Pro | 50 | 25 | Advanced policies, audit logs |
| Enterprise | Unlimited | Unlimited | All features, custom integrations |

### Feature Gates

Functions are automatically gated based on subscription tier:
- Free tier: Basic CRUD operations
- Pro tier: Policies, advanced audit
- Enterprise tier: Bulk operations, custom integrations

## Deployment

### Infrastructure Deployment (Bicep)

1. **Create resource group**
   ```bash
   az group create --name rg-spexternal-dev --location eastus
   ```

2. **Deploy infrastructure**
   ```bash
   cd infrastructure/bicep
   az deployment group create \
     --resource-group rg-spexternal-dev \
     --template-file main.bicep \
     --parameters environment=dev \
                  sqlAdminUsername=sqladmin \
                  sqlAdminPassword='<strong-password>'
   ```

3. **Retrieve outputs**
   ```bash
   az deployment group show \
     --resource-group rg-spexternal-dev \
     --name main \
     --query properties.outputs
   ```

### Function App Deployment (GitHub Actions)

The backend is automatically deployed via GitHub Actions when changes are pushed to `main` or `develop` branches.

**Required Secrets:**
- `AZURE_CREDENTIALS`: Service principal credentials (JSON)

**Manual deployment:**
```bash
cd backend/src
func azure functionapp publish <function-app-name>
```

## Monitoring

### Application Insights

- **Telemetry:** Automatic request/response logging
- **Custom Events:** Business events tracked
- **Errors:** Exceptions with stack traces
- **Performance:** Response time metrics

**Query Examples:**
```kusto
// Failed requests
requests
| where success == false
| project timestamp, name, resultCode, duration

// Licensing enforcement failures
customEvents
| where name == "LicenseEnforcementFailed"
| project timestamp, tenant = tostring(customDimensions.TenantId), reason = tostring(customDimensions.Reason)
```

### Alerts

Configured alerts:
- High error rate (>5% in 5 minutes)
- Slow response time (p95 > 2s)
- Subscription expiration warnings
- Database connection failures

## Security

### Key Vault Integration

All secrets are stored in Azure Key Vault:
- SQL connection strings (per tenant)
- Entra ID client secrets
- API keys
- Encryption keys

**Access:**
- Function App uses Managed Identity
- Automatic secret rotation supported
- Audit logs for all access

### Data Protection

- **Encryption at rest:** TDE for SQL, automatic for Cosmos DB
- **Encryption in transit:** TLS 1.2+ enforced
- **Tenant isolation:** Database-per-tenant model
- **Input validation:** All inputs sanitized

## Testing

### Unit Tests

```bash
cd backend/tests
dotnet test
```

### Integration Tests

```bash
# Set up test environment
export TEST_TENANT_ID=<test-tenant-id>
export TEST_CONNECTION_STRING=<test-connection-string>

# Run integration tests
dotnet test --filter Category=Integration
```

### Load Testing

Use Azure Load Testing or Apache JMeter:
- Target: 1000 requests/minute
- Acceptable response time: <500ms (p95)
- Error rate: <1%

## Troubleshooting

### Common Issues

**Issue: Authentication middleware fails**
- Check Entra ID app registration
- Verify client ID and tenant ID in configuration
- Ensure token audience matches

**Issue: License enforcement blocks all requests**
- Verify tenant exists in database
- Check subscription status in Tenants table
- Review Application Insights logs

**Issue: Database connection timeout**
- Check SQL Server firewall rules
- Verify Managed Identity has database access
- Test connection string in Key Vault

## Contributing

1. Create a feature branch
2. Make changes with tests
3. Run linters and tests locally
4. Submit pull request
5. CI/CD will automatically deploy to dev environment

## License

MIT License - see [LICENSE](/LICENSE) file

## Support

- **Documentation:** [/docs/saas/](/docs/saas/)
- **Issues:** Create GitHub issue
- **Email:** support@spexternal.com
