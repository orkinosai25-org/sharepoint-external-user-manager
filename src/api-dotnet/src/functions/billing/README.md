# Billing API - Stripe Checkout & Subscription Flow

This document describes the billing endpoints for Stripe integration.

## Endpoints

### POST /billing/create-checkout-session

Creates a Stripe checkout session for a subscription purchase.

**Authentication:** Required (Bearer token)

**Request Body:**

Option 1 - Using Stripe Price ID directly:
```json
{
  "priceId": "price_1AbCdEfGhIjKlMnO",
  "successUrl": "https://yoursaas.com/onboarding/success?session_id={CHECKOUT_SESSION_ID}",
  "cancelUrl": "https://yoursaas.com/onboarding/cancel"
}
```

Option 2 - Using Plan Tier and Billing Interval:
```json
{
  "planTier": "Professional",
  "billingInterval": "month",
  "successUrl": "https://yoursaas.com/onboarding/success?session_id={CHECKOUT_SESSION_ID}",
  "cancelUrl": "https://yoursaas.com/onboarding/cancel"
}
```

**Parameters:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| priceId | string | conditional | Stripe price ID. Required if planTier not provided. |
| planTier | string | conditional | Plan tier: "Starter", "Professional", or "Business". Required if priceId not provided. |
| billingInterval | string | conditional | Billing interval: "month" or "year". Required if planTier provided. |
| successUrl | string | yes | URL to redirect after successful payment. Use `{CHECKOUT_SESSION_ID}` placeholder. |
| cancelUrl | string | yes | URL to redirect if user cancels. |

**Response:**

```json
{
  "success": true,
  "data": {
    "sessionId": "cs_test_a1B2c3D4e5F6g7H8i9J0",
    "url": "https://checkout.stripe.com/pay/cs_test_a1B2c3D4e5F6g7H8i9J0",
    "expiresAt": "2024-01-01T12:00:00.000Z"
  },
  "meta": {
    "correlationId": "abc123",
    "timestamp": "2024-01-01T11:00:00.000Z"
  }
}
```

**Error Responses:**

- `400 Bad Request` - Invalid request parameters
- `401 Unauthorized` - Missing or invalid authentication
- `404 Not Found` - Tenant not found
- `500 Internal Server Error` - Server error

**Example Usage:**

```typescript
// Monthly Professional plan
const response = await fetch('/billing/create-checkout-session', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${accessToken}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    planTier: 'Professional',
    billingInterval: 'month',
    successUrl: 'https://yoursaas.com/onboarding/success?session_id={CHECKOUT_SESSION_ID}',
    cancelUrl: 'https://yoursaas.com/onboarding/cancel'
  })
});

const result = await response.json();

// Redirect to Stripe Checkout
window.location.href = result.data.url;
```

### POST /billing/webhook

Handles Stripe webhook events for subscription lifecycle management.

**Authentication:** None (verified via Stripe signature)

**Headers:**

| Header | Required | Description |
|--------|----------|-------------|
| stripe-signature | yes | Stripe webhook signature for verification |

**Supported Events:**

| Event | Description | Action |
|-------|-------------|--------|
| `checkout.session.completed` | Checkout completed successfully | Activate subscription, clear trial |
| `customer.subscription.updated` | Subscription plan changed | Update plan tier and status |
| `customer.subscription.deleted` | Subscription cancelled | Set grace period (7 days) |
| `invoice.paid` | Invoice payment successful | Reactivate subscription |
| `invoice.payment_failed` | Invoice payment failed | Set subscription to grace period |

**Response:**

```json
{
  "received": true
}
```

**Webhook Configuration:**

1. In Stripe Dashboard, go to Developers → Webhooks
2. Add endpoint: `https://your-api.azurewebsites.net/api/billing/webhook`
3. Select events:
   - `checkout.session.completed`
   - `customer.subscription.updated`
   - `customer.subscription.deleted`
   - `invoice.paid`
   - `invoice.payment_failed`
4. Copy webhook signing secret to `STRIPE_WEBHOOK_SECRET` environment variable

## Subscription Flow

### New Customer Onboarding

1. **Customer completes onboarding** → Trial subscription created
2. **Customer clicks "Upgrade"** → Call `POST /billing/create-checkout-session`
3. **Redirect to Stripe** → Customer enters payment details
4. **Payment successful** → Stripe sends `checkout.session.completed` webhook
5. **Webhook handler** → Activates subscription, clears trial
6. **Redirect to success page** → Customer returns to SaaS app

### Trial to Paid Transition

