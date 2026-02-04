# Marketplace Readiness Plan

## Overview

This document outlines the strategy and requirements for publishing the SharePoint External User Manager to the Microsoft Commercial Marketplace (AppSource and Azure Marketplace).

## Marketplace Options

### Option 1: AppSource (Recommended for SPFx)

**Best for**: SaaS + SPFx web part bundle
**Audience**: Microsoft 365 admins, SharePoint users
**Discovery**: In-product (SharePoint Store), web search

**Benefits**:
- Direct visibility to SharePoint users
- Seamless installation experience
- Integration with Microsoft 365 admin center
- Higher conversion rates for productivity solutions

### Option 2: Azure Marketplace

**Best for**: Technical/developer audiences
**Audience**: IT professionals, developers, Azure users
**Discovery**: Azure portal, web search

**Benefits**:
- Broader developer reach
- Integration with Azure subscriptions
- Enterprise procurement processes

### Recommended Approach: Dual Listing

- **AppSource**: SPFx web part + SaaS subscription
- **Azure Marketplace**: SaaS offering only (for non-SPFx scenarios)

## Publishing Phases

### Phase 1: MVP (Own Billing First) ✅ Current Focus

**Timeline**: Months 1-3

**Deliverables**:
- [x] SaaS backend fully functional
- [x] SPFx web part production-ready
- [x] Own billing/payment system (Stripe, Paddle, etc.)
- [x] Basic subscription management
- [x] Trial period implementation

**Why Own Billing First**:
- Faster go-to-market (no marketplace approval wait)
- Validate product-market fit
- Gather customer feedback
- Iterate rapidly without marketplace constraints
- Build sales history for marketplace application

### Phase 2: Marketplace Preparation (Months 4-6)

**Technical Requirements**:
- [ ] Implement SaaS Fulfillment API v2
- [ ] Add webhook handlers for marketplace events
- [ ] Create landing page for marketplace purchases
- [ ] Implement metered billing (if needed)
- [ ] Add customer management portal

**Business Requirements**:
- [ ] Partner Center account setup
- [ ] Legal entity verification
- [ ] Tax information (W-8/W-9)
- [ ] Banking information for payouts
- [ ] Compliance certifications

**Marketing Requirements**:
- [ ] Marketplace listing copy
- [ ] Screenshots and demo videos
- [ ] Customer case studies
- [ ] Support documentation
- [ ] Pricing model finalized

### Phase 3: Marketplace Submission (Month 7)

**Submission Checklist**:
- [ ] Create offer in Partner Center
- [ ] Configure technical configuration
- [ ] Set pricing and plans
- [ ] Upload marketing assets
- [ ] Submit for certification
- [ ] Address certification feedback

**Expected Timeline**:
- Submission: 1 week
- Certification: 2-4 weeks
- Iterations: 1-2 weeks per cycle
- Go-live: After approval

### Phase 4: Marketplace Launch (Month 8+)

**Launch Activities**:
- [ ] Publish listing
- [ ] Enable existing customers for marketplace billing
- [ ] Marketing campaign
- [ ] Monitor marketplace metrics
- [ ] Optimize based on feedback

## SaaS Fulfillment API Integration

### Overview

The SaaS Fulfillment API enables Microsoft to manage subscriptions on your behalf.

**Architecture**:
```
Microsoft Marketplace
      ↓ (1) Purchase Event
Landing Page (Your App)
      ↓ (2) Resolve Token
SaaS Fulfillment API
      ↓ (3) Activate Subscription
Your Backend
      ↓ (4) Provision Resources
Customer Ready
```

### Required Endpoints

#### 1. Landing Page

User lands here after clicking "Configure Account" in marketplace.

**URL**: `https://admin.spexternal.com/marketplace/setup`

**Flow**:
```typescript
export async function handleMarketplaceLanding(
  token: string
): Promise<void> {
  // 1. Resolve marketplace token
  const subscription = await resolveMarketplaceToken(token);
  
  // 2. Show setup page
  const setupData = {
    subscriptionId: subscription.id,
    subscriptionName: subscription.name,
    planId: subscription.planId,
    quantity: subscription.quantity,
    beneficiary: subscription.beneficiary,
    purchaser: subscription.purchaser
  };
  
  // 3. User completes setup
  // 4. Activate subscription via API
  await activateSubscription(subscription.id);
}
```

