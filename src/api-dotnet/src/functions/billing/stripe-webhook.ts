/**
 * POST /billing/webhook - Handle Stripe webhook events
 */

import { app, HttpRequest, HttpResponseInit, InvocationContext } from '@azure/functions';
import { databaseService } from '../../services/database';
import { getStripeService } from '../../services/stripe-service';
import { auditLogger } from '../../services/auditLogger';
import { attachCorrelationId } from '../../utils/correlation';
import Stripe from 'stripe';
import { mapPlanToSubscriptionTier } from '../../models/subscription';

async function handleStripeWebhook(req: HttpRequest, context: InvocationContext): Promise<HttpResponseInit> {
  const correlationId = attachCorrelationId(req);

  try {
    // Get raw body and signature
    const signature = req.headers.get('stripe-signature');
    
    if (!signature) {
      context.log('Missing Stripe signature header');
      return {
        status: 400,
        body: 'Missing Stripe signature'
      };
    }

    // Get raw request body
    const rawBody = await req.text();

    // Verify webhook signature
    const stripeService = getStripeService();
    const event = stripeService.verifyWebhookSignature(rawBody, signature);
    
    if (!event) {
      context.log('Invalid webhook signature');
      return {
        status: 400,
        body: 'Invalid signature'
      };
    }

    context.log(`Received Stripe webhook event: ${event.type}`);

    // Ensure database is connected
    await databaseService.connect();

    // Handle different event types
    switch (event.type) {
      case 'checkout.session.completed':
        await handleCheckoutSessionCompleted(event, context, correlationId);
        break;
      
      case 'customer.subscription.updated':
        await handleSubscriptionUpdated(event, context, correlationId);
        break;
      
      case 'customer.subscription.deleted':
        await handleSubscriptionDeleted(event, context, correlationId);
        break;
      
      case 'invoice.paid':
        await handleInvoicePaid(event, context, correlationId);
        break;
      
      case 'invoice.payment_failed':
        await handleInvoicePaymentFailed(event, context, correlationId);
        break;
      
      default:
        context.log(`Unhandled event type: ${event.type}`);
    }

    return {
      status: 200,
      body: JSON.stringify({ received: true })
    };
  } catch (error) {
    context.error('Error processing webhook:', error);
    return {
      status: 500,
      body: 'Webhook processing failed'
    };
  }
}

/**
 * Handle checkout session completed - activate subscription
 */
async function handleCheckoutSessionCompleted(
  event: Stripe.Event,
  context: InvocationContext,
  correlationId: string
): Promise<void> {
  const session = event.data.object as Stripe.Checkout.Session;
  
  context.log('Processing checkout.session.completed', {
    sessionId: session.id,
    customerId: session.customer,
    subscriptionId: session.subscription
  });

  // Get tenant ID from metadata
  const tenantId = session.metadata?.tenantId;
  
  if (!tenantId) {
    context.error('No tenantId in session metadata');
    return;
  }

  // Get subscription details from Stripe
  const stripeService = getStripeService();
  const subscriptionInfo = await stripeService.getSubscription(session.subscription as string);
  
  if (!subscriptionInfo) {
    context.error('Failed to retrieve subscription from Stripe');
    return;
  }

  // Get current subscription
  const currentSubscription = await databaseService.getSubscriptionByTenantId(parseInt(tenantId));
  
  if (!currentSubscription) {
    context.error('Subscription not found for tenant');
    return;
  }

  // Map Stripe status to internal status - using Active for paid subscriptions
  let internalStatus: 'Trial' | 'Active' | 'Expired' | 'Cancelled' | 'GracePeriod' = 'Active';
  
  if (subscriptionInfo.status === 'trialing') {
    internalStatus = 'Trial';
  } else if (subscriptionInfo.status === 'active') {
    internalStatus = 'Active';
  } else if (subscriptionInfo.status === 'canceled' || subscriptionInfo.status === 'unpaid') {
    internalStatus = 'Cancelled';
  } else if (subscriptionInfo.status === 'past_due') {
    internalStatus = 'GracePeriod';
  }

  // Update subscription in database
  await databaseService.updateSubscription(currentSubscription.id, {
    tier: mapPlanToSubscriptionTier(subscriptionInfo.planTier),
    status: internalStatus,
    stripeCustomerId: subscriptionInfo.stripeCustomerId,
    stripeSubscriptionId: subscriptionInfo.subscriptionId,
    stripePriceId: subscriptionInfo.stripePriceId,
    startDate: subscriptionInfo.currentPeriodStart,
    endDate: subscriptionInfo.currentPeriodEnd,
    trialExpiry: null, // Clear trial when paid subscription starts
    gracePeriodEnd: null
  });

  // Update tenant status
  const tenant = await databaseService.getTenantById(parseInt(tenantId));
  if (tenant) {
    await databaseService.updateTenant(tenant.id, {
      status: 'Active'
    });

    // Log audit event
    const tenantContext = {
      tenantId: tenant.id,
      entraIdTenantId: tenant.entraIdTenantId,
      userId: 'system',
      userEmail: tenant.primaryAdminEmail,
      roles: ['Owner' as const],
      subscriptionTier: mapPlanToSubscriptionTier(subscriptionInfo.planTier)
    };

    await auditLogger.logSuccess(
      tenantContext,
      'SubscriptionActivated',
      'Subscription',
      subscriptionInfo.subscriptionId,
      {
        planTier: subscriptionInfo.planTier,
        billingInterval: subscriptionInfo.billingInterval,
        previousStatus: currentSubscription.status,
        newStatus: internalStatus
      },
      'stripe-webhook',
      correlationId
    );
  }

  context.log('Successfully activated subscription', {
    tenantId,
    subscriptionId: subscriptionInfo.subscriptionId,
    planTier: subscriptionInfo.planTier
  });
}

