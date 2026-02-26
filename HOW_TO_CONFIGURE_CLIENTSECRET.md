# How to Configure ClientSecret in appsettings.json

## Quick Fix for AADSTS7000218 Error

If you're seeing this error:
```
AADSTS7000218: The request body must contain the following parameter: 'client_assertion' or 'client_secret'
```

**Solution**: Add your Azure AD ClientSecret to the appsettings.json file.

---

## Step 1: Get Your ClientSecret

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to **Azure Active Directory** (or **Microsoft Entra ID**)
3. Click **App registrations** in the left menu
4. Find your application with ClientId: `61def48e-a9bc-43ef-932b-10eabef14c2a`
5. Click **Certificates & secrets** in the left menu

### If you already have a secret:
- If you saved the secret value when you created it, use that value
- Go to Step 2

### If you need to create a new secret:
1. Click **New client secret**
2. Add description: `ClientSpace Portal - Dev`
3. Choose expiration: `12 months` (or as per your preference)
4. Click **Add**
5. **IMMEDIATELY COPY THE VALUE** - you cannot see it again!
6. Go to Step 2

---

## Step 2: Add ClientSecret to appsettings.json

### Option A: Edit appsettings.json directly (Simple)

1. Open `src/portal-blazor/SharePointExternalUserManager.Portal/appsettings.json`
2. Find the `AzureAd` section
3. Replace the empty `ClientSecret` value:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "b884f3d2-f3d0-4e67-8470-bc7b0372ebb6",
    "ClientId": "61def48e-a9bc-43ef-932b-10eabef14c2a",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc",
    "ClientSecret": "YOUR_ACTUAL_SECRET_VALUE_HERE"
  }
}
```

4. Save the file
5. Restart your application

**Note**: This file is in source control. If you plan to commit changes, consider using Option B below to keep secrets out of your repo.

### Option B: Use appsettings.Local.json (More Secure)

1. Create a new file: `src/portal-blazor/SharePointExternalUserManager.Portal/appsettings.Local.json`
2. Add the following content:

```json
{
  "AzureAd": {
    "ClientSecret": "YOUR_ACTUAL_SECRET_VALUE_HERE"
  }
}
```

3. Save the file
4. Restart your application

**Benefits**:
- This file is in `.gitignore` and won't be committed to source control
- Overrides the empty ClientSecret from appsettings.json
- Keeps your main appsettings.json clean

---

## Step 3: Verify the Fix

1. Start your application:
   ```bash
   cd src/portal-blazor/SharePointExternalUserManager.Portal
   dotnet run
   ```

2. You should see the app start successfully without critical errors

3. Open the application in your browser (usually http://localhost:5273)

4. Click **Sign In**

5. You should be redirected to Microsoft login page

6. After signing in, you should be authenticated successfully!

---

## What Changed

**Before this fix:**
- The validation was treating missing ClientSecret as a critical ERROR
- App would not start without the ClientSecret
- You had to use environment variables or user secrets

**After this fix:**
- Validation treats missing ClientSecret as a WARNING
- App starts and shows a warning message
- You can add your ClientSecret directly to appsettings.json
- All configuration methods work: appsettings.json, appsettings.Local.json, environment variables, user secrets

---

## Current Application Settings

Your appsettings.json currently has:
- ✅ **TenantId**: `b884f3d2-f3d0-4e67-8470-bc7b0372ebb6` (configured)
- ✅ **ClientId**: `61def48e-a9bc-43ef-932b-10eabef14c2a` (configured)
- ❌ **ClientSecret**: `""` (empty - **YOU NEED TO ADD THIS**)

---

## Security Notes

For development/personal apps:
- ✅ It's OK to put secrets in appsettings.json if it's your personal dev environment
- ✅ Consider using appsettings.Local.json to keep secrets out of source control

For production/shared environments:
- ⚠️ Use Azure App Service environment variables
- ⚠️ Consider using Azure Key Vault
- ⚠️ Never commit secrets to source control

---

## Troubleshooting

### Problem: App still shows warning about ClientSecret

**Solution**: Make sure you:
1. Added the secret value correctly (no extra spaces, quotes removed)
2. Saved the file
3. Restarted the application

### Problem: Authentication still fails

**Solution**: Verify:
1. The ClientSecret value is correct (copy it again from Azure Portal)
2. The ClientId matches your Azure AD app registration
3. The redirect URI in Azure AD includes: `http://localhost:5273/signin-oidc` (for local dev)

---

**Last Updated**: February 26, 2026  
**Status**: Ready to use  
**Commit**: bae4df2
