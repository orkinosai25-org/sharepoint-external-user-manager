# Billing Integration Guide

## Overview

This guide provides a comprehensive overview of the Stripe billing integration in the SharePoint External User Manager SaaS platform. The billing system is fully implemented and production-ready.

## Architecture

### Components

1. **BillingController** (`/api/billing`)
   - Handles checkout session creation
   - Processes Stripe webhooks
   - Provides plan information

2. **SubscriptionController** (`/api/subscription`)
   - Manages subscription lifecycle
   - Handles plan changes and cancellations
   - Returns subscription status

3. **StripeService**
   - Integrates with Stripe API
   - Manages checkout sessions
   - Verifies webhook signatures

4. **PlanConfiguration**
   - Defines available plans and features
   - Manages plan limits and pricing

5. **Database Entities**
   - `SubscriptionEntity` - Stores subscription data
   - `TenantEntity` - Links to subscriptions

## Subscription Plans

### Available Tiers

| Tier | Monthly | Annual | Users | Libraries | Features |
|------|---------|--------|-------|-----------|----------|
| **Starter** | $29 | $290 | 50 | 25 | Basic management, email |
| **Professional** | $99 | $990 | 250 | 100 | + Bulk ops, API, search |
| **Business** | $299 | $2,990 | 1,000 | 500 | + SSO, advanced reports |
| **Enterprise** | Custom | Custom | Unlimited | Unlimited | + SLA, dedicated support |

### Plan Features

Each plan includes different feature sets defined in `PlanConfiguration.cs`:

```csharp
- BasicManagement (All plans)
- LibraryDocumentAccess (All plans)
- EmailNotifications (All plans)
- AuditLogs (All plans)
- BulkOperations (Professional+)
- APIAccess (Professional+)
- GlobalSearch (Professional+)
- AdvancedReporting (Business+)
- SSO (Business+)
- PrioritySupport (Business+)
- CustomBranding (Enterprise only)
- SLA (Enterprise only)
- DedicatedSupport (Enterprise only)
```

## End-to-End Subscription Flow

### 1. User Initiates Subscription

**Frontend Request:**
```http
POST /api/billing/checkout-session
Authorization: Bearer {jwt_token}
Content-Type: application/json

{
  "planTier": "Professional",
  "isAnnual": false,
  "successUrl": "https://app.example.com/billing/success",
  "cancelUrl": "https://app.example.com/billing/cancel"
}
```

**Response:**
```json
{
  "sessionId": "cs_test_...",
  "checkoutUrl": "https://checkout.stripe.com/c/pay/cs_test_..."
}
```

### 2. User Redirected to Stripe Checkout

The frontend redirects the user to the `checkoutUrl` where they:
- Enter payment information
- Complete the purchase
- Get redirected back to `successUrl`

### 3. Stripe Webhook Notification

Stripe sends a `checkout.session.completed` event to:
```
POST /api/billing/webhook
```

The webhook handler:
1. Verifies the webhook signature
2. Extracts tenant and plan information from metadata
3. Creates/updates subscription in database
4. Logs the action in audit trail

### 4. Subscription Activated

The subscription is now active and the tenant has access to plan features:

```http
GET /api/subscription/me
Authorization: Bearer {jwt_token}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "tier": "Professional",
    "status": "Active",
    "isActive": true,
    "startDate": "2024-01-15T10:30:00Z",
    "limits": {
      "maxUsers": 250,
      "maxClients": 100,
      "maxLibrariesPerClient": 25
    },
    "features": {
      "bulkOperations": true,
      "apiAccess": true,
      "globalSearch": true
    }
  }
}
```

## Webhook Events Handled

### checkout.session.completed

**Purpose:** Activates new subscription when payment succeeds

**Processing:**
1. Extract `tenant_id` and `plan_tier` from session metadata
2. Find or create tenant in database
3. Create new subscription record with Stripe IDs
4. Set status to "Active"
5. Log action in audit trail

### customer.subscription.created / updated

**Purpose:** Syncs subscription status changes from Stripe

**Processing:**
1. Find subscription by `stripeSubscriptionId`
2. Map Stripe status to internal status:
   - `active` → `Active`
   - `trialing` → `Trial`
   - `canceled` → `Cancelled`
   - `past_due` → `Suspended`
3. Update database record

### customer.subscription.deleted

**Purpose:** Handles subscription cancellation

