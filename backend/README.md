# SharePoint External User Manager - Backend API

This is the SaaS backend API for SharePoint External User Manager, built with Azure Functions (Node.js/TypeScript).

## Architecture

- **Runtime**: Node.js 18 LTS
- **Framework**: Azure Functions v4
- **Language**: TypeScript
- **Database**: Azure Cosmos DB (shared metadata) + Azure SQL (tenant-specific data)
- **Authentication**: Azure AD (multi-tenant)
- **Hosting**: Azure Functions Consumption Plan

## Project Structure

```
backend/
├── tenants/                    # Tenant management endpoints
│   ├── onboard.ts             # POST /tenants/onboard
│   └── get-tenant.ts          # GET /tenants/me
├── external-users/            # External user management (TODO)
├── policies/                  # Policy management (TODO)
├── audit/                     # Audit log endpoints (TODO)
└── shared/                    # Shared utilities
    ├── auth/                  # Authentication & authorization
    │   ├── jwt-validator.ts
    │   ├── tenant-resolver.ts
    │   └── rbac.ts
    ├── middleware/            # Middleware functions
    │   ├── license-check.ts
    │   ├── rate-limit.ts
    │   └── error-handler.ts
    ├── storage/               # Data access layer
    │   ├── tenant-repository.ts
    │   ├── subscription-repository.ts
    │   └── audit-repository.ts
    ├── models/                # TypeScript interfaces
    │   └── types.ts
    └── utils/                 # Helper functions
        └── helpers.ts
```

## Getting Started

### Prerequisites

- Node.js 18+ 
- Azure Functions Core Tools v4
- Azure subscription with:
  - Cosmos DB account
  - Azure AD application (multi-tenant)

### Installation

```bash
cd backend
npm install
```

### Configuration

1. Copy `local.settings.json.example` (if exists) or create `local.settings.json`
2. Update the following environment variables:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "node",
    "AZURE_AD_TENANT_ID": "your-tenant-id",
    "AZURE_AD_CLIENT_ID": "your-client-id",
    "AZURE_AD_CLIENT_SECRET": "your-client-secret",
    "COSMOS_DB_ENDPOINT": "https://your-cosmos.documents.azure.com:443/",
    "COSMOS_DB_KEY": "your-cosmos-key",
    "APPLICATIONINSIGHTS_CONNECTION_STRING": "your-app-insights-connection"
  }
}
```

### Development

```bash
# Build TypeScript
npm run build

# Watch mode (auto-rebuild on changes)
npm run watch

# Start Azure Functions locally
npm start

# Run tests
npm test

# Lint code
npm run lint
```

The API will be available at: `http://localhost:7071/api`

## API Endpoints

### Tenant Management

#### POST /api/tenants/onboard
Onboard a new tenant to the platform.

**Request:**
```json
{
  "tenantId": "contoso.onmicrosoft.com",
  "adminEmail": "admin@contoso.com",
  "companyName": "Contoso Ltd",
  "subscriptionTier": "trial",
  "dataLocation": "eastus"
}
```

**Response (201):**
```json
{
  "success": true,
  "data": {
    "tenantId": "contoso.onmicrosoft.com",
    "status": "active",
    "subscriptionTier": "trial",
    "trialEndDate": "2024-03-20T00:00:00Z",
    "onboardingCompleted": true,
    "createdDate": "2024-02-20T15:30:00Z"
  }
}
```

#### GET /api/tenants/me
Get current tenant information.

**Headers:**
```
Authorization: Bearer {azure_ad_token}
X-Tenant-ID: {tenant_id}
```

**Response (200):**
```json
{
  "success": true,
  "data": {
    "tenantId": "contoso.onmicrosoft.com",
    "displayName": "Contoso Ltd",
    "status": "active",
    "subscriptionTier": "pro",
    "features": {
      "auditExport": true,
      "bulkOperations": true
    },
    "limits": {
      "maxExternalUsers": 500,
      "currentExternalUsers": 127
    }
  }
}
```

## Authentication

All endpoints (except onboarding) require Azure AD authentication:

```http
Authorization: Bearer {azure_ad_token}
X-Tenant-ID: {tenant_id}
```

### Token Requirements
- Valid Azure AD JWT token
- Audience: Your API app ID
- Issuer: Azure AD tenant
- Required claims: `tid`, `oid`, `email`/`upn`

## Rate Limiting

- **Standard endpoints**: 100 requests/minute per tenant
- **Bulk operations**: 10 requests/minute per tenant

Rate limit headers are included in responses:
```
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1708451600
```

## Error Handling

All errors follow a consistent format:

```json
{
  "success": false,
  "error": {
    "code": "ERROR_CODE",
    "message": "Human-readable message",
    "details": "Additional context",
    "correlationId": "uuid",
    "timestamp": "2024-02-20T15:30:00Z"
  }
}
```

### Error Codes
- `UNAUTHORIZED` (401): Missing or invalid auth token
- `FORBIDDEN` (403): Insufficient permissions
- `NOT_FOUND` (404): Resource doesn't exist
- `CONFLICT` (409): Resource already exists
- `VALIDATION_ERROR` (400): Invalid request body
- `SUBSCRIPTION_REQUIRED` (402): Active subscription required
- `RATE_LIMIT_EXCEEDED` (429): Too many requests
- `INTERNAL_ERROR` (500): Server error

## Subscription Tiers

### Trial
- Duration: 30 days
- Max external users: 25
- Max libraries: 10
- API calls: 10K/month
- Audit retention: 30 days

### Pro
- Price: $49/month
- Max external users: 500
- Max libraries: 100
- API calls: 100K/month
- Audit retention: 1 year

### Enterprise
- Price: $199/month
- Unlimited external users
- Unlimited libraries
- Unlimited API calls
- Audit retention: 7 years
- Priority support

## Testing

```bash
# Run all tests
npm test

# Run tests in watch mode
npm run test:watch

# Generate coverage report
npm run test:coverage
```

## Deployment

### Azure Deployment

1. **Create Azure Resources**:
   ```bash
   # Use Bicep template (see /deployment/backend.bicep)
   az deployment group create \
     --resource-group rg-spexternal \
     --template-file deployment/backend.bicep
   ```

2. **Deploy Function App**:
   ```bash
   func azure functionapp publish spexternal-backend
   ```

### CI/CD Pipeline

GitHub Actions workflow automatically deploys on push to main branch.
See `.github/workflows/deploy-backend.yml` (to be created).

## Security

- All secrets stored in Azure Key Vault
- Managed Identity for Azure resource access
- No hardcoded credentials
- TLS 1.2+ required for all connections
- JWT signature validation
- Tenant isolation enforced

## Monitoring

Application Insights tracks:
- Request/response times
- Error rates
- Dependency calls
- Custom events

Access dashboards at:
https://portal.azure.com → Application Insights → spexternal-backend

## Documentation

- [Architecture](../docs/saas/architecture.md)
- [Data Model](../docs/saas/data-model.md)
- [Security](../docs/saas/security.md)
- [API Specification](../docs/saas/api-spec.md)

## Support

For issues or questions:
- GitHub Issues: https://github.com/orkinosai25-org/sharepoint-external-user-manager/issues
- Email: support@spexternal.com

## License

MIT License - See LICENSE file for details
