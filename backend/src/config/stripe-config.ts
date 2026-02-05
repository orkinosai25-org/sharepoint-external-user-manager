/**
 * Stripe product and price configuration
 * Maps Stripe price IDs to internal plan tiers
 */

import { PlanTier } from '../models/plan';

export type BillingInterval = 'month' | 'year';

/**
 * Stripe price mapping interface
 */
export interface StripePriceMapping {
  priceId: string;
  tier: PlanTier;
  interval: BillingInterval;
  amount: number; // in cents
}

/**
 * Stripe product IDs for each plan tier
 * These should be created in your Stripe dashboard
 * 
 * To create these products in Stripe:
 * 1. Go to Stripe Dashboard > Products
 * 2. Create a product for each tier (Starter, Professional, Business)
 * 3. Add monthly and annual prices for each product
 * 4. Copy the price IDs and update the mappings below
 */
export const STRIPE_PRODUCT_IDS: Record<Exclude<PlanTier, 'Enterprise'>, string> = {
  'Starter': process.env.STRIPE_PRODUCT_ID_STARTER || 'prod_starter',
  'Professional': process.env.STRIPE_PRODUCT_ID_PROFESSIONAL || 'prod_professional',
  'Business': process.env.STRIPE_PRODUCT_ID_BUSINESS || 'prod_business'
};

/**
 * Stripe price mappings
 * Maps Stripe price IDs to internal plan tiers and billing intervals
 * 
 * IMPORTANT: Update these with your actual Stripe price IDs
 * These are placeholder values - replace with real IDs from your Stripe dashboard
 */
export const STRIPE_PRICE_MAPPINGS: StripePriceMapping[] = [
  // Starter Plan
  {
    priceId: process.env.STRIPE_PRICE_ID_STARTER_MONTHLY || 'price_starter_monthly',
    tier: 'Starter',
    interval: 'month',
    amount: 2900 // $29.00
  },
  {
    priceId: process.env.STRIPE_PRICE_ID_STARTER_ANNUAL || 'price_starter_annual',
    tier: 'Starter',
    interval: 'year',
    amount: 29000 // $290.00
  },
  
  // Professional Plan
  {
    priceId: process.env.STRIPE_PRICE_ID_PROFESSIONAL_MONTHLY || 'price_professional_monthly',
    tier: 'Professional',
    interval: 'month',
    amount: 9900 // $99.00
  },
  {
    priceId: process.env.STRIPE_PRICE_ID_PROFESSIONAL_ANNUAL || 'price_professional_annual',
    tier: 'Professional',
    interval: 'year',
    amount: 99000 // $990.00
  },
  
  // Business Plan
  {
    priceId: process.env.STRIPE_PRICE_ID_BUSINESS_MONTHLY || 'price_business_monthly',
    tier: 'Business',
    interval: 'month',
    amount: 29900 // $299.00
  },
  {
    priceId: process.env.STRIPE_PRICE_ID_BUSINESS_ANNUAL || 'price_business_annual',
    tier: 'Business',
    interval: 'year',
    amount: 299000 // $2,990.00
  }
];

/**
 * Create a map for quick price ID lookups
 */
export const STRIPE_PRICE_MAP = new Map<string, StripePriceMapping>(
  STRIPE_PRICE_MAPPINGS.map(mapping => [mapping.priceId, mapping])
);

/**
 * Get Stripe price mapping by price ID
 */
export function getStripePriceMapping(priceId: string): StripePriceMapping | undefined {
  return STRIPE_PRICE_MAP.get(priceId);
}

/**
 * Get Stripe price ID by plan tier and billing interval
 */
export function getStripePriceId(tier: Exclude<PlanTier, 'Enterprise'>, interval: BillingInterval): string | undefined {
  const mapping = STRIPE_PRICE_MAPPINGS.find(
    m => m.tier === tier && m.interval === interval
  );
  return mapping?.priceId;
}

/**
 * Get all price IDs for a given tier
 */
export function getStripePriceIdsForTier(tier: Exclude<PlanTier, 'Enterprise'>): string[] {
  return STRIPE_PRICE_MAPPINGS
    .filter(m => m.tier === tier)
    .map(m => m.priceId);
}

/**
 * Validate if a price ID is valid for our application
 */
export function isValidStripePriceId(priceId: string): boolean {
  return STRIPE_PRICE_MAP.has(priceId);
}

/**
 * Get plan tier from Stripe price ID
 */
export function getPlanTierFromPriceId(priceId: string): PlanTier | null {
  const mapping = getStripePriceMapping(priceId);
  return mapping?.tier || null;
}

/**
 * Get billing interval from Stripe price ID
 */
export function getBillingIntervalFromPriceId(priceId: string): BillingInterval | null {
  const mapping = getStripePriceMapping(priceId);
  return mapping?.interval || null;
}
