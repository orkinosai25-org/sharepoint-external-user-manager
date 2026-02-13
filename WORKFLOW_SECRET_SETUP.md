# GitHub Actions Secret Setup Guide

## Quick Reference for Repository Administrators

This guide explains how to configure the required secrets for automated deployment workflows.

## Required Secrets

### 1. PUBLISH_PROFILE (for main_clientspace.yml)
**Purpose**: Deploy the Blazor Portal application to Azure App Service "ClientSpace"

**How to obtain**:
1. Log in to [Azure Portal](https://portal.azure.com)
2. Navigate to App Service → "ClientSpace"
3. Click **"Get publish profile"** in the Overview or Deployment Center
4. Save the downloaded `.PublishSettings` file
5. Open the file in a text editor
6. Copy the **entire XML content**

**How to add to GitHub**:
1. Go to repository **Settings**
2. Navigate to **Secrets and variables** → **Actions**
3. Click **"New repository secret"**
4. Name: `PUBLISH_PROFILE`
5. Value: Paste the complete XML from the publish profile
6. Click **"Add secret"**

### 2. AZURE_CREDENTIALS (for deploy-dev.yml, deploy-backend.yml)
**Purpose**: Deploy infrastructure and applications to Azure using service principal

**How to obtain**:
```bash
# Create a service principal with contributor role
az ad sp create-for-rbac \
  --name "sharepoint-external-user-manager-github" \
  --role contributor \
  --scopes /subscriptions/<SUBSCRIPTION_ID>/resourceGroups/<RESOURCE_GROUP> \
  --sdk-auth
```

**Expected format** (JSON):
```json
{
  "clientId": "<CLIENT_ID>",
  "clientSecret": "<CLIENT_SECRET>",
  "subscriptionId": "<SUBSCRIPTION_ID>",
  "tenantId": "<TENANT_ID>",
  "activeDirectoryEndpointUrl": "https://login.microsoftonline.com",
  "resourceManagerEndpointUrl": "https://management.azure.com/",
  "activeDirectoryGraphResourceId": "https://graph.windows.net/",
  "sqlManagementEndpointUrl": "https://management.core.windows.net:8443/",
  "galleryEndpointUrl": "https://gallery.azure.com/",
  "managementEndpointUrl": "https://management.core.windows.net/"
}
```

**How to add to GitHub**:
1. Go to repository **Settings** → **Secrets and variables** → **Actions**
2. Click **"New repository secret"**
3. Name: `AZURE_CREDENTIALS`
4. Value: Paste the complete JSON output from the `az ad sp create-for-rbac` command
5. Click **"Add secret"**

### 3. SharePoint Deployment Secrets (for deploy-spfx.yml)

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
| `main_clientspace.yml` | `PUBLISH_PROFILE` | Deploy Blazor Portal to Azure |
| `deploy-dev.yml` | `AZURE_CREDENTIALS`, `API_APP_NAME`, `PORTAL_APP_NAME` | Deploy to dev environment |
| `deploy-spfx.yml` | `SPO_URL`, `SPO_CLIENT_ID`, `SPO_CLIENT_SECRET` | Deploy SPFx to SharePoint |
| `deploy-backend.yml` | `AZURE_CREDENTIALS` | Deploy backend to Azure Functions |

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
