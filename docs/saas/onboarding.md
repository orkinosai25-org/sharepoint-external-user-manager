# Tenant Onboarding Flow

## Overview

This document describes the end-to-end flow for onboarding a new tenant to the SharePoint External User Manager SaaS platform, including Azure AD admin consent and initial setup.

## Onboarding Journey

```
┌─────────────────────────────────────────────────────────────────┐
│ Step 1: Discovery & Sign-up                                     │
│  • Admin discovers solution (marketplace/website/trial)         │
│  • Clicks "Get Started" or "Start Trial"                       │
└─────────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────────┐
│ Step 2: Azure AD Admin Consent                                  │
│  • Redirected to Microsoft login                               │
│  • Admin signs in with organizational account                  │
│  • Reviews requested permissions                               │
│  • Grants admin consent for tenant                             │
└─────────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────────┐
│ Step 3: Tenant Registration (Backend)                           │
│  • Backend receives consent callback                           │
│  • Creates Tenant record in database                           │
│  • Creates Free/Trial Subscription                             │
│  • Initializes default policies                                │
│  • Logs onboarding event                                       │
└─────────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────────┐
│ Step 4: SPFx Web Part Installation                              │
│  • Admin downloads .sppkg from marketplace/portal              │
│  • Uploads to SharePoint App Catalog                           │
│  • Deploys solution tenant-wide                                │
│  • Adds web part to a SharePoint page                          │
└─────────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────────┐
│ Step 5: First Use & Configuration                               │
│  • Web part connects to SaaS backend automatically             │
│  • Admin views subscription status (Trial)                     │
│  • Admin configures collaboration policies                     │
│  • Admin starts managing external users                        │
└─────────────────────────────────────────────────────────────────┘
```

## Detailed Flow

### 1. Admin Consent Flow (Entra ID)

#### 1.1 Initiate Consent

**SPFx Admin Page** triggers consent when "Connect Tenant" is clicked:

```typescript
// SPFx code to initiate admin consent
export async function initiateAdminConsent(): Promise<void> {
  const clientId = 'your-app-client-id';
  const redirectUri = encodeURIComponent(
    'https://api.spexternal.com/auth/callback'
  );
  const scope = encodeURIComponent(
    'https://graph.microsoft.com/Sites.Read.All ' +
    'https://graph.microsoft.com/User.ReadWrite.All'
  );
  
  const consentUrl = 
    `https://login.microsoftonline.com/common/adminconsent?` +
    `client_id=${clientId}&` +
    `redirect_uri=${redirectUri}&` +
    `scope=${scope}&` +
    `state=${generateState()}`;  // CSRF protection
  
  // Redirect to Azure AD
  window.location.href = consentUrl;
}
```

#### 1.2 Consent Prompt

Microsoft presents permission request screen:

```
─────────────────────────────────────────────
  SharePoint External User Manager
  wants to access your organization
─────────────────────────────────────────────

This app would like to:

✓ Read all site collections
✓ Manage external user invitations
✓ Read user profiles

Consenting on behalf of your organization

[Accept]  [Cancel]
─────────────────────────────────────────────
```

#### 1.3 Consent Callback

Azure AD redirects back to backend with consent result:

```http
GET /auth/callback?
  admin_consent=True&
  tenant=12345678-1234-1234-1234-123456789abc&
  state={state_token}&
  client_id={client_id}
