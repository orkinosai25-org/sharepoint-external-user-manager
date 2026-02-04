# Tenant Onboarding Guide

## Overview

This document outlines the end-to-end tenant onboarding process for the SharePoint External User Manager SaaS solution, including Entra ID app registration, admin consent, and resource provisioning.

## Onboarding Flow Diagram

```
┌──────────────────────────────────────────────────────────────────────────┐
│                        Tenant Onboarding Flow                            │
└──────────────────────────────────────────────────────────────────────────┘

[1] Marketplace Discovery/Website
         │
         ▼
[2] Start Trial / Sign Up
    ─────────────────────
    Input: Email, Organization Name
         │
         ▼
[3] Redirect to Entra ID Login
    ─────────────────────────────
    Microsoft Authentication
         │
         ▼
[4] Verify Admin Identity
    ───────────────────────
    Check: Global Admin or SharePoint Admin Role
         │
         ├──[Not Admin]──► Error: Admin permissions required
         │
         ▼
[5] Admin Consent Required
    ───────────────────────
    Request Application Permissions:
    - Sites.ReadWrite.All
    - User.Read.All
    - Directory.Read.All
         │
         ├──[Consent Denied]──► Error: Permissions required
         │
         ▼
[6] Provision Tenant Resources
    ────────────────────────────
    - Create tenant record in master DB
    - Provision tenant-specific database
    - Initialize Cosmos DB containers
    - Store connection strings in Key Vault
    - Set subscription to "Trial" (30 days)
         │
         ▼
[7] Configure Initial Settings
    ────────────────────────────
    - Default policies
    - Email notifications
    - Admin accounts
         │
         ▼
[8] Redirect to Application
    ─────────────────────────
    SPFx Web Part or Admin Portal
         │
         ▼
[9] Onboarding Complete
    ─────────────────────
    Trial active for 30 days
```

## Prerequisites

### For Tenant Administrators

**Required Roles:**
- Global Administrator, OR
- SharePoint Administrator, OR
- Application Administrator

**Microsoft 365 Requirements:**
- Active Microsoft 365 subscription
- SharePoint Online enabled
- Entra ID (Azure AD) tenant
- Ability to consent to application permissions

**Browser Requirements:**
- Modern browser (Chrome, Edge, Firefox, Safari)
- JavaScript enabled
- Cookies enabled
- Pop-ups allowed for authentication

## Step-by-Step Onboarding Process

### Step 1: Initial Sign-Up

**Landing Page:** `https://spexternal.com/signup`

1. User fills out sign-up form:
   ```json
   {
     "email": "admin@contoso.com",
     "organizationName": "Contoso Corporation",
     "organizationSize": "50-200",
     "country": "United States",
     "phone": "+1-555-0100" (optional)
   }
   ```

2. System validates email format and checks for existing tenant

3. User clicks "Start Free Trial" button

### Step 2: Entra ID Authentication

User is redirected to Microsoft login:

```
https://login.microsoftonline.com/common/oauth2/v2.0/authorize?
  client_id=<app_client_id>&
  response_type=code&
  redirect_uri=https://api.spexternal.com/auth/callback&
  response_mode=query&
  scope=openid%20profile%20email%20offline_access&
  state=<random_state_token>&
  prompt=select_account
```

**What Happens:**
- User selects their work/school account
- Authenticates with Microsoft
- MFA prompt if enabled
- Consent screen (first-time only)

### Step 3: Verify Admin Identity

Backend validates the authenticated user:

