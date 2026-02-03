# Tenant Onboarding Flow

## Overview

This document describes the end-to-end tenant onboarding process for the SharePoint External User Manager SaaS platform, from initial signup to full activation.

## Onboarding Journey

```
┌──────────────┐     ┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│   Discover   │────▶│   Sign Up    │────▶│    Consent   │────▶│   Configure  │
│   Product    │     │   Account    │     │   Azure AD   │     │    Tenant    │
└──────────────┘     └──────────────┘     └──────────────┘     └──────────────┘
                                                                        │
                                                                        ▼
┌──────────────┐     ┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│   Complete   │◀────│   Install    │◀────│    Start     │◀────│   Provision  │
│  Onboarding  │     │  SPFx Web    │     │    Trial     │     │  Resources   │
└──────────────┘     └──────────────┘     └──────────────┘     └──────────────┘
```

## Phase 1: Discovery & Sign Up

### 1.1 Landing Page
User visits the marketing website: `https://spexternal.com`

**Key Information:**
- Product features and benefits
- Pricing tiers (Free Trial, Pro, Enterprise)
- Video demo and screenshots
- Customer testimonials
- Documentation links

### 1.2 Sign Up Flow
User clicks "Start Free Trial" button

**Required Information:**
```json
{
  "email": "admin@contoso.com",
  "firstName": "John",
  "lastName": "Admin",
  "companyName": "Contoso Ltd",
  "companySize": "50-200",
  "role": "IT Administrator",
  "phoneNumber": "+1-555-0123"
}
```

**Email Verification:**
1. Send verification email with 6-digit code
2. User enters code to verify email address
3. Account marked as verified

## Phase 2: Azure AD Consent

### 2.1 Admin Consent Flow

**Redirect to Azure AD:**
```
https://login.microsoftonline.com/{tenant}/adminconsent
?client_id={our_app_id}
&state={state_token}
&redirect_uri=https://api.spexternal.com/v1/auth/callback
&scope=https://graph.microsoft.com/.default
```

**Required Permissions:**
- `Sites.Read.All` - Read all site collections
- `Sites.Manage.All` - Manage all site collections
- `User.Read.All` - Read all users' full profiles
- `Directory.Read.All` - Read directory data

### 2.2 Consent Screen
User sees Microsoft consent dialog:
- Application name: "SharePoint External User Manager"
- Publisher: Verified publisher badge
- Permissions requested (listed above)
- Organization name (auto-detected)

### 2.3 Post-Consent Callback
After consent is granted:

1. Azure AD redirects to: `https://api.spexternal.com/v1/auth/callback?admin_consent=True&tenant={tenantId}&state={state_token}`
2. Backend validates state token
3. Backend stores consent record
4. Redirect user to onboarding wizard

**Error Handling:**
- If consent denied: Show message and "Try Again" button
- If consent failed: Show error message with support link

## Phase 3: Tenant Provisioning

### 3.1 Create Tenant Record

**API Call (Internal):**
```http
POST /internal/tenants/provision
Content-Type: application/json

{
  "tenantId": "contoso.onmicrosoft.com",
  "adminEmail": "admin@contoso.com",
  "companyName": "Contoso Ltd",
  "subscriptionTier": "trial",
  "dataLocation": "eastus"
}
```

**Backend Actions:**
1. Create tenant record in Cosmos DB
2. Provision tenant SQL database
3. Run database migrations
4. Initialize default configuration
5. Create default policies
6. Generate API credentials
7. Send welcome email

**Provisioning Time:** ~30 seconds

### 3.2 Default Configuration

**Policies:**
```json
{
  "expirationPolicy": {
    "enabled": true,
    "defaultExpirationDays": 90,
    "sendReminderDays": 7,
    "autoRevoke": false
  },
  "approvalPolicy": {
    "enabled": false,
    "requireApprovalForInvites": false,
    "approvers": []
  },
  "restrictionPolicy": {
    "enabled": false,
    "allowedDomains": [],
    "blockedDomains": []
  }
}
```

**Trial Subscription:**
```json
{
  "tier": "trial",
  "status": "active",
  "startDate": "2024-01-20T00:00:00Z",
  "endDate": "2024-02-19T23:59:59Z",
  "limits": {
    "maxExternalUsers": 25,
    "maxLibraries": 10,
    "apiCallsPerMonth": 10000,
    "auditRetentionDays": 30
  }
}
```

## Phase 4: Configuration Wizard

### 4.1 Welcome Screen
**Content:**
- Welcome message
- Quick overview of setup steps
- Estimated time: 5 minutes
- "Let's Get Started" button

