# Quick Fix: AADSTS7000218 Authentication Error

## Problem

When you click "Sign In" on the application, you see this error:

```
OpenIdConnectProtocolException: Message contains error: 'invalid_client', 
error_description: 'AADSTS7000218: The request body must contain the following 
parameter: 'client_assertion' or 'client_secret'.
```

## Root Cause

The Azure AD `ClientSecret` is not configured in your Azure App Service. The application cannot authenticate users without this credential.

## Quick Fix (5 Minutes)

### Step 1: Get Your ClientSecret

You need to get the client secret from your Azure AD app registration:

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to **Azure Active Directory** (or **Microsoft Entra ID**)
3. Click **App registrations** in the left menu
4. Find your application (Client ID: `61def48e-a9bc-43ef-932b-10eabef14c2a`)
5. Click **Certificates & secrets** in the left menu

#### Option A: Use Existing Secret (If You Have It)
- If you saved the secret value when you created it, use that value
- Go to Step 2 below

#### Option B: Create New Secret
1. Click **New client secret**
2. Add description: `ClientSpace Portal - 2026`
3. Choose expiration: `12 months` (or as per your security policy)
4. Click **Add**
5. **IMMEDIATELY COPY THE VALUE** - you cannot see it again!
6. Go to Step 2 below

### Step 2: Configure Azure App Service

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to your **ClientSpace App Service**
3. In the left menu, click **Settings** → **Environment variables**
   - In older Azure Portal: **Settings** → **Configuration**
4. Look for the setting `AzureAd__ClientSecret`
   - If it exists but is empty, click **Edit**
   - If it doesn't exist, click **New application setting**
5. Set the values:
   - **Name**: `AzureAd__ClientSecret` (use double underscores `__`)
   - **Value**: Paste the client secret value from Step 1
6. Click **OK**
7. Click **Save** at the top
8. Click **Continue** when prompted about restarting

### Step 3: Restart App Service

1. Click **Overview** in the left menu
2. Click **Restart** at the top
3. Click **Yes** to confirm
4. Wait 30-60 seconds for the restart to complete

### Step 4: Test the Fix

1. Open your application URL: `https://clientspace.azurewebsites.net`
2. Click **Sign In**
3. You should now be redirected to Microsoft login
4. Sign in with your Microsoft account
5. You should be successfully authenticated!

## What Changed in This Fix

Previously, the application would start even without the `ClientSecret`, but then fail at runtime when you tried to sign in. This caused the confusing AADSTS7000218 error.

**Now**, the application will **not start** if the `ClientSecret` is missing. Instead, you'll see a clear error message with instructions on how to configure it. This is a "fail fast" approach that makes configuration errors obvious and easier to fix.

## Verify Your Configuration

After following the steps above, your Azure App Service should have these three settings configured:

| Setting Name | Value | Status |
|--------------|-------|--------|
| `AzureAd__ClientId` | `61def48e-a9bc-43ef-932b-10eabef14c2a` | ✅ Already configured |
| `AzureAd__TenantId` | `b884f3d2-f3d0-4e67-8470-bc7b0372ebb6` | ✅ Already configured |
| `AzureAd__ClientSecret` | `[your secret value]` | ⚠️ **NEEDS TO BE CONFIGURED** |

## Still Having Issues?

If you still see errors after following these steps:

1. **Check the secret value**: Make sure you copied the entire secret value without extra spaces
2. **Check the setting name**: It must be `AzureAd__ClientSecret` with double underscores (`__`)
3. **Verify the app registration**: Make sure the ClientId matches the app registration in Azure AD
4. **Check the redirect URI**: In Azure AD app registration, verify the redirect URI is: `https://clientspace.azurewebsites.net/signin-oidc`

## Additional Resources

- [Complete Azure App Service Setup Guide](./AZURE_APP_SERVICE_SETUP.md)
- [Configuration Guide](./CONFIGURATION_GUIDE.md)
- [Portal README](./src/portal-blazor/SharePointExternalUserManager.Portal/README.md)

## Security Best Practices

- **Never commit secrets to source control** - Always use Azure App Service Configuration or Key Vault
- **Rotate secrets regularly** - Set expiration dates and rotate before they expire
- **Use Azure Key Vault** - For production, consider storing secrets in Azure Key Vault
- **Limit permissions** - Only give the Azure AD app the minimum required permissions

---

**Last Updated**: 2026-02-25  
**Issue**: AADSTS7000218 - Missing client_secret parameter  
**Status**: ✅ Fixed with fail-fast validation