```

### 2. Backend Tenant Registration

#### 2.1 Process Consent Callback

```typescript
export async function handleConsentCallback(
  req: HttpRequest
): Promise<HttpResponse> {
  const { admin_consent, tenant, state } = req.query;
  
  // Verify state token (CSRF protection)
  if (!verifyState(state)) {
    return { status: 400, body: 'Invalid state' };
  }
  
  if (admin_consent !== 'True') {
    return { status: 400, body: 'Admin consent not granted' };
  }
  
  // Onboard tenant
  const tenantData = await onboardTenant(tenant);
  
  // Redirect to success page
  return {
    status: 302,
    headers: {
      Location: `https://admin.spexternal.com/onboarding-success?tenant=${tenant}`
    }
  };
}
```

#### 2.2 Create Tenant Record

```typescript
export async function onboardTenant(
  entraIdTenantId: string
): Promise<Tenant> {
  // 1. Get organization info from Graph API
  const orgInfo = await graphClient.getOrganization(entraIdTenantId);
  
  // 2. Create tenant record
  const tenant = await db.tenants.create({
    entraIdTenantId,
    organizationName: orgInfo.displayName,
    primaryAdminEmail: orgInfo.technicalContact || 'admin@org.com',
    status: 'Active',
    onboardedDate: new Date()
  });
  
  // 3. Create free/trial subscription
  const subscription = await db.subscriptions.create({
    tenantId: tenant.id,
    tier: 'Free',
    status: 'Trial',
    startDate: new Date(),
    trialExpiry: addDays(new Date(), 30), // 30-day trial
    maxUsers: 10,
    features: {
      auditHistoryDays: 30,
      exportEnabled: false,
      scheduledReviews: false
    }
  });
  
  // 4. Initialize default policies
  await createDefaultPolicies(tenant.id);
  
  // 5. Audit log the onboarding
  await auditLog.log({
    tenantId: tenant.id,
    action: 'TenantOnboarded',
    userId: 'system',
    details: { tier: 'Free', trial: true }
  });
  
  return tenant;
}
```

#### 2.3 Default Policies

```typescript
async function createDefaultPolicies(tenantId: number): Promise<void> {
  const defaultPolicies = [
    {
      policyType: 'GuestExpiration',
      enabled: false,
      configuration: { expirationDays: 90, notifyBeforeDays: 7 }
    },
    {
      policyType: 'RequireApproval',
      enabled: false,
      configuration: { approvers: [] }
    },
    {
      policyType: 'AllowedDomains',
      enabled: false,
      configuration: { whitelist: [], blacklist: [] }
    }
  ];
  
  for (const policy of defaultPolicies) {
    await db.policies.create({
      tenantId,
      ...policy
    });
  }
}
```

### 3. SPFx Installation

#### 3.1 Download Package

Admin obtains `.sppkg` file from:
- Microsoft AppSource (future)
- GitHub Releases
- Direct download from portal

#### 3.2 Upload to App Catalog

```powershell
# PowerShell script for deployment
Connect-PnPOnline -Url "https://tenant.sharepoint.com/sites/appcatalog" -Interactive

# Upload solution
Add-PnPApp -Path "./sharepoint-external-user-manager.sppkg" -Overwrite

# Deploy tenant-wide
Publish-PnPApp -Identity "SharePointExternalUserManager" -SkipFeatureDeployment
```

#### 3.3 Add Web Part to Page

1. Navigate to SharePoint site
2. Create or edit a page
3. Click "+ Add a web part"
4. Search for "External User Manager"
5. Add to page and publish

### 4. First Connection

#### 4.1 API Token Acquisition

When SPFx web part loads, it acquires a token:

```typescript
import { AadTokenProvider } from '@microsoft/sp-http';