### 4.2 Step 1: Connect SharePoint
**Instructions:**
1. Select SharePoint site to monitor
2. Grant library access permissions
3. Verify connection

**UI Flow:**
```
┌─────────────────────────────────────┐
│  Connect Your SharePoint            │
├─────────────────────────────────────┤
│  [Search for site...]               │
│                                     │
│  ☑ https://contoso.sharepoint.com  │
│  ☑ https://contoso.sharepoint.com/sites/projects  │
│  ☐ https://contoso.sharepoint.com/sites/hr        │
│                                     │
│  [Back]              [Next]         │
└─────────────────────────────────────┘
```

**Backend Validation:**
```http
POST /tenants/me/validate-connection
{
  "siteUrl": "https://contoso.sharepoint.com"
}

Response:
{
  "success": true,
  "data": {
    "siteTitle": "Contoso",
    "librariesCount": 15,
    "hasPermissions": true
  }
}
```

### 4.3 Step 2: Configure Policies
**Options:**
- External user expiration (30, 60, 90, 180, 365 days, or never)
- Require approval for invitations (Yes/No)
- Allowed email domains (optional)
- Notification preferences

**UI Flow:**
```
┌─────────────────────────────────────┐
│  Set Your Policies                  │
├─────────────────────────────────────┤
│  Default user expiration:           │
│  ◉ 90 days  ○ 180 days  ○ Never    │
│                                     │
│  Require approval for invites:      │
│  ○ Yes      ◉ No                    │
│                                     │
│  Allowed email domains (optional):  │
│  [partner.com, vendor.com]          │
│                                     │
│  [Back]              [Next]         │
└─────────────────────────────────────┘
```

### 4.4 Step 3: Invite Team Members
**Optional Step:**
Add additional administrators to the platform

**UI Flow:**
```
┌─────────────────────────────────────┐
│  Invite Your Team (Optional)        │
├─────────────────────────────────────┤
│  Email                    Role      │
│  [manager@contoso.com]    [Admin ▼]│
│  [user@contoso.com]       [Reader▼]│
│                                     │
│  [+ Add Another]                    │
│                                     │
│  [Skip]     [Back]      [Invite]    │
└─────────────────────────────────────┘
```

**Roles:**
- Owner: Full access, billing management
- Admin: Full access, no billing
- Reader: View-only access

### 4.5 Step 4: Install SPFx Web Part
**Instructions:**
1. Download SPFx package (`.sppkg` file)
2. Upload to SharePoint App Catalog
3. Deploy and trust the solution
4. Add web part to a page

**UI Flow:**
```
┌─────────────────────────────────────┐
│  Install SharePoint Web Part        │
├─────────────────────────────────────┤
│  Step 1: Download Package           │
│  [Download .sppkg File]             │
│                                     │
│  Step 2: Upload to App Catalog      │
│  1. Go to SharePoint Admin Center   │
│  2. Apps > App Catalog > Apps       │
│  3. Upload the .sppkg file          │
│  4. Click "Deploy"                  │
│                                     │
│  Step 3: Add to Page                │
│  1. Edit a SharePoint page          │
│  2. Add "External User Manager"     │
│  3. Save and publish                │
│                                     │
│  [View Detailed Instructions]       │
│                                     │
│  [Back]              [Complete]     │
└─────────────────────────────────────┘
```

**Video Tutorial:** Embedded video showing installation process

## Phase 5: Activation & First Use

### 5.1 Onboarding Complete
**Success Screen:**
```
┌─────────────────────────────────────┐
│  🎉 You're All Set!                 │
├─────────────────────────────────────┤
│  Your trial is active until:        │
│  February 19, 2024                  │
│                                     │
│  Next Steps:                        │
│  ☑ Connected SharePoint             │
│  ☑ Configured policies              │
│  ☑ Invited team members             │
│  ☐ Added web part to page           │
│                                     │
│  [Go to Dashboard]                  │
│  [Download Web Part Again]          │
└─────────────────────────────────────┘
```

### 5.2 Welcome Email
Sent automatically after onboarding:

**Subject:** Welcome to SharePoint External User Manager 🎉

**Content:**
```
Hi John,

Welcome to SharePoint External User Manager! Your trial is now active.

Your Account Details:
- Organization: Contoso Ltd
- Tenant ID: contoso.onmicrosoft.com
- Trial ends: February 19, 2024
- Dashboard: https://portal.spexternal.com

Quick Links:
- Documentation: https://docs.spexternal.com
- Video Tutorials: https://docs.spexternal.com/videos
- Support: support@spexternal.com

What's Next?
1. Add the web part to your SharePoint site
2. Invite your first external user
3. Explore audit logs and reports

Need help? Reply to this email or check our documentation.

Best regards,
The SharePoint External User Manager Team
```

