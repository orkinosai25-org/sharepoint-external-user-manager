# GitHub Actions Secret Setup Guide

## Quick Reference for Repository Administrators

This guide explains how to configure the required secrets for automated deployment workflows.

## Overview

The SharePoint External User Manager uses **publish profiles** for deploying .NET applications to Azure App Services. This approach is simpler and more reliable than service principal authentication.

### Why Publish Profiles?

- ✅ **Simpler Setup**: No need for Azure AD app registrations or service principals
- ✅ **More Reliable**: Direct authentication to App Service
- ✅ **Easier Troubleshooting**: Clear error messages when deployment fails
- ✅ **No Azure CLI Required**: Works directly with Azure Web Apps Deploy action
- ✅ **Scoped Permissions**: Each profile only has access to its specific App Service

## Required Secrets by Workflow

### For Development Deployments (deploy-dev.yml)

| Secret Name | Purpose | Required |
|-------------|---------|----------|
| `API_PUBLISH_PROFILE` | Deploy API to development | Yes |
| `PORTAL_PUBLISH_PROFILE` | Deploy Portal to development | Yes |
| `AZURE_AD_CLIENT_ID` | Azure AD Application Client ID | Yes |
| `AZURE_AD_CLIENT_SECRET` | Azure AD Application Client Secret | Yes |
| `AZURE_AD_TENANT_ID` | Azure AD Tenant ID | Yes |

### For Production Deployments (deploy-prod.yml)

| Secret Name | Purpose | Required |
|-------------|---------|----------|
| `API_PUBLISH_PROFILE_PROD` | Deploy API to production | Yes |
| `PORTAL_PUBLISH_PROFILE_PROD` | Deploy Portal to production | Yes |
| `AZURE_AD_CLIENT_ID` | Azure AD Application Client ID | Yes |
| `AZURE_AD_CLIENT_SECRET` | Azure AD Application Client Secret | Yes |
| `AZURE_AD_TENANT_ID` | Azure AD Tenant ID | Yes |

### For ClientSpace Deployment (main_clientspace.yml)

| Secret Name | Purpose | Required |
|-------------|---------|----------|
| `PUBLISH_PROFILE` | Deploy Portal to ClientSpace App Service | Yes |
| `AZURE_AD_CLIENT_ID` | Azure AD Application Client ID | Yes |
| `AZURE_AD_CLIENT_SECRET` | Azure AD Application Client Secret | Yes |
| `AZURE_AD_TENANT_ID` | Azure AD Tenant ID | Yes |

### For SPFx Deployment (deploy-spfx.yml)

| Secret Name | Purpose | Required |
|-------------|---------|----------|
| `SPO_URL` | SharePoint tenant URL | Yes |
| `SPO_CLIENT_ID` | Azure AD App Client ID | Yes |
| `SPO_CLIENT_SECRET` | Azure AD App Client Secret | Yes |
| `SPO_TENANT_ID` | Azure AD Tenant ID | Optional |

## How to Obtain Publish Profiles

### Step 1: Navigate to Azure Portal

