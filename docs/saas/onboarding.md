# Tenant Onboarding & Entra ID Integration

## Overview

This document outlines the complete tenant onboarding process, Microsoft Entra ID (Azure AD) app registration, admin consent flow, and role management for the SharePoint External User Manager SaaS platform.

## Entra ID App Registration

### Multi-Tenant Application Setup

**Step 1: Create App Registration**

1. Navigate to [Azure Portal](https://portal.azure.com) → Azure Active Directory → App registrations
2. Click "New registration"
3. Configure:
   - **Name**: SharePoint External User Manager
   - **Supported account types**: Accounts in any organizational directory (Any Azure AD directory - Multitenant)
   - **Redirect URI**: Web - `https://api.spexternal.com/auth/callback`

**Step 2: Configure Authentication**

```json
{
  "authentication": {
    "redirectUris": [
      "https://api.spexternal.com/auth/callback",
      "https://app.spexternal.com/auth/callback",
      "https://localhost:3000/auth/callback"
    ],
    "logoutUrl": "https://app.spexternal.com/logout",
    "implicitGrant": {
      "accessToken": false,
      "idToken": false
    },
    "supportedAccountTypes": "AzureADMultipleOrgs",
    "enableIdTokenIssuance": true
  }
}
```

**Step 3: Configure API Permissions**

Required Microsoft Graph permissions:

| Permission | Type | Description | Admin Consent Required |
|------------|------|-------------|------------------------|
| `User.Read` | Delegated | Sign in and read user profile | No |
| `User.Read.All` | Delegated | Read all users' full profiles | Yes |
| `Directory.Read.All` | Delegated | Read directory data | Yes |
| `Sites.Manage.All` | Delegated | Create, edit, and delete items in all site collections | Yes |
| `Sites.FullControl.All` | Application | Have full control of all site collections | Yes |

**Azure CLI Commands**:

```bash
# Create app registration
az ad app create \
  --display-name "SharePoint External User Manager" \
  --sign-in-audience AzureADMultipleOrgs \
  --web-redirect-uris https://api.spexternal.com/auth/callback

# Add Microsoft Graph API permissions
APP_ID="<your-app-id>"

# User.Read (Delegated) - e1fe6dd8-ba31-4d61-89e7-88639da4683d
az ad app permission add \
  --id $APP_ID \
  --api 00000003-0000-0000-c000-000000000000 \
  --api-permissions e1fe6dd8-ba31-4d61-89e7-88639da4683d=Scope

# User.Read.All (Delegated) - a154be20-db9c-4678-8ab7-66f6cc099a59
az ad app permission add \
  --id $APP_ID \
  --api 00000003-0000-0000-c000-000000000000 \
  --api-permissions a154be20-db9c-4678-8ab7-66f6cc099a59=Scope

# Directory.Read.All (Delegated) - 06da0dbc-49e2-44d2-8312-53f166ab848a
az ad app permission add \
  --id $APP_ID \
  --api 00000003-0000-0000-c000-000000000000 \
  --api-permissions 06da0dbc-49e2-44d2-8312-53f166ab848a=Scope

# Sites.Manage.All (Delegated) - 65e50fdc-43b7-4915-933e-e8138f11f40a
az ad app permission add \
  --id $APP_ID \
  --api 00000003-0000-0000-c000-000000000000 \
  --api-permissions 65e50fdc-43b7-4915-933e-e8138f11f40a=Scope
```

**Step 4: Create App Roles**

Define custom app roles for the SaaS admin interface:

```json
{
  "appRoles": [
    {
      "id": "00000000-0000-0000-0000-000000000001",
      "displayName": "Tenant Admin",
      "description": "Full administrative access to tenant settings and users",
      "value": "Tenant.Admin",
      "allowedMemberTypes": ["User"],
      "isEnabled": true
    },
    {
      "id": "00000000-0000-0000-0000-000000000002",
      "displayName": "User Manager",
      "description": "Manage external users and permissions",
      "value": "User.Manage",
      "allowedMemberTypes": ["User"],
      "isEnabled": true
    },
    {
      "id": "00000000-0000-0000-0000-000000000003",
      "displayName": "Policy Manager",
      "description": "Create and manage collaboration policies",
      "value": "Policy.Manage",
      "allowedMemberTypes": ["User"],
      "isEnabled": true
    },
    {
      "id": "00000000-0000-0000-0000-000000000004",
      "displayName": "Audit Reader",
      "description": "Read-only access to audit logs and reports",
      "value": "Audit.Read",
      "allowedMemberTypes": ["User"],
      "isEnabled": true
    },
    {
      "id": "00000000-0000-0000-0000-000000000005",
      "displayName": "Billing Manager",
      "description": "Manage subscription and billing settings",
      "value": "Billing.Manage",
      "allowedMemberTypes": ["User"],
      "isEnabled": true
    }
  ]
}
```

**Step 5: Configure Certificates & Secrets**

```bash
# Create client secret (for backend service)
az ad app credential reset \
  --id $APP_ID \
  --append \
  --display-name "Backend API Secret" \
  --years 2

# Store in Azure Key Vault
az keyvault secret set \
  --vault-name spexternal-keyvault \
  --name "EntraID-ClientSecret" \
  --value "<secret-value>"
```

---

## Admin Consent Flow

### Consent URL Generation

**Consent Request URL**:
```
https://login.microsoftonline.com/organizations/v2.0/adminconsent
  ?client_id={application_id}
  &redirect_uri=https://api.spexternal.com/auth/callback
  &state={random_state_value}
  &scope=https://graph.microsoft.com/User.Read.All
         https://graph.microsoft.com/Directory.Read.All
         https://graph.microsoft.com/Sites.Manage.All
```

### Backend Implementation

**Generate Consent Link**:

```typescript
import { v4 as uuidv4 } from 'uuid';

export function generateAdminConsentUrl(
  tenantId: string,
  redirectUri: string
): string {
  const state = uuidv4();
  const clientId = process.env.AZURE_AD_CLIENT_ID;
  
  // Store state for validation
  await redis.setex(`consent-state:${state}`, 3600, tenantId);
  
  const params = new URLSearchParams({
    client_id: clientId,
    redirect_uri: redirectUri,
    state: state,
    scope: [
      'https://graph.microsoft.com/User.Read.All',
      'https://graph.microsoft.com/Directory.Read.All',
      'https://graph.microsoft.com/Sites.Manage.All'
    ].join(' ')
  });
  
  return `https://login.microsoftonline.com/organizations/v2.0/adminconsent?${params}`;
}
```

**Handle Consent Callback**:

```typescript
export async function handleAdminConsentCallback(
  req: Request,
  res: Response
) {
  const { tenant, admin_consent, state, error, error_description } = req.query;
  
  // Validate state
  const storedTenantId = await redis.get(`consent-state:${state}`);
  if (!storedTenantId) {
    return res.status(400).json({
      success: false,
      error: 'Invalid or expired consent request'
    });
  }
  
  // Handle consent error
  if (error) {
    await auditLogger.log({
      tenant_id: storedTenantId,
      event_type: 'AdminConsentDenied',
      event_category: 'Authentication',
      severity: 'Warning',
      action: {
        name: 'AdminConsent',
        result: 'Failure',
        details: { error, error_description }
      }
    });
    
    return res.redirect(`https://app.spexternal.com/onboarding/failed?reason=${error}`);
  }
  
  // Consent granted
  if (admin_consent === 'True') {
    // Update tenant record
    await db.query(`
      UPDATE Tenants
      SET is_verified = 1,
          azure_tenant_id = @azureTenantId,
          status = 'Trial',
          trial_end_date = DATEADD(day, 14, GETUTCDATE())
      WHERE tenant_id = @tenantId
    `, { tenantId: storedTenantId, azureTenantId: tenant });
    
    // Create trial subscription
    await subscriptionService.createTrialSubscription(storedTenantId);
    
    // Send welcome email
    await emailService.sendWelcomeEmail(storedTenantId);
    
    // Log successful onboarding
    await auditLogger.log({
      tenant_id: storedTenantId,
      event_type: 'TenantOnboarded',
      event_category: 'TenantManagement',
      severity: 'Info',
      action: {
        name: 'OnboardTenant',
        result: 'Success',
        details: { azure_tenant_id: tenant }
      }
    });
    
    return res.redirect('https://app.spexternal.com/onboarding/success');
  }
}
```

---

## Onboarding Flow

### Step-by-Step Process

```
┌─────────────────────────────────────────────────────────────────┐
│                    Tenant Onboarding Flow                        │
└─────────────────────────────────────────────────────────────────┘