#### 2. Webhook Endpoint

Receives subscription lifecycle events from Microsoft.

**URL**: `https://api.spexternal.com/v1/marketplace/webhook`

**Events**:
- `Subscribed`: New subscription created
- `Unsubscribed`: Subscription cancelled
- `ChangePlan`: User changed plan
- `ChangeQuantity`: Quantity updated
- `Suspended`: Payment failed
- `Reinstated`: Subscription reactivated

**Handler**:
```typescript
export async function handleMarketplaceWebhook(
  event: MarketplaceEvent
): Promise<void> {
  switch (event.action) {
    case 'Subscribed':
      await handleNewSubscription(event);
      break;
    case 'Unsubscribed':
      await handleCancellation(event);
      break;
    case 'ChangePlan':
      await handlePlanChange(event);
      break;
    case 'Suspended':
      await handleSuspension(event);
      break;
    case 'Reinstated':
      await handleReinstatement(event);
      break;
  }
}
```

### Fulfillment API Calls

#### Resolve Token
```typescript
async function resolveMarketplaceToken(
  token: string
): Promise<SubscriptionDetails> {
  const response = await fetch(
    'https://marketplaceapi.microsoft.com/api/saas/subscriptions/resolve',
    {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${await getMarketplaceToken()}`,
        'Content-Type': 'application/json',
        'x-ms-marketplace-token': token
      }
    }
  );
  return await response.json();
}
```

#### Activate Subscription
```typescript
async function activateSubscription(
  subscriptionId: string
): Promise<void> {
  await fetch(
    `https://marketplaceapi.microsoft.com/api/saas/subscriptions/${subscriptionId}/activate`,
    {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${await getMarketplaceToken()}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        planId: 'pro-plan',
        quantity: 1
      })
    }
  );
}
```

## Pricing Model for Marketplace

### Recommended Plans

#### Plan 1: Starter
- **Price**: $49/month
- **Users**: Up to 25 external users
- **Features**: Basic user management, 30-day audit history
- **Support**: Community support

#### Plan 2: Professional
- **Price**: $149/month
- **Users**: Up to 100 external users
- **Features**: Advanced policies, 90-day audit, CSV export
- **Support**: Email support (48h response)

#### Plan 3: Enterprise
- **Price**: $499/month
- **Users**: Unlimited
- **Features**: All features, 365-day audit, API access, custom reporting
- **Support**: Priority support (24h response) + dedicated CSM

### Pricing Strategy

**Per-User vs Flat Rate**: 
- **Recommendation**: Flat rate per tier (simpler for customers)
- **Alternative**: Base price + per-user overage

**Monthly vs Annual**:
- Offer both with annual discount (15-20%)
- Annual: $499 → $5,988/year ($499/month)
- Annual discounted: $5,088/year ($424/month, 15% off)

## Marketplace Listing

### Offer Details

**Offer Name**: SharePoint External User Manager
**Offer ID**: sharepoint-external-user-mgr
**Publisher**: [Your Company Name]
**Category**: Collaboration, Productivity, IT & Management Tools

### Description (Summary)

*"Streamline external user management in SharePoint with automated workflows, compliance policies, and comprehensive audit trails. Built for Microsoft 365."*

### Long Description

```markdown
# SharePoint External User Manager

Manage external users and guest access across your SharePoint environment with confidence.

## Key Features

✓ **Centralized Management**: View all external users across sites in one place
✓ **Smart Policies**: Automate guest expiration, approval workflows, domain controls
✓ **Compliance Ready**: Comprehensive audit logs for security reviews
✓ **Native Integration**: SPFx web part integrates seamlessly with SharePoint
✓ **Secure & Scalable**: Enterprise-grade security with multi-tenant architecture

## Perfect For

- IT Administrators managing external collaboration
- Compliance officers needing audit trails
- Project managers working with partners
- Organizations with strict security requirements

## How It Works

1. Install SPFx web part from App Catalog
2. Connect your tenant (one-click consent)
3. Start managing external users immediately
4. Set policies to automate governance

## Support & Resources