1. Open [Azure Portal](https://portal.azure.com)
2. Sign in with your Azure account

### Step 2: Find Your App Service

1. Click **"App Services"** in the left menu (or search for it)
2. Select your App Service from the list
   - For API: `spexternal-api-dev` or `spexternal-api-prod`
   - For Portal: `spexternal-portal-dev` or `spexternal-portal-prod`
   - For ClientSpace: `ClientSpace`

### Step 3: Download Publish Profile

1. In the App Service **Overview** page
2. Click **"Get publish profile"** button (top toolbar)
3. A `.PublishSettings` XML file will download automatically

### Step 4: Copy the XML Content

1. Open the downloaded `.PublishSettings` file in a text editor (Notepad, VS Code, etc.)
2. Select and copy **ALL** the content (Ctrl+A, Ctrl+C)

### Step 5: Add to GitHub Secrets

1. Go to your GitHub repository
2. Click **Settings** (top menu)
3. Navigate to **Secrets and variables** → **Actions** (left sidebar)
4. Click **"New repository secret"** button
5. Enter the secret name:
   - For dev API: `API_PUBLISH_PROFILE`
   - For dev Portal: `PORTAL_PUBLISH_PROFILE`
   - For prod API: `API_PUBLISH_PROFILE_PROD`
   - For prod Portal: `PORTAL_PUBLISH_PROFILE_PROD`
   - For ClientSpace: `PUBLISH_PROFILE`
6. Paste the entire XML content in the **Value** field
7. Click **"Add secret"**

## How to Obtain Azure AD Configuration Secrets

### Required for Application Configuration

The deployment workflows inject Azure AD configuration into the application at build time. You need to create the following repository secrets:

#### AZURE_AD_CLIENT_ID
**Purpose**: Azure AD Application (client) ID for authentication  
**How to obtain**:
1. Go to [Azure Portal](https://portal.azure.com) → Azure Active Directory
2. Navigate to **App registrations**
3. Select your application (or create a new one)
4. Copy the **Application (client) ID** from the Overview page
5. **Example**: `61def48e-a9bc-43ef-932b-10eabef14c2a`

#### AZURE_AD_CLIENT_SECRET
**Purpose**: Azure AD Application Client Secret for authentication  
**How to obtain**:
1. In your App Registration → **Certificates & secrets**
2. Click **"New client secret"**
3. Add a description (e.g., "Production deployment")
4. Select an expiration period (recommended: 6 months or 1 year)
5. Click **"Add"**
6. Copy the secret **Value** immediately (not the Secret ID)
7. ⚠️ **Important**: You cannot view the secret value again after leaving this page!
8. **Example**: `abc123~DEF456.ghi789JKL012-MNO345`

#### AZURE_AD_TENANT_ID
**Purpose**: Azure AD Directory (tenant) ID  
**How to obtain**:
1. Azure Portal → Azure Active Directory → Overview
2. Copy the **Directory (tenant) ID**
3. **Example**: `b884f3d2-f3d0-4e67-8470-bc7b0372ebb6`

### Adding Azure AD Secrets to GitHub

1. Go to your GitHub repository
2. Click **Settings** → **Secrets and variables** → **Actions**
3. Add each secret:
   - Click **"New repository secret"**
   - Name: `AZURE_AD_CLIENT_ID`, `AZURE_AD_CLIENT_SECRET`, or `AZURE_AD_TENANT_ID`
   - Value: Paste the value from Azure Portal
   - Click **"Add secret"**

### Verification

After adding the secrets, the next deployment will:
1. Read the secrets from the repository
2. Create an `appsettings.Production.json` file with these values
3. Deploy the application with the proper configuration
4. Application will start successfully with Azure AD authentication enabled

## Publish Profile XML Format

The publish profile is an XML document that looks like this:

```xml
<publishData>
  <publishProfile 
    profileName="your-app-name - Web Deploy"
    publishMethod="MSDeploy"
    publishUrl="your-app-name.scm.azurewebsites.net:443"
    msdeploySite="your-app-name"
    userName="$your-app-name"
    userPWD="your-password-here"
    ...
  />
  <publishProfile 
    profileName="your-app-name - FTP"
    ...
  />
</publishData>
```

**Important**: Copy the entire XML, including multiple `<publishProfile>` sections.

## SharePoint Deployment Secrets (SPFx)

#### SPO_URL
**Purpose**: SharePoint tenant URL  
**Example**: `https://contoso.sharepoint.com`

#### SPO_CLIENT_ID
**Purpose**: Azure AD App Registration Client ID for SharePoint access  
**How to obtain**:
1. Go to [Azure Portal](https://portal.azure.com) → Azure Active Directory
2. Navigate to **App registrations**
3. Create new registration or use existing
4. Copy the **Application (client) ID**

#### SPO_CLIENT_SECRET
**Purpose**: Azure AD App Registration Client Secret  
**How to obtain**:
1. In your App Registration → **Certificates & secrets**
2. Click **"New client secret"**
3. Add description and set expiration
4. Copy the secret **value** (not the ID)
5. ⚠️ **Important**: Copy immediately - you cannot view it again!

#### SPO_TENANT_ID (Optional)
**Purpose**: Azure AD Tenant ID  
**How to obtain**:
1. Azure Portal → Azure Active Directory → Overview
2. Copy the **Tenant ID**
3. Or derive from tenant URL (e.g., `contoso.onmicrosoft.com`)

**Note**: If not provided, the workflow will attempt to derive it from `SPO_URL`

### 4. Additional Deployment Secrets (Optional)

#### API_APP_NAME
**Purpose**: Name of the API App Service in Azure  
**Example**: `spexternal-api-dev`

#### PORTAL_APP_NAME
**Purpose**: Name of the Portal App Service in Azure  
**Example**: `spexternal-portal-dev`

#### API_APP_URL
**Purpose**: URL of the deployed API application  
**Example**: `https://spexternal-api-dev.azurewebsites.net`

#### PORTAL_APP_URL
**Purpose**: URL of the deployed Portal application  
**Example**: `https://spexternal-portal-dev.azurewebsites.net`

#### SQL_ADMIN_USERNAME
**Purpose**: SQL Server administrator username  
**Example**: `sqladmin`

#### SQL_ADMIN_PASSWORD
**Purpose**: SQL Server administrator password  
**Security**: Use a strong password (16+ characters, mixed case, numbers, symbols)

## Workflow-to-Secret Mapping

| Workflow File | Required Secrets | Purpose |
|--------------|------------------|---------|
| `main_clientspace.yml` | `PUBLISH_PROFILE`, `AZURE_AD_CLIENT_ID`, `AZURE_AD_CLIENT_SECRET`, `AZURE_AD_TENANT_ID` | Deploy Blazor Portal to ClientSpace App Service with Azure AD configuration |
| `deploy-dev.yml` | `API_PUBLISH_PROFILE`, `PORTAL_PUBLISH_PROFILE`, `AZURE_AD_CLIENT_ID`, `AZURE_AD_CLIENT_SECRET`, `AZURE_AD_TENANT_ID` | Deploy API and Portal to dev environment with Azure AD configuration |
| `deploy-prod.yml` | `API_PUBLISH_PROFILE_PROD`, `PORTAL_PUBLISH_PROFILE_PROD`, `AZURE_AD_CLIENT_ID`, `AZURE_AD_CLIENT_SECRET`, `AZURE_AD_TENANT_ID` | Deploy API and Portal to production with Azure AD configuration |
| `deploy-spfx.yml` | `SPO_URL`, `SPO_CLIENT_ID`, `SPO_CLIENT_SECRET`, `SPO_TENANT_ID` (optional) | Deploy SPFx to SharePoint |
| `build-api.yml` | None | Build and test API only |
| `build-blazor.yml` | None | Build and test Blazor Portal only |

## Optional Infrastructure Deployment Secrets

These are only needed if you want to deploy Azure infrastructure using Bicep templates:

| Secret Name | Purpose | Workflow |
|-------------|---------|----------|
| `AZURE_CREDENTIALS` | Service principal for infrastructure deployment | `deploy-dev.yml`, `deploy-prod.yml` |
| `SQL_ADMIN_USERNAME` | SQL Server admin username | `deploy-dev.yml`, `deploy-prod.yml` |
| `SQL_ADMIN_PASSWORD` | SQL Server admin password | `deploy-dev.yml`, `deploy-prod.yml` |

**Note**: Infrastructure deployment is only triggered manually via `workflow_dispatch` with `deploy_infrastructure: true`

## Verification

After adding secrets, you can verify by:

1. **Manual Trigger**: Go to Actions → Select workflow → "Run workflow"
2. **Push Trigger**: Push changes to the appropriate branch (main/develop)
3. **Check Logs**: Review workflow run logs for validation messages

Expected validation messages:
- ✅ "Publish profile validated"
- ✅ "Azure credentials validated"
- ✅ "All prerequisites validated"

## Security Best Practices

1. ✅ **Use separate secrets for different environments** (dev/staging/prod)
2. ✅ **Rotate secrets regularly** (every 90 days recommended)
3. ✅ **Use Azure Key Vault** for production secrets when possible
4. ✅ **Limit secret scope** to specific workflows using environment secrets
5. ✅ **Audit secret usage** regularly through GitHub Actions logs
6. ✅ **Never commit secrets** to source code or include in PR descriptions
7. ✅ **Use service principals** with minimum required permissions
8. ✅ **Set expiration dates** on Azure AD client secrets

## Troubleshooting

### Workflow fails with "secret not configured"
**Solution**: Add the required secret as shown above

### Workflow fails at authentication
**Solution**: Verify secret format and permissions:
- Check JSON format for `AZURE_CREDENTIALS`
- Verify service principal has required permissions
- Confirm Azure AD App has SharePoint API permissions

### Deployment succeeds but app doesn't work
**Solution**: Check application configuration:
- Verify App Service application settings
- Confirm connection strings are correct
- Check Azure AD app registration redirect URIs

## Support

For issues or questions:
1. Check workflow logs in GitHub Actions
2. Review `WORKFLOW_VERIFICATION_SUMMARY.md` for detailed information
3. Contact repository administrators
4. Refer to [GitHub Actions documentation](https://docs.github.com/en/actions)

## Last Updated
2026-02-13 - Verified all workflows using correct secret validation patterns