1. Admin visits website
   │
   ├─► Clicks "Get Started" or "Sign Up"
   │
2. Initial registration form
   │
   ├─► Enters:
   │   • Company name
   │   • Email address
   │   • Phone number (optional)
   │   • Country/region
   │
3. Email verification
   │
   ├─► Receives verification email
   ├─► Clicks verification link
   │
4. Admin consent request
   │
   ├─► Redirected to Microsoft login
   ├─► Admin signs in with Azure AD credentials
   ├─► Reviews required permissions
   ├─► Grants admin consent
   │
5. Backend processes consent
   │
   ├─► Validates consent response
   ├─► Creates tenant record
   ├─► Creates trial subscription
   ├─► Assigns default admin role
   │
6. Onboarding complete
   │
   ├─► Redirected to welcome page
   ├─► Receives welcome email
   └─► Can access application
```

### Implementation: Onboarding API

**Endpoint**: `POST /api/v1/tenants/onboard`

```typescript
export async function onboardTenant(req: Request, res: Response) {
  try {
    const {
      tenant_name,
      primary_admin_email,
      country,
      phone
    } = req.body;
    
    // Validate input
    if (!tenant_name || !primary_admin_email) {
      return res.status(400).json({
        success: false,
        error: {
          code: 'VALIDATION_ERROR',
          message: 'Tenant name and admin email are required'
        }
      });
    }
    
    // Check if tenant already exists
    const existingTenant = await db.query(`
      SELECT tenant_id FROM Tenants
      WHERE primary_admin_email = @email
    `, { email: primary_admin_email });
    
    if (existingTenant.length > 0) {
      return res.status(409).json({
        success: false,
        error: {
          code: 'TENANT_ALREADY_EXISTS',
          message: 'A tenant with this email already exists'
        }
      });
    }
    
    // Create tenant record (unverified)
    const tenantId = `tenant-${uuidv4()}`;
    await db.query(`
      INSERT INTO Tenants (
        tenant_id, tenant_name, primary_admin_email,
        status, is_verified, created_at
      ) VALUES (
        @tenantId, @tenantName, @email,
        'Pending', 0, GETUTCDATE()
      )
    `, { tenantId, tenantName: tenant_name, email: primary_admin_email });
    
    // Generate verification email
    const verificationToken = jwt.sign(
      { tenantId, email: primary_admin_email },
      process.env.JWT_SECRET,
      { expiresIn: '24h' }
    );
    
    await emailService.sendVerificationEmail(
      primary_admin_email,
      tenant_name,
      verificationToken
    );
    
    return res.status(201).json({
      success: true,
      data: {
        tenant_id: tenantId,
        status: 'Pending',
        message: 'Verification email sent. Please check your inbox.'
      }
    });
  } catch (error) {
    logger.error('Onboarding error:', error);
    return res.status(500).json({
      success: false,
      error: {
        code: 'INTERNAL_ERROR',
        message: 'Failed to process onboarding request'
      }
    });
  }
}
```

**Endpoint**: `GET /api/v1/tenants/verify`

```typescript
export async function verifyTenantEmail(req: Request, res: Response) {
  try {
    const { token } = req.query;
    
    // Verify token
    const decoded = jwt.verify(token, process.env.JWT_SECRET);
    const { tenantId, email } = decoded;
    
    // Update tenant status
    await db.query(`
      UPDATE Tenants
      SET is_verified = 1,
          status = 'AwaitingConsent',
          updated_at = GETUTCDATE()
      WHERE tenant_id = @tenantId
    `, { tenantId });
    
    // Generate admin consent URL
    const consentUrl = generateAdminConsentUrl(
      tenantId,
      'https://api.spexternal.com/auth/callback'
    );
    
    // Redirect to consent flow
    return res.redirect(consentUrl);
  } catch (error) {
    return res.status(400).json({
      success: false,
      error: {
        code: 'INVALID_TOKEN',
        message: 'Verification token is invalid or expired'
      }
    });
  }
}
```

---

## Role Assignment

### Default Role Assignment

When a tenant is onboarded, the primary admin is automatically assigned the "Tenant Admin" role:

```typescript
export async function assignDefaultAdminRole(
  tenantId: string,
  userId: string,
  email: string
) {
  const roleId = `role-${uuidv4()}`;
  
  await db.query(`
    INSERT INTO AdminRoles (
      role_id, tenant_id, user_id, role_name,
      granted_by, granted_date, is_active, created_at
    ) VALUES (
      @roleId, @tenantId, @userId, 'Tenant.Admin',
      'System', GETUTCDATE(), 1, GETUTCDATE()
    )
  `, { roleId, tenantId, userId });
  
  await auditLogger.log({
    tenant_id: tenantId,
    event_type: 'RoleAssigned',
    event_category: 'Authorization',
    severity: 'Info',
    target: {
      resource_type: 'User',
      resource_id: userId,
      resource_name: email
    },
    action: {
      name: 'AssignRole',
      result: 'Success',
      details: { role: 'Tenant.Admin', granted_by: 'System' }
    }
  });
}
```

### Role Management API

**Assign Role**: `POST /api/v1/tenants/{tenantId}/roles`

```typescript
export async function assignRole(req: Request, res: Response) {
  try {
    const { tenantId } = req.params;
    const { user_id, role_name } = req.body;
    
    // Verify current user has permission
    if (!req.user.roles.includes('Tenant.Admin')) {
      return res.status(403).json({
        success: false,
        error: {
          code: 'FORBIDDEN',
          message: 'Only Tenant Admins can assign roles'
        }
      });
    }
    
    // Validate role name
    const validRoles = ['Tenant.Admin', 'User.Manage', 'Policy.Manage', 'Audit.Read', 'Billing.Manage'];
    if (!validRoles.includes(role_name)) {
      return res.status(400).json({
        success: false,
        error: {
          code: 'INVALID_ROLE',
          message: `Invalid role name. Must be one of: ${validRoles.join(', ')}`
        }
      });
    }
    
    // Check if user already has this role
    const existingRole = await db.query(`
      SELECT role_id FROM AdminRoles
      WHERE tenant_id = @tenantId
      AND user_id = @userId
      AND role_name = @roleName
      AND is_active = 1
    `, { tenantId, userId: user_id, roleName: role_name });
    
    if (existingRole.length > 0) {
      return res.status(409).json({
        success: false,
        error: {
          code: 'ROLE_ALREADY_ASSIGNED',
          message: 'User already has this role'
        }
      });
    }
    
    // Assign role
    const roleId = `role-${uuidv4()}`;
    await db.query(`
      INSERT INTO AdminRoles (
        role_id, tenant_id, user_id, role_name,
        granted_by, granted_date, is_active, created_at
      ) VALUES (
        @roleId, @tenantId, @userId, @roleName,
        @grantedBy, GETUTCDATE(), 1, GETUTCDATE()
      )
    `, {
      roleId,
      tenantId,
      userId: user_id,
      roleName: role_name,
      grantedBy: req.user.id
    });
    
    return res.status(201).json({
      success: true,
      data: {
        role_id: roleId,
        user_id: user_id,
        role_name: role_name,
        granted_by: req.user.email,
        granted_date: new Date()
      }
    });
  } catch (error) {
    logger.error('Role assignment error:', error);
    return res.status(500).json({
      success: false,
      error: {
        code: 'INTERNAL_ERROR',
        message: 'Failed to assign role'
      }
    });
  }
}
```

**Revoke Role**: `DELETE /api/v1/tenants/{tenantId}/roles/{roleId}`

```typescript
export async function revokeRole(req: Request, res: Response) {
  try {
    const { tenantId, roleId } = req.params;
    
    // Verify current user has permission
    if (!req.user.roles.includes('Tenant.Admin')) {
      return res.status(403).json({
        success: false,
        error: {
          code: 'FORBIDDEN',
          message: 'Only Tenant Admins can revoke roles'
        }
      });
    }
    
    // Prevent revoking last Tenant Admin role
    const adminRoles = await db.query(`
      SELECT COUNT(*) as count FROM AdminRoles
      WHERE tenant_id = @tenantId
      AND role_name = 'Tenant.Admin'
      AND is_active = 1
    `, { tenantId });
    
    const roleInfo = await db.query(`
      SELECT role_name FROM AdminRoles
      WHERE role_id = @roleId AND tenant_id = @tenantId
    `, { roleId, tenantId });
    
    if (roleInfo[0]?.role_name === 'Tenant.Admin' && adminRoles[0]?.count <= 1) {
      return res.status(400).json({
        success: false,
        error: {
          code: 'CANNOT_REVOKE_LAST_ADMIN',
          message: 'Cannot revoke the last Tenant Admin role'
        }
      });
    }
    
    // Revoke role
    await db.query(`
      UPDATE AdminRoles
      SET is_active = 0, updated_at = GETUTCDATE()
      WHERE role_id = @roleId AND tenant_id = @tenantId
    `, { roleId, tenantId });
    
    return res.status(200).json({
      success: true,
      message: 'Role revoked successfully'
    });
  } catch (error) {
    logger.error('Role revocation error:', error);
    return res.status(500).json({
      success: false,
      error: {
        code: 'INTERNAL_ERROR',
        message: 'Failed to revoke role'
      }
    });
  }
}
```

---

## Tenant Identity Verification

### Required Information

During onboarding, the backend must verify:

1. **Admin Identity**:
   - Valid Azure AD account
   - Global Administrator or Application Administrator role
   - MFA enabled (recommended)

2. **Tenant Context**:
   - Azure tenant ID
   - Verified domain
   - Organization name

3. **Required Permissions**:
   - All requested API permissions granted
   - Admin consent provided
   - Service principal created in customer tenant

### Verification Implementation

```typescript
export async function verifyTenantIdentity(
  azureTenantId: string,
  accessToken: string
): Promise<TenantVerificationResult> {
  try {
    // 1. Get tenant information
    const tenantInfo = await graphClient
      .api('/organization')
      .version('v1.0')
      .get();
    
    // 2. Verify admin role
    const userRoles = await graphClient
      .api('/me/memberOf')
      .version('v1.0')
      .filter("startswith(displayName, 'Global')")
      .get();
    
    const isGlobalAdmin = userRoles.value.some((role: any) =>
      role.displayName === 'Global Administrator'
    );
    
    if (!isGlobalAdmin) {
      throw new Error('User must be a Global Administrator');
    }
    
    // 3. Verify required permissions
    const servicePrincipal = await graphClient
      .api(`/servicePrincipals?$filter=appId eq '${process.env.AZURE_AD_CLIENT_ID}'`)
      .version('v1.0')
      .get();
    
    if (servicePrincipal.value.length === 0) {
      throw new Error('Service principal not found. Admin consent not granted.');
    }
    
    return {
      verified: true,
      tenant_id: azureTenantId,
      tenant_name: tenantInfo.value[0].displayName,
      domain: tenantInfo.value[0].verifiedDomains[0].name,
      admin_verified: isGlobalAdmin,
      permissions_granted: true
    };
  } catch (error) {
    logger.error('Tenant identity verification failed:', error);
    return {
      verified: false,
      error: error.message
    };
  }
}
```

---

## Onboarding Email Templates

### Verification Email

```html
<!DOCTYPE html>
<html>
<head>
  <meta charset="UTF-8">
  <title>Verify Your Email - SharePoint External User Manager</title>