- 📖 Comprehensive documentation
- 💬 Email support (Pro & Enterprise)
- 🎥 Video tutorials
- 📞 Priority support (Enterprise)
```

### Screenshots

**Required**: 5 screenshots minimum
1. Main dashboard (external users list)
2. Subscription status page
3. Policy configuration screen
4. Audit log viewer
5. User invitation workflow

**Specifications**:
- Size: 1280x720 or 1920x1080
- Format: PNG or JPEG
- No borders, annotations in English

### Demo Video

**Recommended**: 90-second overview video
- Introduction (10s)
- Problem statement (15s)
- Solution demo (45s)
- Key benefits (15s)
- Call to action (5s)

**Hosting**: YouTube (unlisted) or Vimeo

## Certification Requirements

### Functional Testing

Microsoft tests:
- [ ] Subscription activation flow
- [ ] Plan changes
- [ ] Subscription cancellation
- [ ] Webhook event handling
- [ ] Landing page functionality

### Security Requirements

- [ ] HTTPS only (no HTTP)
- [ ] Valid SSL certificate
- [ ] Azure AD authentication
- [ ] Data encryption at rest and in transit
- [ ] GDPR compliance statement

### Support Requirements

- [ ] Support email published
- [ ] Privacy policy URL
- [ ] Terms of use URL
- [ ] Documentation URL
- [ ] Support SLA defined

## Migration Path (Own Billing → Marketplace)

### For Existing Customers

**Option 1: Grandfather Existing Pricing**
- Continue billing via own system
- Offer optional migration to marketplace
- Provide migration incentive (1 month free)

**Option 2: Force Migration**
- Give 90-day notice
- Provide migration wizard
- Maintain same pricing (or better)

**Recommended**: Option 1 (customer choice)

### Technical Migration

```typescript
export async function migrateToMarketplace(
  tenantId: number,
  marketplaceSubscriptionId: string
): Promise<void> {
  // 1. Cancel own billing subscription
  await ownBilling.cancel(tenantId);
  
  // 2. Link marketplace subscription
  await db.subscriptions.update(tenantId, {
    source: 'Marketplace',
    marketplaceSubscriptionId,
    migratedAt: new Date()
  });
  
  // 3. Notify customer
  await emailService.send({
    to: tenant.primaryAdminEmail,
    subject: 'Migration to Marketplace Complete',
    template: 'marketplace-migration-complete'
  });
}
```

## Marketplace Metrics

### Key Performance Indicators

Track:
- **Listing Views**: How many people view your offer
- **Starts**: Users who click "Get It Now"
- **Conversions**: Completed subscriptions
- **Trial-to-Paid**: Conversion from trial to paid subscription
- **Churn Rate**: Cancellations per month

**Targets** (First Year):
- 1,000 listing views/month
- 5% conversion rate (50 trials/month)
- 30% trial-to-paid conversion (15 paid/month)
- < 5% monthly churn

## Post-Launch Optimization

### Continuous Improvement

1. **Analyze Marketplace Analytics**: Partner Center dashboard
2. **Customer Feedback**: Reviews, support tickets
3. **A/B Testing**: Listing copy, screenshots, pricing
4. **Feature Additions**: Based on customer requests
5. **Case Studies**: Highlight success stories

### Review Management

- Respond to all reviews (positive and negative)
- Address issues raised in reviews
- Encourage satisfied customers to leave reviews
- Showcase reviews in marketing materials

## Timeline Summary

| Phase | Duration | Key Milestones |
|-------|----------|---------------|
| **Phase 1: MVP (Own Billing)** | Months 1-3 | Product ready, first customers |
| **Phase 2: Preparation** | Months 4-6 | Fulfillment API, marketing assets |
| **Phase 3: Submission** | Month 7 | Submit to marketplace, certification |
| **Phase 4: Launch** | Month 8 | Go live, marketing campaign |
| **Phase 5: Optimization** | Ongoing | Iterate based on data |

## Success Criteria

✅ **MVP Success** (Phase 1):
- 10+ paying customers on own billing
- Positive customer feedback (NPS > 40)
- < 10% churn rate
- Product-market fit validated

✅ **Marketplace Success** (Phase 4):
- Listing approved and published
- 50+ marketplace subscriptions in first 3 months
- 4+ star average rating
- Featured in marketplace (goal)

## Resources

### Microsoft Documentation
- [SaaS Fulfillment API Reference](https://docs.microsoft.com/azure/marketplace/partner-center-portal/pc-saas-fulfillment-api-v2)
- [AppSource Publishing Guide](https://docs.microsoft.com/azure/marketplace/marketplace-publishers-guide)
- [Certification Checklist](https://docs.microsoft.com/azure/marketplace/marketplace-certification-policies)

### Partner Center
- [Partner Center Portal](https://partner.microsoft.com/dashboard)
- [Marketplace Insights](https://partner.microsoft.com/dashboard/insights/commercial-marketplace)

## Next Steps (MVP Phase)

✅ **Immediate (This Epic)**:
1. Complete SaaS backend implementation
2. Implement subscription enforcement
3. Create admin pages (Connect Tenant, Subscription Status)
4. Deploy to Azure with CI/CD

🎯 **Next Epic (Marketplace Prep)**:
1. Implement SaaS Fulfillment API v2
2. Create marketplace landing page
3. Develop webhook handlers
4. Prepare marketing materials
5. Submit for certification
# Microsoft Commercial Marketplace Plan

## Overview

This document outlines the strategy and technical requirements for publishing SharePoint External User Manager on the Microsoft Commercial Marketplace (AppSource and Azure Marketplace).

## Why Marketplace?

### Business Benefits
1. **Discovery**: Reach millions of Microsoft 365 customers
2. **Trust**: Microsoft verified publisher badge
3. **Procurement**: Simplified for enterprise buyers (invoiced through Microsoft)
4. **Integration**: Native billing through Azure/M365 subscriptions
5. **Marketplace Rewards**: Up to $100K in benefits for new publishers

### Technical Benefits
1. **Single Sign-On**: Seamless Azure AD authentication
2. **License Management**: Automated subscription lifecycle
3. **Billing Integration**: Automatic metering and invoicing
4. **Co-sell**: Eligible for Microsoft co-sell program

## Marketplace Offer Types

### Recommended: SaaS Offer

**Characteristics:**
- Web-based application (our case)
- Users access via browser or client app
- Multi-tenant architecture
- Subscription-based pricing
- Billing through Microsoft

**Transaction Models:**
1. **List**: Customers contact us directly (no transact)
2. **Transact**: Microsoft handles billing (recommended)

**We will use: SaaS Transact Offer**

## Publishing Requirements

### 1. Technical Requirements

#### 1.1 Landing Page
Public-facing landing page for marketplace purchases

**URL**: `https://portal.spexternal.com/marketplace/landing`

