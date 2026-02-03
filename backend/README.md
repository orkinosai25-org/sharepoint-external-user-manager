# SharePoint External User Manager - Backend API

Multi-tenant SaaS backend API for managing external users and collaboration policies in SharePoint.

## Features

- 🔐 **JWT Authentication** with Microsoft Entra ID (Azure AD)
- 🏢 **Multi-Tenant Architecture** with row-level security
- 💳 **Subscription Management** (Free/Pro/Enterprise tiers)
- 📊 **Usage-Based Licensing** enforcement
- 🔍 **Comprehensive Audit Logging**
- 🚀 **Azure-Ready** with infrastructure-as-code

## Quick Start

### Prerequisites

- Node.js 18.x or higher
- Azure subscription (for production deployment)
- Microsoft Entra ID (Azure AD) app registration

### Local Development

1. Install dependencies:
   ```bash
   npm install
   ```

2. Copy environment variables:
   ```bash
   cp .env.example .env
   ```

3. Configure your `.env` file with Azure credentials

4. Start development server:
   ```bash
   npm run dev
   ```

   API will be available at `http://localhost:3000`

### Build for Production

```bash
npm run build
npm start
```

## API Endpoints

### Health Check
```
GET /health
```

### Tenant Management
```
POST   /api/v1/tenants/onboard      - Onboard new tenant
GET    /api/v1/tenants/:id          - Get tenant info
PUT    /api/v1/tenants/:id/settings - Update tenant settings
```

### User Management
```
GET    /api/v1/users          - List external users
POST   /api/v1/users/invite   - Invite external user (with quota check)
GET    /api/v1/users/:id      - Get user details
DELETE /api/v1/users/:id      - Revoke user access
```

### Policies
```
GET    /api/v1/policies        - List policies
POST   /api/v1/policies        - Create policy (Pro/Enterprise only)
PUT    /api/v1/policies/:id    - Update policy
DELETE /api/v1/policies/:id    - Delete policy
```

### Subscription
```
GET    /api/v1/subscription         - Get subscription details
POST   /api/v1/subscription/upgrade - Upgrade tier
POST   /api/v1/subscription/cancel  - Cancel subscription
```

### Audit Logs
```
GET    /api/v1/audit-logs - Query audit logs
```

## Architecture

### Technology Stack

- **Runtime**: Node.js 18.x LTS
- **Framework**: Express.js 4.x
- **Language**: TypeScript 5.x
- **Database**: Azure SQL Database
- **NoSQL**: Azure Cosmos DB (audit logs)
- **Authentication**: JWT + Microsoft Entra ID
- **Hosting**: Azure App Service

### Middleware Stack

1. **Authentication** - JWT validation
2. **Tenant Isolation** - Row-level security
3. **Licensing Enforcement** - Subscription tier gates
4. **Rate Limiting** - Tier-based limits
5. **Error Handling** - Standardized error responses

## Subscription Tiers

| Feature | Free | Pro | Enterprise |
|---------|------|-----|------------|
| External Users | 10 | 100 | Unlimited |
| Audit Logs Retention | 30 days | 1 year | Unlimited |
| API Rate Limit | 50/min | 200/min | 1000/min |
| Advanced Policies | ❌ | ✅ | ✅ |
| Support | Community | Email | Priority |

## Deployment

### Azure Deployment

1. **Deploy Infrastructure**:
   ```bash
   az deployment group create \
     --resource-group spexternal-rg \
     --template-file infrastructure/bicep/main.bicep \
     --parameters environment=prod
   ```

2. **Configure Secrets** in Azure Key Vault:
   - `AzureAdTenantId`
   - `AzureAdClientId`
   - `AzureAdClientSecret`
   - `SqlAdminPassword`

3. **Run Database Migrations**:
   ```bash
   # Connect to Azure SQL and run migration scripts
   sqlcmd -S <server>.database.windows.net -d spexternal -U sqladmin \
     -i database/migrations/001_initial_schema.sql
   ```

4. **Deploy Application** via GitHub Actions or manually:
   ```bash
   npm run build
   az webapp deployment source config-zip \
     --resource-group spexternal-rg \
     --name spexternal-api-prod \
     --src dist.zip
   ```

### CI/CD Pipeline

GitHub Actions workflow automatically:
- Runs tests and linting
- Builds the application
- Deploys to Dev/Staging/Production environments
- Runs database migrations

See `.github/workflows/backend-cicd.yml` for details.

## Security

- ✅ JWT token validation on all protected endpoints
- ✅ Row-level security for multi-tenant data isolation
- ✅ Secrets stored in Azure Key Vault
- ✅ TLS 1.3 for all communications
- ✅ Helmet.js for HTTP security headers
- ✅ Rate limiting to prevent abuse
- ✅ Comprehensive audit logging

## Monitoring

- **Application Insights** - Performance and error tracking
- **Health Checks** - `/health` endpoint
- **Audit Logs** - All operations logged to Cosmos DB
- **Alerts** - Configured for high error rates and performance issues

## Development

### Project Structure

```
backend/
├── src/
│   ├── config/           # Configuration files
│   ├── middleware/       # Express middleware
│   ├── routes/          # API route handlers
│   ├── services/        # Business logic (future)
│   ├── models/          # Data models (future)
│   ├── app.ts           # Express app setup
│   └── index.ts         # Entry point
├── database/
│   └── migrations/      # SQL migration scripts
├── infrastructure/
│   └── bicep/          # Azure infrastructure templates
├── tests/              # Test files (future)
└── package.json
```

### Running Tests

```bash
npm test
```

### Linting

```bash
npm run lint
```

## Contributing

1. Create a feature branch
2. Make your changes
3. Add tests
4. Submit a pull request

## License

MIT License - see LICENSE file for details

## Support

- **Documentation**: [https://docs.spexternal.com](https://docs.spexternal.com)
- **Email**: support@spexternal.com
- **Issues**: GitHub Issues

## Related Documentation

- [Architecture Overview](../docs/saas/architecture.md)
- [API Specification](../docs/saas/api-spec.md)
- [Security Design](../docs/saas/security.md)
- [Data Model](../docs/saas/data-model.md)
- [Marketplace Plan](../docs/saas/marketplace-plan.md)
