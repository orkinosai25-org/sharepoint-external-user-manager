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
2. Go to Settings > Configuration
3. Add Application Settings with the required keys
4. Use the same naming convention as environment variables (with `__`)

### 4. Azure Key Vault (Recommended for Production Secrets)

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