When a customer on trial upgrades to a paid plan:

```
Before:
- status: 'Trial'
- trialExpiry: '2024-01-31T23:59:59Z'
- stripeSubscriptionId: null

After webhook:
- status: 'Active'
- trialExpiry: null (cleared)
- stripeSubscriptionId: 'sub_123abc'
- stripeCustomerId: 'cus_456def'
- stripePriceId: 'price_789ghi'
```

### Subscription Updates

When a customer changes their plan (upgrade/downgrade):

1. Stripe sends `customer.subscription.updated` webhook
2. Handler updates:
   - Plan tier
   - Subscription status
   - Price ID
   - Billing period end date

### Cancellation & Grace Period

When a subscription is cancelled:

1. Stripe sends `customer.subscription.deleted` webhook
2. Handler sets:
   - Status: 'Cancelled'
   - Grace period: 7 days from cancellation
3. During grace period:
   - Customer retains access
   - Can reactivate without data loss

### Payment Failures

When an invoice payment fails:

1. Stripe sends `invoice.payment_failed` webhook
2. Handler sets:
   - Status: 'GracePeriod'
3. Stripe automatically retries payment
4. On successful retry:
   - Stripe sends `invoice.paid` webhook
   - Handler reactivates subscription

## Supported Plan Tiers

| Tier | Monthly | Annual | Stripe Product ID |
|------|---------|--------|-------------------|
| Starter | $29 | $290 (save $58) | `prod_starter` |
| Professional | $99 | $990 (save $198) | `prod_professional` |
| Business | $299 | $2,990 (save $598) | `prod_business` |

**Note:** Enterprise plans are custom and not sold through Stripe.

## Price ID Configuration

Price IDs are configured in `src/config/stripe-config.ts` and can be overridden via environment variables:

```bash
# Starter Plan
STRIPE_PRICE_ID_STARTER_MONTHLY=price_...
STRIPE_PRICE_ID_STARTER_ANNUAL=price_...

# Professional Plan
STRIPE_PRICE_ID_PROFESSIONAL_MONTHLY=price_...
STRIPE_PRICE_ID_PROFESSIONAL_ANNUAL=price_...

# Business Plan
STRIPE_PRICE_ID_BUSINESS_MONTHLY=price_...
STRIPE_PRICE_ID_BUSINESS_ANNUAL=price_...
```

## Error Handling

All endpoints follow the standard error response format:

```json
{
  "success": false,
  "error": {
    "code": "ERROR_CODE",
    "message": "Human-readable error message",
    "details": "Additional details about the error",
    "correlationId": "abc123"
  }
}
```

**Common Error Codes:**

- `VALIDATION_ERROR` - Invalid request parameters
- `UNAUTHORIZED` - Authentication required or failed
- `NOT_FOUND` - Resource not found
- `CONFLICT` - Resource already exists
- `INTERNAL_ERROR` - Server error

## Security

### Webhook Security

Webhooks are secured using Stripe signature verification:

1. Stripe signs each webhook with your webhook secret
2. Handler verifies signature before processing
3. Invalid signatures are rejected with 400 status

### Authentication

The checkout session endpoint requires authentication:

1. Include Bearer token in Authorization header
2. Token must be valid Azure AD JWT
3. Tenant context is extracted from token

## Testing

### Test Mode

During development, use Stripe test mode:

- Use test API keys (`sk_test_...` and `pk_test_...`)
- Use test cards (e.g., `4242 4242 4242 4242`)
- No real charges are made

### Test Cards

```
Success: 4242 4242 4242 4242
Decline: 4000 0000 0000 0002
3D Secure: 4000 0025 0000 3155
```

### Webhook Testing

Test webhooks using Stripe CLI:

```bash
stripe listen --forward-to localhost:7071/api/billing/webhook
stripe trigger checkout.session.completed
```

## Monitoring

### Audit Logs

All subscription events are logged to the audit log:

- `SubscriptionActivated` - New subscription activated
- `SubscriptionUpdated` - Plan changed
- `PaymentFailed` - Payment failed

### Metrics to Monitor

- Checkout session creation rate
- Successful conversions (trial → paid)
- Payment failure rate
- Webhook processing time
- Failed webhook signatures

## Related Documentation

- [STRIPE_SETUP.md](../../STRIPE_SETUP.md) - Complete Stripe setup guide
- [PRICING_AND_PLANS.md](../../PRICING_AND_PLANS.md) - Plan details and limits
- [Stripe API Documentation](https://stripe.com/docs/api) - Official Stripe API docs
