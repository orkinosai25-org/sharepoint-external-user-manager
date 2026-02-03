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
