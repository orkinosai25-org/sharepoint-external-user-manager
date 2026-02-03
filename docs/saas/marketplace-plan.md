# Azure Marketplace & AppSource Readiness Plan

## Overview

This document outlines the strategy and implementation plan for publishing the SharePoint External User Manager to Microsoft AppSource and Azure Marketplace, enabling seamless subscription management and billing integration.

## Marketplace Overview

### Publishing Options

| Option | Description | Best For | Integration Complexity |
|--------|-------------|----------|----------------------|
| **AppSource** | Microsoft business apps marketplace | M365/Dynamics 365 users | Medium |
| **Azure Marketplace** | Azure cloud services marketplace | Azure infrastructure | High |
| **Both** | Cross-publish to reach all audiences | Maximum reach | High |

**Recommendation**: Start with **AppSource** (SaaS offer) for initial MVP, then expand to Azure Marketplace.

---

## Offer Type Selection

### SaaS Offer (Recommended for MVP)

**Characteristics**:
- Software-as-a-Service subscription model
- Transact through Microsoft (Microsoft handles billing)
- Customer subscription managed via Partner Center
- Webhook integration for fulfillment events
- Revenue share: Microsoft takes 20% (3% for private offers)

**Why SaaS Offer**:
- ✅ Aligns with our multi-tenant architecture
- ✅ Microsoft handles billing and collections
- ✅ Simplified customer purchasing experience
- ✅ Integrated with Azure AD for authentication
- ✅ Appears in customers' Azure/M365 admin portals

**Pricing Models Available**:
1. **Flat Rate**: Fixed price per month/year
2. **Per User**: Price per user seat
3. **Metered Billing**: Usage-based pricing (API calls, storage, etc.)

---

## Offer Configuration

### Technical Requirements

#### 1. Landing Page

**Purpose**: Customer is redirected here after purchasing from marketplace

**Requirements**:
- HTTPS enabled
- Azure AD authentication
- Extract marketplace token from URL query parameter
- Resolve subscription details via Marketplace API
- Display subscription information
- Allow subscription activation

**Implementation**:

```typescript
// Landing page endpoint
app.get('/marketplace/landing', async (req, res) => {
  try {
    // Extract token from URL
    const { token } = req.query;
    
    if (!token) {
      return res.status(400).send('Missing marketplace token');
    }
    
    // Resolve subscription details
    const subscriptionDetails = await marketplaceAPI.resolveSubscription(token);
    
    // Store subscription in database
    await db.query(`
      INSERT INTO MarketplaceSubscriptions (
        marketplace_subscription_id,
        subscription_name,
        offer_id,
        plan_id,
        purchaser_tenant_id,
        purchaser_email,
        status,
        created_at
      ) VALUES (
        @subscriptionId,
        @subscriptionName,
        @offerId,
        @planId,
        @tenantId,
        @email,
        'PendingFulfillmentStart',
        GETUTCDATE()
      )
    `, subscriptionDetails);
    
    // Redirect to activation page
    res.redirect(`/marketplace/activate?subscription_id=${subscriptionDetails.id}`);
  } catch (error) {
    logger.error('Landing page error:', error);
    res.status(500).send('Failed to process marketplace subscription');
  }
});
```

**URL Format**:
```
https://app.spexternal.com/marketplace/landing?token={marketplace_purchase_token}
```

---

#### 2. Webhook Endpoint

**Purpose**: Receive real-time notifications about subscription lifecycle events

**Events**:
- `Subscribed`: New subscription purchased
- `Unsubscribed`: Customer cancelled subscription
- `Suspended`: Payment failed or grace period ended
- `Reinstated`: Subscription reactivated after suspension
- `ChangePlan`: Customer upgraded/downgraded plan
- `ChangeQuantity`: Customer changed seat count
- `Renewed`: Subscription automatically renewed

**Implementation**:

