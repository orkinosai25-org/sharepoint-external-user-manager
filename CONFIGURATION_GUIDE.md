# Configuration Guide

## Overview

Both the Portal (Blazor) and API (.NET) applications require configuration to run properly. This guide explains how to configure the applications for different environments.

## Configuration Files

### Portal Application
- `appsettings.json` - Base configuration (committed to repository)
- `appsettings.Development.json` - Development overrides (committed with example values)
- `appsettings.Production.json` - Production overrides (not in repository)

### API Application
- `appsettings.json` - Base configuration with safe defaults (committed to repository)
- `appsettings.Development.json` - Development overrides (committed with example values)
- `appsettings.Production.json` - Production overrides (not in repository)

## Required Configuration

### Azure AD (Required for Authentication)

Both applications require Azure AD configuration:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret"
  }
}
```

### API Settings (Portal Only)

The Portal needs to know where the API is hosted:

```json
{
  "ApiSettings": {
    "BaseUrl": "https://your-api.azurewebsites.net/api",
    "Timeout": 30
  }
}
```

### Stripe Settings (Optional)

For billing functionality:

```json
{
  "StripeSettings": {
    "PublishableKey": "pk_live_your_key"
  }
}
```

For the API, see `appsettings.Stripe.example.json` for the complete Stripe configuration.

## Configuration Methods

### 1. Environment Variables (Recommended for Production)

Set environment variables using the double-underscore (`__`) separator:

```bash
# Linux/Mac
export AzureAd__ClientId="your-client-id"
export AzureAd__ClientSecret="your-secret"
export AzureAd__TenantId="your-tenant-id"
export ApiSettings__BaseUrl="https://your-api.azurewebsites.net/api"

# Windows PowerShell
$env:AzureAd__ClientId="your-client-id"
$env:AzureAd__ClientSecret="your-secret"
$env:AzureAd__TenantId="your-tenant-id"
$env:ApiSettings__BaseUrl="https://your-api.azurewebsites.net/api"
```

### 2. User Secrets (Recommended for Development)

Use the .NET Secret Manager for local development:

```bash
cd src/portal-blazor/SharePointExternalUserManager.Portal
dotnet user-secrets set "AzureAd:ClientId" "your-client-id"
dotnet user-secrets set "AzureAd:ClientSecret" "your-secret"
dotnet user-secrets set "AzureAd:TenantId" "your-tenant-id"
dotnet user-secrets set "ApiSettings:BaseUrl" "https://localhost:5049/api"

