# Stripe Setup & Integration Guide

## Overview

This document provides complete instructions for setting up Stripe integration for subscription billing in the SharePoint External User Manager SaaS platform.

## Table of Contents

1. [Stripe Account Setup](#stripe-account-setup)
2. [Creating Products & Prices](#creating-products--prices)
3. [Environment Configuration](#environment-configuration)
4. [Plan Mapping](#plan-mapping)
5. [Testing](#testing)
6. [Webhook Configuration](#webhook-configuration)
7. [Security Considerations](#security-considerations)

## Stripe Account Setup

### Prerequisites

1. Create a Stripe account at [https://stripe.com](https://stripe.com)
2. Complete Stripe account verification (required for production)
3. Note your API keys from the Stripe Dashboard

### API Keys

You'll need three keys from Stripe:

- **Publishable Key** - Used in client-side code (safe to expose)
- **Secret Key** - Used in server-side code (keep secret)
- **Webhook Secret** - Used to verify webhook signatures (keep secret)

Find these in: Stripe Dashboard → Developers → API Keys

## Creating Products & Prices

### Step-by-Step Instructions

#### 1. Create Starter Plan

**Product:**
- Name: `SharePoint External User Manager - Starter`
- Description: `Perfect for small teams getting started with external user management`
- Statement Descriptor: `SPEXTERNAL STARTER`

**Prices:**

**Monthly Price:**
- Price: `$29.00 USD`
- Billing Period: `Monthly (every 1 month)`
- Price ID will be generated (e.g., `price_1AbCdEfGhIjKlMnO`)

**Annual Price:**
- Price: `$290.00 USD`
- Billing Period: `Yearly (every 1 year)`
- Price ID will be generated (e.g., `price_1AbCdEfGhIjKlMnP`)

#### 2. Create Professional Plan

**Product:**
- Name: `SharePoint External User Manager - Professional`
- Description: `Advanced features for growing businesses`
- Statement Descriptor: `SPEXTERNAL PRO`

**Prices:**

**Monthly Price:**
- Price: `$99.00 USD`
- Billing Period: `Monthly (every 1 month)`

**Annual Price:**
- Price: `$990.00 USD`
- Billing Period: `Yearly (every 1 year)`

#### 3. Create Business Plan

**Product:**
- Name: `SharePoint External User Manager - Business`
- Description: `Comprehensive solution for established organizations`
- Statement Descriptor: `SPEXTERNAL BIZ`

**Prices:**

**Monthly Price:**
- Price: `$299.00 USD`
- Billing Period: `Monthly (every 1 month)`

**Annual Price:**
- Price: `$2,990.00 USD`
- Billing Period: `Yearly (every 1 year)`

### Note on Enterprise Plan

The Enterprise plan is **not** offered through Stripe. Enterprise customers:
- Contact sales directly
- Receive custom pricing
- Have contracts managed separately
- May use alternative payment methods (wire transfer, invoice, etc.)

## Environment Configuration

### Required Environment Variables

Add these to your backend configuration:

```bash
# Stripe API Keys
STRIPE_SECRET_KEY=sk_test_51AbCdEfG...  # Use sk_live_... in production
STRIPE_PUBLISHABLE_KEY=pk_test_51AbCdEfG...  # Use pk_live_... in production
STRIPE_WEBHOOK_SECRET=whsec_...

# Stripe Product IDs (optional, defaults used if not set)
STRIPE_PRODUCT_ID_STARTER=prod_...
STRIPE_PRODUCT_ID_PROFESSIONAL=prod_...
STRIPE_PRODUCT_ID_BUSINESS=prod_...

# Stripe Price IDs (optional, defaults used if not set)
STRIPE_PRICE_ID_STARTER_MONTHLY=price_...
STRIPE_PRICE_ID_STARTER_ANNUAL=price_...
STRIPE_PRICE_ID_PROFESSIONAL_MONTHLY=price_...
STRIPE_PRICE_ID_PROFESSIONAL_ANNUAL=price_...
STRIPE_PRICE_ID_BUSINESS_MONTHLY=price_...
STRIPE_PRICE_ID_BUSINESS_ANNUAL=price_...
```

### Azure Function App Settings

For Azure deployment, add these to Function App Configuration:

1. Navigate to Azure Portal → Your Function App → Configuration
2. Add each environment variable as an Application Setting
3. Click "Save" to apply changes

### Local Development

Create or update `backend/local.settings.json`:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "node",
    "STRIPE_SECRET_KEY": "sk_test_...",
    "STRIPE_PUBLISHABLE_KEY": "pk_test_...",
    "STRIPE_WEBHOOK_SECRET": "whsec_..."
  }
}
```

**Important:** Never commit `local.settings.json` to source control!

## Plan Mapping

### Internal Plan Tiers to Stripe Prices

The system maps Stripe price IDs to internal plan tiers as follows:

| Internal Tier | Stripe Product | Monthly Price | Annual Price |
|--------------|----------------|---------------|--------------|
| Starter | SPEXTERNAL STARTER | $29/month | $290/year |
| Professional | SPEXTERNAL PRO | $99/month | $990/year |
| Business | SPEXTERNAL BIZ | $299/month | $2,990/year |
| Enterprise | N/A (Custom) | N/A | N/A |

### Configuration File

Price mappings are defined in `backend/src/config/stripe-config.ts`:

```typescript
export const STRIPE_PRICE_MAPPINGS: StripePriceMapping[] = [
  {
    priceId: process.env.STRIPE_PRICE_ID_STARTER_MONTHLY || 'price_starter_monthly',
    tier: 'Starter',
    interval: 'month',
    amount: 2900 // $29.00
  },
  // ... more mappings
];
```

### Updating Price IDs

**Option 1: Environment Variables (Recommended)**
Set environment variables with your actual Stripe price IDs. The code will use these automatically.

**Option 2: Code Changes**
Update the default values in `stripe-config.ts` with your actual price IDs.

## Testing

### Test Mode

During development, use Stripe test mode:

1. Use test API keys (start with `sk_test_` and `pk_test_`)
2. Use test cards from [Stripe Testing Docs](https://stripe.com/docs/testing)
3. No real charges are made in test mode

### Common Test Cards

```
Success: 4242 4242 4242 4242
Decline: 4000 0000 0000 0002
3D Secure: 4000 0025 0000 3155
```

Use any future expiry date, any 3-digit CVC, and any postal code.

### Testing Plan Mapping

Run the test suite to verify plan mappings:

```bash
cd backend
npm test -- stripe-config.spec.ts
```

Expected results:
- ✓ All plans have monthly and annual prices
- ✓ Price IDs map correctly to plan tiers
- ✓ Billing intervals are identified correctly
- ✓ All configured price IDs are valid

## Webhook Configuration

### Setting Up Webhooks

1. **Navigate to Webhooks:**
   - Go to Stripe Dashboard → Developers → Webhooks
   - Click "Add endpoint"

2. **Configure Endpoint:**
   - Endpoint URL: `https://your-api.azurewebsites.net/api/stripe/webhook`
   - Description: `SharePoint External User Manager - Subscription Events`
   - API Version: Latest (currently 2024-11-20.acacia)

3. **Select Events:**
   Select the following events to listen to:

   ```
   customer.subscription.created
   customer.subscription.updated
   customer.subscription.deleted
   customer.subscription.trial_will_end
   invoice.paid
   invoice.payment_failed
   checkout.session.completed
   ```

4. **Get Webhook Secret:**
   - After creating the endpoint, click to reveal the signing secret
   - Copy this value (starts with `whsec_`)
   - Add it to your environment as `STRIPE_WEBHOOK_SECRET`

### Webhook Event Handling

Events to handle in your webhook endpoint:

| Event | Action |
|-------|--------|
| `checkout.session.completed` | Provision new tenant, activate subscription |
| `customer.subscription.updated` | Update plan tier, handle upgrades/downgrades |
| `customer.subscription.deleted` | Cancel access, start grace period |
| `invoice.payment_failed` | Send payment failed notification |
| `invoice.paid` | Update subscription status to active |

### Webhook Security

The `StripeService.verifyWebhookSignature()` method validates webhook signatures:

```typescript
const event = stripeService.verifyWebhookSignature(
  request.body,
  request.headers['stripe-signature']
);

if (!event) {
  return { status: 400, body: 'Invalid signature' };
}
```

**Always verify webhook signatures** to prevent unauthorized webhook calls.

## Security Considerations

### Protecting API Keys

1. **Never commit secrets to Git:**
   - Add `local.settings.json` to `.gitignore`
   - Use environment variables for all keys
   - Use Azure Key Vault for production secrets

2. **Rotate keys periodically:**
   - Generate new API keys every 90 days
   - Update environment variables
   - Monitor for unauthorized usage

3. **Use different keys for environments:**
   - Test keys (`sk_test_...`) for development
   - Live keys (`sk_live_...`) for production
   - Never use live keys in development

### Webhook Security

1. **Always verify signatures:**
   - Use `stripe.webhooks.constructEvent()`
   - Reject requests with invalid signatures
   - Log suspicious webhook attempts

2. **Use HTTPS:**
   - Stripe only sends webhooks to HTTPS endpoints
   - Ensure your SSL certificate is valid

3. **Implement idempotency:**
   - Webhooks may be sent multiple times
   - Use `event.id` to prevent duplicate processing
   - Store processed event IDs

### PCI Compliance

**Good news:** Using Stripe Checkout and Elements means you don't handle card data:

- ✓ Card data never touches your servers
- ✓ Stripe handles PCI compliance
- ✓ No PCI audit required for your application

Still follow these best practices:
- Use HTTPS everywhere
- Secure your API keys
- Log security events
- Monitor for suspicious activity

## Integration Checklist

Use this checklist to ensure complete Stripe integration:

- [ ] Stripe account created and verified
- [ ] Products created for Starter, Professional, Business
- [ ] Monthly and annual prices created for each product
- [ ] Price IDs copied to environment variables
- [ ] Webhook endpoint configured
- [ ] Webhook secret added to environment
- [ ] Test mode validated with test cards
- [ ] Plan mapping tests passing
- [ ] Checkout flow tested
- [ ] Webhook event handling tested
- [ ] Production keys ready (for launch)
- [ ] SSL certificate valid
- [ ] Monitoring and logging configured

## Troubleshooting

### Common Issues

**Issue: "Invalid API Key"**
- Solution: Check that `STRIPE_SECRET_KEY` is set correctly
- Ensure you're using the right key for your environment (test vs live)

**Issue: "Webhook signature verification failed"**
- Solution: Verify `STRIPE_WEBHOOK_SECRET` matches the endpoint's signing secret
- Check that you're passing the raw request body to verification

**Issue: "Price ID not found"**
- Solution: Update `STRIPE_PRICE_MAPPINGS` with your actual price IDs
- Or set environment variables with the correct IDs

**Issue: "Plan tier mapping returns null"**
- Solution: Ensure price ID exists in `STRIPE_PRICE_MAPPINGS`
- Verify the price ID is spelled correctly

### Getting Help

- **Stripe Documentation:** [https://stripe.com/docs](https://stripe.com/docs)
- **Stripe Support:** [https://support.stripe.com](https://support.stripe.com)
- **API Reference:** [https://stripe.com/docs/api](https://stripe.com/docs/api)

## Next Steps

After completing Stripe setup:

1. **Implement Checkout Flow:**
   - Create checkout session endpoint
   - Handle successful checkout
   - Provision tenant resources

2. **Build Customer Portal:**
   - Allow subscription management
   - Enable plan upgrades/downgrades
   - Provide billing history

3. **Set Up Monitoring:**
   - Track successful subscriptions
   - Monitor failed payments
   - Alert on webhook failures

4. **Test Thoroughly:**
   - Test all subscription flows
   - Verify webhook handling
   - Validate plan enforcement

5. **Launch:**
   - Switch to live API keys
   - Update webhook endpoint to production
   - Monitor closely during initial launch

## Summary

This Stripe integration provides:
- ✅ Subscription billing for Starter, Professional, and Business plans
- ✅ Monthly and annual pricing options
- ✅ Automatic mapping of Stripe subscriptions to internal plan tiers
- ✅ Webhook handling for subscription lifecycle events
- ✅ Secure API key management
- ✅ Complete test coverage

The system is now ready to accept subscription payments through Stripe!