**Requirements:**
- Accessible without authentication
- Collects marketplace purchase token
- Validates token via SaaS Fulfillment API
- Redirects to tenant provisioning

**Flow:**
```
1. User purchases in Marketplace
   ↓
2. Microsoft redirects to landing page with token
   ↓
3. Landing page displays: "Activating your subscription..."
   ↓
4. Backend calls SaaS Fulfillment API
   ↓
5. Backend provisions tenant
   ↓
6. Redirect to onboarding wizard
```

#### 1.2 Webhook Endpoint
Endpoint to receive subscription lifecycle events from Microsoft

**URL**: `https://api.spexternal.com/v1/marketplace/webhook`

**Events to Handle:**
```json
{
  "events": [
    "Subscribed",        // New subscription activated
    "Unsubscribed",      // Subscription cancelled
    "ChangePlan",        // Customer changed plan
    "ChangeQuantity",    // Seat count changed
    "Suspended",         // Payment failed or compliance issue
    "Reinstated",        // Subscription reactivated
    "Renewed"            // Subscription auto-renewed
  ]
}
```

#### 1.3 SaaS Fulfillment API Integration

**Microsoft APIs to Integrate:**
```typescript
interface SaaSFulfillmentAPI {
  // Resolve purchase token to subscription details
  resolvePurchaseToken(token: string): Promise<Subscription>;
  
  // Activate subscription
  activateSubscription(subscriptionId: string): Promise<void>;
  
  // Update subscription status
  updateSubscription(
    subscriptionId: string, 
    planId: string, 
    quantity: number
  ): Promise<void>;
  
  // Get subscription details
  getSubscription(subscriptionId: string): Promise<Subscription>;
  
  // List all subscriptions
  listSubscriptions(): Promise<Subscription[]>;
  
  // Send usage events (for metered billing)
  sendUsageEvent(usageEvent: UsageEvent): Promise<void>;
}
```

