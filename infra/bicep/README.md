# Azure Infrastructure as Code - Bicep Templates

This directory contains the Azure Bicep templates for deploying the SharePoint External User Manager SaaS platform.

## Architecture Overview

The infrastructure includes:

- **API App Service**: ASP.NET Core Web API (.NET 8) on Linux
- **Blazor Portal App Service**: Blazor Web App (.NET 8) on Linux
- **Azure Functions**: Serverless functions for background processing
- **Azure SQL Database**: Multi-tenant database with elastic pool
- **Cosmos DB**: Document database for metadata and audit logs
- **Key Vault**: Secrets management
- **Application Insights**: Monitoring and telemetry
- **Storage Account**: For Azure Functions runtime

## Prerequisites

Before deploying:

1. **Azure CLI** installed and authenticated
   ```bash
   az login
   az account set --subscription <subscription-id>
   ```

2. **Resource Group** created
   ```bash
   az group create --name <resource-group-name> --location <location>
   ```

3. **Key Vault** with secrets (for production):
   - `sql-admin-username`: SQL Server administrator username
   - `sql-admin-password`: SQL Server administrator password (complex password required)

## Deployment

### Development Environment

For development, you can pass parameters directly:

```bash
cd infra/bicep

az deployment group create \
  --resource-group <resource-group-name> \
  --template-file main.bicep \
  --parameters environment=dev \
  --parameters sqlAdminUsername=<admin-username> \
  --parameters sqlAdminPassword=<admin-password>
```

### Production Environment

For production, use parameter files with Key Vault references:

1. Update `parameters.prod.json` with your Key Vault details
2. Deploy:

```bash
cd infra/bicep

az deployment group create \
  --resource-group <resource-group-name> \
  --template-file main.bicep \
  --parameters @parameters.prod.json
```

### Validate Before Deployment

Always validate the template before deploying:

```bash
az deployment group validate \
  --resource-group <resource-group-name> \
  --template-file main.bicep \
  --parameters environment=dev \
  --parameters sqlAdminUsername=<admin-username> \
  --parameters sqlAdminPassword=<admin-password>
```

## Outputs

After deployment, the following outputs are available:

- `apiAppName`: Name of the API App Service
- `apiAppUrl`: URL of the API App Service
- `portalAppName`: Name of the Blazor Portal App Service
- `portalAppUrl`: URL of the Blazor Portal
- `functionAppName`: Name of the Azure Function App
- `functionAppUrl`: URL of the Azure Function App
- `keyVaultName`: Name of the Key Vault
- `sqlServerName`: Name of the SQL Server
- `cosmosDbAccountName`: Name of the Cosmos DB account
- `appInsightsInstrumentationKey`: Application Insights instrumentation key
- `appInsightsConnectionString`: Application Insights connection string

To retrieve outputs:

```bash
az deployment group show \
  --resource-group <resource-group-name> \
  --name <deployment-name> \
  --query properties.outputs
```

## Post-Deployment Configuration

After infrastructure deployment:

1. **Configure Entra ID (Azure AD)**:
   - Register applications for API and Portal
   - Configure authentication settings
   - Add redirect URIs

2. **Configure SQL Database**:
   - Run EF Core migrations
   - Seed initial data

3. **Configure Key Vault Secrets**:
   - Add Stripe API keys
   - Add Microsoft Graph credentials
   - Add connection strings

4. **Deploy Application Code**:
   - Use GitHub Actions workflows to deploy applications
   - Verify health endpoints

## Environment-Specific Settings

### Development (`dev`)
- Basic tier App Service Plan (B1)
- Basic SQL Database
- No geo-redundancy
- Relaxed CORS policies for local development

### Production (`prod`)
- Standard tier App Service Plan (S1)
- Standard SQL Database with elastic pool
- Geo-redundancy enabled
- Strict CORS policies
- Custom domains configured

## Resource Naming Convention

Resources are named using the pattern: `{appName}-{resource-type}-{environment}-{unique-suffix}`

Example for dev environment:
- API App Service: `spexternal-api-dev-abc123`
- Portal App Service: `spexternal-portal-dev-abc123`
- Key Vault: `spexternal-kv-dev-abc123`
- SQL Server: `spexternal-sql-dev-abc123`

## Security

- All resources use TLS 1.2 or higher
- Managed identities for service-to-service authentication
- Key Vault for secrets management
- HTTPS-only enforced on all web apps
- Network access controlled via CORS policies
- SQL Server firewall rules configured for Azure services

## Cost Estimation

### Development Environment (per month)
- App Service Plan (B1): ~£40
- SQL Database (Basic): ~£4
- Cosmos DB (Serverless): ~£2-10 (usage-based)
- Function App (Consumption): ~£0-5 (usage-based)
- Application Insights: ~£0-5 (usage-based)
- Key Vault: ~£0.50
- Storage Account: ~£0.50
- **Total: ~£47-65/month**

### Production Environment (per month)
- App Service Plan (S1): ~£60
- SQL Database (Standard with elastic pool): ~£120
- Cosmos DB (Serverless): ~£10-50 (usage-based)
- Function App (Consumption): ~£5-20 (usage-based)
- Application Insights: ~£5-20 (usage-based)
- Key Vault: ~£0.50
- Storage Account: ~£1
- **Total: ~£201-271/month**

*Costs are estimates and may vary based on actual usage and region*

## Troubleshooting

### Deployment Fails with "Location not available"
- Ensure the resource type is available in your chosen region
- Try a different Azure region

### SQL Password Complexity Error
- Password must be at least 8 characters
- Must contain uppercase, lowercase, numbers, and special characters

### Key Vault Access Issues
- Ensure your Azure CLI user has "Key Vault Secrets Officer" role
- Verify managed identities are granted appropriate permissions

### App Service Won't Start
- Check Application Insights for errors
- Verify environment variables are configured
- Check logs: `az webapp log tail --name <app-name> --resource-group <rg-name>`

## Support

For issues or questions:
- Check the [main README](/README.md)
- Review [deployment documentation](/docs)
- Check Application Insights for runtime errors
