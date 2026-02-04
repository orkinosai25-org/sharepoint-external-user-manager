# Azure Marketplace Integration Plan

## Overview

This document outlines the strategy and technical implementation for publishing the SharePoint External User Manager to Azure Marketplace and Microsoft AppSource. The plan includes offer configuration, fulfillment integration, and webhook implementation for SaaS subscriptions.

## Marketplace Publishing Strategy

### Phase 1: Initial Launch (MVP with Custom Billing)
- **Timeline:** Months 1-3
- **Billing:** Custom billing portal (Stripe/PayPal)
- **Listing:** Basic landing page with trial sign-up
- **Goal:** Validate product-market fit with early adopters

### Phase 2: Azure Marketplace Preparation
- **Timeline:** Months 4-6
- **Certification:** Azure Marketplace certification process
- **Integration:** Implement SaaS Fulfillment API v2
- **Testing:** Complete marketplace integration testing

### Phase 3: Full Marketplace Launch
- **Timeline:** Month 6+
- **Billing:** Azure Marketplace transactable offer
- **Listing:** Professional AppSource listing
- **Co-sell:** Microsoft co-sell ready status

## Marketplace Offer Types

### Recommended: SaaS Transact Offer

**Advantages:**
- Microsoft handles billing and invoicing
- Customers can purchase through Azure portal
- Marketplace fees (20%) include payment processing
- Eligible for Azure consumption credits
- Co-sell opportunities with Microsoft

**Requirements:**
- Implement SaaS Fulfillment API v2
- Handle subscription lifecycle webhooks
- Provide customer landing page
- Support Azure AD authentication

### Alternative: SaaS Contact Me Offer

**For Initial Launch:**
- No fulfillment API required
- Leads sent to email/CRM
- Manual sales process
- Lower technical barrier
- Not transactable through Azure

## SaaS Fulfillment API V2 Integration

### Architecture Overview

```
┌──────────────────────────────────────────────────────────────────┐
│                    Azure Marketplace                             │
│  - Customer purchases subscription                               │
│  - Redirects to landing page with token                         │
└───────────────────────────┬──────────────────────────────────────┘
                            │
                            │ (1) Redirect with marketplace token
                            │
┌───────────────────────────▼──────────────────────────────────────┐
│              Landing Page (app.spexternal.com/activate)          │
│  - Resolve token to subscription details                        │
│  - Authenticate customer (Azure AD)                             │
│  - Complete tenant provisioning                                 │
└───────────────────────────┬──────────────────────────────────────┘
                            │
                            │ (2) Activate subscription API call
                            │
┌───────────────────────────▼──────────────────────────────────────┐
│          Microsoft SaaS Fulfillment API                          │
│  - Resolve subscription                                          │
│  - Activate subscription                                         │
│  - Receive webhooks for changes                                 │
└───────────────────────────┬──────────────────────────────────────┘
                            │
                            │ (3) Webhooks for lifecycle events
                            │
┌───────────────────────────▼──────────────────────────────────────┐
│         Our Backend (Azure Functions)                            │
│  /api/marketplace/webhook                                        │
│  - Handle subscription changes                                   │
│  - Update tenant status                                          │
│  - Provision/deprovision resources                              │
└──────────────────────────────────────────────────────────────────┘
```

### Required API Endpoints

#### 1. Resolve Token

**Purpose:** Convert marketplace token to subscription details

```csharp
[FunctionName("ResolveMarketplaceToken")]
public async Task<IActionResult> ResolveToken(
    [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
{
    var token = req.Query["token"];
    
    var marketplaceClient = new MarketplaceClient(_configuration);
    var subscription = await marketplaceClient.ResolveAsync(token);
    
    return new OkObjectResult(new
    {
        subscriptionId = subscription.Id,
        subscriptionName = subscription.Name,
        offerId = subscription.OfferId,
        planId = subscription.PlanId,
        purchaser = subscription.Purchaser,
        beneficiary = subscription.Beneficiary,
        quantity = subscription.Quantity,
        term = subscription.Term
    });
}
```

