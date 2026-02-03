# Azure Deployment Guide

## Prerequisites

- Azure CLI installed and logged in
- Azure subscription with sufficient permissions
- Resource group created

## Quick Deploy

### 1. Create Resource Group (if not exists)

```bash
az group create \
  --name rg-spexternal-dev \
  --location eastus
```

### 2. Deploy Backend Infrastructure

```bash
az deployment group create \
  --resource-group rg-spexternal-dev \
  --template-file backend.bicep \
  --parameters environment=dev \
  --parameters appName=spexternal
```

### 3. Deploy Function App Code

```bash
cd ../backend
npm install
npm run build
func azure functionapp publish <function-app-name>
```

## Environment-Specific Deployments

### Development
```bash
az deployment group create \
  --resource-group rg-spexternal-dev \
  --template-file backend.bicep \
  --parameters environment=dev
```

### Staging
```bash
az deployment group create \
  --resource-group rg-spexternal-staging \
  --template-file backend.bicep \
  --parameters environment=staging
```

### Production
```bash
az deployment group create \
  --resource-group rg-spexternal-prod \
  --template-file backend.bicep \
  --parameters environment=prod
```

## Resources Created

The Bicep template creates:

1. **Azure Functions** - Serverless API hosting
2. **Cosmos DB** - Multi-tenant metadata storage
   - Database: `spexternal`
   - Containers: `Tenants`, `Subscriptions`, `GlobalAuditLogs`, `UsageMetrics`
3. **Storage Account** - Function App storage
4. **Application Insights** - Monitoring and telemetry
5. **Key Vault** - Secrets management
6. **App Service Plan** - Consumption plan for Functions

## Post-Deployment Configuration

### 1. Configure Azure AD Application

Create multi-tenant Azure AD app registration:

```bash
az ad app create \
  --display-name "SharePoint External User Manager API" \
  --sign-in-audience AzureADMultipleOrgs \
  --required-resource-accesses @manifest.json
```

### 2. Store Secrets in Key Vault

```bash
# Get Key Vault name from deployment output
KV_NAME=$(az deployment group show \
  --resource-group rg-spexternal-dev \
  --name backend \
  --query properties.outputs.keyVaultName.value -o tsv)

# Add Azure AD secrets
az keyvault secret set \
  --vault-name $KV_NAME \
  --name "AzureAD-ClientId" \
  --value "<your-client-id>"

az keyvault secret set \
  --vault-name $KV_NAME \
  --name "AzureAD-ClientSecret" \
  --value "<your-client-secret>"
```

### 3. Update Function App Settings

```bash
FUNC_NAME=$(az deployment group show \
  --resource-group rg-spexternal-dev \
  --name backend \
  --query properties.outputs.functionAppName.value -o tsv)

az functionapp config appsettings set \
  --name $FUNC_NAME \
  --resource-group rg-spexternal-dev \
  --settings \
    "AZURE_AD_CLIENT_ID=<your-client-id>" \
    "AZURE_AD_TENANT_ID=common"
```

## Monitoring

### View Application Insights

```bash
# Get App Insights instrumentation key
AI_KEY=$(az deployment group show \
  --resource-group rg-spexternal-dev \
  --name backend \
  --query properties.outputs.appInsightsInstrumentationKey.value -o tsv)

echo "App Insights Key: $AI_KEY"
```

Access dashboard: https://portal.azure.com → Application Insights

### View Function App Logs

```bash
func azure functionapp logstream $FUNC_NAME
```

## Troubleshooting

### Check Deployment Status

```bash
az deployment group show \
  --resource-group rg-spexternal-dev \
  --name backend \
  --query properties.provisioningState
```

### Validate Resources

```bash
az resource list \
  --resource-group rg-spexternal-dev \
  --output table
```

### Test API Endpoint

```bash
FUNC_URL=$(az deployment group show \
  --resource-group rg-spexternal-dev \
  --name backend \
  --query properties.outputs.functionAppUrl.value -o tsv)

curl "$FUNC_URL/api/health"
```

## Cleanup

To delete all resources:

```bash
az group delete \
  --name rg-spexternal-dev \
  --yes --no-wait
```

## CI/CD Integration

See `.github/workflows/deploy-backend.yml` for automated deployments.

## Cost Estimation

**Monthly costs (approximate):**

- Azure Functions (Consumption): $0-50
- Cosmos DB (Serverless): $25-100
- Storage Account: $5
- Application Insights: $10-50
- Key Vault: $5

**Total: ~$45-210/month** (varies with usage)

## Security Best Practices

1. ✅ All secrets in Key Vault
2. ✅ HTTPS only
3. ✅ TLS 1.2 minimum
4. ✅ Managed Identity enabled
5. ✅ RBAC for Key Vault
6. ✅ Soft delete enabled
7. ✅ Application Insights monitoring

## Support

For deployment issues:
- Check Azure Portal → Resource Group → Deployments
- Review Application Insights logs
- Contact: devops@spexternal.com
