# Azure App Service Configuration Setup Guide

## Overview

This guide provides step-by-step instructions for configuring the SharePoint External User Manager Portal application in Azure App Service. The application requires Azure AD authentication configuration to function properly.

## Prerequisites

- Azure subscription with an active App Service
- Azure AD (Entra ID) tenant
- Application deployed to Azure App Service (via GitHub Actions or manual deployment)
- Azure Portal access with appropriate permissions

## Common Error: AADSTS7000218

If you see this error when accessing the application:

```
OpenIdConnectProtocolException: Message contains error: 'invalid_client', 
error_description: 'AADSTS7000218: The request body must contain the following 
parameter: 'client_assertion' or 'client_secret'.
```

**This means the Azure AD ClientSecret is not configured in your App Service.**

Follow the steps below to fix this issue.

## Step-by-Step Configuration

### Step 1: Register Application in Azure AD

If you haven't already registered an application in Azure AD:

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to **Azure Active Directory** (or **Microsoft Entra ID**)
3. Select **App registrations** from the left menu
4. Click **New registration**
5. Configure the application:
   - **Name**: `SharePoint External User Manager - Portal`
   - **Supported account types**: Select based on your needs:
     - Single tenant: `Accounts in this organizational directory only`
     - Multi-tenant: `Accounts in any organizational directory`
   - **Redirect URI**: 
     - Type: `Web`
     - URI: `https://YOUR_APP_SERVICE_NAME.azurewebsites.net/signin-oidc`
     - Replace `YOUR_APP_SERVICE_NAME` with your actual App Service name
6. Click **Register**

### Step 2: Note Application Details

After registration, you'll see the **Overview** page. Copy these values:

- **Application (client) ID**: This is your `ClientId`
- **Directory (tenant) ID**: This is your `TenantId`

### Step 3: Create Client Secret

1. From the app registration page, select **Certificates & secrets** from the left menu
2. Click **New client secret**
3. Configure the secret:
   - **Description**: `Azure App Service Portal`
   - **Expires**: Choose an appropriate expiration (e.g., 12-24 months)
4. Click **Add**
5. **IMPORTANT**: Copy the **Value** immediately - it won't be shown again
   - This is your `ClientSecret`
   - Store it securely (e.g., in Azure Key Vault or a password manager)

### Step 4: Configure API Permissions (if needed)

If your application needs to access Microsoft Graph or other APIs:

1. Select **API permissions** from the left menu
2. Click **Add a permission**
3. Add the required permissions based on your needs
4. Click **Grant admin consent** if required

### Step 5: Configure Azure App Service Settings

Now that you have the Azure AD application registered, configure your App Service:

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to your App Service (e.g., `ClientSpace`)
3. In the left menu, select **Settings** → **Environment variables** (or **Configuration** in older portal versions)
4. Click **New application setting** for each of the following:

#### Required Application Settings

| Name | Value | Example |
|------|-------|---------|
| `AzureAd__ClientId` | Application (client) ID from Step 2 | `61def48e-a9bc-43ef-932b-10eabef14c2a` |
| `AzureAd__ClientSecret` | Client secret value from Step 3 | `abc123~DEF456.ghi789` |
| `AzureAd__TenantId` | Directory (tenant) ID from Step 2 | `b884f3d2-f3d0-4e67-8470-bc7b0372ebb6` |
| `ApiSettings__BaseUrl` | Your backend API URL | `https://your-api.azurewebsites.net/api` |

**Important Notes:**
- Use double underscores (`__`) in the setting names
- The `ClientSecret` should be the actual secret value, not a placeholder
- Values are case-sensitive
- Do NOT include quotes around the values

#### Optional Application Settings

| Name | Value | Description |
|------|-------|-------------|
| `StripeSettings__PublishableKey` | Your Stripe publishable key | For billing functionality |
| `AzureOpenAI__Endpoint` | Azure OpenAI endpoint URL | For AI chat assistant |
| `AzureOpenAI__ApiKey` | Azure OpenAI API key | For AI chat assistant |

### Step 6: Save and Restart

1. Click **Save** (or **OK** then **Save**) at the top of the Configuration page
2. Wait for the settings to be applied
3. Restart your App Service:
   - Click **Restart** in the Overview page
   - Confirm the restart

### Step 7: Verify Configuration

1. Navigate to your application URL (e.g., `https://clientspace.azurewebsites.net`)
2. The application should now load without the AADSTS7000218 error
3. Try signing in with your Azure AD credentials
4. If you see any errors, check the Application Insights or Log Stream for detailed error messages

## Troubleshooting

### Configuration Not Taking Effect

**Problem**: Changes to App Service settings don't seem to work

**Solutions**:
1. Ensure you clicked **Save** after adding settings
2. Restart the App Service
3. Wait a few minutes for changes to propagate
4. Check the **Log stream** for startup errors

### Invalid Client Error Persists

**Problem**: Still seeing AADSTS7000218 error after configuration

