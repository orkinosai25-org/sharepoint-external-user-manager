# ISSUE-10: Azure Deployment - Quick Reference

## What Was Implemented

### 1. Enhanced Bicep Infrastructure
**File**: `infra/bicep/main.bicep`

Added complete Azure infrastructure for SaaS platform:
- ✅ API App Service (Linux, .NET 8)
- ✅ Blazor Portal App Service (Linux, .NET 8)
- ✅ Separate App Service Plans (consumption for Functions, Basic/Standard for apps)
- ✅ Azure SQL with elastic pool
- ✅ Cosmos DB for metadata
- ✅ Key Vault with managed identity access
- ✅ Application Insights for monitoring
- ✅ CORS configuration for SPFx integration

### 2. Parameter Files
- `infra/bicep/parameters.dev.json` - Development environment config
- `infra/bicep/parameters.prod.json` - Production environment config

### 3. GitHub Actions Workflows

#### Build Workflows
- **build-api.yml** - Builds ASP.NET Core API
  - Triggers: Push to main/develop, PRs, manual
  - Runs on: API and shared code changes
  - Outputs: Build artifacts for deployment

- **build-blazor.yml** - Builds Blazor Portal
  - Triggers: Push to main/develop, PRs, manual
  - Runs on: Portal and shared code changes
  - Outputs: Build artifacts for deployment

#### Deployment Workflow
- **deploy-dev.yml** - Complete dev environment deployment
  - Triggers: Push to develop, manual
  - Jobs:
    1. Build all components (API, Portal, SPFx)
    2. Deploy infrastructure (optional, manual only)
    3. Deploy API to Azure
    4. Deploy Portal to Azure
    5. Run health checks
  - Requires secrets: AZURE_CREDENTIALS, API_APP_NAME, PORTAL_APP_NAME, etc.

### 4. Deployment Script
**File**: `deploy-dev.sh`

One-command deployment script:
```bash
./deploy-dev.sh
```

Features:
- Interactive prompts for SQL credentials
- Creates resource group
- Deploys Bicep infrastructure
- Builds and deploys API
- Builds and deploys Portal
- Builds SPFx package
- Outputs deployment information

### 5. Documentation
- **infra/bicep/README.md** - Infrastructure deployment guide
- **docs/DEPLOYMENT.md** - Complete deployment documentation

## Quick Start

### Deploy Everything (One Command)
```bash
./deploy-dev.sh
```

### Deploy Infrastructure Only
```bash
cd infra/bicep
az deployment group create \
  --resource-group spexternal-dev-rg \
  --template-file main.bicep \
  --parameters environment=dev \
  --parameters sqlAdminUsername=sqladmin \
  --parameters sqlAdminPassword='YourComplexPassword123!'
```

### Validate Bicep Template
```bash
cd infra/bicep
az bicep build --file main.bicep
```

## Required Secrets for GitHub Actions

Add these secrets to your GitHub repository:

### Azure Authentication
- `AZURE_CREDENTIALS` - Service principal credentials (JSON)

### Database
- `SQL_ADMIN_USERNAME` - SQL Server admin username
- `SQL_ADMIN_PASSWORD` - SQL Server admin password

### App Services
- `API_APP_NAME` - Name of API App Service (from deployment outputs)
- `PORTAL_APP_NAME` - Name of Portal App Service (from deployment outputs)
- `API_APP_URL` - URL of API App Service
- `PORTAL_APP_URL` - URL of Portal App Service

## Post-Deployment Checklist

After infrastructure deployment:

1. **Configure Entra ID**
   - Register API app
   - Register Portal app
   - Configure authentication

2. **Add Key Vault Secrets**
   - Stripe API key
   - Microsoft Graph credentials
   - Connection strings

3. **Run Database Migrations**
   ```bash
   cd src/api-dotnet/WebApi/SharePointExternalUserManager.Api
   dotnet ef database update
   ```

4. **Configure App Settings**
   - Entra ID tenant and client IDs
   - API base URL in Portal
   - CORS origins

5. **Deploy SPFx Package**
   - Upload to SharePoint App Catalog
   - Configure backend URL in SPFx

## Architecture

```
Azure Resources:
├── App Service Plan (Basic/Standard)
│   ├── API App Service (Linux, .NET 8)
│   └── Portal App Service (Linux, .NET 8)
├── App Service Plan (Consumption)
│   └── Function App (.NET 8 isolated)
├── Azure SQL Server
│   ├── Master Database
│   └── Elastic Pool (for tenant DBs)
├── Cosmos DB (Serverless)
│   ├── TenantMetadata container
│   └── AuditEvents container
├── Key Vault
├── Application Insights
└── Storage Account (for Functions)
```

## Resource Naming

Pattern: `{appName}-{type}-{environment}-{uniqueSuffix}`

Examples:
- API: `spexternal-api-dev-abc123`
- Portal: `spexternal-portal-dev-abc123`
- Key Vault: `spexternal-kv-dev-abc123`

## Health Endpoints

After deployment, verify:
- API: `https://{api-app-name}.azurewebsites.net/health`
- Portal: `https://{portal-app-name}.azurewebsites.net/`

## Cost Estimate

### Development (~£50/month)
- App Service Plan (B1): £40
- SQL Database (Basic): £4
- Other services: £6

### Production (~£200-270/month)
- App Service Plan (S1): £60
- SQL Database (Standard): £120
- Other services: £20-90

## Troubleshooting

### Bicep Deployment Fails
```bash
# Validate template
az bicep build --file infra/bicep/main.bicep

# Check deployment logs
az deployment group show \
  --resource-group <rg-name> \
  --name <deployment-name>
```

### App Service Won't Start
```bash
# Check logs
az webapp log tail --name <app-name> --resource-group <rg-name>

# Restart app
az webapp restart --name <app-name> --resource-group <rg-name>
```

### Key Vault Access Issues
```bash
# Grant access to app service
PRINCIPAL_ID=$(az webapp identity show \
  --name <app-name> \
  --resource-group <rg-name> \
  --query principalId -o tsv)

az keyvault set-policy \
  --name <kv-name> \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list
```

## Documentation

For detailed information, see:
- [Complete Deployment Guide](docs/DEPLOYMENT.md)
- [Bicep Infrastructure Guide](infra/bicep/README.md)
- [Main README](README.md)

## Status: ✅ COMPLETE

All requirements for ISSUE-10 have been implemented:
- ✅ Bicep templates for all Azure resources
- ✅ GitHub Actions workflows for CI/CD
- ✅ One-command deployment script
- ✅ Comprehensive documentation
- ✅ Parameter files for different environments
- ✅ Health check procedures