```csharp
public async Task<AdminVerificationResult> VerifyAdminAsync(string accessToken)
{
    // Decode JWT token
    var claims = ValidateToken(accessToken);
    var tenantId = claims.FindFirst("tid").Value;
    var userId = claims.FindFirst("oid").Value;
    var userPrincipalName = claims.FindFirst("upn").Value;
    
    // Call Microsoft Graph to check admin roles
    var roles = await _graphClient.Users[userId]
        .AppRoleAssignments
        .Request()
        .GetAsync();
    
    var isGlobalAdmin = roles.Any(r => r.ResourceDisplayName == "Global Administrator");
    var isSharePointAdmin = roles.Any(r => r.ResourceDisplayName == "SharePoint Administrator");
    var isAppAdmin = roles.Any(r => r.ResourceDisplayName == "Application Administrator");
    
    if (!isGlobalAdmin && !isSharePointAdmin && !isAppAdmin)
    {
        return new AdminVerificationResult
        {
            IsAuthorized = false,
            ErrorMessage = "User must have Global Administrator or SharePoint Administrator role"
        };
    }
    
    return new AdminVerificationResult
    {
        IsAuthorized = true,
        TenantId = tenantId,
        UserId = userId,
        UserPrincipalName = userPrincipalName,
        Roles = new[] { "TenantAdmin" }
    };
}
```

### Step 4: Admin Consent Flow

**Required Permissions:**

| Permission | Type | Purpose |
|------------|------|---------|
| `User.Read.All` | Application | Read all user profiles |
| `Sites.ReadWrite.All` | Application | Manage SharePoint sites |
| `Sites.FullControl.All` | Application | Full control for external sharing |
| `Directory.Read.All` | Application | Read directory data |

**Admin Consent URL:**

```
https://login.microsoftonline.com/{tenant_id}/v2.0/adminconsent?
  client_id=<app_client_id>&
  redirect_uri=https://api.spexternal.com/auth/consent-callback&
  state=<random_state_token>&
  scope=https://graph.microsoft.com/.default
```

**Consent Screen Shows:**
- Application name: "SharePoint External User Manager"
- Publisher: "Your Organization"
- Permissions requested (listed above)
- Warning: "This app will have access to your organization's data"

**After Consent:**
- Backend receives consent callback
- Stores consent status in tenant record
- Proceeds to provisioning

### Step 5: Tenant Resource Provisioning

Backend automatically provisions tenant resources:

#### 5.1 Master Database Record

```sql
INSERT INTO Tenants (
    TenantId,
    TenantDomain,
    DisplayName,
    EntraIdTenantId,
    DatabaseName,
    SubscriptionTier,
    SubscriptionStatus,
    SubscriptionStartDate,
    TrialEndDate,
    IsActive,
    CreatedDate,
    CreatedBy
)
VALUES (
    NEWID(),
    'contoso.com',
    'Contoso Corporation',
    '<entra_id_tenant_id>',
    'tenant_<guid>',
    'Free',
    'Trial',
    GETUTCDATE(),
    DATEADD(DAY, 30, GETUTCDATE()),
    1,
    GETUTCDATE(),
    'admin@contoso.com'
);
```

#### 5.2 Tenant Database Creation

```csharp
public async Task ProvisionTenantDatabaseAsync(Guid tenantId, string databaseName)
{
    // Create database from template
    var createDbCommand = $@"
        CREATE DATABASE [{databaseName}]
        AS COPY OF [TenantTemplate]
        (SERVICE_OBJECTIVE = ELASTIC_POOL(name = [TenantPool]));";
    
    await ExecuteSqlCommandAsync(createDbCommand, masterConnection);
    
    // Wait for database to be ready
    await WaitForDatabaseReadyAsync(databaseName);
    
    // Run initialization scripts
    await InitializeTenantDatabaseAsync(tenantId, databaseName);
    
    // Store connection string in Key Vault
    var connectionString = BuildConnectionString(databaseName);
    await _keyVaultService.SetSecretAsync(
        $"sql-connection-tenant-{tenantId}",
        connectionString
    );
}
```

#### 5.3 Cosmos DB Container Initialization

```csharp
public async Task InitializeCosmosContainersAsync(Guid tenantId)
{
    // Create tenant metadata document
    var tenantMetadata = new
    {
        id = $"tenant-{tenantId}-metadata",
        tenantId = tenantId.ToString(),
        type = "metadata",
        settings = new
        {
            externalSharingEnabled = true,
            allowAnonymousLinks = false,
            defaultLinkPermission = "View",
            externalUserExpirationDays = 90
        },
        features = new
        {
            bulkOperations = false,  // Free tier
            advancedAudit = false,   // Free tier
            customPolicies = false   // Free tier
        }
    };
    
    await _cosmosContainer.CreateItemAsync(
        tenantMetadata,
        new PartitionKey(tenantId.ToString())
    );
}
```