**Microsoft API:**
```http
POST https://marketplaceapi.microsoft.com/api/saas/subscriptions/resolve?api-version=2018-08-31
Authorization: Bearer <Azure_AD_token>
Content-Type: application/json
x-ms-marketplace-token: <token_from_landing_page>
```

#### 2. Activate Subscription

**Purpose:** Confirm subscription activation after provisioning

```csharp
[FunctionName("ActivateMarketplaceSubscription")]
public async Task<IActionResult> ActivateSubscription(
    [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
{
    var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
    var data = JsonConvert.DeserializeObject<ActivationRequest>(requestBody);
    
    // 1. Provision tenant resources
    var tenantId = await ProvisionTenantAsync(data);
    
    // 2. Activate in marketplace
    var marketplaceClient = new MarketplaceClient(_configuration);
    await marketplaceClient.ActivateSubscriptionAsync(
        data.SubscriptionId,
        new ActivationRequest
        {
            PlanId = data.PlanId,
            Quantity = data.Quantity
        }
    );
    
    // 3. Update tenant record
    await UpdateTenantSubscriptionAsync(tenantId, data.SubscriptionId);
    
    return new OkObjectResult(new { tenantId, status = "Active" });
}
```

**Microsoft API:**
```http
POST https://marketplaceapi.microsoft.com/api/saas/subscriptions/{subscriptionId}/activate?api-version=2018-08-31
Authorization: Bearer <Azure_AD_token>
Content-Type: application/json

{
  "planId": "pro-monthly",
  "quantity": 1
}
```

#### 3. Webhook Handler

**Purpose:** Receive and process subscription lifecycle events

```csharp
[FunctionName("MarketplaceWebhook")]
public async Task<IActionResult> HandleWebhook(
    [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
{
    // Verify webhook signature
    if (!await VerifyWebhookSignatureAsync(req))
        return new UnauthorizedResult();
    
    var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
    var webhook = JsonConvert.DeserializeObject<MarketplaceWebhook>(requestBody);
    
    switch (webhook.Action)
    {
        case "Unsubscribe":
            await HandleUnsubscribeAsync(webhook);
            break;
            
        case "ChangePlan":
            await HandlePlanChangeAsync(webhook);
            break;
            
        case "ChangeQuantity":
            await HandleQuantityChangeAsync(webhook);
            break;
            
        case "Suspend":
            await HandleSuspendAsync(webhook);
            break;
            
        case "Reinstate":
            await HandleReinstateAsync(webhook);
            break;
            
        case "Renew":
            await HandleRenewAsync(webhook);
            break;
            
        default:
            _logger.LogWarning($"Unknown webhook action: {webhook.Action}");
            break;
    }
    
    return new OkResult();
}
```

**Webhook Event Types:**

| Event | Action Required |
|-------|----------------|
| `Unsubscribe` | Revoke access, schedule data deletion |
| `ChangePlan` | Update tier, enable/disable features |
| `ChangeQuantity` | Update license limits |
| `Suspend` | Disable access, notify admin |
| `Reinstate` | Re-enable access after suspension |
| `Renew` | Update subscription end date |

### Webhook Security

```csharp
public async Task<bool> VerifyWebhookSignatureAsync(HttpRequest req)
{
    // Extract signature from header
    var signature = req.Headers["x-ms-marketplace-signature"].FirstOrDefault();
    
    if (string.IsNullOrEmpty(signature))
        return false;
    
    // Read request body
    var body = await new StreamReader(req.Body).ReadToEndAsync();
    req.Body.Position = 0; // Reset stream
    
    // Compute HMAC signature using shared secret
    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_webhookSecret));
    var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
    var computedSignature = Convert.ToBase64String(computedHash);
    
    return signature == computedSignature;
}
```

## Subscription Lifecycle Handling

### 1. New Subscription (Purchase)

