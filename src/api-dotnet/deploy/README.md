# Azure Deployment

Bicep templates and deployment instructions for the SharePoint External User Manager SaaS backend.

## Quick Deploy

```bash
# Login
az login

# Create resource group
az group create --name rg-spexternal-dev --location eastus2

# Deploy infrastructure
az deployment group create \
  --resource-group rg-spexternal-dev \
  --template-file main.bicep \
  --parameters parameters.dev.json

# Deploy function app code
cd ..
npm run build
func azure functionapp publish spexternal-func-dev
```

## Resources Created

- Azure Functions (Consumption Plan)
- Application Insights
- Azure SQL Database
- Key Vault
- Storage Account

## Cost Estimate

**Dev**: ~$20/month
**Prod**: ~$100/month

See main README for detailed deployment guide.
