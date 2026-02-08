# Azure Deployment Guide

This guide provides step-by-step instructions for deploying the SharePoint External User Manager SaaS platform to Azure.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Quick Start - One-Command Deployment](#quick-start---one-command-deployment)
3. [Detailed Deployment Steps](#detailed-deployment-steps)
4. [Environment Configuration](#environment-configuration)
5. [Post-Deployment Setup](#post-deployment-setup)
6. [Troubleshooting](#troubleshooting)
7. [Rollback Procedures](#rollback-procedures)

## Prerequisites

Before deploying, ensure you have:

### Required Tools
- **Azure CLI** (version 2.50.0 or later)
  ```bash
  # Install Azure CLI
  curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
  
  # Verify installation
  az --version
  ```

- **Azure Account** with appropriate permissions
  - Subscription Contributor role
  - Ability to create Resource Groups
  - Ability to create Service Principals

- **.NET 8 SDK** (for local builds and verification)
  ```bash
  # Install .NET 8 SDK
  wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
  chmod +x ./dotnet-install.sh
  ./dotnet-install.sh --channel 8.0
  ```

- **Node.js 18.x** (for SPFx client builds)
  ```bash
  # Install Node.js 18.x
  curl -fsSL https://deb.nodesource.com/setup_18.x | sudo -E bash -
  sudo apt-get install -y nodejs
  ```

### Required Azure Resources
- Azure Subscription
- Resource Group (will be created if doesn't exist)
- Service Principal for GitHub Actions (optional, for CI/CD)

## Quick Start - One-Command Deployment

For a complete dev environment deployment:

```bash
# 1. Clone the repository
git clone https://github.com/orkinosai25-org/sharepoint-external-user-manager.git
cd sharepoint-external-user-manager

# 2. Login to Azure
az login

# 3. Set your subscription
az account set --subscription "<your-subscription-id>"

# 4. Run the deployment script
./deploy-dev.sh
```

The `deploy-dev.sh` script will:
1. Create a resource group (if it doesn't exist)
2. Deploy the Bicep infrastructure
3. Build and deploy the API
4. Build and deploy the Blazor Portal
5. Build the SPFx package
6. Display deployment information and next steps

## Detailed Deployment Steps

### Step 1: Prepare Your Environment

1. **Login to Azure:**
   ```bash
   az login
   az account set --subscription "<your-subscription-id>"
   ```

2. **Create a Resource Group:**
   ```bash
   az group create \
     --name spexternal-dev-rg \
     --location uksouth
   ```

3. **Set Environment Variables:**
   ```bash
   export RESOURCE_GROUP="spexternal-dev-rg"
   export LOCATION="uksouth"
   export ENVIRONMENT="dev"
   ```

### Step 2: Deploy Azure Infrastructure

1. **Navigate to the Bicep directory:**
   ```bash
   cd infra/bicep
   ```

2. **Validate the Bicep template:**
   ```bash
   az deployment group validate \
     --resource-group $RESOURCE_GROUP \
     --template-file main.bicep \
     --parameters environment=$ENVIRONMENT \
     --parameters sqlAdminUsername=sqladmin \
     --parameters sqlAdminPassword='YourComplexPassword123!'
   ```

3. **Deploy the infrastructure:**
   ```bash
   az deployment group create \
     --resource-group $RESOURCE_GROUP \
     --template-file main.bicep \
     --parameters environment=$ENVIRONMENT \
     --parameters sqlAdminUsername=sqladmin \
     --parameters sqlAdminPassword='YourComplexPassword123!' \
     --name main-deployment
   ```

4. **Capture deployment outputs:**
   ```bash
   az deployment group show \
     --resource-group $RESOURCE_GROUP \
     --name main-deployment \
     --query properties.outputs > deployment-outputs.json
   
   # Extract key values
   export API_APP_NAME=$(cat deployment-outputs.json | jq -r '.apiAppName.value')
   export PORTAL_APP_NAME=$(cat deployment-outputs.json | jq -r '.portalAppName.value')
   export KEY_VAULT_NAME=$(cat deployment-outputs.json | jq -r '.keyVaultName.value')
   ```

### Step 3: Build Applications

1. **Build the API:**
   ```bash
   cd ../../src/api-dotnet/WebApi/SharePointExternalUserManager.Api
   dotnet restore
   dotnet build --configuration Release
   dotnet publish --configuration Release --output ./publish
   ```

2. **Build the Blazor Portal:**
   ```bash
   cd ../../../portal-blazor/SharePointExternalUserManager.Portal
   dotnet restore
   dotnet build --configuration Release
   dotnet publish --configuration Release --output ./publish
   ```

3. **Build the SPFx Client:**
   ```bash
   cd ../../../client-spfx
   npm ci --no-optional --legacy-peer-deps
   npm run build
   npm run package-solution
   ```

### Step 4: Deploy Applications to Azure

1. **Deploy the API:**
   ```bash
   cd ../api-dotnet/WebApi/SharePointExternalUserManager.Api
   
   # Create a deployment package
   cd publish
   zip -r ../api-deploy.zip .
   cd ..
   
   # Deploy to Azure App Service
   az webapp deployment source config-zip \
     --resource-group $RESOURCE_GROUP \
     --name $API_APP_NAME \
     --src api-deploy.zip
   ```

2. **Deploy the Blazor Portal:**
   ```bash
   cd ../../../portal-blazor/SharePointExternalUserManager.Portal
   
   # Create a deployment package
   cd publish
   zip -r ../portal-deploy.zip .
   cd ..
   
   # Deploy to Azure App Service
   az webapp deployment source config-zip \
     --resource-group $RESOURCE_GROUP \
     --name $PORTAL_APP_NAME \
     --src portal-deploy.zip
   ```

### Step 5: Verify Deployment

1. **Check API health:**
   ```bash
   API_URL=$(az webapp show \
     --name $API_APP_NAME \
     --resource-group $RESOURCE_GROUP \
     --query defaultHostName -o tsv)
   
   curl https://$API_URL/health
   ```

2. **Check Portal health:**
   ```bash
   PORTAL_URL=$(az webapp show \
     --name $PORTAL_APP_NAME \
     --resource-group $RESOURCE_GROUP \
     --query defaultHostName -o tsv)
   
   curl -I https://$PORTAL_URL
   ```

3. **View application logs:**
   ```bash
   # API logs
   az webapp log tail --name $API_APP_NAME --resource-group $RESOURCE_GROUP
   
   # Portal logs
   az webapp log tail --name $PORTAL_APP_NAME --resource-group $RESOURCE_GROUP
   ```

## Environment Configuration

### Development Environment

```bash
export ENVIRONMENT="dev"
export RESOURCE_GROUP="spexternal-dev-rg"
export LOCATION="uksouth"
```

Configuration:
- App Service Plan: Basic (B1)
- SQL Database: Basic tier
- Cosmos DB: Serverless
- Auto-scaling: Disabled
- Custom domain: Not configured

### Production Environment

```bash
export ENVIRONMENT="prod"
export RESOURCE_GROUP="spexternal-prod-rg"
export LOCATION="uksouth"
```

Configuration:
- App Service Plan: Standard (S1) or higher
- SQL Database: Standard tier with elastic pool
- Cosmos DB: Serverless with geo-redundancy
- Auto-scaling: Enabled
- Custom domain: Configured
- SSL certificates: Configured

## Post-Deployment Setup

After infrastructure and applications are deployed:

### 1. Configure Entra ID (Azure AD)

Register applications for authentication:

```bash
# API App Registration
az ad app create \
  --display-name "SharePoint External User Manager API - $ENVIRONMENT" \
  --identifier-uris "api://spexternal-api-$ENVIRONMENT" \
  --sign-in-audience AzureADMultipleOrgs

# Portal App Registration
az ad app create \
  --display-name "SharePoint External User Manager Portal - $ENVIRONMENT" \
  --web-redirect-uris "https://$PORTAL_URL/signin-oidc" \
  --sign-in-audience AzureADMultipleOrgs
```

### 2. Configure Key Vault Secrets

Add required secrets to Key Vault:

```bash
# Stripe API Key
az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name StripeApiKey \
  --value "your-stripe-api-key"

# Microsoft Graph Client Secret
az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name GraphClientSecret \
  --value "your-graph-client-secret"

# Connection Strings
az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name SqlConnectionString \
  --value "your-sql-connection-string"
```

### 3. Run Database Migrations

```bash
# SSH into API App Service or run locally with connection string
cd src/api-dotnet/WebApi/SharePointExternalUserManager.Api

# Set connection string
export ConnectionStrings__DefaultConnection="your-connection-string"

# Run migrations
dotnet ef database update
```

### 4. Configure Application Settings

Update App Service configuration:

```bash
# API App Settings
az webapp config appsettings set \
  --resource-group $RESOURCE_GROUP \
  --name $API_APP_NAME \
  --settings \
    AzureAd__TenantId="your-tenant-id" \
    AzureAd__ClientId="your-api-client-id" \
    Stripe__ApiKey="@Microsoft.KeyVault(SecretUri=https://$KEY_VAULT_NAME.vault.azure.net/secrets/StripeApiKey/)"

# Portal App Settings
az webapp config appsettings set \
  --resource-group $RESOURCE_GROUP \
  --name $PORTAL_APP_NAME \
  --settings \
    AzureAd__TenantId="your-tenant-id" \
    AzureAd__ClientId="your-portal-client-id" \
    ApiBaseUrl="https://$API_URL"
```

## CI/CD with GitHub Actions

### Setup GitHub Secrets

1. **Create Azure Service Principal:**
   ```bash
   az ad sp create-for-rbac \
     --name "spexternal-github-actions" \
     --role contributor \
     --scopes /subscriptions/<subscription-id>/resourceGroups/$RESOURCE_GROUP \
     --sdk-auth
   ```

2. **Add secrets to GitHub repository:**
   - `AZURE_CREDENTIALS`: Output from service principal creation
   - `SQL_ADMIN_USERNAME`: SQL Server admin username
   - `SQL_ADMIN_PASSWORD`: SQL Server admin password
   - `API_APP_NAME`: Name of the API App Service
   - `PORTAL_APP_NAME`: Name of the Portal App Service
   - `API_APP_URL`: URL of the API App Service
   - `PORTAL_APP_URL`: URL of the Portal App Service

### Trigger Deployments

- **Automatic deployment to dev**: Push to `develop` branch
- **Manual infrastructure deployment**: Workflow dispatch with "deploy_infrastructure" option
- **Automatic builds**: Pull requests to `main` branch

## Troubleshooting

### Common Issues

#### 1. Deployment Fails with SQL Password Error

**Problem:** SQL password doesn't meet complexity requirements

**Solution:**
```bash
# Password must have:
# - At least 8 characters
# - Uppercase and lowercase letters
# - Numbers
# - Special characters
export SQL_PASSWORD='MyComplexP@ssw0rd123!'
```

#### 2. App Service Won't Start

**Problem:** Application fails to start after deployment

**Solution:**
```bash
# Check application logs
az webapp log tail --name $API_APP_NAME --resource-group $RESOURCE_GROUP

# Check Application Insights for errors
az monitor app-insights component show \
  --app $APP_INSIGHTS_NAME \
  --resource-group $RESOURCE_GROUP

# Restart the app service
az webapp restart --name $API_APP_NAME --resource-group $RESOURCE_GROUP
```

#### 3. Key Vault Access Denied

**Problem:** App Service can't access Key Vault secrets

**Solution:**
```bash
# Get the App Service managed identity
PRINCIPAL_ID=$(az webapp identity show \
  --name $API_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query principalId -o tsv)

# Grant Key Vault access
az keyvault set-policy \
  --name $KEY_VAULT_NAME \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list
```

#### 4. CORS Errors

**Problem:** SPFx client can't connect to API

**Solution:**
```bash
# Add SharePoint origin to API CORS policy
az webapp cors add \
  --name $API_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --allowed-origins "https://*.sharepoint.com"
```

### Diagnostic Commands

```bash
# View all resources in resource group
az resource list --resource-group $RESOURCE_GROUP --output table

# Check App Service status
az webapp show --name $API_APP_NAME --resource-group $RESOURCE_GROUP --query state

# View recent deployments
az deployment group list --resource-group $RESOURCE_GROUP --output table

# Check SQL Server firewall rules
az sql server firewall-rule list \
  --server $SQL_SERVER_NAME \
  --resource-group $RESOURCE_GROUP \
  --output table

# View Key Vault secrets (names only)
az keyvault secret list --vault-name $KEY_VAULT_NAME --output table
```

## Rollback Procedures

### Rollback Application Deployment

To rollback to a previous application version:

```bash
# List deployment slots
az webapp deployment list-publishing-profiles \
  --name $API_APP_NAME \
  --resource-group $RESOURCE_GROUP

# Swap deployment slots (if using slots)
az webapp deployment slot swap \
  --name $API_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --slot staging \
  --target-slot production
```

### Rollback Infrastructure Changes

To rollback infrastructure changes:

```bash
# List previous deployments
az deployment group list \
  --resource-group $RESOURCE_GROUP \
  --query "[].{Name:name, Timestamp:properties.timestamp, State:properties.provisioningState}" \
  --output table

# Redeploy a previous template
az deployment group create \
  --resource-group $RESOURCE_GROUP \
  --template-file main.bicep \
  --parameters @previous-parameters.json \
  --mode Complete
```

## Health Check Endpoints

After deployment, verify these endpoints:

- **API Health**: `https://{api-app-name}.azurewebsites.net/health`
- **Portal**: `https://{portal-app-name}.azurewebsites.net/`
- **Application Insights**: View in Azure Portal

## Support and Resources

- **Azure Documentation**: https://docs.microsoft.com/azure
- **Bicep Documentation**: https://docs.microsoft.com/azure/azure-resource-manager/bicep/
- **Project README**: [/README.md](/README.md)
- **Architecture Documentation**: [/ARCHITECTURE.md](/ARCHITECTURE.md)

## Next Steps

After successful deployment:

1. ✅ Configure custom domains (production only)
2. ✅ Configure SSL certificates
3. ✅ Set up monitoring alerts in Application Insights
4. ✅ Configure auto-scaling rules (production only)
5. ✅ Set up backup policies for SQL Database
6. ✅ Deploy SPFx package to SharePoint App Catalog
7. ✅ Test end-to-end functionality
8. ✅ Document tenant-specific configuration