**Processing:**
1. Find subscription by `stripeSubscriptionId`
2. Set status to "Cancelled"
3. Set `endDate` to now
4. Set `gracePeriodEnd` to now + 7 days
5. Log cancellation in audit trail

### invoice.paid

**Purpose:** Reactivates suspended subscriptions after successful payment

**Processing:**
1. Extract subscription ID from invoice
2. Find subscription in database
3. Set status to "Active"
4. Log payment success

### invoice.payment_failed

**Purpose:** Suspends access when payment fails

**Processing:**
1. Extract subscription ID from invoice
2. Find subscription in database
3. Set status to "Suspended"
4. Log payment failure

## Plan Change Workflow

### Upgrade/Downgrade for Paid Plans

For subscriptions with a Stripe ID, users must go through checkout:

```http
POST /api/subscription/change-plan
Authorization: Bearer {jwt_token}
Content-Type: application/json

{
  "newPlanTier": "Business"
}
```

**Response (if Stripe subscription exists):**
```json
{
  "success": false,
  "error": {
    "code": "USE_CHECKOUT",
    "message": "Please use the checkout process to change your plan..."
  }
}
```

### Direct Change for Trial/Free Plans

For trial or free plans without Stripe integration:

**Response:**
```json
{
  "success": true,
  "data": {
    "message": "Successfully changed plan to Business",
    "newTier": "Business"
  }
}
```

## Cancellation Workflow

### With Stripe Subscription

```http
POST /api/subscription/cancel
Authorization: Bearer {jwt_token}
```

**Processing:**
1. Call Stripe API to cancel subscription
2. Update local database status to "Cancelled"
3. Set 7-day grace period
4. Log cancellation

**Response:**
```json
{
  "success": true,
  "data": {
    "message": "Subscription cancelled successfully",
    "gracePeriodEnd": "2024-01-22T10:30:00Z",
    "gracePeriodDays": 7
  }
}
```

### Without Stripe (Trial/Free)

For trial subscriptions, only the local database is updated (no Stripe API call).

## Plan Enforcement

### RequiresPlanAttribute

Controllers use the `[RequiresPlan]` attribute to enforce plan access:

```csharp
[RequiresPlan(SubscriptionTier.Professional)]
[HttpPost("bulk-invite")]
public async Task<IActionResult> BulkInvite([FromBody] BulkInviteRequest request)
{
    // Only accessible to Professional+ plans
}
```

**Enforcement Logic:**
1. Check if subscription is active or in trial
2. Verify plan tier meets or exceeds required tier
3. Check trial expiry date if applicable
4. Return 403 with "UPGRADE_REQUIRED" if insufficient

### PlanEnforcementService

Programmatic enforcement for resource limits:

```csharp
// Check if tenant can create a new client space
await _planEnforcementService.CanCreateClientSpaceAsync(tenantId);

// Throws UnauthorizedAccessException if limit exceeded
```

## Security Considerations

### Webhook Signature Verification

All webhooks are verified using Stripe's signature mechanism:

```csharp
var stripeEvent = _stripeService.VerifyWebhookSignature(json, stripeSignature);
if (stripeEvent == null)
{
    return BadRequest(new { error = "Invalid signature" });
}
```

### Sensitive Data Protection

- Stripe API keys stored in environment variables
- Webhook secrets never committed to source control
- Customer payment info never touches our servers (PCI compliance)

### Audit Trail

All billing actions are logged:
- Checkout session creation
- Subscription activation
- Plan changes
- Cancellations
- Payment failures

## Configuration

### Required Environment Variables

```bash
# Stripe API Keys
Stripe__SecretKey=sk_test_... (or sk_live_... for production)
Stripe__PublishableKey=pk_test_... (or pk_live_... for production)
Stripe__WebhookSecret=whsec_...

# Price IDs for each plan and billing cycle
Stripe__Price__Starter__Monthly=price_...
Stripe__Price__Starter__Annual=price_...
Stripe__Price__Professional__Monthly=price_...
Stripe__Price__Professional__Annual=price_...
Stripe__Price__Business__Monthly=price_...
Stripe__Price__Business__Annual=price_...
```

### Stripe Dashboard Setup

1. **Create Products & Prices**
   - Navigate to Products in Stripe Dashboard
   - Create products for Starter, Professional, Business
   - Add monthly and annual recurring prices
   - Copy price IDs to configuration

