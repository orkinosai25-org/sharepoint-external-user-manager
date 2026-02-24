# Quick Fix Guide - Workflow Configuration Error

## Problem
Your application was failing to start with this error:
```
InvalidOperationException: Application configuration is invalid. 
Required Azure AD settings (ClientId, ClientSecret, TenantId) are missing.
```

## Solution
The workflow has been fixed to properly read Azure AD configuration from GitHub Actions secrets and inject them into the application during deployment.

## What You Need to Do

### Step 1: Configure GitHub Actions Secrets (If Not Already Done)

1. Go to your repository: https://github.com/orkinosai25-org/sharepoint-external-user-manager
2. Click **Settings** → **Secrets and variables** → **Actions**
3. Add these secrets (if they don't exist):
   - `AZURE_AD_CLIENT_ID` = Your Azure AD Application Client ID
   - `AZURE_AD_CLIENT_SECRET` = Your Azure AD Application Client Secret
   - `AZURE_AD_TENANT_ID` = Your Azure AD Tenant ID

### Step 2: Configure Azure App Service

**This is the CRITICAL step that was likely missing:**

1. Go to **Azure Portal** (https://portal.azure.com)
2. Find your **ClientSpace** App Service
3. Click **Settings** → **Environment variables** (or **Configuration**)
4. Add this application setting:
   - **Name**: `ASPNETCORE_ENVIRONMENT`
   - **Value**: `Production`
5. Click **Save**
6. Click **Continue** to restart the app

### Step 3: Verify

1. Wait for the app to restart (1-2 minutes)
2. Visit your application URL
3. Try to sign in
4. If it works, you're done! ✅

## Troubleshooting

### Still getting configuration error?

**Check the workflow logs:**
1. Go to Actions tab in GitHub
2. Look for the latest "Deploy Portal to ClientSpace (Production)" workflow
3. Check if it says "✅ Production configuration created with secrets from repository"
4. If not, verify GitHub Actions secrets are configured correctly

**Check Azure App Service:**
1. Verify `ASPNETCORE_ENVIRONMENT` is set to `Production`
2. Restart the App Service
3. Check Log Stream (Monitoring → Log stream) for any errors

### Sign-in fails but app starts?

This could be an Azure AD configuration issue:
- Verify ClientId and TenantId are correct
- Check if ClientSecret has expired
- Ensure the Azure AD App Registration exists

## Alternative: Use Azure Environment Variables Only

If you prefer NOT to use GitHub Actions secrets, you can configure everything in Azure App Service:

1. In Azure Portal → ClientSpace App Service → Environment variables
2. Add these settings:
   - `ASPNETCORE_ENVIRONMENT` = `Production`
   - `AzureAd__ClientId` = Your Azure AD Application Client ID  
   - `AzureAd__ClientSecret` = Your Azure AD Application Client Secret
   - `AzureAd__TenantId` = Your Azure AD Tenant ID

Note: Use double underscores (`__`) in Azure environment variable names.

## What Changed

1. **Workflow (.github/workflows/main_clientspace.yml)**
   - Now creates complete `appsettings.Production.json` with all required fields
   - Verifies the file is included in deployment
   - Provides clear deployment instructions

2. **Documentation**
   - Added AZURE_APP_SERVICE_CONFIGURATION.md with detailed setup guide
   - Updated error messages to guide you to the right documentation

3. **Security**
   - Removed secret exposure from workflow logs
   - Ensured appsettings.Production.json is never committed to source control

## Need More Help?

See these detailed guides:
- [AZURE_APP_SERVICE_CONFIGURATION.md](AZURE_APP_SERVICE_CONFIGURATION.md) - Complete Azure configuration guide
- [WORKFLOW_FIX_IMPLEMENTATION_SUMMARY.md](WORKFLOW_FIX_IMPLEMENTATION_SUMMARY.md) - Technical details
- [WORKFLOW_FIX_SECURITY_SUMMARY.md](WORKFLOW_FIX_SECURITY_SUMMARY.md) - Security information

## TL;DR

**The fix is complete, but you MUST set `ASPNETCORE_ENVIRONMENT=Production` in Azure App Service environment variables for it to work!**

This tells the application to use the `appsettings.Production.json` file that contains your Azure AD configuration.