**Implementation Example:**
```typescript
import axios from 'axios';

class MarketplaceService {
  private readonly baseUrl = 'https://marketplaceapi.microsoft.com/api';
  
  async resolvePurchaseToken(token: string): Promise<any> {
    const response = await axios.post(
      `${this.baseUrl}/saas/subscriptions/resolve`,
      { token },
      {
        headers: {
          'Authorization': `Bearer ${await this.getAccessToken()}`,
          'Content-Type': 'application/json',
          'x-ms-marketplace-token': token
        }
      }
    );
    return response.data;
  }
  
  async activateSubscription(subscriptionId: string, planId: string): Promise<void> {
    await axios.post(
      `${this.baseUrl}/saas/subscriptions/${subscriptionId}/activate`,
      { planId },
      {
        headers: {
          'Authorization': `Bearer ${await this.getAccessToken()}`,
          'Content-Type': 'application/json'
        }
      }
    );
  }
  
  private async getAccessToken(): Promise<string> {
    // Get Azure AD token for marketplace API
    // Scope: 20e940b3-4c77-4b0b-9a53-9e16a1b010a7/.default
    return 'access_token_here';
  }
}
```

### 2. Business Requirements

#### 2.1 Publisher Profile
**Microsoft Partner Center Account:**
- Company legal name: [Your Company Name]
- Business registration: Required
- Tax information: W-9 or W-8 form
- Bank account: For payouts (70/30 split with Microsoft)

#### 2.2 Offer Details

**Offer Information:**
```yaml
Offer ID: sharepoint-external-user-manager
Offer Alias: SharePoint External User Manager
Publisher: [Your Publisher Name]
Category: Productivity > Collaboration
Industries: All industries
App Type: Web App
```

**Listing Details:**
- **Name**: SharePoint External User Manager
- **Summary**: Manage external users and guest access across SharePoint sites with ease
- **Description**: 
  ```
  SharePoint External User Manager is a comprehensive SaaS solution that helps 
  organizations manage external user access, track collaboration, enforce policies, 
  and maintain compliance across all SharePoint sites.
  
  Key Features:
  - Centralized external user management
  - Automated expiration and access reviews
  - Comprehensive audit logging
  - Company and project metadata tracking
  - Policy enforcement and compliance
  - Real-time usage analytics
  ```

**Marketing Assets:**
- Logo (216x216 PNG with transparency)
- Screenshots (1280x720, at least 3)
- Video demo (YouTube/Vimeo link)
- Support document (PDF)
- Privacy policy URL
- Terms of use URL

#### 2.3 Pricing Plans

**Plans to Offer:**

**1. Professional**
```yaml
Plan ID: pro
Name: Professional
Description: For small to medium teams
Price: $49 USD/month
Features:
  - Up to 500 external users
  - 100 libraries
  - 100K API calls/month
  - 1 year audit retention
  - Email support
```

**2. Enterprise**
```yaml
Plan ID: enterprise
Name: Enterprise
Description: For large organizations
Price: $199 USD/month
Features:
  - Unlimited external users
  - Unlimited libraries
  - Unlimited API calls
  - 7 years audit retention
  - Priority support
  - Custom policies
  - Dedicated success manager
```

**3. Free Trial**
```yaml
Plan ID: trial
Name: Free Trial
Description: 30-day free trial
Price: $0 USD
Duration: 30 days
Features:
  - Up to 25 external users
  - 10 libraries
  - 10K API calls/month
  - 30 days audit retention
Auto-converts to: Professional (if credit card provided)
```

#### 2.4 Billing Models

**Option 1: Flat Rate (Recommended for MVP)**
- Fixed monthly/annual price per plan
- Simpler to implement
- Easier for customers to understand

**Option 2: Per-User (Future)**
- Price per external user managed
- More granular pricing
- Requires metered billing implementation