cd ../../api-dotnet/WebApi/SharePointExternalUserManager.Api
dotnet user-secrets set "AzureAd:ClientId" "your-api-client-id"
dotnet user-secrets set "AzureAd:ClientSecret" "your-secret"
dotnet user-secrets set "AzureAd:TenantId" "organizations"
```

### 3. Azure App Service Configuration

For Azure deployments, configure settings in the Azure Portal:
1. Navigate to your App Service
2. Go to Settings > Environment variables (or Configuration)
3. Add Application Settings with the required keys
4. Use the same naming convention as environment variables (with `__`)
5. Restart the App Service after saving changes

**📖 For detailed Azure App Service setup instructions, see [AZURE_APP_SERVICE_SETUP.md](./AZURE_APP_SERVICE_SETUP.md)**

This guide includes:
- Step-by-step Azure AD app registration
- How to create and configure client secrets
- Exact configuration settings for Azure App Service
- Troubleshooting common issues like AADSTS7000218 error

### 4. Repository Secrets for CI/CD (Required for Automated Deployments)

For automated deployments via GitHub Actions, configure repository secrets:

1. Go to your GitHub repository
2. Navigate to **Settings** → **Secrets and variables** → **Actions**
3. Add the following secrets:

| Secret Name | Value | Purpose |
|-------------|-------|---------|
| `AZURE_AD_CLIENT_ID` | Your Azure AD Application Client ID | Azure AD authentication |
| `AZURE_AD_CLIENT_SECRET` | Your Azure AD Application Client Secret | Azure AD authentication |
| `AZURE_AD_TENANT_ID` | Your Azure AD Tenant ID | Azure AD authentication |
| `PUBLISH_PROFILE` | ClientSpace App Service publish profile XML | Deploy to ClientSpace |
| `PORTAL_PUBLISH_PROFILE` | Dev Portal App Service publish profile XML | Deploy to dev Portal |
| `API_PUBLISH_PROFILE` | Dev API App Service publish profile XML | Deploy to dev API |
| `PORTAL_PUBLISH_PROFILE_PROD` | Prod Portal App Service publish profile XML | Deploy to prod Portal |
| `API_PUBLISH_PROFILE_PROD` | Prod API App Service publish profile XML | Deploy to prod API |

**How it works**:
- During the CI/CD build process, the workflows read these repository secrets
- They create an `appsettings.Production.json` file with the Azure AD configuration
- This file is included in the deployment package
- The deployed application reads configuration from this file

**📖 For detailed instructions on obtaining and configuring these secrets, see [WORKFLOW_SECRET_SETUP.md](./WORKFLOW_SECRET_SETUP.md)**

### 5. Azure Key Vault (Recommended for Production Secrets)

For production deployments, store secrets in Azure Key Vault:
1. Create an Azure Key Vault
2. Store secrets in the vault
3. Configure Managed Identity for your App Service
4. Grant the App Service access to the Key Vault
5. Reference secrets using Key Vault references in App Service Configuration

## Environment-Specific Behavior

### Development Environment
- Missing or placeholder configuration values will generate **warnings**
- The application will start but authentication may not work
- Useful for testing UI without full authentication setup

### Production Environment
- Missing or placeholder configuration values will generate **errors**
- The application will **fail to start** with clear error messages
- Ensures production deployments are properly configured

## Validation

The applications validate configuration at startup:

### Portal Validation
- **Required**: AzureAd:ClientId, AzureAd:ClientSecret, AzureAd:TenantId
- **Warnings**: ApiSettings:BaseUrl, StripeSettings:PublishableKey

### API Validation
- **Required**: AzureAd:ClientId, AzureAd:TenantId (for authentication)
- **Optional**: Database connection string defaults to LocalDB

## Troubleshooting

### HTTP Error 500.30 - App Failed to Start

This error typically means required configuration is missing. Check:

1. **View Application Logs**: Check stdout logs in Azure App Service or local console
2. **Verify Configuration**: Ensure all required settings are present
3. **Check for Placeholders**: Configuration values containing "YOUR_", "_HERE", etc. will be rejected
4. **Validate Connection Strings**: Ensure database connection strings are properly formatted

**Common Causes**:
- Missing `AzureAd__ClientSecret` in Azure App Service configuration
- Placeholder values in configuration settings
- Incorrect setting names (must use double underscores `__`)

**Solution**: See [AZURE_APP_SERVICE_SETUP.md](./AZURE_APP_SERVICE_SETUP.md) for detailed Azure App Service configuration

### AADSTS7000218: Missing client_secret Error

**Error Message**:
```
OpenIdConnectProtocolException: Message contains error: 'invalid_client', 
error_description: 'AADSTS7000218: The request body must contain the following 
parameter: 'client_assertion' or 'client_secret'.
```

**Cause**: The Azure AD ClientSecret is not configured in your environment.

**Solution**:
1. For Azure App Service: Follow [AZURE_APP_SERVICE_SETUP.md](./AZURE_APP_SERVICE_SETUP.md)
2. For local development: Set the ClientSecret using user secrets:
   ```bash
   dotnet user-secrets set "AzureAd:ClientSecret" "YOUR_SECRET_VALUE"
   ```
3. Ensure the secret is not expired in Azure AD
4. Verify the setting name uses the correct format

### Configuration Not Loading

If changes don't seem to take effect:

1. Restart the application
2. Check configuration precedence (Environment Variables override appsettings.json)
3. Verify the environment name (ASPNETCORE_ENVIRONMENT)
4. Check for typos in configuration keys (they are case-sensitive in some providers)

## Security Best Practices

1. **Never commit secrets** to source control
2. Use **User Secrets** for local development
3. Use **Azure Key Vault** for production secrets
4. Use **Managed Identity** when accessing Azure resources
5. Rotate secrets regularly
6. Use different credentials for each environment

## Quick Start for Development

1. Clone the repository
2. Register applications in Azure AD
3. Configure user secrets:
   ```bash
   cd src/portal-blazor/SharePointExternalUserManager.Portal
   dotnet user-secrets set "AzureAd:ClientId" "your-client-id"
   dotnet user-secrets set "AzureAd:ClientSecret" "your-secret"
   dotnet user-secrets set "AzureAd:TenantId" "your-tenant-id"
   ```
4. Run the application:
   ```bash
   dotnet run
   ```

## Additional Resources

- [Azure AD App Registration](https://learn.microsoft.com/azure/active-directory/develop/quickstart-register-app)
- [ASP.NET Core Configuration](https://learn.microsoft.com/aspnet/core/fundamentals/configuration/)
- [Azure Key Vault](https://learn.microsoft.com/azure/key-vault/general/overview)
- [Managed Identity](https://learn.microsoft.com/azure/active-directory/managed-identities-azure-resources/overview)