/**
 * Handle subscription updated
 */
async function handleSubscriptionUpdated(
  event: Stripe.Event,
  context: InvocationContext,
  _correlationId: string
): Promise<void> {
  const subscription = event.data.object as Stripe.Subscription;
  
  context.log('Processing customer.subscription.updated', {
    subscriptionId: subscription.id,
    customerId: subscription.customer,
    status: subscription.status
  });

  // Map subscription to internal format
  const stripeService = getStripeService();
  const subscriptionInfo = stripeService.mapSubscriptionToInternal(subscription);
  
  if (!subscriptionInfo) {
    context.error('Failed to map subscription');
    return;
  }

  // Find tenant by Stripe customer ID
  const dbSubscription = await databaseService.getSubscriptionByStripeCustomerId(
    subscriptionInfo.stripeCustomerId
  );
  
  if (!dbSubscription) {
    context.log('Subscription not found in database - may not be onboarded yet');
    return;
  }

  // Map Stripe status to internal status
  let internalStatus: 'Trial' | 'Active' | 'Expired' | 'Cancelled' | 'GracePeriod' = 'Active';
  
  if (subscriptionInfo.status === 'trialing') {
    internalStatus = 'Trial';
  } else if (subscriptionInfo.status === 'active') {
    internalStatus = 'Active';
  } else if (subscriptionInfo.status === 'canceled' || subscriptionInfo.status === 'unpaid') {
    internalStatus = 'Cancelled';
  } else if (subscriptionInfo.status === 'past_due') {
    internalStatus = 'GracePeriod';
  }

  // Update subscription
  await databaseService.updateSubscription(dbSubscription.id, {
    tier: mapPlanToSubscriptionTier(subscriptionInfo.planTier),
    status: internalStatus,
    stripePriceId: subscriptionInfo.stripePriceId,
    endDate: subscriptionInfo.currentPeriodEnd
  });

  context.log('Successfully updated subscription', {
    subscriptionId: subscription.id,
    planTier: subscriptionInfo.planTier,
    status: internalStatus
  });
}

/**
 * Handle subscription deleted
 */