```csharp
public async Task HandleNewSubscriptionAsync(MarketplaceSubscription subscription)
{
    // 1. Extract customer info
    var customerId = subscription.Beneficiary.EmailId;
    var tenantId = subscription.Beneficiary.TenantId;
    
    // 2. Check if tenant already exists
    var existingTenant = await GetTenantByEntraIdAsync(tenantId);
    
    if (existingTenant != null)
    {
        // Upgrade existing trial/free tenant
        await UpgradeTenantAsync(existingTenant.TenantId, subscription);
    }
    else
    {
        // Provision new tenant
        await ProvisionNewTenantAsync(subscription);
    }
    
    // 3. Send welcome email
    await SendWelcomeEmailAsync(customerId, subscription.PlanId);
    
    // 4. Log event
    await LogAuditEventAsync("SubscriptionCreated", subscription);
}
```

### 2. Plan Change (Upgrade/Downgrade)

```csharp
public async Task HandlePlanChangeAsync(MarketplaceWebhook webhook)
{
    var tenant = await GetTenantBySubscriptionIdAsync(webhook.SubscriptionId);
    
    var oldPlan = tenant.SubscriptionTier;
    var newPlan = webhook.PlanId;
    
    // Update tenant tier
    await UpdateTenantTierAsync(tenant.TenantId, newPlan);
    
    // Apply tier limits
    await ApplyTierLimitsAsync(tenant.TenantId, newPlan);
    
    // Handle downgrade restrictions
    if (IsDowngrade(oldPlan, newPlan))
    {
        await HandleDowngradeRestrictionsAsync(tenant.TenantId, newPlan);
    }
    
    // Send notification
    await SendPlanChangeNotificationAsync(tenant, oldPlan, newPlan);
    
    // Acknowledge to marketplace
    await marketplace.AcknowledgeAsync(webhook.SubscriptionId, webhook.OperationId);
}
```

### 3. Suspension

```csharp
public async Task HandleSuspendAsync(MarketplaceWebhook webhook)
{
    var tenant = await GetTenantBySubscriptionIdAsync(webhook.SubscriptionId);
    
    // Update status
    tenant.SubscriptionStatus = SubscriptionStatus.Suspended;
    await UpdateTenantAsync(tenant);
    
    // Disable access but keep data
    await DisableTenantAccessAsync(tenant.TenantId);
    
    // Send notification
    await SendSuspensionNotificationAsync(tenant);
    
    // Log event
    await LogAuditEventAsync("SubscriptionSuspended", webhook);
}
```

### 4. Cancellation

```csharp
public async Task HandleUnsubscribeAsync(MarketplaceWebhook webhook)
{
    var tenant = await GetTenantBySubscriptionIdAsync(webhook.SubscriptionId);
    
    // 1. Start grace period (7 days)
    tenant.SubscriptionStatus = SubscriptionStatus.Cancelled;
    tenant.SubscriptionEndDate = DateTime.UtcNow.AddDays(7);
    await UpdateTenantAsync(tenant);
    
    // 2. Restrict to read-only
    await SetTenantReadOnlyAsync(tenant.TenantId);
    
    // 3. Send cancellation confirmation
    await SendCancellationEmailAsync(tenant);
    
    // 4. Schedule data deletion (30 days)
    await ScheduleDataDeletionAsync(tenant.TenantId, days: 30);
    
    // 5. Offer feedback survey
    await SendFeedbackSurveyAsync(tenant);
}
```

## Landing Page Implementation

**URL:** `https://app.spexternal.com/activate?token={marketplace_token}`

### Landing Page Flow

