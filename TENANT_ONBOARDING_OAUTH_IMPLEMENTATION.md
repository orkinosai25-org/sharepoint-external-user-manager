# Tenant Onboarding & OAuth Consent Flow - Implementation Summary

## Overview

This implementation adds Microsoft Graph admin consent flow to the tenant onboarding process. This allows the SaaS backend to call Microsoft Graph APIs on behalf of tenants to manage SharePoint external users.

## Architecture

### OAuth 2.0 Admin Consent Flow

```
┌──────────────────────────────────────────────────────────────────────┐
│                    Tenant Admin (User)                               │
└────────────────────────────┬─────────────────────────────────────────┘
                             │
                             ▼
┌──────────────────────────────────────────────────────────────────────┐
│  1. User navigates to /onboarding/consent page in Blazor Portal     │
│     - Clicks "Grant Permissions" button                              │
└────────────────────────────┬─────────────────────────────────────────┘
                             │
                             ▼
┌──────────────────────────────────────────────────────────────────────┐
│  2. Blazor Portal calls POST /auth/connect API endpoint             │
│     - Sends redirectUri                                              │
└────────────────────────────┬─────────────────────────────────────────┘
                             │
                             ▼
┌──────────────────────────────────────────────────────────────────────┐
│  3. Backend generates admin consent URL with state parameter         │
│     - URL: https://login.microsoftonline.com/common/v2.0/           │
│             adminconsent?client_id=...&redirect_uri=...&state=...    │
│     - Returns URL to frontend                                        │
└────────────────────────────┬─────────────────────────────────────────┘
                             │
                             ▼
┌──────────────────────────────────────────────────────────────────────┐
│  4. Frontend redirects user to Microsoft login page                  │
│     - User authenticates with Microsoft                              │
│     - User sees consent screen with requested permissions            │
│     - User grants consent (or denies)                                │
└────────────────────────────┬─────────────────────────────────────────┘
                             │
                             ▼
┌──────────────────────────────────────────────────────────────────────┐
│  5. Microsoft redirects back to GET /auth/callback                   │
│     - With authorization code and state                              │
│     - Or with error if consent denied                                │
└────────────────────────────┬─────────────────────────────────────────┘
                             │
                             ▼
┌──────────────────────────────────────────────────────────────────────┐
│  6. Backend exchanges authorization code for tokens                  │
│     - Calls Microsoft token endpoint                                 │
│     - Receives access_token, refresh_token, expires_in               │
└────────────────────────────┬─────────────────────────────────────────┘
                             │
                             ▼
┌──────────────────────────────────────────────────────────────────────┐
│  7. Backend stores tokens in TenantAuth table                        │
│     - accessToken (encrypted in production)                          │
│     - refreshToken (encrypted in production)                         │
│     - tokenExpiresAt                                                 │
│     - scope, consentGrantedBy, consentGrantedAt                      │
└────────────────────────────┬─────────────────────────────────────────┘
                             │
                             ▼
┌──────────────────────────────────────────────────────────────────────┐
│  8. Backend validates granted permissions                            │
│     - Calls Microsoft Graph to check permissions                     │
│     - Logs audit event: TenantConsentGranted                         │
└────────────────────────────┬─────────────────────────────────────────┘
                             │
                             ▼
┌──────────────────────────────────────────────────────────────────────┐
│  9. Backend redirects to /onboarding/consent?admin_consent=True      │
│     - Frontend detects successful callback                           │
│     - Frontend validates permissions with GET /auth/permissions      │
│     - Shows success message                                          │
└──────────────────────────────────────────────────────────────────────┘
```

## Database Schema

### TenantAuth Table

