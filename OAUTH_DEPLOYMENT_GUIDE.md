# OAuth Tenant Onboarding - Deployment Guide

## Overview
This guide provides step-by-step instructions for deploying the OAuth tenant onboarding feature to production.

## Prerequisites

- Azure subscription with appropriate permissions
- Azure AD Global Administrator or Application Administrator access
- SQL Server database deployed
- API and Portal deployed to Azure App Service
- Visual Studio or .NET CLI for migrations

## Deployment Steps

### 1. Azure AD App Registration

#### Create or Update App Registration

1. Navigate to [Azure Portal](https://portal.azure.com)
2. Go to **Azure Active Directory** > **App registrations**
3. Select your existing app or create new:
   - **Name**: SharePoint External User Manager
   - **Supported account types**: Accounts in any organizational directory (Multi-tenant)
   - **Redirect URI**: Leave blank for now

#### Configure API Permissions

1. In your app registration, go to **API permissions**
2. Click **Add a permission**
3. Select **Microsoft Graph**
4. Choose **Application permissions**
5. Add the following permissions:
   - `User.Read.All` - Read all users' profiles
   - `Sites.ReadWrite.All` - Read and write items in all site collections
   - `Sites.FullControl.All` - Have full control of all site collections
   - `Directory.Read.All` - Read directory data

6. Click **Grant admin consent for [Your Organization]**
7. Wait for status to show "Granted"

#### Configure Redirect URIs

1. Go to **Authentication**
2. Click **Add a platform** > **Web**
3. Add redirect URIs:
   ```
   https://your-api.azurewebsites.net/auth/callback
   https://your-api-staging.azurewebsites.net/auth/callback (if using staging)
   ```
4. Under **Implicit grant and hybrid flows**: Leave unchecked
5. Under **Allow public client flows**: No
6. Click **Save**

#### Generate Client Secret

1. Go to **Certificates & secrets**
2. Click **New client secret**
3. Add description: "OAuth Tenant Onboarding"
4. Select expiration: 24 months (recommended)
5. Click **Add**
6. **CRITICAL**: Copy the secret value immediately (you won't be able to see it again)
7. Store securely in Azure Key Vault or password manager

### 2. Database Migration

#### Run Migration on Development

1. Connect to development database:
   ```bash
   cd src/api-dotnet/WebApi/SharePointExternalUserManager.Api
   
   # Update connection string in appsettings.Development.json or use environment variable
   export ConnectionStrings__DefaultConnection="Server=your-server;Database=your-db;..."
   
   dotnet ef database update
   ```

2. Verify migration:
   ```sql
   SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'TenantAuth'
   ```

#### Run Migration on Staging

1. Option A - Using EF CLI (recommended):
   ```bash
   export ConnectionStrings__DefaultConnection="<staging-connection-string>"
   dotnet ef database update
   ```

2. Option B - Generate SQL script:
   ```bash
   dotnet ef migrations script --idempotent --output migration.sql
   ```
   Then execute the SQL script in Azure SQL Database using Azure Portal or SSMS.

#### Run Migration on Production

1. **IMPORTANT**: Back up production database first!
   ```sql
   -- In Azure Portal: SQL Database > Automated backups > Create on-demand backup
   ```

2. Generate idempotent migration script:
   ```bash
   dotnet ef migrations script --idempotent --output production-migration.sql
   ```

3. Review the script carefully
4. Execute during maintenance window
5. Verify table creation:
   ```sql
   SELECT COUNT(*) FROM TenantAuth -- Should return 0
   ```

### 3. Azure App Service Configuration

#### API Configuration

1. Navigate to your API App Service in Azure Portal
2. Go to **Configuration** > **Application settings**
3. Add/Update the following settings:

   ```
   AzureAd__ClientId = <your-client-id-from-step-1>
   AzureAd__ClientSecret = <your-client-secret-from-step-1>
   AzureAd__TenantId = common
   AzureAd__Instance = https://login.microsoftonline.com/
   AzureAd__AllowedRedirectUris__0 = https://your-portal.azurewebsites.net/onboarding/consent
   AzureAd__AllowedRedirectUris__1 = https://your-portal-staging.azurewebsites.net/onboarding/consent
   ```

4. Click **Save** and wait for restart

#### Portal Configuration

1. Navigate to your Portal App Service in Azure Portal
2. Go to **Configuration** > **Application settings**
3. Verify these settings exist:
   ```
   AzureAd__ClientId = <same-client-id>
   AzureAd__ClientSecret = <same-client-secret>
   ApiSettings__BaseUrl = https://your-api.azurewebsites.net
   ```

4. Click **Save** and wait for restart

### 4. CORS Configuration

Ensure API allows requests from Portal:

1. In API App Service, go to **CORS**
2. Add portal origins:
   ```
   https://your-portal.azurewebsites.net
   https://your-portal-staging.azurewebsites.net (if applicable)
   ```
3. **Do not** select "Enable Access-Control-Allow-Credentials" (unless using cookies)
4. Click **Save**

### 5. Verify Deployment

#### Health Check

1. Test API is running:
   ```bash
   curl https://your-api.azurewebsites.net/health
   ```
   Expected: `{"status": "Healthy"}`

2. Test Portal is running:
   ```
   Navigate to: https://your-portal.azurewebsites.net
   ```
   Expected: Portal loads successfully

#### Database Verification

```sql
-- Check TenantAuth table exists
SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = 'TenantAuth'

-- Check indexes
SELECT name, type_desc 
FROM sys.indexes 
WHERE object_id = OBJECT_ID('TenantAuth')
```

#### OAuth Endpoints Check

```bash
# Test connect endpoint (requires authentication token)
curl -X POST https://your-api.azurewebsites.net/auth/connect \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"redirectUri":"https://your-portal.azurewebsites.net/onboarding/consent"}'
```

Expected: 200 OK with authorizationUrl in response

### 6. End-to-End Testing

#### Test OAuth Flow

1. Navigate to: `https://your-portal.azurewebsites.net`
2. Sign in with Microsoft account (as Global Admin)
3. Complete tenant registration if needed
4. Navigate to: `/onboarding/consent`
5. Click **"Grant Permissions"**
6. You should be redirected to Microsoft login
7. Review permissions requested
8. Click **"Accept"**
9. You should be redirected back to portal
10. Verify success message appears
11. Check dashboard for confirmation

#### Verify Database Entry

```sql
-- Check tenant auth was created
SELECT 
    t.OrganizationName,
    ta.ConsentGrantedBy,
    ta.ConsentGrantedAt,
    ta.TokenExpiresAt,
    ta.Scope
FROM TenantAuth ta
JOIN Tenants t ON ta.TenantId = t.Id
WHERE t.EntraIdTenantId = '<your-test-tenant-id>'
```

Expected: One row with valid tokens and consent information

#### Test Permission Validation

1. In portal, navigate to Dashboard
2. Check for any warning banners (should be none)
3. Or make API call:
   ```bash
   curl -X GET https://your-api.azurewebsites.net/auth/permissions \
     -H "Authorization: Bearer <token>"
   ```
4. Expected: `hasRequiredPermissions: true`

### 7. Monitoring Setup

#### Application Insights

1. Ensure Application Insights is connected to both API and Portal
2. Create custom alerts:
   - Failed token refreshes
   - OAuth callback errors
   - Permission validation failures

#### Audit Logs

Set up queries in Application Insights:

```kusto
// OAuth consent initiated
traces
| where message contains "OAUTH_CONNECT_INITIATED"
| project timestamp, message, customDimensions

// Consent granted successfully
traces  
| where message contains "TENANT_CONSENT_GRANTED"
| project timestamp, message, customDimensions

// Token refresh failures
traces
| where message contains "Failed to refresh access token"
| project timestamp, message, severityLevel
```

#### Security Alerts

Create alerts for:
1. Multiple failed consent attempts
2. Invalid state parameters (potential CSRF)
3. Unauthorized redirect URIs
4. Token refresh failures

### 8. Production Security Checklist

- [ ] Client secret stored in Azure Key Vault (not App Settings)
- [ ] HTTPS enforced on all endpoints
- [ ] CORS configured with specific origins (not *)
- [ ] Application Insights monitoring enabled
- [ ] Security alerts configured
- [ ] Database backups configured and tested
- [ ] Redirect URI allowlist configured
- [ ] Rate limiting enabled on OAuth endpoints
- [ ] Token encryption implemented (see Future Enhancements)

### 9. Rollback Procedure

If issues occur after deployment:

#### Immediate Rollback

1. Revert App Service to previous deployment:
   ```bash
   # In Azure Portal: Deployment Center > Deployment History > Redeploy
   ```

2. Or use Azure CLI:
   ```bash
   az webapp deployment slot swap \
     --name your-api \
     --resource-group your-rg \
     --slot staging \
     --target-slot production
   ```

#### Database Rollback

1. Restore database from backup (if issues persist):
   ```bash
   # In Azure Portal: SQL Database > Restore
   ```

2. Or remove migration:
   ```sql
   DROP TABLE TenantAuth;
   ```

### 10. Post-Deployment Tasks

- [ ] Send announcement to customers about new feature
- [ ] Update documentation with production URLs
- [ ] Schedule client secret rotation reminder
- [ ] Review and analyze OAuth usage metrics
- [ ] Plan for token encryption implementation

## Common Issues

### Issue: "Invalid client secret"
**Solution**: Regenerate client secret in Azure AD and update App Settings

### Issue: "Redirect URI mismatch"
**Solution**: Ensure redirect URI in Azure AD matches exactly (including https/http, trailing slash)

### Issue: "Missing permissions"
**Solution**: Re-grant admin consent in Azure AD App Registration

### Issue: "Token expired"
**Solution**: Normal behavior - tokens auto-refresh. If persistent, check refresh token validity.

### Issue: "CORS error"
**Solution**: Add portal origin to API CORS settings

## Support

For issues during deployment:
1. Check Application Insights logs
2. Check Azure App Service logs
3. Review database migration logs
4. Contact support team

## Next Steps

After successful deployment:
1. Implement token encryption with Azure Key Vault
2. Add monitoring dashboard
3. Set up automated client secret rotation
4. Plan for sovereign cloud support
5. Add MFA requirements for admin consent

---

**Document Version**: 1.0  
**Last Updated**: February 18, 2026  
**Owner**: DevOps Team