```html
<!DOCTYPE html>
<html>
<head>
    <title>Activate Your Subscription - SharePoint External User Manager</title>
</head>
<body>
    <div id="activation-flow">
        <!-- Step 1: Resolving subscription -->
        <div id="step-resolve" class="active">
            <h1>Activating Your Subscription...</h1>
            <p>Please wait while we set up your account.</p>
            <div class="spinner"></div>
        </div>
        
        <!-- Step 2: Confirm details -->
        <div id="step-confirm" class="hidden">
            <h1>Confirm Your Subscription</h1>
            <dl>
                <dt>Plan:</dt>
                <dd id="plan-name">Pro Monthly</dd>
                
                <dt>Organization:</dt>
                <dd id="org-name">Contoso Corporation</dd>
                
                <dt>Administrator:</dt>
                <dd id="admin-email">admin@contoso.com</dd>
            </dl>
            <button onclick="activateSubscription()">Activate</button>
        </div>
        
        <!-- Step 3: Provisioning -->
        <div id="step-provision" class="hidden">
            <h1>Setting Up Your Account</h1>
            <ul class="progress-list">
                <li id="step-database">Creating database...</li>
                <li id="step-storage">Initializing storage...</li>
                <li id="step-config">Configuring settings...</li>
                <li id="step-complete">Finalizing setup...</li>
            </ul>
        </div>
        
        <!-- Step 4: Success -->
        <div id="step-success" class="hidden">
            <h1>🎉 Your Account is Ready!</h1>
            <p>Start managing external users in SharePoint.</p>
            <a href="/dashboard" class="btn-primary">Go to Dashboard</a>
        </div>
        
        <!-- Error state -->
        <div id="step-error" class="hidden">
            <h1>⚠️ Activation Failed</h1>
            <p id="error-message"></p>
            <button onclick="retryActivation()">Try Again</button>
            <a href="/support">Contact Support</a>
        </div>
    </div>
    
    <script src="/js/marketplace-activation.js"></script>
</body>
</html>
```

### Activation JavaScript

