# Blazor Portal - Quick Start Guide

> **⚠️ CRITICAL:** This application requires Azure AD configuration before it will work!
> 
> **Error you might see:** `AADSTS700016: Application with identifier 'YOUR_CLIENT_ID' was not found`
> 
> **Why:** The placeholder values in configuration files must be replaced with real Azure AD credentials.

Get the SharePoint External User Manager portal running locally in 5 minutes.

## Prerequisites

- ✅ .NET 8 SDK installed
- ✅ Azure subscription (for Azure AD)
- ✅ Stripe account (for testing checkout)
- ✅ Backend API running on `localhost:7071`

## Step 1: Azure AD App Registration

1. Go to [Azure Portal](https://portal.azure.com) → Azure Active Directory → App registrations
2. Click "New registration"
3. Settings:
   - **Name**: SharePoint User Manager Portal (Dev)
   - **Supported account types**: Accounts in any organizational directory (Multitenant)
   - **Redirect URI**: Web → `https://localhost:7001/signin-oidc`
4. Click **Register**
5. Copy the **Application (client) ID** - you'll need this
6. Go to **Certificates & secrets** → **New client secret**
   - Description: "Dev Secret"
   - Expires: 6 months
   - Click **Add**
   - **Copy the Value immediately** (you won't see it again!)

## Step 2: Configure the Portal

Navigate to the portal directory:

```bash
cd src/portal-blazor/SharePointExternalUserManager.Portal
```

### Option A: User Secrets (Recommended for Development)

```bash
# Set Azure AD credentials
dotnet user-secrets set "AzureAd:ClientId" "YOUR_CLIENT_ID_FROM_STEP_1"
dotnet user-secrets set "AzureAd:ClientSecret" "YOUR_CLIENT_SECRET_FROM_STEP_1"

# Set Stripe (optional - use test key from https://dashboard.stripe.com/test/apikeys)
dotnet user-secrets set "StripeSettings:PublishableKey" "pk_test_YOUR_KEY"

# Set API URL (if API is running on different port)
dotnet user-secrets set "ApiSettings:BaseUrl" "http://localhost:7071/api"
```

### Option B: appsettings.Development.json

**⚠️ WARNING: Don't commit secrets to git!**

```json
{
  "AzureAd": {
    "ClientId": "YOUR_CLIENT_ID",
    "ClientSecret": "YOUR_CLIENT_SECRET"
  },
  "ApiSettings": {
    "BaseUrl": "http://localhost:7071/api"
  },
  "StripeSettings": {
    "PublishableKey": "pk_test_YOUR_KEY"
  }
}
```

## Step 3: Run the Backend API

In a separate terminal:

```bash
cd src/api-dotnet/WebApi/SharePointExternalUserManager.Api
dotnet run
```

Verify it's running on `http://localhost:7071`

## Step 4: Run the Portal

```bash
cd src/portal-blazor/SharePointExternalUserManager.Portal
dotnet run
```

You should see:

```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:7001
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:7000
```

## Step 5: Test the Portal

1. Open browser: `https://localhost:7001`
2. You should see the **Home page**
3. Click **Pricing** - should show subscription plans (loaded from API or fallback)
4. Click **Sign In** - redirects to Microsoft login
5. Sign in with your Microsoft/Azure AD account
6. After sign-in, you'll be redirected back to the portal
7. Navigate to **Dashboard** - shows subscription status and clients

## Expected Flow

```
Home → Pricing → Sign In (Microsoft) → Onboarding → Choose Plan → Stripe Checkout → Success → Dashboard
```

## Troubleshooting

### "The reply URL specified in the request does not match"

**Problem**: Azure AD redirect URI mismatch

**Solution**: 
1. Check Azure Portal → App Registration → Authentication
2. Add redirect URI: `https://localhost:7001/signin-oidc`
3. Save

### "Failed to load plans"

**Problem**: Portal can't reach the API

**Solution**:
1. Verify backend API is running: `curl http://localhost:7071/api/health`
2. Check `ApiSettings:BaseUrl` in user secrets or appsettings
3. Check browser console for CORS errors

### "IDX10205: Issuer validation failed"

**Problem**: Token issuer doesn't match expected value

**Solution**:
- Ensure `TenantId` is set to `"common"` in appsettings.json
- This allows multi-tenant authentication

### Build Errors

```bash
# Clean and rebuild
dotnet clean
dotnet build
```

## Quick Commands Reference

```bash
# Check .NET version
dotnet --version

# Restore packages
dotnet restore

# Build
dotnet build

# Run with specific port
dotnet run --urls "https://localhost:7001"

# View user secrets
dotnet user-secrets list

# Remove a user secret
dotnet user-secrets remove "AzureAd:ClientSecret"

# Clear all user secrets
dotnet user-secrets clear
```

## Testing Stripe Checkout

Use Stripe test mode with test cards:
- **Success**: 4242 4242 4242 4242
- **Decline**: 4000 0000 0000 0002
- Any future expiry date (e.g., 12/34)
- Any 3-digit CVC

Get test keys from: https://dashboard.stripe.com/test/apikeys

## Next Steps

Once the portal is running:

1. **Create a Test Subscription**
   - Sign in → Onboarding → Choose Starter plan
   - Complete Stripe checkout (test mode)
   - Verify subscription appears in Dashboard

2. **Create a Client Space**
   - Dashboard → "Create Client Space"
   - Fill in details
   - Watch provisioning status

3. **Explore the UI**
   - Check subscription limits
   - View client list
   - Test navigation between pages

## Production Deployment

For production deployment, see the main [README.md](README.md) for:
- Azure App Service setup
- Key Vault configuration
- Environment variable management
- Domain configuration
- SSL certificates

---

**Need Help?**
- Check the full [README.md](README.md)
- Review API documentation in `src/api-dotnet/README.md`
- Check GitHub Issues for known problems