### 5.3 In-App Tour
First-time user experience in the SPFx web part:

**Tour Steps:**
1. "This is your External User Dashboard"
2. "Click here to invite external users"
3. "View and manage user access here"
4. "Check audit logs and reports here"
5. "Configure policies and settings here"

**Dismissible:** User can skip tour or complete it

## Phase 6: Trial Management

### 6.1 Trial Reminders

**7 Days Before Expiration:**
```
Subject: Your trial expires in 7 days

Hi John,

Your SharePoint External User Manager trial expires on February 19, 2024.

Current Usage:
- External Users: 12 / 25
- API Calls: 3,420 / 10,000

Ready to upgrade? Choose a plan that fits your needs:
- Pro: $49/month (up to 500 users)
- Enterprise: $199/month (unlimited users + advanced features)

[View Pricing] [Upgrade Now]
```

**1 Day Before Expiration:**
```
Subject: Your trial expires tomorrow

Hi John,

Your trial ends tomorrow (February 19, 2024).

Upgrade now to keep your data and continue managing external users.

[Upgrade Now]
```

### 6.2 Trial Expiration
**On Expiration Day:**
- Status changed to "trial_expired"
- API returns 402 Payment Required
- SPFx web part shows upgrade banner
- Email sent with upgrade instructions

**Grace Period:** 7 days
- Read-only access to data
- Cannot invite/remove users
- Can export audit logs
- Can upgrade to paid plan

### 6.3 Post-Grace Period
**After Grace Period:**
- Status changed to "suspended"
- All API access disabled (except /tenants/me)
- Data retained for 30 days
- Can reactivate by upgrading

## Phase 7: Upgrade to Paid Plan

### 7.1 Choose Plan
**Available Plans:**
```
┌─────────────────────────────────────┐
│  Choose Your Plan                   │
├─────────────────────────────────────┤
│  Pro                                │
│  $49 /month                         │
│  - Up to 500 external users         │
│  - 100 libraries                    │
│  - 100K API calls/month             │
│  - 1 year audit retention           │
│  [Select Pro]                       │
│                                     │
│  Enterprise                         │
│  $199 /month                        │
│  - Unlimited external users         │
│  - Unlimited libraries              │
│  - Unlimited API calls              │
│  - 7 years audit retention          │
│  - Priority support                 │
│  - Custom policies                  │
│  [Select Enterprise]                │
└─────────────────────────────────────┘
```

### 7.2 Payment (Own Billing)
**Payment Methods:**
- Credit Card (Stripe)
- Bank Transfer / Invoice (Enterprise only)
- Purchase Order (Enterprise only)

**Billing Frequency:**
- Monthly
- Annual (10% discount)

### 7.3 Activation
After successful payment:
1. Update subscription status to "active"
2. Update limits based on plan
3. Send payment receipt
4. Send activation confirmation
5. Enable all features

## Onboarding Metrics

### Success Criteria
- **Onboarding Completion Rate**: > 80%
- **Time to Value**: < 10 minutes (from signup to first use)
- **Trial Conversion Rate**: > 25%
- **Support Tickets During Onboarding**: < 5%

### Tracking Events
```typescript
// Analytics events to track
analytics.track('Onboarding:Started');
analytics.track('Onboarding:ConsentGranted');
analytics.track('Onboarding:TenantProvisioned');
analytics.track('Onboarding:ConfigurationCompleted');
analytics.track('Onboarding:SPFxInstalled');
analytics.track('Onboarding:FirstUserInvited');
analytics.track('Onboarding:Completed');
```

## Troubleshooting

### Common Issues

**Issue: Consent Failed**
- **Cause**: User not global admin
- **Solution**: Must be Global Admin or Application Admin

**Issue: Provisioning Timeout**
- **Cause**: High load or Azure service issue
- **Solution**: Automatic retry after 1 minute

**Issue: SharePoint Connection Failed**
- **Cause**: Insufficient permissions or incorrect URL
- **Solution**: Verify permissions and URL format

**Issue: Web Part Not Appearing**
- **Cause**: Not deployed in App Catalog
- **Solution**: Deploy solution in App Catalog

## Support During Onboarding

**In-App Help:**
- Help tooltips on every step
- "Need Help?" button (opens chat)
- Link to documentation

**Email Support:**
- onboarding@spexternal.com
- Response time: < 2 hours during business hours

**Live Chat:**
- Available during onboarding wizard
- Powered by Intercom or similar

---

**Last Updated**: 2024-02-03
**Version**: 1.0