**Option 3: Usage-Based (Future)**
- Price per API call or feature usage
- Requires SaaS Metering Service integration
- Complex but flexible

**MVP Recommendation: Flat Rate with Plan Tiers**

### 3. Technical Integration Plan

#### 3.1 Phase 1: Marketplace-Ready Infrastructure (2-3 weeks)

**Tasks:**
1. Create marketplace landing page
2. Implement webhook endpoint
3. Integrate SaaS Fulfillment API
4. Add marketplace subscription table to database
5. Update subscription management logic
6. Add marketplace tracking to analytics

**Database Schema:**
```sql
CREATE TABLE MarketplaceSubscriptions (
    MarketplaceSubscriptionId UNIQUEIDENTIFIER PRIMARY KEY,
    TenantId NVARCHAR(255) NOT NULL,
    PlanId NVARCHAR(100) NOT NULL,
    Status NVARCHAR(50) NOT NULL, -- 'active', 'suspended', 'unsubscribed'
    Quantity INT NOT NULL DEFAULT 1,
    PurchaseToken NVARCHAR(MAX),
    SubscriptionName NVARCHAR(255),
    PurchaserEmail NVARCHAR(255),
    OfferName NVARCHAR(255),
    CreatedDate DATETIME2 NOT NULL,
    ActivatedDate DATETIME2,
    LastModifiedDate DATETIME2,
    
    FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId),
    INDEX IX_TenantId (TenantId),
    INDEX IX_Status (Status)
);
```

#### 3.2 Phase 2: Partner Center Setup (1-2 weeks)

**Tasks:**
1. Register as Microsoft Publisher
2. Submit company verification
3. Create offer in Partner Center
4. Configure technical details
5. Upload marketing assets
6. Set up test environment
7. Submit for certification

**Partner Center Configuration:**
```json
{
  "technicalConfiguration": {
    "landingPageUrl": "https://portal.spexternal.com/marketplace/landing",
    "connectionWebhook": "https://api.spexternal.com/v1/marketplace/webhook",
    "azureActiveDirectoryTenantId": "your-aad-tenant-id",
    "azureActiveDirectoryApplicationId": "your-aad-app-id"
  },
  "plans": [
    {
      "planId": "pro",
      "planName": "Professional",
      "description": "For small to medium teams",
      "pricingModel": "flatRate",
      "price": 49.00,
      "currency": "USD",
      "billingTerm": "monthly"
    },
    {
      "planId": "enterprise",
      "planName": "Enterprise",
      "description": "For large organizations",
      "pricingModel": "flatRate",
      "price": 199.00,
      "currency": "USD",
      "billingTerm": "monthly"
    }
  ]
}
```

#### 3.3 Phase 3: Testing & Certification (2-3 weeks)

**Testing Checklist:**
- [ ] Purchase flow works end-to-end
- [ ] Landing page redirects correctly
- [ ] Webhook receives all event types
- [ ] Subscription activation works
- [ ] Plan changes work correctly
- [ ] Cancellation flow works
- [ ] Reactivation works
- [ ] Billing is accurate

**Microsoft Certification Requirements:**
- All purchase scenarios tested
- Security review passed
- Performance requirements met
- No blocking bugs
- Documentation complete

#### 3.4 Phase 4: Go-Live (1 week)

**Launch Tasks:**
1. Final certification approval
2. Set offer to "Live"
3. Monitor first purchases
4. Customer support ready
5. Marketing launch coordinated

## Webhook Event Handling

### Event: Subscribed
```typescript
async handleSubscribed(event: MarketplaceEvent): Promise<void> {
  const subscription = await this.resolvePurchaseToken(event.purchaseToken);
  
  // Create or update tenant
  await this.createTenant({
    tenantId: subscription.beneficiary.tenantId,
    email: subscription.beneficiary.email,
    subscriptionTier: subscription.planId,
    marketplaceSubscriptionId: subscription.id
  });
  
  // Activate marketplace subscription
  await this.activateSubscription(subscription.id, subscription.planId);
  
  // Send welcome email
  await this.sendWelcomeEmail(subscription.beneficiary.email);
}
```