**Solutions**:
1. Verify the `ClientSecret` is correct (not expired or regenerated)
2. Check that the setting name uses double underscores: `AzureAd__ClientSecret`
3. Ensure no extra spaces in the secret value
4. Verify the `ClientId` matches the registered application
5. Regenerate the client secret if unsure, and update the App Service setting

### Redirect URI Mismatch Error

**Problem**: AADSTS50011 error about redirect URI

**Solution**:
1. In Azure AD app registration, go to **Authentication**
2. Ensure the redirect URI matches exactly: `https://YOUR_APP_NAME.azurewebsites.net/signin-oidc`
3. Add both `http://localhost` and `https://localhost` URIs for local development
4. Save the changes

### Application Won't Start

**Problem**: App Service shows "Application Error" or HTTP 500.30

**Solution**:
1. Check the **Log stream** or **Application Insights** for detailed errors
2. Verify all required settings are configured (see Required Application Settings above)
3. Ensure no placeholder values (containing "YOUR_", "_HERE", etc.)
4. Check for typos in setting names

## Security Best Practices

1. **Use Azure Key Vault** for production secrets:
   - Store `ClientSecret` in Azure Key Vault
   - Reference it in App Service using Key Vault references: `@Microsoft.KeyVault(SecretUri=https://...)`

2. **Enable Managed Identity**:
   - Configure your App Service to use Managed Identity
   - Grant the identity access to required Azure resources
   - This eliminates the need for stored credentials for Azure services

3. **Rotate Secrets Regularly**:
   - Set calendar reminders before client secret expiration
   - Generate a new secret in Azure AD
   - Update the App Service configuration
   - Delete the old secret after verifying the new one works

4. **Monitor Sign-ins**:
   - Review Azure AD sign-in logs regularly
   - Set up alerts for failed authentication attempts
   - Use Conditional Access policies for additional security

5. **Use Deployment Slots**:
   - Test configuration changes in a staging slot first
   - Swap to production only after verification
   - Keep production configuration in source control (without secrets)

## Alternative Configuration Methods

### Using Azure CLI

```bash
# Set application settings using Azure CLI
az webapp config appsettings set \
  --name YOUR_APP_SERVICE_NAME \
  --resource-group YOUR_RESOURCE_GROUP \
  --settings \
    AzureAd__ClientId="YOUR_CLIENT_ID" \
    AzureAd__ClientSecret="YOUR_CLIENT_SECRET" \
    AzureAd__TenantId="YOUR_TENANT_ID" \
    ApiSettings__BaseUrl="YOUR_API_URL"

# Restart the app service
az webapp restart \
  --name YOUR_APP_SERVICE_NAME \
  --resource-group YOUR_RESOURCE_GROUP
```

### Using Azure PowerShell

```powershell
# Set application settings using PowerShell
$settings = @{
    "AzureAd__ClientId" = "YOUR_CLIENT_ID"
    "AzureAd__ClientSecret" = "YOUR_CLIENT_SECRET"
    "AzureAd__TenantId" = "YOUR_TENANT_ID"
    "ApiSettings__BaseUrl" = "YOUR_API_URL"
}

Set-AzWebApp -Name "YOUR_APP_SERVICE_NAME" `
             -ResourceGroupName "YOUR_RESOURCE_GROUP" `
             -AppSettings $settings

# Restart the app service
Restart-AzWebApp -Name "YOUR_APP_SERVICE_NAME" `
                 -ResourceGroupName "YOUR_RESOURCE_GROUP"
```

## Verification Checklist

After completing the configuration:

- [ ] Azure AD application is registered
- [ ] Client secret is created and copied
- [ ] Redirect URI is configured correctly
- [ ] App Service has all required settings configured
- [ ] Settings use double underscores (`__`) in names
- [ ] No placeholder values in settings
- [ ] App Service has been restarted
- [ ] Application loads without errors
- [ ] Sign-in works with Azure AD credentials
- [ ] Backend API connectivity works (if applicable)

## Additional Resources

- [Configure ASP.NET Core apps in Azure App Service](https://learn.microsoft.com/azure/app-service/configure-common)
- [Azure AD App Registration](https://learn.microsoft.com/azure/active-directory/develop/quickstart-register-app)
- [Use Key Vault references for App Service](https://learn.microsoft.com/azure/app-service/app-service-key-vault-references)
- [ASP.NET Core Configuration](https://learn.microsoft.com/aspnet/core/fundamentals/configuration/)
- [Microsoft Identity Web](https://github.com/AzureAD/microsoft-identity-web)

## Support

If you continue to experience issues after following this guide:

1. Check the App Service **Log stream** for detailed error messages
2. Review **Application Insights** for exceptions
3. Verify all steps in this guide have been completed
4. Check the Azure AD sign-in logs for authentication failures
5. Refer to the [CONFIGURATION_GUIDE.md](./CONFIGURATION_GUIDE.md) for general configuration information

---

**Last Updated**: 2026-02-22
**Version**: 1.0