```sql
CREATE TABLE [dbo].[TenantAuth] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [TenantId] INT NOT NULL,
    [AccessToken] NVARCHAR(MAX) NULL,              -- OAuth access token
    [RefreshToken] NVARCHAR(MAX) NULL,             -- OAuth refresh token
    [TokenExpiresAt] DATETIME2 NULL,               -- Token expiration timestamp
    [Scope] NVARCHAR(MAX) NULL,                    -- Granted OAuth scopes
    [ConsentGrantedBy] NVARCHAR(255) NULL,         -- Admin who granted consent
    [ConsentGrantedAt] DATETIME2 NULL,             -- When consent was granted
    [LastTokenRefresh] DATETIME2 NULL,             -- Last token refresh timestamp
    [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [ModifiedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [PK_TenantAuth] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TenantAuth_Tenant] FOREIGN KEY ([TenantId]) 
        REFERENCES [dbo].[Tenant] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [UQ_TenantAuth_TenantId] UNIQUE ([TenantId])
);
```

## API Endpoints

### POST /auth/connect

Initiates the OAuth admin consent flow.

**Request:**
```json
{
  "redirectUri": "https://portal.example.com/onboarding/consent"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "authorizationUrl": "https://login.microsoftonline.com/common/v2.0/adminconsent?...",
    "state": "base64-encoded-state-data"
  }
}
```

### GET /auth/callback

Handles OAuth callback from Microsoft.

**Query Parameters:**
- `code`: Authorization code from Microsoft
- `state`: State parameter for CSRF protection
- `tenant`: Tenant ID from Microsoft
- `admin_consent`: Whether consent was granted ("True" or "False")
- `error`: Error code if consent failed
- `error_description`: Error description if consent failed

**Response:**
- Redirects to success URL with 302 status

### GET /auth/permissions

Validates granted Microsoft Graph permissions.

**Response:**
```json
{
  "success": true,
  "data": {
    "hasRequiredPermissions": true,
    "grantedPermissions": [
      "User.Read.All",
      "Sites.ReadWrite.All",
      "Sites.FullControl.All",
      "Directory.Read.All"
    ],
    "missingPermissions": [],
    "tokenExpired": false,
    "tokenRefreshed": false,
    "consentGrantedAt": "2026-02-17T22:00:00Z",
    "consentGrantedBy": "admin@contoso.com"
  }
}
```

## Required Microsoft Graph Permissions

| Permission | Type | Purpose |
|------------|------|---------|
| `User.Read.All` | Application | Read all user profiles for external user management |
| `Sites.ReadWrite.All` | Application | Manage SharePoint sites and permissions |
| `Sites.FullControl.All` | Application | Full control for external sharing configuration |
| `Directory.Read.All` | Application | Read directory data |

## UI Components

### /onboarding/consent Page

A dedicated Blazor page that:
1. Explains why permissions are needed
2. Lists all required permissions with descriptions
3. Initiates the consent flow
4. Handles OAuth callback
5. Validates permissions were granted
6. Shows success/error messages

### Dashboard Banner

The dashboard shows a warning banner if:
- Tenant has not completed OAuth consent
- Required permissions are missing
- Token has expired and cannot be refreshed

## Security Considerations

### State Parameter

The `state` parameter is used for CSRF protection:
- Generated using crypto.randomBytes(32)
- Includes tenant context and timestamp
- Base64 encoded for transmission
- Validated on callback to prevent CSRF attacks
- Expires after 10 minutes

### Token Storage

**Current Implementation (Development):**
- Tokens stored in SQL Server as plain text
- Suitable for development/testing only

**Production Requirements:**
- Tokens MUST be encrypted at rest
- Use Azure Key Vault for token storage
- Or encrypt tokens with Azure Key Vault-managed keys before storing in database
- Implement token rotation policy
- Set up alerts for failed token refreshes

### Token Refresh

- Access tokens expire after 1 hour (typical)
- Refresh tokens used to obtain new access tokens
- Automatic token refresh before API calls
- Falls back to requiring re-consent if refresh fails

## Testing

### Manual Testing Steps

1. **Initial Setup:**
   ```bash
   # Run database migration
   cd src/api-dotnet/database/migrations
   sqlcmd -S <server> -d <database> -i 004_tenant_auth_tokens.sql
   ```

2. **Start Backend:**
   ```bash
   cd src/api-dotnet
   npm install --ignore-scripts
   npm run build
   func start
   ```

