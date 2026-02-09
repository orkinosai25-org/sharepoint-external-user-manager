/**
 * Stripe service for subscription management
 * Handles Stripe API integration and subscription mapping
 */

import Stripe from 'stripe';
import { config } from '../utils/config';
import { PlanTier } from '../models/plan';
import { 
  getPlanTierFromPriceId,
  getBillingIntervalFromPriceId,
  isValidStripePriceId,
  getStripePriceId,
  BillingInterval
} from '../config/stripe-config';

/**
 * Stripe subscription info mapped to internal structure
 */
export interface MappedSubscriptionInfo {
  subscriptionId: string;
  stripeCustomerId: string;
  stripePriceId: string;
  planTier: PlanTier;
  billingInterval: BillingInterval;
  status: Stripe.Subscription.Status;
  currentPeriodStart: Date;
  currentPeriodEnd: Date;
  cancelAtPeriodEnd: boolean;
}

/**
 * Stripe service class
 */
export class StripeService {
  private stripe: Stripe;

  constructor(secretKey?: string) {
    const apiKey = secretKey || config.stripe.secretKey;
    
    if (!apiKey) {
      throw new Error('Stripe secret key is not configured');
    }

    this.stripe = new Stripe(apiKey, {
      apiVersion: '2023-10-16',
      typescript: true
    });
  }

  /**
   * Get the Stripe instance (for advanced usage)
   */
  getStripeInstance(): Stripe {
    return this.stripe;
  }

  /**
   * Map a Stripe subscription to internal subscription info
   */
  mapSubscriptionToInternal(subscription: Stripe.Subscription): MappedSubscriptionInfo | null {
    // Get the first price item (we assume single-item subscriptions)
    const priceItem = subscription.items.data[0];
    
    if (!priceItem || !priceItem.price.id) {
      return null;
    }

    const stripePriceId = priceItem.price.id;
    const planTier = getPlanTierFromPriceId(stripePriceId);
    const billingInterval = getBillingIntervalFromPriceId(stripePriceId);

    if (!planTier || !billingInterval) {
      return null;
    }

    return {
      subscriptionId: subscription.id,
      stripeCustomerId: typeof subscription.customer === 'string' 
        ? subscription.customer 
        : subscription.customer.id,
      stripePriceId,
      planTier,
      billingInterval,
      status: subscription.status,
      currentPeriodStart: new Date(subscription.current_period_start * 1000),
      currentPeriodEnd: new Date(subscription.current_period_end * 1000),
      cancelAtPeriodEnd: subscription.cancel_at_period_end
    };
  }

  /**
   * Get subscription by ID and map to internal format
   */
  async getSubscription(subscriptionId: string): Promise<MappedSubscriptionInfo | null> {
    try {
      const subscription = await this.stripe.subscriptions.retrieve(subscriptionId);
      return this.mapSubscriptionToInternal(subscription);
    } catch (error) {
      console.error('Error retrieving Stripe subscription:', error);
      return null;
    }
  }

  /**
   * Get all subscriptions for a customer
   */
  async getCustomerSubscriptions(customerId: string): Promise<MappedSubscriptionInfo[]> {
    try {
      const subscriptions = await this.stripe.subscriptions.list({
        customer: customerId,
        status: 'all'
      });

      return subscriptions.data
        .map(sub => this.mapSubscriptionToInternal(sub))
        .filter((mapped): mapped is MappedSubscriptionInfo => mapped !== null);
    } catch (error) {
      console.error('Error retrieving customer subscriptions:', error);
      return [];
    }
  }

  /**
   * Validate Stripe price ID against our configured plans
   */
  validatePriceId(priceId: string): boolean {
    return isValidStripePriceId(priceId);
  }

  /**
   * Get plan tier from Stripe price ID
   */
  getPlanFromPriceId(priceId: string): PlanTier | null {
    return getPlanTierFromPriceId(priceId);
  }

  /**
   * Get Stripe price ID for a plan tier and billing interval
   */
  getPriceIdForPlan(tier: Exclude<PlanTier, 'Enterprise'>, interval: BillingInterval): string | undefined {
    return getStripePriceId(tier, interval);
  }

  /**
   * Verify webhook signature
   */
  verifyWebhookSignature(payload: string | Buffer, signature: string): Stripe.Event | null {
    const webhookSecret = config.stripe.webhookSecret;
    
    if (!webhookSecret) {
      throw new Error('Stripe webhook secret is not configured');
    }

    try {
      return this.stripe.webhooks.constructEvent(payload, signature, webhookSecret);
    } catch (error) {
      console.error('Error verifying webhook signature:', error);
      return null;
    }
  }

  /**
   * Create a checkout session for a new subscription
   */
  async createCheckoutSession(params: {
    priceId: string;
    customerEmail: string;
    successUrl: string;
    cancelUrl: string;
    metadata?: Record<string, string>;
  }): Promise<Stripe.Checkout.Session> {
    if (!this.validatePriceId(params.priceId)) {
      throw new Error(`Invalid price ID: ${params.priceId}`);
    }

    return await this.stripe.checkout.sessions.create({
      mode: 'subscription',
      payment_method_types: ['card'],
      line_items: [
        {
          price: params.priceId,
          quantity: 1
        }
      ],
      customer_email: params.customerEmail,
      success_url: params.successUrl,
      cancel_url: params.cancelUrl,
      metadata: params.metadata
    });
  }

  /**
   * Create a customer portal session for subscription management
   */
  async createPortalSession(customerId: string, returnUrl: string): Promise<Stripe.BillingPortal.Session> {
    return await this.stripe.billingPortal.sessions.create({
      customer: customerId,
      return_url: returnUrl
    });
  }
}

/**
 * Singleton instance of StripeService
 */
let stripeServiceInstance: StripeService | null = null;

/**
 * Get or create the Stripe service instance
 */
export function getStripeService(): StripeService {
  if (!stripeServiceInstance) {
    stripeServiceInstance = new StripeService();
  }
  return stripeServiceInstance;
}

/**
 * Helper function to map Stripe subscription status to internal status
 */
export function mapStripeStatusToInternal(stripeStatus: Stripe.Subscription.Status): 'Active' | 'Cancelled' | 'PastDue' | 'Trialing' {
  switch (stripeStatus) {
    case 'active':
      return 'Active';
    case 'canceled':
      return 'Cancelled';
    case 'past_due':
    case 'unpaid':
      return 'PastDue';
    case 'trialing':
      return 'Trialing';
    case 'incomplete':
    case 'incomplete_expired':
    case 'paused':
    default:
      return 'Cancelled';
  }
}
