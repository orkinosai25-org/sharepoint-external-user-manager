# SharePoint External User Manager - Backend API

Multi-tenant SaaS backend built on Azure Functions v4 for managing SharePoint external users.

## Architecture

- **Runtime**: Node.js 18+ with TypeScript
- **Framework**: Azure Functions v4 (HTTP triggers)
- **Database**: Azure SQL Database with tenant isolation
- **Authentication**: Azure AD JWT tokens
- **API Style**: RESTful with JSON responses

## Project Structure

```
backend/
├── src/
│   ├── functions/          # Azure Functions (HTTP endpoints)
│   │   ├── tenant/         # Tenant management
│   │   ├── users/          # External user operations
│   │   ├── policies/       # Policy management
│   │   └── audit/          # Audit log access
│   ├── middleware/         # Request middleware
│   │   ├── auth.ts         # JWT authentication
│   │   ├── subscription.ts # Subscription enforcement
│   │   ├── errorHandler.ts# Global error handling
│   │   └── cors.ts         # CORS configuration
│   ├── services/          # Business logic services
│   │   ├── database.ts    # Database operations
│   │   ├── auditLogger.ts # Audit logging
│   │   └── graphClient.ts # Microsoft Graph integration
│   ├── models/            # TypeScript interfaces
│   │   ├── common.ts      # Common types
│   │   ├── tenant.ts      # Tenant models
│   │   ├── subscription.ts# Subscription models
│   │   ├── user.ts        # User models
│   │   ├── policy.ts      # Policy models
│   │   └── audit.ts       # Audit models
│   └── utils/             # Utilities
│       ├── config.ts      # Configuration management
│       ├── validation.ts  # Input validation
│       └── correlation.ts # Correlation IDs
├── host.json              # Azure Functions host config
├── local.settings.json    # Local environment variables
├── package.json           # Dependencies
└── tsconfig.json          # TypeScript configuration
```

## API Endpoints

### Tenant Management
- `POST /api/tenants/onboard` - Onboard new tenant
- `GET /api/tenants/me` - Get current tenant info
- `GET /api/tenants/subscription` - Get subscription status

### External User Management
- `GET /api/external-users` - List external users (with filters)
- `POST /api/external-users/invite` - Invite external user (future)
- `POST /api/external-users/remove` - Remove external user (future)

### Policy Management
- `GET /api/policies` - Get collaboration policies
- `PUT /api/policies` - Update policies

### Audit Logs
- `GET /api/audit` - Get audit logs (with filters)

## Setup

### Prerequisites
- Node.js 18+
- Azure Functions Core Tools v4
- Azure SQL Database
- Azure AD app registration

### Installation

```bash
cd backend
npm install
```

### Configuration

Copy `local.settings.json.example` to `local.settings.json` and configure:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "",
    "FUNCTIONS_WORKER_RUNTIME": "node",
    "SQL_SERVER": "your-server.database.windows.net",
    "SQL_DATABASE": "spexternal",
    "SQL_USER": "admin",
    "SQL_PASSWORD": "password",
    "AZURE_CLIENT_ID": "your-app-id",
    "AZURE_CLIENT_SECRET": "your-secret",
    "AZURE_TENANT_ID": "common",
    "AZURE_AD_AUDIENCE": "api://your-app-id",
    "CORS_ALLOWED_ORIGINS": "https://*.sharepoint.com",
    "ENABLE_GRAPH_INTEGRATION": "false",
    "ENABLE_AUDIT_LOGGING": "true"
  }
}
```

### Development

```bash
# Build TypeScript
npm run build

# Watch for changes
npm run watch

# Start local Functions runtime
npm start

# Run tests
npm test

# Lint code
npm run lint
```

## Authentication

All endpoints (except `/tenants/onboard`) require Azure AD Bearer token:

```http
GET /api/tenants/me
Authorization: Bearer eyJ0eXAiOiJKV1Qi...
X-Correlation-ID: 550e8400-e29b-41d4-a716-446655440000
```

## Multi-Tenant Isolation

- **Row-Level Security**: All queries filtered by `tenantId`
- **Tenant Context**: Resolved from JWT token `tid` claim
- **Database Isolation**: Enforced at application layer

## Subscription Tiers

| Tier | Max Users | Audit History | Features |
|------|-----------|---------------|----------|
| Free | 10 | 30 days | Basic |
| Pro | 100 | 90 days | Advanced policies, Export |
| Enterprise | Unlimited | 365 days | All features |

## Error Handling

Standard error response format:

```json
{
  "success": false,
  "error": {
    "code": "TENANT_NOT_FOUND",
    "message": "Tenant not found or not onboarded",
    "details": "No tenant found for Entra ID tenant: 12345...",
    "correlationId": "550e8400-e29b-41d4-a716-446655440000"
  }
}
```

## Deployment

### Azure Function App

```bash
# Build for production
npm run build

# Deploy to Azure
func azure functionapp publish <function-app-name>
```

### Environment Variables (Production)

Configure in Azure Portal under Function App > Configuration:
- `SQL_SERVER`
- `SQL_DATABASE`
- `SQL_USER`
- `SQL_PASSWORD`
- `AZURE_CLIENT_ID`
- `AZURE_CLIENT_SECRET`
- `AZURE_TENANT_ID`
- `KEY_VAULT_URL`
- `CORS_ALLOWED_ORIGINS`

## Database Schema

See `docs/saas/data-model.md` for complete schema definition.

Key tables:
- `Tenant` - Tenant registration
- `Subscription` - Subscription tiers and status
- `Policy` - Collaboration policies
- `AuditLog` - Immutable audit trail
- `UserAction` - External user actions

## Security

- **Authentication**: Azure AD JWT validation
- **Authorization**: Role-based access control
- **Encryption**: TLS 1.2+ in transit, AES-256 at rest
- **Secrets**: Azure Key Vault integration
- **Audit**: All operations logged
- **CORS**: Restricted to SharePoint domains

## Monitoring

- **Application Insights**: Automatic telemetry
- **Custom Events**: Subscription changes, tenant onboarding
- **Alerts**: Error rates, slow responses
- **Correlation IDs**: Request tracking across services

## Testing

```bash
# Run all tests
npm test

# Watch mode
npm run test:watch

# Coverage report
npm test -- --coverage
```

## License

MIT
