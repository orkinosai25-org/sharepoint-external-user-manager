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
