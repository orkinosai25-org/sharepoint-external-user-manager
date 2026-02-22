# Blazor SaaS Portal

## ⚠️ IMPORTANT: Configuration Required

**The application will not work until you configure Azure AD credentials!**

### Common Errors and Solutions

#### AADSTS7000218: Missing client_secret Error

If you see this error:
```
OpenIdConnectProtocolException: Message contains error: 'invalid_client', 
error_description: 'AADSTS7000218: The request body must contain the following 
parameter: 'client_assertion' or 'client_secret'.
```

**This means the Azure AD ClientSecret is not configured.**

**Solutions:**
- **For Azure App Service**: Follow [../../AZURE_APP_SERVICE_SETUP.md](../../AZURE_APP_SERVICE_SETUP.md) for detailed setup instructions
- **For local development**: Use User Secrets (see below)

#### AADSTS700016: Application not found Error

If you see an error like:
```
AADSTS700016: Application with identifier 'YOUR_CLIENT_ID' was not found...
```

This means the placeholder values in `appsettings.json` need to be replaced with actual Azure AD credentials.

### Quick Fix for Local Development

1. Register an app in [Azure Portal](https://portal.azure.com) (see [Azure AD Setup](#azure-ad-setup) below)
2. Configure credentials using User Secrets (recommended):
   ```bash
   dotnet user-secrets set "AzureAd:ClientId" "YOUR_ACTUAL_CLIENT_ID"
   dotnet user-secrets set "AzureAd:ClientSecret" "YOUR_ACTUAL_SECRET"
   dotnet user-secrets set "AzureAd:TenantId" "YOUR_TENANT_ID"
   ```
3. See [QUICKSTART.md](QUICKSTART.md) for detailed step-by-step instructions

### Configuration Guides

- **[Azure App Service Setup](../../AZURE_APP_SERVICE_SETUP.md)** - Complete guide for configuring the application in Azure App Service
- **[Configuration Guide](../../CONFIGURATION_GUIDE.md)** - General configuration information for all environments
- **[QUICKSTART.md](QUICKSTART.md)** - Quick start guide for local development

**Configuration Check:** Access `/config-check` in your browser to validate your configuration.

---

## Overview
The Blazor Web App portal provides the administrative interface for the SharePoint External User Manager SaaS platform. It handles pricing, onboarding, subscription management, and client space administration.

## Features

### Implemented (ISSUE-08)
- ✅ **Pricing Page** - Displays subscription tiers (Starter, Professional, Business, Enterprise)
- ✅ **Authentication** - Microsoft Entra ID (Azure AD) integration
- ✅ **Onboarding Wizard**
  - Sign in with Microsoft
  - Choose subscription plan
  - Stripe checkout integration
  - Completion confirmation
- ✅ **Dashboard**
  - Subscription status overview
  - Client space management
  - Create new client spaces
  - View provisioning status
  - SPFx package installation instructions

## Technology Stack
- **Framework**: ASP.NET Core Blazor Web App (.NET 8)
- **Authentication**: Microsoft Identity Web 4.3.0
- **UI**: Blazor Server with Bootstrap 5
- **HTTP Client**: HttpClient for API communication

## Project Structure

```
SharePointExternalUserManager.Portal/
├── Components/
│   ├── Auth/
│   │   └── RedirectToLogin.razor      # Redirects to sign-in
│   ├── Layout/
│   │   ├── MainLayout.razor           # Main layout with auth state
│   │   ├── NavMenu.razor              # Navigation menu
│   │   └── *.razor.css                # Scoped styles
│   ├── Pages/
│   │   ├── Home.razor                 # Landing page
│   │   ├── Pricing.razor              # Subscription plans
│   │   ├── Onboarding.razor           # Multi-step onboarding wizard
│   │   ├── OnboardingSuccess.razor    # Post-payment success page
│   │   └── Dashboard.razor            # Main dashboard
│   ├── App.razor                      # Root component
│   ├── Routes.razor                   # Routing configuration
│   └── _Imports.razor                 # Global using directives
├── Models/
│   ├── ApiModels.cs                   # DTOs for API communication
│   ├── ApiSettings.cs                 # API configuration
│   └── StripeSettings.cs              # Stripe configuration
├── Services/
│   └── ApiClient.cs                   # HTTP client for backend API
├── wwwroot/                           # Static files
├── Program.cs                         # Application startup
└── appsettings.json                   # Configuration
```

## Configuration

### Azure AD Setup

1. **Register Application in Azure Portal**
   - Go to Azure Active Directory → App registrations → New registration
   - Name: "SharePoint User Manager Portal"
   - Supported account types: "Accounts in any organizational directory (Any Azure AD directory - Multitenant)"
   - Redirect URI: `https://localhost:7001/signin-oidc` (for development)
   - After registration, note the **Application (client) ID** and **Directory (tenant) ID**

2. **Create Client Secret**
   - Go to Certificates & secrets → New client secret
   - Description: "Portal Secret"
   - Expiry: Choose appropriate duration
   - Copy the secret **Value** (you won't see it again)

3. **Configure API Permissions** (optional, for downstream API calls)
   - API permissions → Add a permission → Microsoft Graph
   - Delegated permissions: `User.Read`

### appsettings.json

Update `appsettings.json` or use environment variables:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "common",
    "ClientId": "YOUR_CLIENT_ID_FROM_AZURE_PORTAL",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc",
    "ClientSecret": "YOUR_CLIENT_SECRET"
  },
  "ApiSettings": {
    "BaseUrl": "https://localhost:7071/api",
    "Timeout": 30
  },
  "StripeSettings": {
    "PublishableKey": "pk_test_YOUR_STRIPE_PUBLISHABLE_KEY"
  }
}
```

**⚠️ Security Note**: Never commit secrets to source control. Use:
- User Secrets for local development
- Azure Key Vault for production
- Environment variables for CI/CD

## Local Development

### Prerequisites
- .NET 8 SDK or later
- Azure subscription (for Azure AD app registration)
- Stripe account (for billing integration)
- Backend API running on `localhost:7071`

### Running Locally

1. **Configure User Secrets** (recommended)
   ```bash
   cd SharePointExternalUserManager.Portal
   dotnet user-secrets set "AzureAd:ClientId" "YOUR_CLIENT_ID"
   dotnet user-secrets set "AzureAd:ClientSecret" "YOUR_CLIENT_SECRET"
   dotnet user-secrets set "StripeSettings:PublishableKey" "pk_test_YOUR_KEY"
   ```

2. **Start the Portal**
   ```bash
   dotnet run
   ```

3. **Access the Portal**
   - Navigate to `https://localhost:7001` (or the port shown in console)
   - You'll be redirected to sign in with Microsoft