```typescript
// Webhook endpoint
app.post('/marketplace/webhook', async (req, res) => {
  try {
    // Validate webhook signature
    const signature = req.headers['x-ms-signature'];
    if (!validateWebhookSignature(signature, req.body)) {
      return res.status(401).send('Invalid signature');
    }
    
    const { action, subscriptionId, planId, quantity, timeStamp } = req.body;
    
    // Process event
    switch (action) {
      case 'Unsubscribe':
        await handleUnsubscribe(subscriptionId);
        break;
        
      case 'ChangePlan':
        await handlePlanChange(subscriptionId, planId);
        break;
        
      case 'ChangeQuantity':
        await handleQuantityChange(subscriptionId, quantity);
        break;
        
      case 'Suspend':
        await handleSuspend(subscriptionId);
        break;
        
      case 'Reinstate':
        await handleReinstate(subscriptionId);
        break;
        
      case 'Renew':
        await handleRenew(subscriptionId);
        break;
        
      default:
        logger.warn(`Unknown webhook action: ${action}`);
    }
    
    // Acknowledge receipt
    res.status(200).send('OK');
    
    // Log event
    await auditLogger.log({
      event_type: `Marketplace${action}`,
      event_category: 'Subscription',
      severity: 'Info',
      action: {
        name: action,
        result: 'Success',
        details: { subscriptionId, planId, quantity }
      }
    });
  } catch (error) {
    logger.error('Webhook error:', error);
    res.status(500).send('Internal error');
  }
});

// Webhook event handlers
async function handleUnsubscribe(subscriptionId: string) {
  // Update subscription status
  await db.query(`
    UPDATE MarketplaceSubscriptions
    SET status = 'Unsubscribed',
        unsubscribe_date = GETUTCDATE()
    WHERE marketplace_subscription_id = @subscriptionId
  `, { subscriptionId });
  
  // Mark tenant as churned (with grace period)
  const gracePeriodDays = 30;
  await db.query(`
    UPDATE Tenants
    SET status = 'Churned',
        service_end_date = DATEADD(day, @gracePeriod, GETUTCDATE())
    WHERE tenant_id = (
      SELECT tenant_id FROM MarketplaceSubscriptions
      WHERE marketplace_subscription_id = @subscriptionId
    )
  `, { subscriptionId, gracePeriod: gracePeriodDays });
  
  // Send cancellation email
  await emailService.sendCancellationEmail(subscriptionId);
}

async function handlePlanChange(subscriptionId: string, newPlanId: string) {
  // Get plan details
  const planTier = getPlanTier(newPlanId);
  
  // Update subscription
  await db.query(`
    UPDATE Subscriptions
    SET tier = @tier,
        updated_at = GETUTCDATE()
    WHERE tenant_id = (
      SELECT tenant_id FROM MarketplaceSubscriptions
      WHERE marketplace_subscription_id = @subscriptionId
    )
  `, { subscriptionId, tier: planTier });
  
  // Send upgrade/downgrade confirmation
  await emailService.sendPlanChangeEmail(subscriptionId, planTier);
}

async function handleSuspend(subscriptionId: string) {
  // Suspend tenant (payment failed)
  await db.query(`
    UPDATE Tenants
    SET status = 'Suspended'
    WHERE tenant_id = (
      SELECT tenant_id FROM MarketplaceSubscriptions
      WHERE marketplace_subscription_id = @subscriptionId
    )
  `, { subscriptionId });
  
  // Send payment failure email
  await emailService.sendPaymentFailureEmail(subscriptionId);
}
```

**URL Format**:
```
https://api.spexternal.com/marketplace/webhook
```

---

#### 3. Connection Webhook (Optional)

**Purpose**: Receive notifications when customer connects/disconnects from your service

**Implementation**: Same pattern as main webhook

**URL Format**:
```
https://api.spexternal.com/marketplace/connection-webhook
```

---

### Marketplace API Integration

#### Fulfillment API

