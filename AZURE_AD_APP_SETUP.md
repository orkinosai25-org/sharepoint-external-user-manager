# Azure AD App Registration Setup for SharePoint Deployment

This guide explains how to set up an Azure AD (Entra ID) App Registration for automated SharePoint Framework (SPFx) solution deployment using GitHub Actions.

## Overview

Modern authentication for SharePoint Online requires using Azure AD App Registrations instead of username/password credentials. This approach is more secure and is the only supported method for automated deployments.

## Prerequisites

- Global Administrator or Application Administrator role in Azure AD
- SharePoint Administrator role in Microsoft 365
- Access to Azure Portal (https://portal.azure.com)
- Repository admin access to configure GitHub secrets

## Step 1: Create Azure AD App Registration

1. Navigate to [Azure Portal](https://portal.azure.com)
2. Go to **Azure Active Directory** (or **Microsoft Entra ID**)
3. Select **App registrations** from the left menu
4. Click **New registration**
5. Configure the app:
   - **Name**: `SharePoint SPFx Deployment` (or any descriptive name)
   - **Supported account types**: `Accounts in this organizational directory only (Single tenant)`
   - **Redirect URI**: Leave blank
6. Click **Register**

## Step 2: Note the Application Details

After registration, you'll see the app's **Overview** page. Note down these values:

- **Application (client) ID**: This is your `SPO_CLIENT_ID`
- **Directory (tenant) ID**: This is your `SPO_TENANT_ID` (optional but recommended)

## Step 3: Create Client Secret

1. From the app's page, select **Certificates & secrets** from the left menu
2. Click **New client secret**
3. Configure the secret:
   - **Description**: `GitHub Actions Deployment`
   - **Expires**: Choose an appropriate expiration (recommended: 12-24 months)
4. Click **Add**
5. **IMPORTANT**: Copy the secret **Value** immediately - it won't be shown again
   - This is your `SPO_CLIENT_SECRET`

## Step 4: Configure API Permissions

The app needs permissions to manage SharePoint sites and app catalog.

1. Select **API permissions** from the left menu
2. Click **Add a permission**
3. Select **SharePoint**
4. Choose **Application permissions** (not Delegated)
5. Search and select these permissions:
   - `Sites.FullControl.All` - Required for app catalog management
6. Click **Add permissions**
7. **IMPORTANT**: Click **Grant admin consent for [Your Organization]**
   - You must have Global Administrator or appropriate role to grant consent
   - Wait for the status to show "Granted"

### Required Permissions Summary

| API | Permission | Type | Reason |
|-----|------------|------|--------|
| SharePoint | Sites.FullControl.All | Application | Upload and manage apps in the app catalog |

## Step 5: Configure GitHub Repository Secrets

1. Go to your GitHub repository
2. Navigate to **Settings** > **Secrets and variables** > **Actions**
3. Click **New repository secret** for each of the following:

### Required Secrets

| Secret Name | Value | Example |
|-------------|-------|---------|
| `SPO_URL` | Your SharePoint tenant URL | `https://contoso.sharepoint.com` |
| `SPO_CLIENT_ID` | Application (client) ID from Step 2 | `12345678-1234-1234-1234-123456789abc` |
| `SPO_CLIENT_SECRET` | Client secret value from Step 3 | `abc123~DEF456.ghi789` |
| `SPO_TENANT_ID` | Directory (tenant) ID from Step 2 (recommended) | `87654321-4321-4321-4321-cba987654321` |

**Note**: `SPO_TENANT_ID` is optional but **recommended**. 
- **If provided**: Ensures accurate tenant identification regardless of URL format
- **If not provided**: Workflow attempts to derive tenant name from `SPO_URL` (works for standard `*.sharepoint.com` URLs)
- **Custom domains**: If using custom SharePoint domains or vanity URLs, `SPO_TENANT_ID` must be provided

## Step 6: Verify the Setup

After configuring all secrets:

1. Go to your repository's **Actions** tab
2. Manually trigger the "Deploy SPFx Solution to SharePoint" workflow
3. Monitor the workflow run to ensure it connects successfully
4. Check the deployment logs for success confirmation

## Troubleshooting

### Authentication Errors

**Error**: `Please specify a valid client id for an Entra ID App Registration`
- **Solution**: Ensure `SPO_CLIENT_ID` is correctly set in repository secrets

**Error**: `The client secret is invalid`
- **Solution**: Regenerate the client secret and update `SPO_CLIENT_SECRET` in repository secrets

**Error**: `Access denied. You do not have permission`
- **Solution**: 
  1. Verify API permissions are granted
  2. Ensure admin consent was granted
  3. Wait a few minutes for permissions to propagate

### Tenant Configuration Issues

**Error**: `Could not extract tenant name from SPO_URL`
- **Solution**: Ensure `SPO_URL` is in the format `https://yourtenant.sharepoint.com` or `https://yourtenant-admin.sharepoint.com`
- **Alternative**: Explicitly set `SPO_TENANT_ID` secret (recommended for custom domains)
- **Note**: Custom SharePoint domains or vanity URLs require explicit `SPO_TENANT_ID`

**Error**: `Tenant identification failed`
- **Solution**: 
  1. Set `SPO_TENANT_ID` explicitly in repository secrets
  2. Verify the tenant ID is correct (find it in Azure AD overview)
  3. Ensure the Client ID is from the correct tenant

### App Catalog Access

**Error**: `Access to the app catalog is denied`
- **Solution**: 
  1. Ensure your SharePoint tenant has an App Catalog created
  2. The Azure AD app needs `Sites.FullControl.All` permission
  3. Admin consent must be granted for the permission

## Security Best Practices

1. **Rotate Client Secrets Regularly**: Set up calendar reminders before secret expiration
2. **Use Least Privilege**: Only grant necessary permissions
3. **Monitor App Usage**: Regularly review app sign-in logs in Azure AD
4. **Document Secret Locations**: Keep a secure record of where secrets are used
5. **Use Production Environment**: Configure GitHub environment protection rules

## Secret Expiration Management

Client secrets expire based on the duration you selected. Before expiration:

1. Go to Azure Portal > Azure AD > App registrations
2. Select your app
3. Go to **Certificates & secrets**
4. Create a new client secret
5. Update `SPO_CLIENT_SECRET` in GitHub repository secrets
6. Delete the old secret after confirming the new one works

## Alternative Authentication Methods

### Using Certificate Authentication (More Secure)

Instead of client secrets, you can use certificates:

1. Generate a self-signed certificate or use one from your CA
2. Upload the certificate to the Azure AD app
3. Use PnP PowerShell's certificate authentication:
   ```powershell
   Connect-PnPOnline -Url $url -ClientId $clientId -Tenant $tenant -CertificatePath $certPath
   ```

This method is more secure but requires certificate management infrastructure.

## Additional Resources

- [Microsoft Docs: App-only access using Azure AD](https://docs.microsoft.com/en-us/sharepoint/dev/solution-guidance/security-apponly-azuread)
- [PnP PowerShell Documentation](https://pnp.github.io/powershell/)
- [Azure AD App Registration Guide](https://docs.microsoft.com/en-us/azure/active-directory/develop/quickstart-register-app)

## Support

If you encounter issues:

1. Check the GitHub Actions workflow logs for detailed error messages
2. Verify all secrets are correctly configured
3. Ensure permissions are granted and consent is completed
4. Review the troubleshooting section above
5. Check Azure AD sign-in logs for authentication attempts

---

**Last Updated**: 2026-02-12
