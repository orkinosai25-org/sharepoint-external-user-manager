# ClientSpace MVP Deployment Runbook

**Complete deployment and operational guide for ClientSpace MVP**

This runbook provides step-by-step instructions for deploying, configuring, monitoring, and troubleshooting ClientSpace in production and development environments.

## Table of Contents

1. [Overview](#overview)
2. [Pre-Deployment Checklist](#pre-deployment-checklist)
3. [Azure Infrastructure Setup](#azure-infrastructure-setup)
4. [Application Deployment](#application-deployment)
5. [Post-Deployment Configuration](#post-deployment-configuration)
6. [Health Checks and Validation](#health-checks-and-validation)
7. [Monitoring Setup](#monitoring-setup)
8. [Troubleshooting Guide](#troubleshooting-guide)
9. [Rollback Procedures](#rollback-procedures)
10. [Maintenance Procedures](#maintenance-procedures)
11. [Support Escalation](#support-escalation)

---

## Overview

ClientSpace consists of three main components:

```
┌─────────────────────────────────────────────────────────┐
│  ClientSpace Architecture                               │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  ┌──────────────────┐    ┌──────────────────┐          │
│  │  Blazor Portal   │    │   .NET API       │          │
│  │  (App Service)   │◄──►│  (App Service)   │          │
│  └──────────────────┘    └──────────────────┘          │
│          │                        │                     │
│          │                        │                     │
│          ▼                        ▼                     │
│  ┌──────────────────────────────────────┐              │
│  │     Azure SQL Database               │              │
│  │     (Multi-tenant)                   │              │
│  └──────────────────────────────────────┘              │
│          │                        │                     │
│          │                        │                     │
│          ▼                        ▼                     │
│  ┌──────────────┐    ┌──────────────────┐              │
│  │  Key Vault   │    │ App Insights     │              │
│  └──────────────┘    └──────────────────┘              │
│                                                         │
└─────────────────────────────────────────────────────────┘

Optional: SPFx Web Parts (customer SharePoint)
```

### Deployment Environments

- **Development**: `spexternal-dev-rg` (uksouth)
- **Production**: `spexternal-prod-rg` (uksouth)

---

## Pre-Deployment Checklist

### Required Tools

- [ ] **Azure CLI** (v2.50.0+)
  ```bash
  az --version
  ```

- [ ] **.NET 8 SDK**
  ```bash
  dotnet --version  # Should show 8.0.x
  ```

- [ ] **Node.js 18.x** (for SPFx)
  ```bash
  node --version  # Should show v18.x.x
  ```

- [ ] **Git**
  ```bash
  git --version
  ```

### Azure Prerequisites

- [ ] **Azure Subscription** with Contributor role
- [ ] **Resource Group** created or permission to create
- [ ] **Service Principal** for CI/CD (optional but recommended)
- [ ] **Domain name** for production (optional but recommended)

### Credentials Required

Collect these before deployment:

- [ ] **Azure AD Tenant ID**
- [ ] **Azure AD Client ID** (for API)
- [ ] **Azure AD Client Secret** (for API)
- [ ] **Portal Client ID** (for Portal)
- [ ] **Portal Client Secret** (for Portal)
- [ ] **Stripe API Key** (for billing)
- [ ] **SQL Admin Password** (secure, 12+ chars)
- [ ] **Application Insights Instrumentation Key** (or auto-generated)

### Security Checklist

- [ ] All secrets stored in Azure Key Vault
- [ ] No secrets in source code or config files
- [ ] GitHub secrets configured for CI/CD
- [ ] SQL firewall rules configured
- [ ] CORS policies defined
- [ ] SSL/TLS enforced on all endpoints

---

## Azure Infrastructure Setup

### Option 1: Quick Deployment Script

Use the provided script for automated deployment:

```bash
# Clone repository
git clone https://github.com/orkinosai25-org/sharepoint-external-user-manager.git
cd sharepoint-external-user-manager

# Login to Azure
az login
az account set --subscription "<your-subscription-id>"

# Run deployment script
./deploy-dev.sh
```

The script will:
1. Create resource group
2. Deploy Bicep infrastructure
3. Build and deploy API
4. Build and deploy Portal
5. Configure initial settings

### Option 2: Manual Infrastructure Deployment

For more control, deploy manually:

#### Step 1: Create Resource Group

```bash
# Development
az group create \
  --name spexternal-dev-rg \
  --location uksouth

# Production
az group create \
  --name spexternal-prod-rg \
  --location uksouth
```

#### Step 2: Deploy Bicep Template

```bash
cd infra/bicep

# Validate template
az deployment group validate \
  --resource-group spexternal-dev-rg \
  --template-file main.bicep \
  --parameters environment=dev \
  --parameters sqlAdminPassword='YourSecureP@ssw0rd!'

# Deploy infrastructure
az deployment group create \
  --resource-group spexternal-dev-rg \
  --template-file main.bicep \
  --parameters environment=dev \
  --parameters sqlAdminPassword='YourSecureP@ssw0rd!' \
  --name infrastructure-deployment
```

#### Step 3: Capture Deployment Outputs

```bash
# Save outputs to file
az deployment group show \
  --resource-group spexternal-dev-rg \
  --name infrastructure-deployment \
  --query properties.outputs > deployment-outputs.json

# Extract key values
export API_APP_NAME=$(jq -r '.apiAppName.value' deployment-outputs.json)
export PORTAL_APP_NAME=$(jq -r '.portalAppName.value' deployment-outputs.json)
export SQL_SERVER=$(jq -r '.sqlServerName.value' deployment-outputs.json)
export KEY_VAULT=$(jq -r '.keyVaultName.value' deployment-outputs.json)
export APP_INSIGHTS=$(jq -r '.appInsightsName.value' deployment-outputs.json)
```

---

## Application Deployment

### Deploy API

```bash
cd src/api-dotnet/WebApi/SharePointExternalUserManager.Api

# Restore dependencies
dotnet restore

# Build for production
dotnet build --configuration Release

# Publish
dotnet publish --configuration Release --output ./publish

# Create deployment package
cd publish
zip -r ../api-deploy.zip .
cd ..

# Deploy to Azure
az webapp deployment source config-zip \
  --resource-group spexternal-dev-rg \
  --name $API_APP_NAME \
  --src api-deploy.zip

# Verify deployment
az webapp show \
  --name $API_APP_NAME \
  --resource-group spexternal-dev-rg \
  --query state
```

### Deploy Blazor Portal

```bash
cd src/portal-blazor/SharePointExternalUserManager.Portal

# Restore dependencies
dotnet restore

# Build for production
dotnet build --configuration Release

# Publish
dotnet publish --configuration Release --output ./publish

# Create deployment package
cd publish
zip -r ../portal-deploy.zip .
cd ..

# Deploy to Azure
az webapp deployment source config-zip \
  --resource-group spexternal-dev-rg \
  --name $PORTAL_APP_NAME \
  --src portal-deploy.zip

# Verify deployment
az webapp show \
  --name $PORTAL_APP_NAME \
  --resource-group spexternal-dev-rg \
  --query state
```

### Build SPFx Package (Optional)

```bash
cd src/client-spfx

# Install dependencies
npm ci --no-optional --legacy-peer-deps

# Build solution
npm run build

# Create package
npm run package-solution

# Package location:
# sharepoint/solution/sharepoint-external-user-manager.sppkg
```

---

## Post-Deployment Configuration

### Configure Azure AD Applications

#### API Application Registration

```bash
# Create API app registration
az ad app create \
  --display-name "ClientSpace API - Dev" \
  --identifier-uris "api://spexternal-api-dev" \
  --sign-in-audience AzureADMultipleOrgs

# Note the Application (client) ID
# Add to Key Vault as AzureAd--ClientId
```

#### Portal Application Registration

```bash
# Create Portal app registration
az ad app create \
  --display-name "ClientSpace Portal - Dev" \
  --web-redirect-uris "https://$PORTAL_APP_NAME.azurewebsites.net/signin-oidc" \
  --sign-in-audience AzureADMultipleOrgs

# Note the Application (client) ID
# Generate client secret
# Add both to Key Vault
```

### Configure Key Vault Secrets

```bash
# Set Azure AD secrets
az keyvault secret set \
  --vault-name $KEY_VAULT \
  --name AzureAd--ClientId \
  --value "your-api-client-id"

az keyvault secret set \
  --vault-name $KEY_VAULT \
  --name AzureAd--ClientSecret \
  --value "your-api-client-secret"

az keyvault secret set \
  --vault-name $KEY_VAULT \
  --name Portal--AzureAd--ClientId \
  --value "your-portal-client-id"

az keyvault secret set \
  --vault-name $KEY_VAULT \
  --name Portal--AzureAd--ClientSecret \
  --value "your-portal-client-secret"

# Set Stripe secret
az keyvault secret set \
  --vault-name $KEY_VAULT \
  --name Stripe--ApiKey \
  --value "your-stripe-api-key"

# Set connection string
az keyvault secret set \
  --vault-name $KEY_VAULT \
  --name ConnectionStrings--DefaultConnection \
  --value "Server=tcp:$SQL_SERVER.database.windows.net,1433;Database=clientspace-db;..."
```

### Grant Key Vault Access to App Services

```bash
# Enable managed identity for API
az webapp identity assign \
  --name $API_APP_NAME \
  --resource-group spexternal-dev-rg

# Get principal ID
API_PRINCIPAL_ID=$(az webapp identity show \
  --name $API_APP_NAME \
  --resource-group spexternal-dev-rg \
  --query principalId -o tsv)

# Grant Key Vault access
az keyvault set-policy \
  --name $KEY_VAULT \
  --object-id $API_PRINCIPAL_ID \
  --secret-permissions get list

# Repeat for Portal
az webapp identity assign \
  --name $PORTAL_APP_NAME \
  --resource-group spexternal-dev-rg

PORTAL_PRINCIPAL_ID=$(az webapp identity show \
  --name $PORTAL_APP_NAME \
  --resource-group spexternal-dev-rg \
  --query principalId -o tsv)

az keyvault set-policy \
  --name $KEY_VAULT \
  --object-id $PORTAL_PRINCIPAL_ID \
  --secret-permissions get list
```

### Configure App Service Settings

```bash
# API App Settings
az webapp config appsettings set \
  --resource-group spexternal-dev-rg \
  --name $API_APP_NAME \
  --settings \
    AzureAd__TenantId="common" \
    AzureAd__ClientId="@Microsoft.KeyVault(SecretUri=https://$KEY_VAULT.vault.azure.net/secrets/AzureAd--ClientId/)" \
    Stripe__ApiKey="@Microsoft.KeyVault(SecretUri=https://$KEY_VAULT.vault.azure.net/secrets/Stripe--ApiKey/)" \
    ApplicationInsights__InstrumentationKey="$APP_INSIGHTS_KEY"

# Portal App Settings
az webapp config appsettings set \
  --resource-group spexternal-dev-rg \
  --name $PORTAL_APP_NAME \
  --settings \
    AzureAd__TenantId="common" \
    AzureAd__ClientId="@Microsoft.KeyVault(SecretUri=https://$KEY_VAULT.vault.azure.net/secrets/Portal--AzureAd--ClientId/)" \
    ApiBaseUrl="https://$API_APP_NAME.azurewebsites.net" \
    ApplicationInsights__InstrumentationKey="$APP_INSIGHTS_KEY"
```

### Run Database Migrations

```bash
# Option 1: From local machine (requires connection string)
cd src/api-dotnet/WebApi/SharePointExternalUserManager.Api
export ConnectionStrings__DefaultConnection="..."
dotnet ef database update

# Option 2: Via App Service SSH
# Navigate to Azure Portal → API App Service → SSH
# Then run:
cd /home/site/wwwroot
dotnet SharePointExternalUserManager.Api.dll migrate
```

---

## Health Checks and Validation

### API Health Check

```bash
# Get API URL
API_URL=$(az webapp show \
  --name $API_APP_NAME \
  --resource-group spexternal-dev-rg \
  --query defaultHostName -o tsv)

# Test health endpoint
curl -v https://$API_URL/health

# Expected response:
# HTTP/1.1 200 OK
# {"status":"Healthy","checks":[...]}
```

### Portal Health Check

```bash
# Get Portal URL
PORTAL_URL=$(az webapp show \
  --name $PORTAL_APP_NAME \
  --resource-group spexternal-dev-rg \
  --query defaultHostName -o tsv)

# Test portal homepage
curl -I https://$PORTAL_URL

# Expected response:
# HTTP/1.1 200 OK
```

### Database Connectivity Check

```bash
# Test SQL connection
az sql db show \
  --resource-group spexternal-dev-rg \
  --server $SQL_SERVER \
  --name clientspace-db

# Query database (requires SQL admin credentials)
az sql db query \
  --server $SQL_SERVER \
  --database clientspace-db \
  --admin-user sqladmin \
  --admin-password "YourPassword" \
  --queries "SELECT COUNT(*) FROM Tenants;"
```

### Key Vault Access Check

```bash
# Verify secrets are accessible
az keyvault secret show \
  --vault-name $KEY_VAULT \
  --name AzureAd--ClientId

# Should return secret metadata (not value)
```

### Application Insights Check

```bash
# Query recent logs
az monitor app-insights query \
  --app $APP_INSIGHTS \
  --analytics-query "requests | where timestamp > ago(1h) | summarize count() by resultCode" \
  --offset 1h
```

### End-to-End Validation

1. **Test Portal Login**:
   - Navigate to portal URL
   - Sign in with test account
   - Verify dashboard loads

2. **Test Client Creation**:
   - Create a test client space
   - Verify SharePoint site is provisioned
   - Check database has client record

3. **Test External User Invitation**:
   - Invite test external user
   - Verify invitation email sent
   - Check user can access SharePoint

4. **Test Search** (if enabled):
   - Perform search query
   - Verify results returned
   - Check permissions enforced

5. **Test Billing** (if configured):
   - View subscription page
   - Test upgrade flow
   - Verify Stripe webhook

---

## Monitoring Setup

### Configure Application Insights Alerts

```bash
# CPU alert
az monitor metrics alert create \
  --name "$API_APP_NAME-high-cpu" \
  --resource-group spexternal-dev-rg \
  --scopes "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/spexternal-dev-rg/providers/Microsoft.Web/sites/$API_APP_NAME" \
  --condition "avg Percentage CPU > 80" \
  --description "Alert when CPU exceeds 80%"

# Response time alert
az monitor metrics alert create \
  --name "$API_APP_NAME-slow-response" \
  --resource-group spexternal-dev-rg \
  --scopes "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/spexternal-dev-rg/providers/Microsoft.Web/sites/$API_APP_NAME" \
  --condition "avg Http2xx > 2000" \
  --description "Alert when response time exceeds 2s"

# Error rate alert
az monitor metrics alert create \
  --name "$API_APP_NAME-error-rate" \
  --resource-group spexternal-dev-rg \
  --scopes "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/spexternal-dev-rg/providers/Microsoft.Web/sites/$API_APP_NAME" \
  --condition "avg Http5xx > 10" \
  --description "Alert when 5xx errors exceed 10/min"
```

### Configure Log Analytics Workspace

```bash
# Create Log Analytics workspace
az monitor log-analytics workspace create \
  --resource-group spexternal-dev-rg \
  --workspace-name clientspace-logs-dev

# Link App Services to workspace
az monitor diagnostic-settings create \
  --resource "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/spexternal-dev-rg/providers/Microsoft.Web/sites/$API_APP_NAME" \
  --name api-diagnostics \
  --workspace clientspace-logs-dev \
  --logs '[{"category":"AppServiceHTTPLogs","enabled":true},{"category":"AppServiceConsoleLogs","enabled":true}]' \
  --metrics '[{"category":"AllMetrics","enabled":true}]'
```

### Key Metrics to Monitor

| Metric | Threshold | Action |
|--------|-----------|--------|
| **CPU Usage** | > 80% | Scale up App Service Plan |
| **Memory Usage** | > 85% | Scale up or optimize code |
| **Response Time** | > 2s | Investigate performance |
| **Error Rate (5xx)** | > 1% | Check logs, rollback if needed |
| **Database DTU** | > 80% | Scale up SQL tier |
| **Request Count** | Unusual spike | Check for attack or viral growth |
| **Failed Requests** | > 5% | Investigate errors |

---

## Troubleshooting Guide

### Issue: API Returns 500 Errors

**Symptoms**: API endpoints return HTTP 500 Internal Server Error

**Diagnosis**:
```bash
# Check API logs
az webapp log tail \
  --name $API_APP_NAME \
  --resource-group spexternal-dev-rg

# Check Application Insights
az monitor app-insights query \
  --app $APP_INSIGHTS \
  --analytics-query "exceptions | where timestamp > ago(1h) | order by timestamp desc"
```

**Common Causes**:
1. Database connection failure
2. Key Vault access denied
3. Missing configuration
4. Code exception

**Solutions**:
```bash
# Restart app service
az webapp restart \
  --name $API_APP_NAME \
  --resource-group spexternal-dev-rg

# Check Key Vault access
az keyvault set-policy \
  --name $KEY_VAULT \
  --object-id $API_PRINCIPAL_ID \
  --secret-permissions get list

# Verify database firewall
az sql server firewall-rule create \
  --resource-group spexternal-dev-rg \
  --server $SQL_SERVER \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0
```

### Issue: Portal Can't Authenticate

**Symptoms**: Users can't sign in, "Invalid client" error

**Diagnosis**:
- Verify Azure AD app registration exists
- Check redirect URIs are correct
- Verify client ID and secret in Key Vault

**Solutions**:
```bash
# Update redirect URI
az ad app update \
  --id $PORTAL_CLIENT_ID \
  --web-redirect-uris "https://$PORTAL_APP_NAME.azurewebsites.net/signin-oidc"

# Generate new client secret
az ad app credential reset \
  --id $PORTAL_CLIENT_ID

# Update Key Vault with new secret
az keyvault secret set \
  --vault-name $KEY_VAULT \
  --name Portal--AzureAd--ClientSecret \
  --value "new-client-secret"
```

### Issue: Database Connection Timeout

**Symptoms**: Slow performance, timeout errors

**Diagnosis**:
```bash
# Check database performance
az sql db show \
  --resource-group spexternal-dev-rg \
  --server $SQL_SERVER \
  --name clientspace-db \
  --query "currentServiceObjectiveName"

# Check DTU usage
az monitor metrics list \
  --resource "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/spexternal-dev-rg/providers/Microsoft.Sql/servers/$SQL_SERVER/databases/clientspace-db" \
  --metric "dtu_consumption_percent" \
  --interval PT1M
```

**Solutions**:
```bash
# Scale up database
az sql db update \
  --resource-group spexternal-dev-rg \
  --server $SQL_SERVER \
  --name clientspace-db \
  --service-objective S3

# Add connection pool settings
az webapp config appsettings set \
  --resource-group spexternal-dev-rg \
  --name $API_APP_NAME \
  --settings "ConnectionStrings__DefaultConnection=...;Max Pool Size=100;"
```

### Issue: High Memory Usage

**Symptoms**: App Service consuming > 90% memory

**Diagnosis**:
```bash
# Check memory metrics
az monitor metrics list \
  --resource "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/spexternal-dev-rg/providers/Microsoft.Web/sites/$API_APP_NAME" \
  --metric "MemoryPercentage" \
  --interval PT1M
```

**Solutions**:
```bash
# Scale up App Service Plan
az appservice plan update \
  --name clientspace-plan-dev \
  --resource-group spexternal-dev-rg \
  --sku S2

# Or scale out (add instances)
az appservice plan update \
  --name clientspace-plan-dev \
  --resource-group spexternal-dev-rg \
  --number-of-workers 2
```

### Issue: SPFx Web Part Not Working

**Symptoms**: Web part shows error or doesn't load

**Diagnosis**:
1. Check browser console for errors
2. Verify API CORS settings
3. Check API endpoint is accessible
4. Verify Azure AD authentication

**Solutions**:
```bash
# Add CORS origin
az webapp cors add \
  --resource-group spexternal-dev-rg \
  --name $API_APP_NAME \
  --allowed-origins "https://*.sharepoint.com"

# Verify web part package deployed
# Check SharePoint App Catalog → Apps for SharePoint
```

---

## Rollback Procedures

### Rollback Application Deployment

#### Using Azure Portal:
1. Navigate to App Service
2. Go to **Deployment Center** → **Logs**
3. Find previous successful deployment
4. Click **Redeploy**

#### Using Azure CLI:

```bash
# List previous deployments
az webapp deployment list \
  --name $API_APP_NAME \
  --resource-group spexternal-dev-rg

# Redeploy specific version
az webapp deployment slot swap \
  --name $API_APP_NAME \
  --resource-group spexternal-dev-rg \
  --slot staging \
  --target-slot production
```

### Rollback Database Migration

```bash
# Connect to database
cd src/api-dotnet/WebApi/SharePointExternalUserManager.Api

# Rollback to specific migration
dotnet ef database update <PreviousMigrationName> \
  --connection "Server=..."

# Example:
dotnet ef database update AddTenantTable \
  --connection "..."
```

### Rollback Infrastructure Changes

```bash
# List previous Bicep deployments
az deployment group list \
  --resource-group spexternal-dev-rg \
  --query "[].{Name:name, Timestamp:properties.timestamp, State:properties.provisioningState}" \
  --output table

# Redeploy previous template version
git checkout <previous-commit>
cd infra/bicep
az deployment group create \
  --resource-group spexternal-dev-rg \
  --template-file main.bicep \
  --parameters @previous-params.json
```

---

## Maintenance Procedures

### Regular Maintenance Tasks

#### Daily
- [ ] Check Application Insights for errors
- [ ] Monitor response times
- [ ] Review failed requests
- [ ] Check database performance

#### Weekly
- [ ] Review audit logs
- [ ] Check storage usage
- [ ] Review Key Vault access logs
- [ ] Test backup restoration

#### Monthly
- [ ] Update dependencies (security patches)
- [ ] Review and rotate secrets
- [ ] Check for Azure updates
- [ ] Review cost optimization
- [ ] Test disaster recovery procedures

### Database Backup

```bash
# Manual backup
az sql db export \
  --resource-group spexternal-dev-rg \
  --server $SQL_SERVER \
  --name clientspace-db \
  --admin-user sqladmin \
  --admin-password "..." \
  --storage-key-type StorageAccessKey \
  --storage-key "..." \
  --storage-uri "https://backupstorage.blob.core.windows.net/backups/clientspace-$(date +%Y%m%d).bacpac"

# Configure automated backups (already enabled by default)
az sql db show \
  --resource-group spexternal-dev-rg \
  --server $SQL_SERVER \
  --name clientspace-db \
  --query "backupStorageRedundancy"
```

### Secret Rotation

```bash
# Generate new client secret
NEW_SECRET=$(az ad app credential reset \
  --id $API_CLIENT_ID \
  --query password -o tsv)

# Update Key Vault
az keyvault secret set \
  --vault-name $KEY_VAULT \
  --name AzureAd--ClientSecret \
  --value "$NEW_SECRET"

# Restart app services to pick up new secret
az webapp restart \
  --name $API_APP_NAME \
  --resource-group spexternal-dev-rg
```

### Update Dependencies

```bash
# Update .NET packages
cd src/api-dotnet/WebApi/SharePointExternalUserManager.Api
dotnet outdated
dotnet add package Microsoft.EntityFrameworkCore --version 8.0.x

# Update npm packages (SPFx)
cd src/client-spfx
npm outdated
npm update --save
```

---

## Support Escalation

### Escalation Levels

#### Level 1: Basic Support (Self-Service)
- Documentation and guides
- AI chat assistant
- Community forum
- Email support (response within 24 hours)

**Contact**: support@clientspace.com

#### Level 2: Technical Support
- API issues
- Integration problems
- Performance issues
- Configuration help

**Contact**: techsupport@clientspace.com  
**Response Time**: 4 hours (business hours)

#### Level 3: Engineering
- System outages
- Security incidents
- Data corruption
- Critical bugs

**Contact**: engineering@clientspace.com  
**Response Time**: 1 hour (24/7)

#### Level 4: Executive
- Major incidents
- Service disruptions
- Escalated customer issues

**Contact**: exec@clientspace.com  
**Response Time**: 30 minutes (24/7)

### Incident Severity Levels

| Severity | Description | Response Time | Example |
|----------|-------------|---------------|---------|
| **P0 - Critical** | Service down, no workaround | 15 minutes | Portal inaccessible |
| **P1 - High** | Major feature broken | 1 hour | Can't invite users |
| **P2 - Medium** | Minor feature broken, workaround available | 4 hours | Search not working |
| **P3 - Low** | Cosmetic issue, no impact | 24 hours | UI alignment issue |

### Incident Response Process

1. **Detection**: Monitoring alerts or user report
2. **Triage**: Assess severity and impact
3. **Communication**: Notify stakeholders
4. **Investigation**: Identify root cause
5. **Resolution**: Deploy fix or workaround
6. **Validation**: Confirm resolution
7. **Post-Mortem**: Document learnings

---

## Additional Resources

- **[Deployment Guide](DEPLOYMENT.md)**: Detailed deployment instructions
- **[Installation Guide](INSTALLATION_GUIDE.md)**: Complete setup guide
- **[User Guide](USER_GUIDE.md)**: End-user documentation
- **[API Reference](MVP_API_REFERENCE.md)**: API endpoint documentation
- **[Support Runbook](MVP_SUPPORT_RUNBOOK.md)**: Support procedures
- **[CI/CD Documentation](../ISSUE_F_CI_CD_IMPLEMENTATION.md)**: Pipeline details
- **[Security Notes](SECURITY_NOTES.md)**: Security best practices

---

## Appendix: Quick Reference Commands

### Get All Resource Names
```bash
export RG="spexternal-dev-rg"
export API_APP=$(az webapp list --resource-group $RG --query "[?contains(name, 'api')].name" -o tsv)
export PORTAL_APP=$(az webapp list --resource-group $RG --query "[?contains(name, 'portal')].name" -o tsv)
export SQL_SERVER=$(az sql server list --resource-group $RG --query "[0].name" -o tsv)
export KEY_VAULT=$(az keyvault list --resource-group $RG --query "[0].name" -o tsv)
```

### View All Application Logs
```bash
az webapp log tail --name $API_APP --resource-group $RG &
az webapp log tail --name $PORTAL_APP --resource-group $RG &
```

### Restart All Services
```bash
az webapp restart --name $API_APP --resource-group $RG
az webapp restart --name $PORTAL_APP --resource-group $RG
```

### Check All Health Endpoints
```bash
API_URL=$(az webapp show --name $API_APP --resource-group $RG --query defaultHostName -o tsv)
PORTAL_URL=$(az webapp show --name $PORTAL_APP --resource-group $RG --query defaultHostName -o tsv)

curl https://$API_URL/health
curl -I https://$PORTAL_URL
```

---

*Last Updated: February 2026*  
*Version: MVP 1.0*  
*Maintainer: DevOps Team*