```typescript
import axios from 'axios';

class MarketplaceFulfillmentAPI {
  private baseURL = 'https://marketplaceapi.microsoft.com/api';
  private apiVersion = '2018-08-31';
  
  async getAccessToken(): Promise<string> {
    // Use Azure AD client credentials flow
    const tokenResponse = await axios.post(
      `https://login.microsoftonline.com/${process.env.AZURE_AD_TENANT_ID}/oauth2/v2.0/token`,
      new URLSearchParams({
        client_id: process.env.AZURE_AD_CLIENT_ID,
        client_secret: await secretsManager.getSecret('EntraID-ClientSecret'),
        scope: '20e940b3-4c77-4b0b-9a53-9e16a1b010a7/.default',
        grant_type: 'client_credentials'
      })
    );
    
    return tokenResponse.data.access_token;
  }
  
  async resolveSubscription(token: string): Promise<MarketplaceSubscription> {
    const accessToken = await this.getAccessToken();
    
    const response = await axios.post(
      `${this.baseURL}/saas/subscriptions/resolve?api-version=${this.apiVersion}`,
      { 'x-ms-marketplace-token': token },
      {
        headers: {
          'Authorization': `Bearer ${accessToken}`,
          'Content-Type': 'application/json'
        }
      }
    );
    
    return response.data;
  }
  
  async activateSubscription(subscriptionId: string, planId: string, quantity: number) {
    const accessToken = await this.getAccessToken();
    
    await axios.post(
      `${this.baseURL}/saas/subscriptions/${subscriptionId}/activate?api-version=${this.apiVersion}`,
      {
        planId,
        quantity
      },
      {
        headers: {
          'Authorization': `Bearer ${accessToken}`,
          'Content-Type': 'application/json'
        }
      }
    );
  }
  
  async getSubscriptionDetails(subscriptionId: string): Promise<MarketplaceSubscription> {
    const accessToken = await this.getAccessToken();
    
    const response = await axios.get(
      `${this.baseURL}/saas/subscriptions/${subscriptionId}?api-version=${this.apiVersion}`,
      {
        headers: {
          'Authorization': `Bearer ${accessToken}`
        }
      }
    );
    
    return response.data;
  }
  
  async listSubscriptions(): Promise<MarketplaceSubscription[]> {
    const accessToken = await this.getAccessToken();
    
    const response = await axios.get(
      `${this.baseURL}/saas/subscriptions?api-version=${this.apiVersion}`,
      {
        headers: {
          'Authorization': `Bearer ${accessToken}`
        }
      }
    );
    
    return response.data.subscriptions;
  }
}

export const marketplaceAPI = new MarketplaceFulfillmentAPI();
```

#### Metering Service API (Optional - for usage-based billing)

```typescript
class MarketplaceMeteringAPI {
  private baseURL = 'https://marketplaceapi.microsoft.com/api';
  private apiVersion = '2018-08-31';
  
  async reportUsage(
    subscriptionId: string,
    dimensionId: string,
    quantity: number,
    effectiveStartTime: Date
  ) {
    const accessToken = await marketplaceAPI.getAccessToken();
    
    await axios.post(
      `${this.baseURL}/usageEvent?api-version=${this.apiVersion}`,
      {
        resourceId: subscriptionId,
        planId: 'plan-id',
        dimension: dimensionId,
        quantity,
        effectiveStartTime: effectiveStartTime.toISOString()
      },
      {
        headers: {
          'Authorization': `Bearer ${accessToken}`,
          'Content-Type': 'application/json'
        }
      }
    );
  }
  