### Event: Unsubscribed
```typescript
async handleUnsubscribed(event: MarketplaceEvent): Promise<void> {
  // Update subscription status
  await this.updateSubscriptionStatus(
    event.subscriptionId, 
    'cancelled'
  );
  
  // Update tenant status
  await this.updateTenantStatus(
    event.tenantId,
    'cancelled'
  );
  
  // Start data retention countdown (30 days)
  await this.scheduleDataDeletion(event.tenantId, 30);
  
  // Send cancellation confirmation
  await this.sendCancellationEmail(event.tenantId);
}
```

### Event: ChangePlan
```typescript
async handleChangePlan(event: MarketplaceEvent): Promise<void> {
  // Update subscription plan
  await this.updateSubscription(
    event.subscriptionId,
    event.newPlanId
  );
  
  // Update tenant limits
  await this.updateTenantLimits(
    event.tenantId,
    event.newPlanId
  );
  
  // Send plan change confirmation
  await this.sendPlanChangeEmail(event.tenantId, event.newPlanId);
}
```

## Financial Model

### Revenue Split
- **Microsoft Fee**: 20% of gross revenue (SaaS transact)
- **Net Revenue**: 80% to publisher
- **Payout Frequency**: Monthly
- **Currency**: USD (Microsoft handles multi-currency)

### Example Calculation
```
Monthly Price: $49
Microsoft Fee (20%): $9.80
Net Revenue: $39.20

100 customers × $39.20 = $3,920/month net revenue
1,000 customers × $39.20 = $39,200/month net revenue
```

### Marketplace Rewards Program
- **Eligibility**: New publishers
- **Benefit**: Up to $100K in Azure credits and benefits
- **Requirements**: Co-sell ready, specific revenue targets

## Go-to-Market Strategy

### Pre-Launch (4 weeks before)
1. Beta testing with 10-20 customers
2. Collect testimonials and case studies
3. Prepare launch materials (blog, video, social)
4. Train support team
5. Finalize documentation

### Launch Week
1. Announce on social media
2. Email existing customers about marketplace option
3. Press release (if budget allows)
4. Blog post on Microsoft Tech Community
5. Monitor marketplace listing and support

### Post-Launch (First 90 days)
1. Collect customer feedback
2. Monitor conversion rates
3. Optimize listing (title, description, screenshots)
4. A/B test pricing (if needed)
5. Build co-sell relationships with Microsoft

## Success Metrics

### Target Metrics (First Year)
- **Marketplace Purchases**: 50 in first month, 500 in first year
- **Conversion Rate**: 15% of visitors purchase
- **Trial-to-Paid**: 30% conversion rate
- **Customer Retention**: 85% annual retention
- **NPS Score**: > 40

### Tracking
```typescript
// Analytics events
analytics.track('Marketplace:ListingViewed');
analytics.track('Marketplace:TrialStarted');
analytics.track('Marketplace:PurchaseCompleted');
analytics.track('Marketplace:PlanChanged');
analytics.track('Marketplace:Cancelled');
```

## Risks & Mitigation

| Risk | Impact | Mitigation |
|------|--------|------------|
| Certification Delays | High | Start early, thorough testing |
| Webhook Reliability | High | Implement retry logic, monitoring |
| Payment Processing Issues | High | Comprehensive error handling |
| Customer Support Overload | Medium | Knowledge base, automated responses |
| Pricing Too High/Low | Medium | A/B testing, competitor analysis |

## Timeline to Marketplace

**Total Estimated Time: 8-12 weeks**

```
Week 1-3:   Implement marketplace integration
Week 4-5:   Partner Center setup
Week 6-8:   Testing and bug fixes
Week 9-10:  Certification process
Week 11-12: Go-live and monitoring
```

## Next Steps (MVP to Marketplace)

**Priority 1: Block MVP shipping**
- None - marketplace integration can happen after MVP

**Priority 2: Post-MVP, Pre-Marketplace**
1. Implement landing page
2. Implement webhook endpoint
3. Integrate SaaS Fulfillment API
4. Add marketplace subscription management

**Priority 3: Marketplace Submission**
1. Register as publisher
2. Create and configure offer
3. Submit for certification
4. Go live

---

**Status**: Planning Phase
**Target Launch**: Q3 2024 (after MVP launch Q2)
**Owner**: Product & Engineering Teams
**Last Updated**: 2024-02-03