#### 5.4 Initial Admin Account

```sql
INSERT INTO TenantAdmins (
    AdminId,
    TenantId,
    UserPrincipalName,
    DisplayName,
    Email,
    Role,
    IsActive,
    CreatedDate
)
VALUES (
    NEWID(),
    '<tenant_id>',
    'admin@contoso.com',
    'Admin User',
    'admin@contoso.com',
    'TenantAdmin',
    1,
    GETUTCDATE()
);
```

### Step 6: Initial Configuration

Backend sets up default configurations:

#### Default Policies

```json
{
  "policies": [
    {
      "policyName": "Default External Sharing Policy",
      "policyType": "ExternalSharingPolicy",
      "isEnabled": true,
      "configuration": {
        "allowAnonymousLinks": false,
        "defaultExpiration": 90,
        "requireApproval": false
      },
      "appliesTo": "AllLibraries"
    }
  ]
}
```

#### Notification Settings

```json
{
  "notifications": {
    "trialExpiring": {
      "enabled": true,
      "daysBeforeExpiration": [7, 3, 1]
    },
    "newExternalUser": {
      "enabled": true,
      "notifyAdmin": true
    },
    "accessRevoked": {
      "enabled": true
    }
  }
}
```

### Step 7: Welcome Email

System sends welcome email to administrator:

```
Subject: Welcome to SharePoint External User Manager!

Hi Admin,

Your trial has been successfully activated! Here's what you need to know:

Trial Details:
- Trial Period: 30 days (expires on Feb 14, 2024)
- Features: All Free tier features included
- External Users: Up to 5
- Libraries: Up to 3

Get Started:
1. Install the SPFx web part in your SharePoint site
2. Add external users to your libraries
3. Set up collaboration policies
4. Review audit logs

Resources:
- Documentation: https://docs.spexternal.com
- Video Tutorials: https://spexternal.com/tutorials
- Support: support@spexternal.com

Best regards,
SharePoint External User Manager Team
```

### Step 8: Redirect to Application

User is redirected to the application dashboard:

```
https://app.spexternal.com/dashboard?tenantId=<guid>&onboarding=complete
```

Dashboard shows:
- Trial status banner
- Quick start guide
- SPFx installation instructions
- Sample data (optional)

## Post-Onboarding Tasks

### For Administrators

**Immediate Actions:**
1. **Install SPFx Web Part**
   - Download from App Catalog
   - Deploy to SharePoint sites
   - Add web part to pages

2. **Configure Settings**
   - External sharing policies
   - Notification preferences
   - User invitation templates

3. **Add Team Members**
   - Invite additional administrators
   - Assign roles (LibraryOwner, ReadOnly)

**Within First Week:**
1. Sync existing external users
2. Set up collaboration policies
3. Configure access reviews
4. Test user invitation flow

### For End Users

**SPFx Web Part Installation:**
1. Navigate to SharePoint site
2. Edit page where web part should appear
3. Add "External User Manager" web part
4. Configure web part properties:
   - API endpoint
   - Refresh interval
   - Display options

## Tenant Offboarding

When a tenant cancels subscription or trial expires:

### Grace Period (7 days)

- Read-only access maintained
- No new invitations allowed
- Notifications sent daily
- Option to reactivate subscription

### After Grace Period

```csharp
public async Task OffboardTenantAsync(Guid tenantId)
{
    // 1. Export audit logs (if requested)
    await ExportAuditLogsAsync(tenantId);
    
    // 2. Revoke all external user access
    await RevokeAllExternalAccessAsync(tenantId);
    
    // 3. Mark tenant as inactive
    await DeactivateTenantAsync(tenantId);
    
    // 4. Schedule database deletion (30 days)
    await ScheduleDatabaseDeletionAsync(tenantId, days: 30);
    
    // 5. Remove from active routing
    await RemoveFromRoutingTableAsync(tenantId);
    
    // 6. Send confirmation email
    await SendOffboardingConfirmationAsync(tenantId);
}
```