</head>
<body>
  <h1>Welcome to SharePoint External User Manager!</h1>
  <p>Hi {{tenant_name}},</p>
  <p>Thank you for signing up! Please verify your email address to continue with the onboarding process.</p>
  <p><a href="{{verification_link}}" style="background-color: #0078d4; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px;">Verify Email Address</a></p>
  <p>Or copy and paste this link into your browser:</p>
  <p>{{verification_link}}</p>
  <p>This link will expire in 24 hours.</p>
  <p>If you didn't create this account, please ignore this email.</p>
</body>
</html>
```

### Welcome Email

```html
<!DOCTYPE html>
<html>
<head>
  <meta charset="UTF-8">
  <title>Welcome - SharePoint External User Manager</title>
</head>
<body>
  <h1>You're all set!</h1>
  <p>Hi {{primary_admin_name}},</p>
  <p>Your tenant <strong>{{tenant_name}}</strong> has been successfully onboarded to SharePoint External User Manager.</p>
  
  <h2>Your Trial Subscription</h2>
  <ul>
    <li><strong>Tier:</strong> Pro (14-day trial)</li>
    <li><strong>Trial Ends:</strong> {{trial_end_date}}</li>
    <li><strong>Max External Users:</strong> 100</li>
  </ul>
  
  <h2>Next Steps</h2>
  <ol>
    <li><a href="{{app_url}}/onboarding/quickstart">Complete the Quick Start Guide</a></li>
    <li><a href="{{app_url}}/settings">Configure your tenant settings</a></li>
    <li><a href="{{app_url}}/users/invite">Invite your first external user</a></li>
  </ol>
  
  <p><a href="{{app_url}}" style="background-color: #0078d4; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px;">Go to Dashboard</a></p>
  
  <h2>Need Help?</h2>
  <ul>
    <li><a href="{{docs_url}}">Documentation</a></li>
    <li><a href="{{support_url}}">Support Center</a></li>
    <li>Email: support@spexternal.com</li>
  </ul>
