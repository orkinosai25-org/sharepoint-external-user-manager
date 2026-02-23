# Azure AD Secrets Setup for CI/CD

## Overview

This guide explains how to configure Azure AD authentication secrets for automated deployments through GitHub Actions.

## Problem

The SharePoint External User Manager Portal requires Azure AD configuration (ClientId, ClientSecret, TenantId) to authenticate users. For security reasons, these values should NOT be committed to the repository. Instead, they are:

1. Stored as GitHub repository secrets
2. Injected into the application configuration during the CI/CD build process
3. Deployed with the application package

## Required Repository Secrets

You need to configure three repository secrets for Azure AD authentication:

| Secret Name | Description | Example Value |
|-------------|-------------|---------------|
| `AZURE_AD_CLIENT_ID` | Azure AD Application (client) ID | `61def48e-a9bc-43ef-932b-10eabef14c2a` |
| `AZURE_AD_CLIENT_SECRET` | Azure AD Application Client Secret | `abc123~DEF456.ghi789JKL012` |
| `AZURE_AD_TENANT_ID` | Azure AD Directory (tenant) ID | `b884f3d2-f3d0-4e67-8470-bc7b0372ebb6` |

## How to Obtain Azure AD Secrets

### Step 1: Get Client ID and Tenant ID

1. Open [Azure Portal](https://portal.azure.com)
2. Navigate to **Azure Active Directory**
3. Go to **App registrations**
4. Select your application (e.g., "ClientSpace" or "SharePoint External User Manager")
5. From the **Overview** page, copy:
   - **Application (client) ID** → This is your `AZURE_AD_CLIENT_ID`
   - **Directory (tenant) ID** → This is your `AZURE_AD_TENANT_ID`

### Step 2: Create Client Secret

1. In your App Registration, go to **Certificates & secrets**
2. Click **"New client secret"**
3. Enter a description (e.g., "GitHub Actions Deployment")
4. Select an expiration period (recommended: 6-12 months)
5. Click **"Add"**
6. **IMPORTANT**: Immediately copy the secret **Value** (not the Secret ID)
   - This is your `AZURE_AD_CLIENT_SECRET`
   - ⚠️ You cannot view this value again after leaving the page!

## How to Add Secrets to GitHub Repository

### Step 1: Navigate to Repository Settings

1. Go to your GitHub repository: https://github.com/orkinosai25-org/sharepoint-external-user-manager
2. Click **Settings** (top navigation)
3. In the left sidebar, click **Secrets and variables** → **Actions**

### Step 2: Add Each Secret

For each of the three secrets:

1. Click **"New repository secret"** button
2. Enter the secret name exactly as shown:
   - `AZURE_AD_CLIENT_ID`
   - `AZURE_AD_CLIENT_SECRET`
   - `AZURE_AD_TENANT_ID`
3. Paste the corresponding value from Azure Portal
4. Click **"Add secret"**

### Step 3: Verify Secrets Are Added

After adding all three secrets, you should see them listed in the repository secrets page. They will be shown with masked values for security.

## How It Works

When you trigger a deployment workflow (e.g., pushing to main branch):

1. **Build Job** reads the repository secrets
2. **Validation Step** checks that all required secrets are configured
3. **Configuration Step** creates `appsettings.Production.json` with the secret values
4. **Build Step** compiles the application
5. **Publish Step** packages the application (including the generated appsettings.Production.json)
6. **Deploy Step** deploys the package to Azure App Service

When the application starts in Azure:
- ASP.NET Core loads configuration in this order:
  1. `appsettings.json` (base configuration)
  2. `appsettings.Production.json` (overrides with secrets)
  3. Environment variables (if configured in Azure App Service)
- The Azure AD configuration is available for authentication
- Users can sign in successfully

## Affected Workflows

These workflows have been updated to use Azure AD repository secrets:

- `.github/workflows/main_clientspace.yml` - Deploy Portal to ClientSpace
- `.github/workflows/deploy-dev.yml` - Deploy to Development environment
- `.github/workflows/deploy-prod.yml` - Deploy to Production environment

## Verification

After configuring the secrets, the next deployment will:

1. ✅ Pass the secret validation step
2. ✅ Create appsettings.Production.json with your Azure AD configuration
3. ✅ Build the application successfully
4. ✅ Deploy to Azure App Service
5. ✅ Application starts without configuration errors

## Security Notes

1. ✅ **Secrets are not committed** to the repository
2. ✅ **Secrets are encrypted** in GitHub
3. ✅ **Secrets are only accessible** to authorized workflows
4. ✅ **Secrets are masked** in workflow logs
5. ✅ **Production config file** is in .gitignore
6. ⚠️ **Rotate secrets regularly** (recommended: every 6-12 months)
7. ⚠️ **Use different secrets** for dev and prod environments (if needed)

## Troubleshooting

### Workflow fails with "Required Azure AD secrets are not configured"

**Solution**: Follow the steps above to add all three required secrets to your repository.

### Deployment succeeds but application shows configuration error

**Possible causes**:
1. Secrets contain placeholder or empty values
2. Azure App Service environment variables are overriding the configuration
3. appsettings.Production.json was not included in the deployment package

**Solution**: 
- Verify the secret values in Azure Portal
- Check Azure App Service configuration for conflicting values
- Review deployment logs to confirm appsettings.Production.json was created

### Client secret expired

**Symptoms**: Authentication fails with token errors

**Solution**:
1. Create a new client secret in Azure Portal
2. Update the `AZURE_AD_CLIENT_SECRET` repository secret with the new value
3. Redeploy the application

## Additional Resources

- [WORKFLOW_SECRET_SETUP.md](./WORKFLOW_SECRET_SETUP.md) - Complete guide for all repository secrets
- [CONFIGURATION_GUIDE.md](./CONFIGURATION_GUIDE.md) - Application configuration guide
- [AZURE_APP_SERVICE_SETUP.md](./AZURE_APP_SERVICE_SETUP.md) - Azure App Service setup guide

## Support

If you encounter issues:
1. Check the workflow run logs in GitHub Actions
2. Review the error messages in the validation steps
3. Verify all three secrets are configured correctly
4. Consult the documentation linked above