3. **Start Portal:**
   ```bash
   cd src/portal-blazor/SharePointExternalUserManager.Portal
   dotnet run
   ```

4. **Test Consent Flow:**
   - Navigate to https://localhost:7001
   - Sign in with Microsoft account
   - Go to /onboarding/consent
   - Click "Grant Permissions"
   - Complete Microsoft consent screen
   - Verify redirect back to portal with success message

5. **Verify Database:**
   ```sql
   SELECT * FROM TenantAuth WHERE TenantId = <your-tenant-id>
   ```

6. **Test Permissions Validation:**
   - Go to Dashboard
   - Verify no warning banner appears
   - Call GET /auth/permissions endpoint
   - Verify all required permissions are granted

### Automated Tests

Create test cases for:
- OAuth service URL generation
- Token exchange
- Token refresh logic
- Permission validation
- State parameter validation
- Error handling (consent denied, invalid code, etc.)

## Environment Variables

Add these to your Azure Functions configuration:

```bash
# Existing (already configured)
AZURE_CLIENT_ID=<your-app-client-id>
AZURE_CLIENT_SECRET=<your-app-client-secret>
AZURE_TENANT_ID=common

# Database (already configured)
SQL_SERVER=<your-sql-server>
SQL_DATABASE=<your-database>
SQL_USER=<sql-user>
SQL_PASSWORD=<sql-password>
```

## Deployment Checklist

- [ ] Run database migration 004_tenant_auth_tokens.sql
- [ ] Update Azure AD app registration with redirect URI
- [ ] Add API permissions to Azure AD app:
  - User.Read.All
  - Sites.ReadWrite.All
  - Sites.FullControl.All
  - Directory.Read.All
- [ ] Grant admin consent in Azure Portal
- [ ] Implement token encryption for production
- [ ] Set up Azure Key Vault for token storage
- [ ] Configure CORS to allow portal origin
- [ ] Test end-to-end flow in staging
- [ ] Monitor audit logs for TenantConsentGranted events
- [ ] Set up alerts for failed token refreshes

## Troubleshooting

### "Admin consent was not granted"

**Cause:** User clicked "Cancel" or lacks admin privileges

**Solution:** 
- Ensure user is Global Admin or SharePoint Admin
- Try consent flow again

### "Missing required permissions"

**Cause:** Some permissions were not granted during consent

**Solution:**
- Check Azure AD app registration for configured permissions
- Grant admin consent in Azure Portal
- Retry consent flow

### "Token has expired"

**Cause:** Access token expired and refresh failed

**Solution:**
- Check refresh token validity
- Retry consent flow to get new tokens
- Check for network/auth issues

### "State parameter has expired"

**Cause:** More than 10 minutes passed between initiating consent and callback

**Solution:**
- Retry consent flow
- User must complete consent within 10 minutes

## Future Enhancements

1. **Token Encryption**
   - Implement Azure Key Vault integration
   - Encrypt tokens before storing in database

2. **Multi-Region Support**
   - Handle sovereign cloud tenants (e.g., GCC, DoD)
   - Support different Microsoft login endpoints

3. **Permission Scope Management**
   - Allow admins to view granted permissions
   - Request additional permissions incrementally
   - Revoke permissions

4. **Token Monitoring**
   - Dashboard widget showing token health
   - Alerts for expiring/expired tokens
   - Automatic re-consent notifications

5. **Audit Trail**
   - Detailed logging of all OAuth operations
   - Track permission changes over time
   - Compliance reporting

## References

- [Microsoft Identity Platform - Admin Consent](https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-admin-consent)
- [Microsoft Graph Permissions Reference](https://docs.microsoft.com/en-us/graph/permissions-reference)
- [OAuth 2.0 Authorization Code Flow](https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-oauth2-auth-code-flow)
- [Azure Functions Security Best Practices](https://docs.microsoft.com/en-us/azure/azure-functions/security-concepts)

---

**Implementation Date:** 2026-02-17  
**Status:** ✅ Complete - Ready for Testing