### Data Retention

- **Audit logs:** Exported and retained for 7 years
- **User data:** Soft-deleted, purged after 30 days
- **Backups:** Retained for 90 days after offboarding

## Troubleshooting

### Common Onboarding Issues

**Issue: Admin consent fails**
- **Cause:** User doesn't have admin role
- **Solution:** Use account with Global Administrator role

**Issue: Database provisioning timeout**
- **Cause:** Azure SQL elastic pool at capacity
- **Solution:** Auto-retry with exponential backoff, scale pool if needed

**Issue: SharePoint permissions insufficient**
- **Cause:** Conditional access policies blocking service principal
- **Solution:** Add service principal to exclusion list

### Support Contacts

- **Technical Support:** support@spexternal.com
- **Onboarding Help:** onboarding@spexternal.com
- **Emergency:** +1-555-0199 (24/7)

## SaaS Admin Role Model

### Role Hierarchy

```
┌─────────────────────────┐
│    SaaS Admin          │  ← Our team, full system access
└───────────┬─────────────┘
            │
┌───────────▼─────────────┐
│   Tenant Admin         │  ← Customer admin, full tenant access
└───────────┬─────────────┘
            │
      ┌─────┴──────┐
      │            │
┌─────▼────┐  ┌───▼────────┐
│ Library  │  │ Library    │  ← Limited access
│ Owner    │  │ Contributor│
└──────────┘  └────────────┘
```

### Role Permissions Matrix

| Action | SaaS Admin | Tenant Admin | Library Owner | Library Contributor | Read-Only |
|--------|-----------|--------------|---------------|-------------------|-----------|
| View all tenants | ✓ | - | - | - | - |
| Manage subscriptions | ✓ | - | - | - | - |
| View tenant data | ✓ | ✓ | - | - | - |
| Create/delete libraries | ✓ | ✓ | - | - | - |
| Invite external users | ✓ | ✓ | ✓ | ✓ | - |
| Revoke user access | ✓ | ✓ | ✓ | - | - |
| Manage policies | ✓ | ✓ | - | - | - |
| View audit logs | ✓ | ✓ | ✓ | - | - |
| Export audit logs | ✓ | ✓ | - | - | - |
| Change settings | ✓ | ✓ | - | - | - |

### SaaS Admin Functions

**Tenant Management:**
- View all tenants across the platform
- Suspend/reactivate tenants
- Manual subscription changes
- Data export requests
- Offboarding support

**Platform Operations:**
- Monitor system health
- Review error logs
- Performance tuning
- Scale resources
- Security incident response

**Support:**
- Handle escalated support tickets
- Troubleshoot tenant issues
- Assist with data migrations
- Provide configuration guidance

## Security Considerations

### During Onboarding

1. **Validate Admin Identity**
   - Verify admin role via Graph API
   - Check MFA status
   - Log all authentication attempts

2. **Secure Token Handling**
   - Short-lived tokens (1 hour)
   - Secure storage (encrypted)
   - Regular token refresh

3. **Audit Onboarding Events**
   - Log all steps
   - Track failures and retries
   - Monitor for suspicious patterns

### Ongoing Security

1. **Conditional Access**
   - Require MFA for admins
   - Block legacy authentication
   - Require compliant devices

2. **Regular Access Reviews**
   - Quarterly admin role review
   - Remove inactive users
   - Verify permissions

## Metrics & Monitoring

### Onboarding Success Metrics

- **Time to Onboard:** Target < 5 minutes
- **Success Rate:** Target > 95%
- **Admin Consent Rate:** Target > 90%
- **Trial to Paid Conversion:** Target > 20%

### Alerts

- Failed onboarding attempts (> 3 in 10 minutes)
- Admin consent denials
- Database provisioning failures
- Long onboarding times (> 10 minutes)

## References

- [Entra ID Admin Consent](https://learn.microsoft.com/entra/identity-platform/v2-admin-consent)
- [Microsoft Graph Permissions](https://learn.microsoft.com/graph/permissions-reference)
- [Azure SQL Elastic Pools](https://learn.microsoft.com/azure/azure-sql/database/elastic-pool-overview)
