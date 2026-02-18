# Installation Guide - ClientSpace

This guide walks you through installing and configuring ClientSpace (SharePoint External User Manager) for your organization.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Tenant Onboarding Process](#tenant-onboarding-process)
3. [Azure AD Configuration](#azure-ad-configuration)
4. [Backend API Deployment](#backend-api-deployment)
5. [Blazor Portal Deployment](#blazor-portal-deployment)
6. [SPFx Client Installation](#spfx-client-installation)
7. [Initial Configuration](#initial-configuration)
8. [Verification](#verification)
9. [Troubleshooting](#troubleshooting)

## Prerequisites

### Required Accounts and Permissions

- **Microsoft 365 Tenant**: SharePoint Online with admin access
- **Azure Subscription**: For hosting backend services
- **Azure AD Permissions**: 
  - Global Administrator or Application Administrator role
  - Ability to grant admin consent
- **SharePoint Permissions**: 
  - SharePoint Administrator or Global Administrator
  - Access to App Catalog
- **Stripe Account**: For billing integration (optional for initial setup)

### Required Software

- **Azure CLI**: Version 2.40 or later
- **Git**: For cloning the repository
- **Node.js**: Version 16.x or 18.x (for SPFx client)
- **.NET SDK**: Version 8.0 or later (for API and Portal)
- **PowerShell**: Version 7+ or Windows PowerShell 5.1

## Tenant Onboarding Process

### Step 1: Sign Up

1. **Visit the Portal**: Navigate to your ClientSpace portal URL (e.g., `https://clientspace.yourdomain.com`)
2. **Start Trial**: Click "Start Free Trial" or "Sign Up"
3. **Enter Details**:
   - Work email address
   - Organization name
   - Contact information

### Step 2: Azure AD Authentication

1. **Microsoft Login**: You'll be redirected to Microsoft login
2. **Authenticate**: Sign in with your Microsoft 365 admin account
3. **Multi-Factor Authentication**: Complete MFA if required

### Step 3: Admin Verification

The system verifies you have one of the following roles:
- Global Administrator
- SharePoint Administrator
- Application Administrator

> **Note**: If you don't have admin permissions, the onboarding will fail with an error message.

### Step 4: Admin Consent

Grant the application the following permissions:

**Microsoft Graph API Permissions**:
- `Sites.ReadWrite.All` - Manage SharePoint sites
- `User.Read.All` - Read user profiles
- `Directory.Read.All` - Read directory data
- `User.Invite.All` - Invite external users

**SharePoint Permissions**:
- `Sites.FullControl.All` - Full control of SharePoint sites

To grant consent:
1. Review the permissions list carefully
2. Click "Accept" to grant admin consent
3. Wait for confirmation

### Step 5: Resource Provisioning

The system automatically provisions:
- Tenant record in master database
- Tenant-specific SQL database
- Azure Cosmos DB containers (if applicable)
- Secure connection strings in Azure Key Vault
- Initial subscription (30-day trial)

This process typically takes 2-3 minutes.

### Step 6: Configuration

Complete the onboarding wizard:
1. **Tenant Settings**:
   - Company name
   - Primary domain
   - Administrator email
2. **SharePoint Configuration**:
   - Root site URL
   - App catalog URL
3. **Subscription Selection**:
   - Choose your plan (or continue with trial)
   - Enter billing information (can be skipped during trial)

## Azure AD Configuration

### Create Multi-Tenant Azure AD Application

1. **Navigate to Azure Portal**: https://portal.azure.com
2. **Azure Active Directory** → **App registrations** → **New registration**
3. **Configure Application**:
   - Name: `ClientSpace-API`
   - Supported account types: **Multitenant and personal Microsoft accounts**
   - Redirect URI: `https://your-api-url.azurewebsites.net/signin-oidc`

4. **API Permissions**:
   - Add Microsoft Graph permissions:
     - `Sites.ReadWrite.All` (Application)
     - `User.Read.All` (Application)
     - `Directory.Read.All` (Application)
     - `User.Invite.All` (Application)
   - Click "Grant admin consent"

5. **Certificates & Secrets**:
   - Create a new client secret
   - Copy the secret value (you won't see it again)
   - Store securely in Azure Key Vault

6. **Expose an API**:
   - Set Application ID URI: `api://your-app-id`
   - Add scope: `access_as_user`

### Create Blazor Portal App Registration

1. **Create Second App Registration**: `ClientSpace-Portal`
2. **Supported account types**: Multitenant
3. **Redirect URIs**:
   - `https://your-portal-url.azurewebsites.net/signin-oidc`
   - `https://localhost:7001/signin-oidc` (for local development)

4. **API Permissions**:
   - Add `User.Read` (Delegated)
   - Add permission to access your API: `api://your-api-app-id/access_as_user`

5. **Configure Token**:
   - Token configuration → Add optional claims
   - ID token: `email`, `preferred_username`

## Backend API Deployment

### Option 1: Automated Deployment (Recommended)

```bash
# Clone the repository
git clone https://github.com/orkinosai25-org/sharepoint-external-user-manager.git
cd sharepoint-external-user-manager

# Run the deployment script
./deploy-dev.sh
```

The script will:
- Create resource group
- Deploy Azure infrastructure (Bicep)
- Build and deploy API
- Configure app settings

### Option 2: Manual Deployment

#### Deploy Infrastructure

```bash
# Login to Azure
az login

# Set subscription
az account set --subscription "Your-Subscription-Name"

# Create resource group
az group create \
  --name rg-clientspace-prod \
  --location eastus

# Deploy infrastructure
az deployment group create \
  --resource-group rg-clientspace-prod \
  --template-file infra/bicep/main.bicep \
  --parameters environment=prod
```

#### Build and Deploy API

**For Azure Functions (TypeScript)**:
```bash
# Navigate to API directory
cd src/api-dotnet

# Restore dependencies
npm install

# Build the API
npm run build

# Deploy to Azure Functions
func azure functionapp publish clientspace-api-prod
```

**For .NET Web API** (if using the new ASP.NET Core API):
```bash
# Navigate to Web API directory
cd src/api-dotnet/WebApi/SharePointExternalUserManager.Api

# Restore dependencies
dotnet restore

# Build the API
dotnet build -c Release

# Publish for deployment
dotnet publish -c Release -o ./publish

# Deploy to Azure App Service
az webapp deploy \
  --name clientspace-api-prod \
  --resource-group rg-clientspace-prod \
  --src-path ./publish \
  --type zip
```

> **Note**: The project includes both TypeScript Azure Functions (legacy) and .NET Web API. Use the approach that matches your deployment.

#### Configure App Settings

```bash
# Set environment variables
az functionapp config appsettings set \
  --name clientspace-api-prod \
  --resource-group rg-clientspace-prod \
  --settings \
    "AzureAd__ClientId=your-client-id" \
    "AzureAd__ClientSecret=@Microsoft.KeyVault(SecretUri=https://your-vault.vault.azure.net/secrets/api-client-secret/)" \
    "AzureAd__TenantId=organizations" \
    "ConnectionStrings__DefaultConnection=@Microsoft.KeyVault(SecretUri=https://your-vault.vault.azure.net/secrets/sql-connection/)"
```

## Blazor Portal Deployment

### Build the Portal

```bash
# Navigate to portal directory
cd src/portal-blazor/SharePointExternalUserManager.Portal

# Restore dependencies
dotnet restore

# Build for production
dotnet publish -c Release -o ./publish
```

### Deploy to Azure App Service

```bash
# Create App Service Plan
az appservice plan create \
  --name asp-clientspace-prod \
  --resource-group rg-clientspace-prod \
  --sku B2 \
  --is-linux

# Create Web App
az webapp create \
  --name clientspace-portal-prod \
  --resource-group rg-clientspace-prod \
  --plan asp-clientspace-prod \
  --runtime "DOTNET|8.0"

# Deploy the application
az webapp deploy \
  --name clientspace-portal-prod \
  --resource-group rg-clientspace-prod \
  --src-path ./publish \
  --type zip
```

### Configure Portal Settings

```bash
# Set environment variables
az webapp config appsettings set \
  --name clientspace-portal-prod \
  --resource-group rg-clientspace-prod \
  --settings \
    "AzureAd__ClientId=your-portal-client-id" \
    "AzureAd__ClientSecret=@Microsoft.KeyVault(SecretUri=https://your-vault.vault.azure.net/secrets/portal-client-secret/)" \
    "AzureAd__TenantId=organizations" \
    "ApiSettings__BaseUrl=https://clientspace-api-prod.azurewebsites.net/api" \
    "StripeSettings__PublishableKey=pk_live_your_key"
```

## SPFx Client Installation

The SharePoint Framework client is installed by customers in their own SharePoint tenants.

### Build SPFx Package

```bash
# Navigate to SPFx directory
cd src/client-spfx

# Install dependencies
npm install

# Build the solution
npm run build

# Create deployment package
npm run package-solution

# Package location: sharepoint/solution/sharepoint-external-user-manager.sppkg
```

### Deploy to SharePoint

#### Upload to App Catalog

1. **Navigate to SharePoint Admin Center**: https://your-tenant-admin.sharepoint.com
2. **Go to Apps** → **App Catalog**
3. **Upload the Package**:
   - Click "Upload"
   - Select `sharepoint-external-user-manager.sppkg`
4. **Deploy**:
   - Check "Make this solution available to all sites"
   - Click "Deploy"

#### Add to Site

1. **Navigate to your SharePoint site**
2. **Edit a page**
3. **Add a web part**:
   - Search for "ClientSpace"
   - Select "Client Dashboard" or "External User Manager"
4. **Configure the web part**:
   - Set API URL (if not auto-configured)
   - Set tenant ID
5. **Save and publish the page**

### Configure Web Part Properties

Each web part can be configured with:

**Client Dashboard**:
- API Base URL: `https://clientspace-api-prod.azurewebsites.net/api`
- Tenant ID: Your tenant identifier
- Refresh Interval: 300000 (5 minutes)

**External User Manager**:
- API Base URL: Same as above
- Tenant ID: Same as above
- Auto-refresh: Enabled

## Initial Configuration

### Configure Database

**For .NET Web API with Entity Framework**:
```bash
# Navigate to Web API directory
cd src/api-dotnet/WebApi/SharePointExternalUserManager.Api

# Run migrations
dotnet ef database update

# Seed initial data (optional)
dotnet run --seed
```

**For TypeScript Azure Functions**:
The database schema is managed through SQL scripts in the `database/` directory. Run the initialization scripts:
```bash
cd src/api-dotnet/database
# Execute SQL scripts against your Azure SQL database
```

### Configure Stripe (Optional)

1. **Create Stripe Account**: https://dashboard.stripe.com/register
2. **Get API Keys**:
   - Publishable key: `pk_test_...` or `pk_live_...`
   - Secret key: `sk_test_...` or `sk_live_...`
3. **Store in Key Vault**:
   ```bash
   az keyvault secret set \
     --vault-name your-keyvault \
     --name stripe-secret-key \
     --value "sk_live_..."
   ```
4. **Configure Webhooks**:
   - Endpoint: `https://clientspace-api-prod.azurewebsites.net/api/webhooks/stripe`
   - Events: `customer.subscription.created`, `customer.subscription.updated`, `customer.subscription.deleted`

### Configure Monitoring

1. **Application Insights**:
   - Automatically configured via Bicep deployment
   - Instrumentation key stored in app settings
2. **Log Analytics**:
   - Review logs in Azure Portal
   - Set up alerts for errors and performance
3. **Azure Monitor**:
   - Configure metrics and dashboards

## Verification

### Verify Backend API

```bash
# Health check
curl https://clientspace-api-prod.azurewebsites.net/api/health

# Expected response: {"status": "healthy", "version": "1.0.0"}
```

### Verify Portal

1. Navigate to: `https://clientspace-portal-prod.azurewebsites.net`
2. Verify you can log in
3. Check configuration page: `/config-check`
4. Expected: All green checkmarks

### Verify SPFx Client

1. Navigate to SharePoint site with web part
2. Verify web part loads without errors
3. Test functionality:
   - View client dashboard
   - List external users
   - Create test library (optional)

### Verify Integration

1. **Portal → API**: 
   - Login to portal
   - Navigate to Tenants page
   - Verify tenants are listed
2. **SPFx → API**:
   - Open web part in SharePoint
   - Verify data loads from API
   - Check browser console for errors
3. **Stripe** (if configured):
   - Create test subscription
   - Verify webhook receives event

## Troubleshooting

### Common Issues

#### "Application with identifier was not found"

**Cause**: Azure AD application not configured or wrong Client ID

**Solution**:
1. Verify Client ID in app settings
2. Check Azure AD app registration exists
3. Ensure redirect URIs are correct

#### "Insufficient permissions"

**Cause**: Admin consent not granted

**Solution**:
1. Navigate to Azure AD app registration
2. Go to API Permissions
3. Click "Grant admin consent"

#### "SPFx package won't deploy"

**Cause**: Node version incompatibility or build errors

**Solution**:
1. Check Node version: `node --version` (should be 16.x or 18.x)
2. Clean build: `npm run clean && npm install && npm run build`
3. Trust dev certificate: `gulp trust-dev-cert`

#### "Database connection failed"

**Cause**: Connection string not configured or SQL firewall

**Solution**:
1. Verify connection string in Key Vault
2. Check Azure SQL firewall rules
3. Add App Service outbound IP to SQL firewall

#### "API returns 401 Unauthorized"

**Cause**: Token validation failing

**Solution**:
1. Verify Azure AD configuration
2. Check token audience and issuer
3. Ensure time sync on servers

### Getting Help

- **Documentation**: See [docs/README.md](./README.md)
- **Developer Guide**: See [DEVELOPER_GUIDE.md](../DEVELOPER_GUIDE.md)
- **Architecture**: See [ARCHITECTURE.md](../ARCHITECTURE.md)
- **GitHub Issues**: Create an issue for bugs or feature requests

## Next Steps

After installation:

1. **Review User Guide**: See [USER_GUIDE.md](./USER_GUIDE.md) for how to use the system
2. **Configure Tenant Settings**: Customize your tenant in the portal
3. **Invite Users**: Start inviting external users to collaborate
4. **Set Up Monitoring**: Configure alerts and dashboards
5. **Plan Training**: Train your team on using ClientSpace

## Security Best Practices

- ✅ Store all secrets in Azure Key Vault
- ✅ Use managed identities where possible
- ✅ Enable Azure AD Conditional Access
- ✅ Configure firewall rules on all Azure resources
- ✅ Enable audit logging
- ✅ Review security recommendations regularly
- ✅ Keep all components up to date

---

**Installation Support**: For assistance with installation, please contact your system administrator or refer to the troubleshooting section above.
