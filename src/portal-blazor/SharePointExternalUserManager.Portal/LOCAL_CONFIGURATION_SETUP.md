# Local Configuration Setup

This guide explains how to configure the SharePoint External User Manager Portal for local development using `appsettings.Local.json`.

## Quick Start

### Step 1: Create appsettings.Local.json

Copy the example configuration file:

```bash
cd src/portal-blazor/SharePointExternalUserManager.Portal
cp appsettings.example.json appsettings.Local.json
```

### Step 2: Configure Azure AD Settings

Edit `appsettings.Local.json` and update the following values:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "YOUR_TENANT_ID",
    "ClientId": "YOUR_CLIENT_ID",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc",
    "ClientSecret": "YOUR_CLIENT_SECRET"
  },
  "ApiSettings": {
    "BaseUrl": "http://localhost:7071/api",
    "Timeout": 30
  }
}
```

### Step 3: Get Your Azure AD Credentials

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to **Azure Active Directory** → **App registrations**
3. Select your application (or create a new one)
4. Copy the following values:
   - **Application (client) ID** → Use as `ClientId`
   - **Directory (tenant) ID** → Use as `TenantId`
5. Go to **Certificates & secrets** → **Client secrets**
6. Create a new client secret (or use existing one)
7. Copy the **Value** (not the Secret ID) → Use as `ClientSecret`

### Step 4: Run the Application

```bash
dotnet run
```

The application will now start with your local configuration.

## Configuration Priority

ASP.NET Core loads configuration in the following order (later sources override earlier ones):

1. `appsettings.json` (base configuration)
2. `appsettings.{Environment}.json` (e.g., `appsettings.Development.json`)
3. `appsettings.Local.json` (your local overrides)
4. User Secrets (if configured)
5. Environment Variables
6. Command-line arguments

## Security Notes

⚠️ **IMPORTANT**: The `appsettings.Local.json` file is included in `.gitignore` and will NOT be committed to source control.

- ✅ Safe to put secrets in `appsettings.Local.json` for local development
- ✅ File will not be committed to Git
- ✅ File will not be included in deployments
- ❌ Never commit files with secrets to source control
- ❌ Never use `appsettings.Local.json` in production

## Alternative Configuration Methods

### Option 1: User Secrets (Recommended for sharing)

If you're working in a team and want to avoid managing local files:

```bash
dotnet user-secrets set "AzureAd:ClientId" "YOUR_CLIENT_ID"
dotnet user-secrets set "AzureAd:ClientSecret" "YOUR_SECRET"
dotnet user-secrets set "AzureAd:TenantId" "YOUR_TENANT_ID"
```

### Option 2: Environment Variables

For temporary testing:

```bash
# Linux/Mac
export AzureAd__ClientId="YOUR_CLIENT_ID"
export AzureAd__ClientSecret="YOUR_SECRET"
export AzureAd__TenantId="YOUR_TENANT_ID"

# Windows PowerShell
$env:AzureAd__ClientId="YOUR_CLIENT_ID"
$env:AzureAd__ClientSecret="YOUR_SECRET"
$env:AzureAd__TenantId="YOUR_TENANT_ID"
```

## Troubleshooting

### Application starts but authentication fails

- Verify your `ClientId` and `TenantId` are correct
- Check that `ClientSecret` is the actual secret value (not the Secret ID)
- Ensure the secret hasn't expired in Azure Portal
- Verify redirect URIs are configured in Azure AD app registration

### Configuration warnings on startup

If you see warnings like "Azure AD Client Secret is not configured":

1. Check that `appsettings.Local.json` exists
2. Verify the file has valid JSON syntax
3. Ensure `ClientSecret` is not empty
4. Try restarting the application

### Changes not taking effect

- Stop the application completely
- Delete `bin` and `obj` folders
- Run `dotnet clean`
- Run `dotnet build`
- Run `dotnet run`

## Production Deployment

For production deployments to Azure App Service:

- Use Azure App Service Configuration (Environment Variables)
- Or use `appsettings.Production.json` (created during deployment)
- Never use `appsettings.Local.json` in production

See [AZURE_APP_SERVICE_CONFIGURATION.md](../../../AZURE_APP_SERVICE_CONFIGURATION.md) for production setup.