</body>
</html>
```

---

## Onboarding Checklist

### For Customers

- [ ] Sign up with company email
- [ ] Verify email address
- [ ] Review required permissions
- [ ] Grant admin consent (Global Admin required)
- [ ] Complete quick start guide
- [ ] Configure tenant settings
- [ ] Invite first external user
- [ ] Set up collaboration policies (optional)

### For Backend System

- [ ] Validate email address
- [ ] Send verification email
- [ ] Generate admin consent URL
- [ ] Handle consent callback
- [ ] Verify admin identity
- [ ] Verify tenant context
- [ ] Verify required permissions
- [ ] Create tenant record
- [ ] Create trial subscription
- [ ] Assign default admin role
- [ ] Send welcome email
- [ ] Log onboarding event

---

## Troubleshooting

### Common Issues

**1. Admin Consent Denied**
- **Cause**: Admin declined permissions or doesn't have sufficient privileges
- **Solution**: Ensure user is Global Administrator; retry consent flow

**2. Invalid Redirect URI**
- **Cause**: Redirect URI mismatch in app registration
- **Solution**: Verify redirect URIs match exactly in Azure AD and code

**3. Token Validation Failed**
- **Cause**: Clock skew, expired token, or invalid signature
- **Solution**: Check system time, token expiration, and signing keys

**4. Tenant Already Exists**
- **Cause**: Email address already associated with a tenant
- **Solution**: Use tenant recovery flow or contact support

---

## Next Steps

1. Implement onboarding UI in admin portal
2. Create email templates and sending service
3. Set up monitoring for onboarding funnel
4. Implement tenant recovery/reactivation flow
5. Create automated tests for onboarding process