  // Report usage for custom dimensions (e.g., API calls)
  async reportAPICallsUsage(tenantId: string, apiCalls: number) {
    const subscription = await this.getTenantMarketplaceSubscription(tenantId);
    
    await this.reportUsage(
      subscription.marketplace_subscription_id,
      'api-calls',  // Custom dimension ID
      apiCalls,
      new Date()
    );
  }
}
```

---

## Pricing & Plans

### Recommended Plan Structure

#### Plan 1: Free (Lead Generation)

```json
{
  "plan_id": "free",
  "display_name": "Free",
  "description": "Perfect for small teams getting started",
  "pricing": {
    "type": "FlatRate",
    "price": 0,
    "currency": "USD",
    "billing_term": "Monthly"
  },
  "limits": {
    "max_external_users": 10,
    "audit_log_retention_days": 30,
    "api_rate_limit": 50,
    "advanced_policies": false,
    "support_level": "community"
  }
}
```

#### Plan 2: Pro

```json
{
  "plan_id": "pro",
  "display_name": "Pro",
  "description": "For growing teams with advanced needs",
  "pricing": {
    "type": "FlatRate",
    "price": 49.00,
    "currency": "USD",
    "billing_term": "Monthly"
  },
  "limits": {
    "max_external_users": 100,
    "audit_log_retention_days": 365,
    "api_rate_limit": 200,
    "advanced_policies": true,
    "support_level": "email"
  }
}
```

#### Plan 3: Enterprise

```json
{
  "plan_id": "enterprise",
  "display_name": "Enterprise",
  "description": "For large organizations requiring unlimited scale",
  "pricing": {
    "type": "FlatRate",
    "price": 199.00,
    "currency": "USD",
    "billing_term": "Monthly"
  },
  "limits": {
    "max_external_users": -1,  // Unlimited
    "audit_log_retention_days": -1,  // Unlimited
    "api_rate_limit": 1000,
    "advanced_policies": true,
    "support_level": "priority"
  }
}
```

**Alternative: Per-User Pricing**

```json
{
  "pricing": {
    "type": "PerSeat",
    "price_per_user": 5.00,
    "minimum_seats": 10,
    "maximum_seats": 1000
  }
}
```

---

## Offer Listing Details

### Offer Summary

**Name**: SharePoint External User Manager

**Search Results Summary** (max 50 chars):
> Manage external users in SharePoint with ease

**Short Description** (max 100 chars):
> Streamline external collaboration with automated user management, policies, and comprehensive audit logs.

**Description** (max 5000 chars):

```markdown
# SharePoint External User Manager

Simplify and secure external collaboration in Microsoft SharePoint with our comprehensive SaaS solution.

## Key Features

### 🎯 Centralized User Management
- Invite and manage external users from a single dashboard
- Bulk invitation and permission management
- Automatic access expiration and renewal workflows
- Track guest user activity and last access times

### 🔒 Security & Compliance
- Comprehensive audit logging for all external access
- Automated compliance reporting for GDPR, SOC 2
- Custom collaboration policies per site or library
- Anomaly detection and security alerts

### 📊 Advanced Analytics
- Usage dashboards and insights
- Cost optimization recommendations
- External user engagement metrics
- Detailed audit trail with export capabilities

### ⚡ Automation & Policies
- Auto-revoke inactive users
- Scheduled access reviews
- Custom approval workflows
- Email notifications and reminders

### 🌐 Microsoft 365 Integration
- Native Azure AD authentication
- Works with any SharePoint site or library
- Seamless integration with Microsoft Teams
- Power Platform connectors available

## Why Choose Us?

✅ **Easy Setup**: Get started in minutes with our guided onboarding
✅ **Enterprise-Grade Security**: Built on Azure with SOC 2 compliance
✅ **Responsive Support**: Priority support for all paid plans
✅ **Continuous Innovation**: Regular feature updates and improvements

## Perfect For

- IT administrators managing external collaboration
- Security and compliance teams
- Project managers working with external partners
- Organizations with strict security requirements

## Get Started Today

Try our Free plan with up to 10 external users, or start a 14-day trial of Pro to unlock advanced features.
```

**Categories**:
- Collaboration
- Productivity
- IT & Management

**Industries**:
- Professional Services
- Financial Services
- Healthcare
- Government

---

## Marketing Assets

### Screenshots (Required: 5 minimum, PNG, 1280x720)

1. **Dashboard Overview** - Main dashboard showing external users summary
2. **User Management** - List of external users with permissions
3. **Invitation Flow** - Invite external user workflow
4. **Policies** - Collaboration policies configuration
5. **Audit Logs** - Comprehensive audit log viewer

### Videos (Optional but recommended)

1. **Product Demo** (2-3 minutes)
   - Overview of key features
   - Quick walkthrough of user invitation
   - Demo of policies and automation

2. **Customer Testimonial** (1-2 minutes)
   - Real customer success story
   - ROI and value delivered

### Logo Requirements

- **Small**: 48x48 PNG
- **Medium**: 90x90 PNG
- **Large**: 216x216 PNG
- **Wide**: 255x115 PNG
- **Hero**: 815x290 PNG

---

## Legal & Support

### Privacy Policy

**URL**: `https://spexternal.com/privacy`