async function handleSubscriptionDeleted(
  event: Stripe.Event,
  context: InvocationContext,
  _correlationId: string
): Promise<void> {
  const subscription = event.data.object as Stripe.Subscription;
  
  context.log('Processing customer.subscription.deleted', {
    subscriptionId: subscription.id,
    customerId: subscription.customer
  });

  // Find tenant by Stripe customer ID
  const customerId = typeof subscription.customer === 'string' 
    ? subscription.customer 
    : subscription.customer.id;
  
  const dbSubscription = await databaseService.getSubscriptionByStripeCustomerId(customerId);
  
  if (!dbSubscription) {
    context.log('Subscription not found in database');
    return;
  }

  // Set grace period (e.g., 7 days)
  const gracePeriodEnd = new Date();
  gracePeriodEnd.setDate(gracePeriodEnd.getDate() + 7);

  // Update subscription status
  await databaseService.updateSubscription(dbSubscription.id, {
    status: 'Cancelled',
    gracePeriodEnd
  });

  // Update tenant status if needed
  const tenant = await databaseService.getTenantById(dbSubscription.tenantId);
  if (tenant) {
    await databaseService.updateTenant(tenant.id, {
      status: 'Active' // Keep active during grace period
    });
  }

  context.log('Successfully marked subscription as cancelled', {
    subscriptionId: subscription.id,
    gracePeriodEnd: gracePeriodEnd.toISOString()
  });
}

/**
 * Handle invoice paid
 */
async function handleInvoicePaid(
  event: Stripe.Event,
  context: InvocationContext,
  _correlationId: string
): Promise<void> {
  const invoice = event.data.object as Stripe.Invoice;
  
  context.log('Processing invoice.paid', {
    invoiceId: invoice.id,
    customerId: invoice.customer,
    subscriptionId: invoice.subscription
  });

  // Find tenant by Stripe customer ID
  const customerId = typeof invoice.customer === 'string' 
    ? invoice.customer 
    : invoice.customer?.id;
  
  if (!customerId) {
    return;
  }

  const dbSubscription = await databaseService.getSubscriptionByStripeCustomerId(customerId);
  
  if (!dbSubscription) {
    context.log('Subscription not found in database');
    return;
  }

  // Update subscription status to Active
  await databaseService.updateSubscription(dbSubscription.id, {
    status: 'Active'
  });

  context.log('Successfully updated subscription after invoice payment', {
    invoiceId: invoice.id,
    subscriptionId: invoice.subscription
  });
}

/**
 * Handle invoice payment failed
 */
async function handleInvoicePaymentFailed(
  event: Stripe.Event,
  context: InvocationContext,
  correlationId: string
): Promise<void> {
  const invoice = event.data.object as Stripe.Invoice;
  
  context.log('Processing invoice.payment_failed', {
    invoiceId: invoice.id,
    customerId: invoice.customer,
    subscriptionId: invoice.subscription
  });

  // Find tenant by Stripe customer ID
  const customerId = typeof invoice.customer === 'string' 
    ? invoice.customer 
    : invoice.customer?.id;
  
  if (!customerId) {
    return;
  }

  const dbSubscription = await databaseService.getSubscriptionByStripeCustomerId(customerId);
  
  if (!dbSubscription) {
    context.log('Subscription not found in database');
    return;
  }

  // Update subscription status to PastDue (map to GracePeriod in internal system)
  await databaseService.updateSubscription(dbSubscription.id, {
    status: 'GracePeriod'
  });

  // Log the failure
  const tenant = await databaseService.getTenantById(dbSubscription.tenantId);
  if (tenant) {
    const tenantContext = {
      tenantId: tenant.id,
      entraIdTenantId: tenant.entraIdTenantId,
      userId: 'system',
      userEmail: tenant.primaryAdminEmail,
      roles: ['Owner' as const],
      subscriptionTier: dbSubscription.tier
    };

    await auditLogger.logSuccess(
      tenantContext,
      'PaymentFailed',
      'Invoice',
      invoice.id,
      {
        amount: invoice.amount_due,
        currency: invoice.currency,
        attemptCount: invoice.attempt_count
      },
      'stripe-webhook',
      correlationId
    );
  }

  context.log('Successfully marked subscription as past due', {
    invoiceId: invoice.id,
    subscriptionId: invoice.subscription
  });
}

app.http('stripeWebhook', {
  methods: ['POST'],
  authLevel: 'anonymous',
  route: 'billing/webhook',
  handler: handleStripeWebhook
});