export async function getApiToken(
  context: WebPartContext
): Promise<string> {
  const provider = await context.aadTokenProviderFactory
    .getTokenProvider();
  
  const token = await provider.getToken(
    'api://spexternal.com'  // Backend API resource
  );
  
  return token;
}
```

#### 4.2 First API Call

Web part calls backend to verify tenant status:

```typescript
export async function verifyTenantConnection(): Promise<TenantInfo> {
  const token = await getApiToken(this.context);
  
  const response = await fetch(
    'https://api.spexternal.com/v1/tenants/me',
    {
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      }
    }
  );
  
  if (!response.ok) {
    if (response.status === 404) {
      // Tenant not onboarded - show setup wizard
      this.showOnboardingWizard();
    }
    throw new Error('Failed to connect');
  }
  
  return await response.json();
}
```

### 5. Onboarding Wizard (if not yet onboarded)

If tenant is not found, SPFx shows setup wizard:

```typescript
export const OnboardingWizard: React.FC = () => {
  const [step, setStep] = useState(1);
  
  return (
    <Stack>
      {step === 1 && (
        <WelcomeStep onNext={() => setStep(2)} />
      )}
      {step === 2 && (
        <ConsentStep 
          onConsent={initiateAdminConsent}
          onNext={() => setStep(3)}
        />
      )}
      {step === 3 && (
        <ConfigurationStep onNext={() => setStep(4)} />
      )}
      {step === 4 && (
        <CompleteStep />
      )}
    </Stack>
  );
};
```

## User Roles During Onboarding

### Tenant Owner (First Admin)

- User who completes admin consent
- Automatically assigned "Owner" role
- Has full access to all features
- Can invite additional admins

### Additional Admins

Can be invited post-onboarding:

```typescript
export async function inviteAdmin(
  email: string,
  role: 'Admin' | 'ReadOnly'
): Promise<void> {
  // 1. Send invitation email
  await emailService.send({
    to: email,
    subject: 'You\'ve been invited as admin',
    template: 'admin-invite',
    data: { role }
  });
  
  // 2. Create pending invitation record
  await db.adminInvites.create({
    tenantId: context.tenantId,
    email,
    role,
    invitedBy: context.userId,
    expiresAt: addDays(new Date(), 7)
  });
}
```

## Trial Period

### Trial Characteristics

- **Duration**: 30 days from onboarding
- **Tier**: Free tier features
- **User Limit**: 10 external users
- **No Credit Card**: Required only at upgrade

### Trial Expiry Notifications

```typescript
// Scheduled job: Check trial expiry
export async function checkTrialExpiry(): Promise<void> {
  const expiringTrials = await db.subscriptions.find({
    status: 'Trial',
    trialExpiry: {
      $lte: addDays(new Date(), 7),  // Expiring in 7 days
      $gte: new Date()
    }
  });
  
  for (const subscription of expiringTrials) {
    const tenant = await db.tenants.findById(subscription.tenantId);
    
    // Send expiry warning email
    await emailService.send({
      to: tenant.primaryAdminEmail,
      subject: 'Your trial expires soon',
      template: 'trial-expiry-warning',
      data: {
        daysRemaining: differenceInDays(subscription.trialExpiry, new Date()),
        upgradeLInk: 'https://admin.spexternal.com/upgrade'
      }
    });
    
    // In-app notification
    await notificationService.create({
      tenantId: tenant.id,
      type: 'TrialExpiring',
      message: 'Your trial expires in 7 days. Upgrade to continue.',
      actionUrl: '/upgrade'
    });
  }
}
```

## Upgrade Flow

### Self-Service Upgrade (Phase 1: Own Billing)

```typescript
export async function initiateUpgrade(
  tier: 'Pro' | 'Enterprise'
): Promise<CheckoutSession> {
  // 1. Create checkout session (Stripe/own billing)
  const session = await paymentProvider.createCheckoutSession({
    tenantId: context.tenantId,
    tier,
    returnUrl: 'https://admin.spexternal.com/upgrade-success',
    cancelUrl: 'https://admin.spexternal.com/subscription'
  });
  
  // 2. Return checkout URL
  return session;
}
```

### Marketplace Purchase (Phase 2: Future)

- User clicks "Buy" in Microsoft AppSource
- Marketplace redirects to SaaS landing page
- Backend receives webhook from marketplace
- Subscription activated automatically

## Offboarding / Cancellation

### Cancellation Flow

```typescript
export async function cancelSubscription(
  tenantId: number
): Promise<void> {
  const subscription = await db.subscriptions.findByTenantId(tenantId);
  
  // 1. Mark subscription as cancelled
  await db.subscriptions.update(subscription.id, {
    status: 'Cancelled',
    gracePeriodEnd: addDays(new Date(), 90)  // 90-day grace period
  });
  
  // 2. Send cancellation confirmation
  const tenant = await db.tenants.findById(tenantId);
  await emailService.send({
    to: tenant.primaryAdminEmail,
    subject: 'Subscription cancelled',
    template: 'subscription-cancelled',
    data: {
      gracePeriodEnd: subscription.gracePeriodEnd,
      dataExportLink: 'https://api.spexternal.com/export'
    }
  });
  
  // 3. Schedule data deletion after grace period
  await scheduleDataDeletion(tenantId, subscription.gracePeriodEnd);
}
```

## Onboarding Metrics

Track onboarding success:

```typescript
export interface OnboardingMetrics {
  totalOnboarded: number;
  onboardedThisMonth: number;
  averageTimeToFirstUse: number;  // minutes
  conversionRate: number;  // trial → paid %
  dropoffPoints: {
    consent: number;
    spfxInstall: number;
    firstUse: number;
  };
}
```

## Troubleshooting Common Issues

### Issue: Admin Consent Failed

**Symptoms**: Consent redirect returns error
**Causes**: 
- Insufficient privileges (not global admin)
- App not properly registered in Azure AD

**Resolution**: 
1. Verify user has Global Admin role
2. Check app registration in Azure portal
3. Retry consent flow

### Issue: Tenant Not Found After Consent

**Symptoms**: SPFx shows "Not Connected"
**Causes**:
- Backend callback failed
- Database connection issue

**Resolution**:
1. Check backend logs in App Insights
2. Verify database connectivity
3. Retry onboarding API call

### Issue: SPFx Cannot Acquire Token

**Symptoms**: SPFx shows auth error
**Causes**:
- Missing API permission in SPFx
- Incorrect resource URI

**Resolution**:
1. Update `package-solution.json` with API permissions
2. Re-deploy SPFx package
3. Admin re-consents in SharePoint

## Security Considerations

- **State Parameter**: CSRF protection in consent flow
- **Token Validation**: Backend validates all tokens
- **Audit Logging**: All onboarding steps logged
- **Email Verification**: Verify primary admin email (future)
- **MFA Requirement**: Require MFA for admin consent (future)