**Required Sections**:
- Data collection and usage
- Data retention and deletion
- Third-party services
- Security measures
- User rights (GDPR)
- Cookie policy
- Contact information

### Terms of Use

**URL**: `https://spexternal.com/terms`

**Required Sections**:
- Service description
- Subscription and billing
- User responsibilities
- Intellectual property
- Limitation of liability
- Termination
- Dispute resolution

### Support Contact

**URL**: `https://spexternal.com/support`

**Support Options**:
- Email: support@spexternal.com
- Phone: +1 (555) 123-4567
- Support Portal: https://support.spexternal.com
- Knowledge Base: https://docs.spexternal.com

**SLA Commitments**:
- Free: Community support (best effort)
- Pro: Email support (24-hour response)
- Enterprise: Priority support (4-hour response) + phone support

---

## Publishing Process

### Step-by-Step Checklist

#### Phase 1: Preparation (2-3 weeks)

- [ ] Complete technical requirements
  - [ ] Implement landing page
  - [ ] Implement webhook endpoint
  - [ ] Integrate Fulfillment API
  - [ ] Test subscription lifecycle
  - [ ] Set up monitoring and logging

- [ ] Prepare marketing assets
  - [ ] Take 5+ screenshots
  - [ ] Record product demo video
  - [ ] Create logos in all required sizes
  - [ ] Write compelling offer description

- [ ] Legal documents
  - [ ] Publish privacy policy
  - [ ] Publish terms of use
  - [ ] Set up support channels
  - [ ] Prepare SLA documentation

#### Phase 2: Partner Center Setup (1 week)

- [ ] Create Partner Center account
- [ ] Complete tax profile
- [ ] Set up payout account
- [ ] Complete identity verification
- [ ] Sign marketplace publisher agreement

#### Phase 3: Offer Creation (1 week)

- [ ] Create new SaaS offer in Partner Center
- [ ] Configure offer setup
  - [ ] Landing page URL
  - [ ] Webhook URL
  - [ ] Azure AD tenant ID
  - [ ] Azure AD app ID
- [ ] Add plans and pricing
- [ ] Upload marketing assets
- [ ] Configure availability (markets/regions)
- [ ] Set up test drive (optional)

#### Phase 4: Technical Validation (1 week)

- [ ] Submit for technical validation
- [ ] Fix any validation errors
- [ ] Test purchase flow in preview
- [ ] Test webhook events
- [ ] Verify subscription management

#### Phase 5: Go Live (1-2 weeks)

- [ ] Submit for certification
- [ ] Address certification feedback
- [ ] Final review and approval
- [ ] **Go Live!** 🚀

**Total Timeline**: 6-8 weeks from start to marketplace publication

---

## Testing Strategy

### Preview Audience

Before going live, test with a preview audience:

```typescript
// Test subscription in preview mode
const testSubscription = {
  marketplace_subscription_id: 'test-sub-123',
  offer_id: 'spexternal-offer',
  plan_id: 'pro',
  quantity: 1,
  is_test: true
};

// Verify all functionality
await testLandingPageFlow(testSubscription);
await testWebhookEvents(testSubscription);
await testSubscriptionActivation(testSubscription);
await testPlanUpgrade(testSubscription);
await testCancellation(testSubscription);
```

### Test Scenarios

1. **Purchase Flow**
   - New subscription purchase
   - Subscription activation
   - Admin access granted

2. **Subscription Management**
   - Upgrade plan
   - Downgrade plan
   - Change quantity (if per-user pricing)
   - Renewal

3. **Cancellation & Suspension**
   - User-initiated cancellation
   - Payment failure suspension
   - Reinstatement after suspension
   - Grace period expiration