2. **Configure Webhook Endpoint**
   - Navigate to Developers → Webhooks
   - Add endpoint: `https://your-api.azurewebsites.net/api/billing/webhook`
   - Select events:
     - checkout.session.completed
     - customer.subscription.created
     - customer.subscription.updated
     - customer.subscription.deleted
     - invoice.paid
     - invoice.payment_failed
   - Copy webhook signing secret

3. **Test with Test Mode**
   - Use test API keys for development
   - Use test cards from Stripe documentation
   - Verify webhooks using Stripe CLI

## Testing

### Unit Tests

**BillingControllerTests.cs** - 10 tests:
- ✅ GetPlans returns available plans
- ✅ GetPlans with includeEnterprise flag
- ✅ CreateCheckoutSession with valid request
- ✅ CreateCheckoutSession rejects Enterprise plans
- ✅ CreateCheckoutSession requires tenant ID
- ✅ GetSubscriptionStatus returns active subscription
- ✅ GetSubscriptionStatus returns default for no subscription
- ✅ StripeWebhook requires valid signature
- ✅ StripeWebhook processes checkout.session.completed
- ✅ StripeWebhook handles invalid signatures

**SubscriptionControllerTests.cs** - 10 tests:
- ✅ GetMySubscription returns active subscription
- ✅ GetMySubscription returns default for no subscription
- ✅ ChangePlan upgrades trial subscriptions
- ✅ ChangePlan rejects same tier
- ✅ ChangePlan rejects Enterprise tier
- ✅ ChangePlan requires checkout for paid plans
- ✅ CancelSubscription cancels active subscription
- ✅ CancelSubscription handles Stripe API
- ✅ CancelSubscription works for trial subscriptions
- ✅ CancelSubscription validates active subscription

### Running Tests

```bash
cd src/api-dotnet/WebApi/SharePointExternalUserManager.Api.Tests
dotnet test --filter "FullyQualifiedName~BillingController"
dotnet test --filter "FullyQualifiedName~SubscriptionController"
```

## Monitoring & Troubleshooting

### Common Issues

**"Price ID not configured" error**
- Verify all price IDs are set in configuration
- Ensure key names match exactly (case-sensitive)

**Webhook signature verification failed**
- Check webhook secret is correct
- Verify using signing secret, not endpoint ID
- Ensure using correct environment (test vs live)

**Payment succeeded but subscription not activated**
- Check webhook logs in Stripe Dashboard
- Verify webhook endpoint is accessible
- Check application logs for errors

### Logging

All billing operations log to Application Insights:
- Info: Normal operations (checkout created, subscription activated)
- Warning: Validation failures, missing metadata
- Error: Stripe API errors, webhook processing failures

Each log includes a correlation ID for tracing.

## Production Readiness Checklist

- [x] Stripe API integration complete
- [x] Webhook handling implemented
- [x] Subscription lifecycle management
- [x] Plan enforcement working
- [x] Comprehensive unit tests (20 tests passing)
- [x] Audit logging implemented
- [x] Security verification (webhook signatures)
- [x] Grace period handling
- [x] Error handling and logging
- [ ] Production Stripe account configured
- [ ] Webhook endpoint registered in Stripe
- [ ] Environment variables configured in Azure
- [ ] Monitoring alerts set up
- [ ] Customer support documentation

## Next Steps

1. **Production Stripe Setup**
   - Switch from test to live API keys
   - Configure production webhook endpoint
   - Set up real pricing

2. **Customer Portal**
   - Add self-service billing management
   - Invoice history
   - Payment method updates

3. **Usage-Based Billing** (Future)
   - Track API calls per tenant
   - Implement overage charges
   - Usage dashboards

4. **Proration** (Future)
   - Handle mid-cycle upgrades/downgrades
   - Credit calculations
   - Refund processing

## Support

For issues or questions:
- Check logs in Application Insights
- Review Stripe Dashboard for webhook status
- Contact support with correlation ID from error

## References

- [Stripe API Documentation](https://stripe.com/docs/api)
- [Stripe Webhook Testing](https://stripe.com/docs/webhooks/test)
- [Stripe Test Cards](https://stripe.com/docs/testing)
- [Internal Configuration Guide](../src/api-dotnet/WebApi/SharePointExternalUserManager.Api/STRIPE_CONFIGURATION.md)