### Development URLs

Update your Azure AD app registration with these redirect URIs:
- `https://localhost:7001/signin-oidc`
- `https://localhost:7001/signout-callback-oidc`

## API Integration

The portal communicates with the backend API via the `ApiClient` service:

### Endpoints Used
- `GET /billing/plans` - Get available subscription plans
- `POST /billing/checkout-session` - Create Stripe checkout session
- `GET /billing/subscription/status` - Get current subscription status
- `GET /clients` - List client spaces
- `POST /clients` - Create new client space
- `GET /clients/{id}` - Get specific client

### Authentication Flow
1. User signs in with Microsoft Entra ID
2. Portal receives JWT access token
3. `ApiClient` includes token in API requests
4. Backend validates token and extracts tenant ID
5. Data is filtered by tenant (multi-tenant isolation)

## Onboarding Flow

The onboarding wizard guides new users through subscription setup:

### Steps

1. **Sign In**
   - User authenticates with Microsoft
   - Portal extracts tenant ID from claims

2. **Choose Plan**
   - Display available plans (Starter, Professional, Business)
   - Enterprise requires contacting sales
   - User selects plan (monthly/annual)

3. **Payment**
   - Portal calls `POST /billing/checkout-session`
   - Redirects to Stripe Checkout
   - User completes payment

4. **Complete**
   - Stripe webhook activates subscription
   - User redirected to success page
   - Can access dashboard

## Deployment

### Azure App Service

1. **Create App Service**
   ```bash
   az webapp create \
     --resource-group rg-sharepoint-manager \
     --plan plan-sharepoint-manager \
     --name portal-sharepoint-manager \
     --runtime "DOTNETCORE|8.0"
   ```

2. **Configure App Settings**
   ```bash
   az webapp config appsettings set \
     --resource-group rg-sharepoint-manager \
     --name portal-sharepoint-manager \
     --settings \
       "AzureAd__ClientId=YOUR_CLIENT_ID" \
       "AzureAd__ClientSecret=@Microsoft.KeyVault(SecretUri=...)" \
       "ApiSettings__BaseUrl=https://api-sharepoint-manager.azurewebsites.net/api"
   ```

3. **Publish**
   ```bash
   dotnet publish -c Release
   # Deploy using Azure CLI, GitHub Actions, or Azure DevOps
   ```

### Environment Variables

Production deployments should use:
- **Key Vault** for secrets (ClientSecret, Stripe keys)
- **App Settings** for non-sensitive configuration
- **Managed Identity** for authentication

## Testing

### Manual Testing Checklist

- [ ] Home page loads without authentication
- [ ] Pricing page displays all plans correctly
- [ ] Sign in redirects to Microsoft login
- [ ] Onboarding wizard progresses through all steps
- [ ] Stripe checkout session is created
- [ ] Dashboard shows subscription status
- [ ] Client space creation works
- [ ] API errors are handled gracefully

### Local Testing Without API

The portal includes fallback data for pricing when the API is unavailable, allowing UI development without a running backend.

## Security Considerations

1. **Authentication**
   - All authenticated pages use `[Authorize]` attribute
   - Token validation handled by Microsoft Identity Web
   - Tenant ID extracted from JWT claims

2. **API Communication**
   - HTTPS only in production
   - JWT bearer tokens for authorization
   - CORS configured on API side

3. **Secrets Management**
   - Never commit secrets to git
   - Use Azure Key Vault in production
   - Rotate secrets regularly

4. **CSRF Protection**
   - Blazor includes built-in antiforgery tokens
   - Enabled via `app.UseAntiforgery()`

## Troubleshooting

### "Failed to load plans" Error
- Check that backend API is running
- Verify `ApiSettings:BaseUrl` is correct
- Check API CORS configuration

### Authentication Redirect Loop
- Verify Azure AD `ClientId` is correct
- Check redirect URIs match in Azure portal
- Ensure `TenantId` is set to "common" for multi-tenant

### Stripe Checkout Not Working
- Verify Stripe publishable key is correct
- Check API is creating checkout session successfully
- Ensure success/cancel URLs are configured

## Future Enhancements (Not in ISSUE-08)

- [ ] User management and team invitations
- [ ] Billing history and invoices
- [ ] Usage analytics dashboard
- [ ] Custom branding configuration
- [ ] API key management for integrations
- [ ] Audit log viewer

## Support

For issues or questions:
- Create an issue in the GitHub repository
- Contact: support@example.com (Enterprise customers)

---

**Implementation**: ISSUE-08 Complete  
**Last Updated**: February 2026  
**Framework**: ASP.NET Core Blazor (.NET 8)