4. **Edge Cases**
   - Multiple purchases by same tenant
   - Rapid plan changes
   - Webhook delivery failures
   - Token expiration during activation

---

## Monitoring & Analytics

### Key Metrics to Track

```typescript
interface MarketplaceMetrics {
  // Funnel metrics
  listing_views: number;
  trial_starts: number;
  conversions: number;
  
  // Subscription metrics
  active_subscriptions: number;
  monthly_recurring_revenue: number;
  churn_rate: number;
  
  // Plan distribution
  subscriptions_by_plan: {
    free: number;
    pro: number;
    enterprise: number;
  };
  
  // Technical health
  webhook_success_rate: number;
  activation_success_rate: number;
  average_activation_time_seconds: number;
}
```

### Alerting

```typescript
// Critical alerts
const alerts = [
  {
    name: 'HighWebhookFailureRate',
    condition: 'webhook_failure_rate > 5%',
    severity: 'Critical',
    action: 'Page on-call engineer'
  },
  {
    name: 'SubscriptionActivationFailed',
    condition: 'activation_failure',
    severity: 'High',
    action: 'Alert support team'
  },
  {
    name: 'UnexpectedCancellationSpike',
    condition: 'cancellations > 10 in 1 hour',
    severity: 'Medium',
    action: 'Notify product team'
  }
];
```

---

## Revenue Share & Payout

### Microsoft Fees

| Transaction Type | Microsoft Fee | You Receive |
|------------------|---------------|-------------|
| Standard Offer | 20% | 80% |
| Private Offer | 3% | 97% |
| Metered Billing | 20% | 80% |

### Payout Schedule

- **Frequency**: Monthly
- **Delay**: 30 days after end of month
- **Minimum**: $50 USD
- **Methods**: Bank transfer, PayPal

### Example Revenue Calculation

```
Customer pays: $49/month
Microsoft fee (20%): $9.80
Your payout: $39.20

Annual revenue (1000 customers):
Customer payments: $588,000
Your revenue: $470,400
```

---

## Post-Launch Optimization

### Conversion Rate Optimization

1. **Optimize Listing**
   - A/B test screenshots
   - Refine description based on feedback
   - Add customer testimonials

2. **Improve Trial Experience**
   - Guided onboarding in-app
   - Proactive support during trial
   - Feature highlights and tips

3. **Reduce Churn**
   - Exit surveys for cancellations
   - Win-back campaigns
   - Feature adoption analysis

### Marketplace SEO

- Use relevant keywords in title and description
- Regularly update screenshots and videos
- Encourage customer reviews
- Respond to all reviews promptly
- Maintain high quality score

---

## Alternative: Azure Managed Application (Future)

For infrastructure-heavy deployments:

**Characteristics**:
- Deploy Azure resources to customer subscription
- Customer has full control
- Can include VMs, databases, networking
- More complex but higher flexibility

**When to Consider**:
- Customer requires data residency
- Need to deploy custom infrastructure
- Want to avoid service multi-tenancy

---

## Next Steps

### Immediate (MVP)

1. Implement landing page and webhook endpoints
2. Integrate Marketplace Fulfillment API
3. Create Partner Center account
4. Prepare marketing assets

### Short-term (Post-MVP)

1. Create offer listing in Partner Center
2. Submit for technical validation
3. Test with preview audience
4. Go live on AppSource

### Long-term (Growth)

1. Add metered billing for usage-based pricing
2. Create private offers for enterprise customers
3. Expand to Azure Marketplace
4. Consider Azure Managed Application offering

---

## Resources

- [Microsoft Commercial Marketplace Documentation](https://docs.microsoft.com/azure/marketplace/)
- [SaaS Fulfillment APIs](https://docs.microsoft.com/azure/marketplace/partner-center-portal/pc-saas-fulfillment-api-v2)
- [Marketplace Metering Service](https://docs.microsoft.com/azure/marketplace/partner-center-portal/marketplace-metering-service-apis)
- [Partner Center](https://partner.microsoft.com/dashboard)
- [Marketplace Publisher Guide](https://docs.microsoft.com/azure/marketplace/marketplace-publishers-guide)
