# Deployment Runbook

This document provides step-by-step procedures for deploying the SharePoint External User Manager SaaS platform.

## Table of Contents

- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [Deployment Environments](#deployment-environments)
- [Deployment Procedures](#deployment-procedures)
- [Rollback Procedures](#rollback-procedures)
- [Health Checks](#health-checks)
- [Troubleshooting](#troubleshooting)

## Overview

The platform consists of three main components:
1. **.NET Web API** - Backend API hosted on Azure App Service
2. **Blazor Portal** - Frontend portal hosted on Azure App Service
3. **SPFx Solution** - SharePoint web parts deployed to App Catalog

Deployments are automated via GitHub Actions workflows but can also be performed manually if needed.

## Prerequisites

### For Automated Deployments

**Required Secrets** (configured in GitHub repository settings):

#### Development Environment
- `AZURE_CREDENTIALS` - Azure service principal for development
- `API_APP_NAME` - Azure App Service name for API (dev)
- `PORTAL_APP_NAME` - Azure App Service name for Portal (dev)
- `API_APP_URL` - API URL for health checks (optional)
- `PORTAL_APP_URL` - Portal URL for health checks (optional)

#### Production Environment
- `AZURE_CREDENTIALS_PROD` - Azure service principal for production
- `API_APP_NAME_PROD` - Azure App Service name for API (prod)
- `PORTAL_APP_NAME_PROD` - Azure App Service name for Portal (prod)
- `API_APP_URL_PROD` - API URL for health checks (optional)
- `PORTAL_APP_URL_PROD` - Portal URL for health checks (optional)

#### SharePoint Deployment
- `SPO_URL` - SharePoint tenant URL
- `SPO_CLIENT_ID` - Azure AD App Client ID
- `SPO_CLIENT_SECRET` - Azure AD App Client Secret
- `SPO_TENANT_ID` - Azure AD Tenant ID (optional)

### For Manual Deployments

**Required Tools**:
- Azure CLI 2.0 or later
- .NET 8 SDK
- Node.js 20.x (for API), 18.19.0 (for SPFx)
- PowerShell 7+ (for SPFx deployment)
- PnP PowerShell module

**Required Permissions**:
- Contributor access to Azure resource groups
- SharePoint Administrator for SPFx deployment
- Access to deployment secrets

## Deployment Environments

### Development Environment

**Purpose**: Testing and validation of new features

**Infrastructure**:
- Resource Group: `spexternal-dev-rg`
- API App Service: `spexternal-api-dev`
- Portal App Service: `spexternal-portal-dev`
- Deployment: Automatic on push to `develop` branch

**Access**:
- API: `https://spexternal-api-dev.azurewebsites.net`
- Portal: `https://spexternal-portal-dev.azurewebsites.net`

### Production Environment

**Purpose**: Live customer-facing environment

**Infrastructure**:
- Resource Group: `spexternal-prod-rg`
- API App Service: `spexternal-api-prod`
- Portal App Service: `spexternal-portal-prod`
- Deployment: Manual approval required after push to `main` branch

**Access**:
- API: `https://spexternal-api-prod.azurewebsites.net`
- Portal: `https://spexternal-portal-prod.azurewebsites.net`

## Deployment Procedures

### Automated Deployment (Recommended)

#### Deploy to Development

1. **Merge to develop branch**:
   ```bash
   git checkout develop
   git pull origin develop
   git merge feature/your-feature
   git push origin develop
   ```

2. **Monitor workflow**:
   - Go to GitHub Actions tab
   - Watch "Deploy to Dev Environment" workflow
   - Check each job status

3. **Verify deployment**:
   - Check deployment summary in workflow
   - Run health checks (automated)
   - Manual smoke testing

#### Deploy to Production

1. **Merge to main branch** (after PR approval):
   ```bash
   git checkout main
   git pull origin main
   git merge develop
   git push origin main
   ```

2. **Approve production deployment**:
   - GitHub Actions will pause for approval
   - Review the changes and deployment plan
   - Click "Review deployments" button
   - Approve production environment

3. **Monitor deployment**:
   - Watch workflow progress
   - Check for errors or warnings

4. **Verify deployment**:
   - Run health checks
   - Perform smoke tests
   - Check application logs

#### Manual Workflow Trigger

You can manually trigger any deployment workflow:

1. Go to **Actions** tab in GitHub
2. Select the workflow (e.g., "Deploy to Production")
3. Click **Run workflow**
4. Select branch (usually `main` for prod, `develop` for dev)
5. Optional: Enable infrastructure deployment
6. Click **Run workflow**

### Manual Deployment Procedures

Use these procedures when automated deployment is not available or fails.

#### Manual API Deployment

1. **Build the API**:
   ```bash
   cd src/api-dotnet/WebApi/SharePointExternalUserManager.Api
   dotnet restore
   dotnet build --configuration Release
   dotnet publish --configuration Release --output ./publish
   ```

2. **Create deployment package**:
   ```bash
   cd publish
   zip -r ../api-package.zip .
   ```

3. **Deploy to Azure**:
   ```bash
   az login
   az webapp deployment source config-zip \
     --resource-group spexternal-prod-rg \
     --name spexternal-api-prod \
     --src ../api-package.zip
   ```

4. **Verify deployment**:
   ```bash
   curl https://spexternal-api-prod.azurewebsites.net/health
   ```

#### Manual Portal Deployment

1. **Build the Portal**:
   ```bash
   cd src/portal-blazor/SharePointExternalUserManager.Portal
   dotnet restore
   dotnet build --configuration Release
   dotnet publish --configuration Release --output ./publish
   ```

2. **Create deployment package**:
   ```bash
   cd publish
   zip -r ../portal-package.zip .
   ```

3. **Deploy to Azure**:
   ```bash
   az webapp deployment source config-zip \
     --resource-group spexternal-prod-rg \
     --name spexternal-portal-prod \
     --src ../portal-package.zip
   ```

4. **Verify deployment**:
   ```bash
   curl https://spexternal-portal-prod.azurewebsites.net
   ```

#### Manual SPFx Deployment

1. **Build SPFx solution**:
   ```bash
   cd src/client-spfx
   npm ci --no-optional --legacy-peer-deps
   npm run build
   npm run package-solution
   ```

2. **Verify package**:
   ```bash
   ls -lh sharepoint/solution/*.sppkg
   ```

3. **Deploy with PnP PowerShell**:
   ```powershell
   # Install PnP PowerShell (if not already installed)
   Install-Module -Name PnP.PowerShell -Force -Scope CurrentUser
   
   # Connect to SharePoint
   $clientId = "your-client-id"
   $clientSecret = "your-client-secret"
   $tenantUrl = "https://yourtenant.sharepoint.com"
   $tenantId = "yourtenant.onmicrosoft.com"
   
   Connect-PnPOnline -Url $tenantUrl -ClientId $clientId `
     -ClientSecret $clientSecret -Tenant $tenantId
   
   # Upload and publish package
   $packagePath = "./sharepoint/solution/sharepoint-external-user-manager.sppkg"
   Add-PnPApp -Path $packagePath -Publish -Overwrite
   
   # Verify
   Get-PnPApp | Where-Object { $_.Title -like "*external-user*" }
   
   # Disconnect
   Disconnect-PnPOnline
   ```

4. **Verify in SharePoint**:
   - Go to Site Contents > Add an app
   - Find "SharePoint External User Manager"
   - Add to a page to test

## Rollback Procedures

### Automated Rollback

If a deployment fails or causes issues, you can roll back to the previous version:

#### Using Azure Portal

1. Go to Azure Portal > App Services
2. Select the app (API or Portal)
3. Go to **Deployment Center** > **Deployment slots** (if using slots) or **Logs**
4. Find the previous successful deployment
5. Click **Redeploy**

#### Using Azure CLI

```bash
# List recent deployments
az webapp deployment list \
  --resource-group spexternal-prod-rg \
  --name spexternal-api-prod

# Redeploy a specific deployment
az webapp deployment source delete \
  --resource-group spexternal-prod-rg \
  --name spexternal-api-prod \
  --deployment-id <deployment-id>
```

### Manual Rollback

1. **Find the last good commit**:
   ```bash
   git log --oneline
   ```

2. **Create a rollback branch**:
   ```bash
   git checkout -b rollback/revert-to-<commit>
   git revert <bad-commit-sha>
   git push origin rollback/revert-to-<commit>
   ```

3. **Create PR and merge** to trigger redeployment

### SPFx Rollback

1. **Connect to SharePoint**:
   ```powershell
   Connect-PnPOnline -Url "https://tenant.sharepoint.com" `
     -ClientId $clientId -ClientSecret $clientSecret -Tenant $tenantId
   ```

2. **Get previous version**:
   ```powershell
   # List all versions
   $apps = Get-PnPApp
   $targetApp = $apps | Where-Object { $_.Title -like "*external-user*" }
   ```

3. **Remove current version** (if needed):
   ```powershell
   Remove-PnPApp -Identity $targetApp.Id
   ```

4. **Redeploy previous package** from your backups

## Health Checks

### Automated Health Checks

The deployment workflows automatically run health checks:

1. **API Health Check**:
   - Waits 30 seconds for app to start
   - Calls `/health` endpoint
   - Expects HTTP 200 response

2. **Portal Health Check**:
   - Waits 30 seconds for app to start
   - Calls root URL
   - Expects HTTP 200 or 302 response

### Manual Health Checks

#### API Health Check

```bash
# Basic health check
curl https://spexternal-api-prod.azurewebsites.net/health

# Check API version
curl https://spexternal-api-prod.azurewebsites.net/api/version

# Test authentication endpoint
curl https://spexternal-api-prod.azurewebsites.net/auth/status
```

#### Portal Health Check

```bash
# Check portal is responding
curl -I https://spexternal-portal-prod.azurewebsites.net

# Check specific pages
curl https://spexternal-portal-prod.azurewebsites.net/dashboard
curl https://spexternal-portal-prod.azurewebsites.net/onboarding
```

#### Database Health Check

```bash
# Via API
curl https://spexternal-api-prod.azurewebsites.net/health/database

# Direct SQL query
sqlcmd -S your-server.database.windows.net -d spexternal-db \
  -U admin -P password -Q "SELECT @@VERSION"
```

#### SharePoint Health Check

```powershell
Connect-PnPOnline -Url "https://tenant.sharepoint.com" `
  -ClientId $clientId -ClientSecret $clientSecret -Tenant $tenantId

# Check app is deployed
Get-PnPApp | Where-Object { $_.Title -like "*external-user*" }

# Test permissions
Get-PnPSite
```

### Smoke Tests

After deployment, run these smoke tests:

1. **Portal Login**:
   - Navigate to portal URL
   - Attempt to log in
   - Verify redirect to Azure AD

2. **API Endpoints**:
   - Call `/health` endpoint
   - Call `/api/clients` (if authenticated)
   - Check response times

3. **SPFx Web Parts**:
   - Add web part to test page
   - Verify it loads
   - Test basic functionality

4. **Database Connectivity**:
   - Check API can connect to database
   - Verify data is being returned

## Troubleshooting

### Deployment Failures

#### "Secrets not configured"

**Symptom**: Deployment skips with message about missing secrets

**Solution**:
1. Check GitHub repository Settings > Secrets
2. Verify all required secrets are present
3. Ensure secret names match exactly (case-sensitive)
4. Check secret values are not expired

#### "Azure login failed"

**Symptom**: Azure login step fails

**Solution**:
1. Verify AZURE_CREDENTIALS secret format is correct JSON
2. Check service principal credentials are valid
3. Ensure service principal has Contributor role
4. Verify subscription ID is correct

#### "Build failed"

**Symptom**: Build job fails during compilation

**Solution**:
1. Check build logs for specific errors
2. Run build locally to reproduce
3. Fix errors and commit
4. Check Node.js version matches workflow

### Runtime Issues

#### "502 Bad Gateway"

**Symptom**: API or Portal returns 502 error

**Solution**:
1. Check App Service logs in Azure Portal
2. Verify app is running (not stopped)
3. Check for startup errors
4. Restart the App Service
5. Verify app settings are correct

#### "Database connection failed"

**Symptom**: API can't connect to database

**Solution**:
1. Check connection string in App Settings
2. Verify SQL Server firewall allows Azure services
3. Check database is running
4. Verify credentials are correct

#### "SPFx web part not loading"

**Symptom**: Web part shows error or doesn't load

**Solution**:
1. Check browser console for errors
2. Verify package is deployed to App Catalog
3. Check app is trusted/approved
4. Verify CDN settings if using external CDN
5. Clear browser cache

### Performance Issues

#### "Slow response times"

**Symptom**: API or Portal responds slowly

**Solution**:
1. Check App Service metrics in Azure Portal
2. Scale up App Service plan if needed
3. Check database performance
4. Review application logs for slow queries
5. Enable Application Insights

#### "High memory usage"

**Symptom**: App Service shows high memory usage

**Solution**:
1. Check for memory leaks in code
2. Scale up App Service plan
3. Restart App Service to clear memory
4. Review and optimize code

## Post-Deployment

### Verification Checklist

After each deployment, verify:

- [ ] All health checks pass
- [ ] API responds correctly
- [ ] Portal loads and is accessible
- [ ] Authentication works
- [ ] Database connectivity is working
- [ ] SPFx web parts load (if deployed)
- [ ] No errors in application logs
- [ ] Performance is acceptable

### Monitoring

Set up monitoring for:

1. **Application Insights** - Application performance monitoring
2. **Azure Monitor** - Infrastructure monitoring
3. **Log Analytics** - Centralized logging
4. **Alerts** - Critical error notifications

### Documentation Updates

After deployment:

1. Update deployment history log
2. Document any issues encountered
3. Update runbook if procedures changed
4. Notify stakeholders of deployment completion

## Emergency Contacts

- **DevOps Lead**: [Name/Email]
- **Azure Administrator**: [Name/Email]
- **SharePoint Administrator**: [Name/Email]
- **On-Call Engineer**: [Phone/Pager]

## Related Documentation

- [GitHub Workflows Documentation](.github/workflows/README.md)
- [Developer Guide](DEVELOPER_GUIDE.md)
- [Architecture Documentation](ARCHITECTURE.md)
- [Security Guide](docs/SECURITY_NOTES.md)