```javascript
async function activateLandingPage() {
    const urlParams = new URLSearchParams(window.location.search);
    const token = urlParams.get('token');
    
    if (!token) {
        showError('Missing marketplace token');
        return;
    }
    
    try {
        // Step 1: Resolve token
        showStep('resolve');
        const subscription = await resolveToken(token);
        
        // Step 2: Show confirmation
        populateConfirmation(subscription);
        showStep('confirm');
        
    } catch (error) {
        showError(error.message);
    }
}

async function activateSubscription() {
    try {
        // Step 3: Provision tenant
        showStep('provision');
        
        const result = await fetch('/api/marketplace/activate', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${getAccessToken()}`
            },
            body: JSON.stringify({
                token: getMarketplaceToken(),
                tenantId: getTenantId()
            })
        });
        
        // Monitor provisioning progress
        const data = await result.json();
        await pollProvisioningStatus(data.provisioningId);
        
        // Step 4: Success
        showStep('success');
        
    } catch (error) {
        showError(error.message);
    }
}
```

## Marketplace Offer Configuration

### Offer Details

```json
{
  "offerId": "sharepoint-external-user-manager",
  "offerType": "SaaS",
  "displayName": "SharePoint External User Manager",
  "publisherId": "your-publisher-id",
  "description": "Streamline external collaboration in SharePoint with automated user management, compliance policies, and comprehensive audit trails.",
  "categories": [
    "Collaboration",
    "Productivity",
    "IT & Management Tools"
  ],
  "industries": [
    "Financial Services",
    "Healthcare",
    "Professional Services"
  ],
  "privacyPolicyLink": "https://spexternal.com/privacy",
  "termsOfUseLink": "https://spexternal.com/terms",
  "supportLink": "https://spexternal.com/support",
  "engineeringContact": "engineering@spexternal.com",
  "supportContact": "support@spexternal.com"
}
```

### Plans & Pricing

```json
{
  "plans": [
    {
      "planId": "free",
      "displayName": "Free",
      "description": "Get started with basic external user management",
      "price": {
        "currencyCode": "USD",
        "amount": 0,
        "billingPeriod": "Monthly"
      },
      "features": [
        "Up to 5 external users",
        "Up to 3 libraries",
        "Basic audit logging",
        "Email support"
      ]
    },
    {
      "planId": "pro-monthly",
      "displayName": "Pro (Monthly)",
      "description": "Advanced features for growing teams",
      "price": {
        "currencyCode": "USD",
        "amount": 49.99,
        "billingPeriod": "Monthly"
      },
      "features": [
        "Up to 50 external users",
        "Up to 25 libraries",
        "Advanced audit logging",
        "Custom policies",
        "Priority support"
      ]
    },
    {
      "planId": "pro-annual",
      "displayName": "Pro (Annual)",
      "description": "Save 20% with annual billing",
      "price": {
        "currencyCode": "USD",
        "amount": 479.99,
        "billingPeriod": "Annual"
      },
      "features": [
        "All Pro features",
        "20% discount",
        "Dedicated onboarding"
      ]
    },
    {
      "planId": "enterprise",
      "displayName": "Enterprise",
      "description": "Unlimited scale for large organizations",
      "price": {
        "currencyCode": "USD",
        "amount": 199.99,
        "billingPeriod": "Monthly"
      },
      "features": [
        "Unlimited external users",
        "Unlimited libraries",
        "Premium audit & compliance",
        "Custom integrations",
        "Dedicated support",
        "SLA guarantee"
      ]
    }
  ]
}
```

### Technical Configuration

```json
{
  "technicalConfiguration": {
    "landingPageUrl": "https://app.spexternal.com/activate",
    "connectionWebhookUrl": "https://api.spexternal.com/api/marketplace/webhook",
    "azureActiveDirectoryTenantId": "<your_entra_id_tenant>",
    "azureActiveDirectoryApplicationId": "<your_app_id>",
    "enableSingleSignOn": true,
    "enableMeteredBilling": false
  }
}
```

## Co-Sell Readiness

### Requirements for Microsoft Co-Sell

1. **Revenue Threshold:** $1M+ in Azure consumption or $300K+ in Azure Marketplace sales
2. **Reference Customers:** 3+ customer references
3. **Landing Page:** Professional marketplace listing
4. **Deployment Guide:** Complete setup documentation
5. **Support:** 24/7 support or business hours with SLA

### Benefits of Co-Sell Ready Status

- Microsoft field sellers can recommend solution
- Eligible for co-selling incentives
- Priority in marketplace search results
- Co-marketing opportunities
- Access to Microsoft's customer base

## Testing & Validation

### Marketplace Integration Testing

**Test Scenarios:**
1. ✅ New subscription purchase
2. ✅ Upgrade from Free to Pro
3. ✅ Downgrade from Pro to Free
4. ✅ Subscription cancellation
5. ✅ Subscription suspension
6. ✅ Subscription reinstatement
7. ✅ Quantity change (if applicable)
8. ✅ Webhook failure handling

**Test Environment:**
- Use marketplace test environment
- Create test subscriptions
- Verify webhook delivery
- Test landing page flow

### Certification Checklist

- [ ] All API endpoints implemented
- [ ] Webhook handler tested
- [ ] Landing page functional
- [ ] Azure AD authentication working
- [ ] Subscription lifecycle handled
- [ ] Error handling implemented
- [ ] Logging and monitoring configured
- [ ] Security review completed
- [ ] Privacy policy published
- [ ] Terms of use published
- [ ] Support contact verified

## Monitoring & Analytics

### Key Metrics to Track

**Marketplace Performance:**
- Page views on listing
- Trial activations
- Conversion rate (trial → paid)
- Plan distribution
- Churn rate

**Webhook Reliability:**
- Webhook delivery success rate
- Processing latency
- Retry attempts
- Failed webhooks

**Customer Health:**
- Active subscriptions
- Usage by tier
- Feature adoption
- Support ticket volume

## References

- [Azure Marketplace Documentation](https://learn.microsoft.com/marketplace/)
- [SaaS Fulfillment API v2](https://learn.microsoft.com/azure/marketplace/partner-center-portal/pc-saas-fulfillment-api-v2)
- [Marketplace Offer Publishing](https://learn.microsoft.com/azure/marketplace/create-new-saas-offer)
- [Co-sell Requirements](https://learn.microsoft.com/partner-center/co-sell-overview)
