# Azure App Service Configuration Guide

This guide explains how to configure Azure App Service to use the Azure AD settings from GitHub Actions secrets.

## Overview

The deployment workflow creates an `appsettings.Production.json` file with Azure AD configuration from GitHub Actions secrets. For this configuration to be used at runtime, Azure App Service must be configured to run in Production mode.

## Prerequisites

- Azure AD secrets must be configured in GitHub repository secrets:
  - `AZURE_AD_CLIENT_ID`
  - `AZURE_AD_CLIENT_SECRET`
  - `AZURE_AD_TENANT_ID`

## Configuration Steps

### 1. Set Environment Variable in Azure App Service

The application needs to know it's running in Production mode to use `appsettings.Production.json`.

1. Go to **Azure Portal** (https://portal.azure.com)
2. Navigate to your **ClientSpace App Service**
3. In the left menu, go to **Settings** → **Environment variables** (or **Configuration** in older portal versions)
4. Under **Application settings**, click **+ New application setting**
5. Add the following setting:
   - **Name**: `ASPNETCORE_ENVIRONMENT`
   - **Value**: `Production`
6. Click **OK**
7. Click **Save** at the top of the page
8. Click **Continue** to confirm the restart

### 2. Alternative: Use Azure App Service Environment Variables

If you prefer to manage Azure AD configuration entirely through Azure App Service (without using GitHub secrets), you can set these environment variables instead:

1. Go to **Azure Portal** → Your App Service → **Settings** → **Environment variables**
2. Add the following application settings:
   - **Name**: `AzureAd__ClientId`, **Value**: `Your Azure AD Application Client ID`
   - **Name**: `AzureAd__ClientSecret`, **Value**: `Your Azure AD Application Client Secret`
   - **Name**: `AzureAd__TenantId`, **Value**: `Your Azure AD Tenant ID`
   - **Name**: `ASPNETCORE_ENVIRONMENT`, **Value**: `Production`
3. Click **Save** and restart the app

> **Note**: In Azure App Service, use double underscores (`__`) to represent nested configuration sections. For example, `AzureAd__ClientId` maps to `AzureAd:ClientId` in appsettings.json.

## Verification

After configuring the environment variables:

1. **Restart** the App Service if it didn't restart automatically
2. Check the **Log stream** in Azure Portal:
   - Go to **Monitoring** → **Log stream**
   - Look for startup messages
   - Verify there are no configuration errors
3. Test the application by navigating to your app URL
4. Try to sign in - it should redirect to Microsoft login page

## Troubleshooting

### Application fails to start with configuration error

**Error**: "Application configuration is invalid. Required Azure AD settings (ClientId, ClientSecret, TenantId) are missing."

**Solution**: 
- Ensure `ASPNETCORE_ENVIRONMENT` is set to `Production` in Azure App Service
- Verify the GitHub Actions workflow successfully created `appsettings.Production.json` (check the workflow logs)
- Alternatively, set Azure AD configuration in Azure App Service environment variables (see Alternative method above)

### Sign-in redirects to Microsoft but fails

**Error**: "AADSTS700016: Application with identifier 'xxx' was not found in the directory"

**Solution**:
- Verify the `ClientId` matches your Azure AD App Registration
- Verify the `TenantId` is correct
- Check the Azure AD App Registration exists and is not deleted

### Sign-in fails with "invalid_client" error

**Error**: "AADSTS7000215: Invalid client secret is provided"

**Solution**:
- Verify the `ClientSecret` is correct and not expired
- Create a new client secret in Azure AD App Registration if needed
- Update the secret in GitHub Actions secrets or Azure App Service environment variables

## Configuration Priority

ASP.NET Core loads configuration in the following order (later sources override earlier ones):

1. `appsettings.json`
2. `appsettings.{Environment}.json` (e.g., `appsettings.Production.json`)
3. User secrets (Development only)
4. Environment variables
5. Command-line arguments

This means:
- Azure App Service environment variables will override `appsettings.Production.json`
- `appsettings.Production.json` will override `appsettings.json`

## Security Best Practices

1. **Never commit secrets** to source control
2. Use GitHub Actions secrets for CI/CD pipelines
3. Use Azure App Service environment variables or Azure Key Vault for production secrets
4. Rotate secrets regularly
5. Use managed identities when possible instead of client secrets

## Related Documentation

- [WORKFLOW_SECRET_SETUP.md](WORKFLOW_SECRET_SETUP.md) - How to configure GitHub Actions secrets
- [AZURE_AD_APP_SETUP.md](AZURE_AD_APP_SETUP.md) - How to set up Azure AD App Registration
- [CONFIGURATION_GUIDE.md](CONFIGURATION_GUIDE.md) - General configuration guide
