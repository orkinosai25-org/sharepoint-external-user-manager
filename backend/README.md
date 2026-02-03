# Backend API

SharePoint External User Manager - SaaS Backend

## Structure

```
backend/
├── src/
│   ├── functions/          # Azure Function endpoints
│   ├── middleware/         # Authentication & subscription checks
│   ├── services/           # Business logic layer
│   ├── models/             # TypeScript interfaces
│   ├── database/           # Data access layer
│   └── utils/              # Shared utilities
├── database/
│   └── migrations/         # SQL schema migrations
└── deploy/                 # Azure deployment templates
```

## Getting Started

### Prerequisites

- Node.js 18.x or 20.x
- Azure Functions Core Tools v4
- SQL Server (local or Azure)

### Installation

```bash
npm install
```

### Configuration

Copy `local.settings.json.example` to `local.settings.json` and update values:

```json
{
  "Values": {
    "AZURE_AD_CLIENT_ID": "your-client-id",
    "DATABASE_SERVER": "localhost",
    "DATABASE_NAME": "spexternal_dev"
  }
}
```

### Development

```bash
# Build TypeScript
npm run build

# Watch mode
npm run watch

# Start Functions locally
npm start
```

### Testing

```bash
# Run tests
npm test

# Watch mode
npm run test:watch
```

## API Endpoints

See `/docs/saas/api-spec.md` for complete API documentation.

### Core Endpoints

- `POST /api/tenants/onboard` - Onboard new tenant
- `GET /api/tenants/me` - Get current tenant info
- `GET /api/external-users` - List external users
- `GET /api/policies` - Get policies
- `PUT /api/policies` - Update policies  
- `GET /api/audit` - Get audit logs

## Database

### Migrations

Run database migrations:

```bash
cd database/migrations
sqlcmd -S localhost -d spexternal_dev -i 001_initial_schema.sql
```

## Deployment

### Azure Deployment

```bash
# Deploy using Azure CLI
func azure functionapp publish your-function-app-name
```

See `/docs/saas/architecture.md` for deployment architecture.

## Security

- All endpoints require Azure AD authentication
- Tenant isolation via row-level security
- Secrets stored in Azure Key Vault
- Input validation on all endpoints

See `/docs/saas/security.md` for security details.

## License

MIT
