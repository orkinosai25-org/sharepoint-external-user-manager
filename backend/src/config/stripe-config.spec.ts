/**
 * Tests for Stripe service and configuration
 */

import { 
  getStripePriceMapping,
  getPlanTierFromPriceId,
  getBillingIntervalFromPriceId,
  isValidStripePriceId,
  getStripePriceId,
  getStripePriceIdsForTier,
  STRIPE_PRICE_MAPPINGS
} from '../config/stripe-config';

describe('Stripe Configuration', () => {
  describe('Price Mappings', () => {
    it('should have mappings for all non-Enterprise plans', () => {
      const requiredTiers = ['Starter', 'Professional', 'Business'];
      const requiredIntervals = ['month', 'year'];
      
      requiredTiers.forEach(tier => {
        requiredIntervals.forEach(interval => {
          const mapping = STRIPE_PRICE_MAPPINGS.find(
            m => m.tier === tier && m.interval === interval
          );
          expect(mapping).toBeDefined();
          expect(mapping?.priceId).toBeDefined();
          expect(mapping?.amount).toBeGreaterThan(0);
        });
      });
    });

    it('should have correct monthly prices', () => {
      const starterMonthly = STRIPE_PRICE_MAPPINGS.find(
        m => m.tier === 'Starter' && m.interval === 'month'
      );
      const professionalMonthly = STRIPE_PRICE_MAPPINGS.find(
        m => m.tier === 'Professional' && m.interval === 'month'
      );
      const businessMonthly = STRIPE_PRICE_MAPPINGS.find(
        m => m.tier === 'Business' && m.interval === 'month'
      );

      expect(starterMonthly?.amount).toBe(2900); // $29.00
      expect(professionalMonthly?.amount).toBe(9900); // $99.00
      expect(businessMonthly?.amount).toBe(29900); // $299.00
    });

    it('should have correct annual prices', () => {
      const starterAnnual = STRIPE_PRICE_MAPPINGS.find(
        m => m.tier === 'Starter' && m.interval === 'year'
      );
      const professionalAnnual = STRIPE_PRICE_MAPPINGS.find(
        m => m.tier === 'Professional' && m.interval === 'year'
      );
      const businessAnnual = STRIPE_PRICE_MAPPINGS.find(
        m => m.tier === 'Business' && m.interval === 'year'
      );

      expect(starterAnnual?.amount).toBe(29000); // $290.00
      expect(professionalAnnual?.amount).toBe(99000); // $990.00
      expect(businessAnnual?.amount).toBe(299000); // $2,990.00
    });

    it('should have unique price IDs', () => {
      const priceIds = STRIPE_PRICE_MAPPINGS.map(m => m.priceId);
      const uniquePriceIds = new Set(priceIds);
      
      expect(priceIds.length).toBe(uniquePriceIds.size);
    });
  });

  describe('getStripePriceMapping', () => {
    it('should return mapping for valid price ID', () => {
      const mapping = STRIPE_PRICE_MAPPINGS[0];
      const result = getStripePriceMapping(mapping.priceId);
      
      expect(result).toBeDefined();
      expect(result?.tier).toBe(mapping.tier);
      expect(result?.interval).toBe(mapping.interval);
      expect(result?.amount).toBe(mapping.amount);
    });

    it('should return undefined for invalid price ID', () => {
      const result = getStripePriceMapping('invalid_price_id');
      expect(result).toBeUndefined();
    });
  });

  describe('getPlanTierFromPriceId', () => {
    it('should return correct tier for Starter monthly', () => {
      const mapping = STRIPE_PRICE_MAPPINGS.find(
        m => m.tier === 'Starter' && m.interval === 'month'
      );
      const tier = getPlanTierFromPriceId(mapping!.priceId);
      expect(tier).toBe('Starter');
    });

    it('should return correct tier for Professional annual', () => {
      const mapping = STRIPE_PRICE_MAPPINGS.find(
        m => m.tier === 'Professional' && m.interval === 'year'
      );
      const tier = getPlanTierFromPriceId(mapping!.priceId);
      expect(tier).toBe('Professional');
    });

    it('should return correct tier for Business monthly', () => {
      const mapping = STRIPE_PRICE_MAPPINGS.find(
        m => m.tier === 'Business' && m.interval === 'month'
      );
      const tier = getPlanTierFromPriceId(mapping!.priceId);
      expect(tier).toBe('Business');
    });

    it('should return null for invalid price ID', () => {
      const tier = getPlanTierFromPriceId('invalid_price_id');
      expect(tier).toBeNull();
    });
  });

  describe('getBillingIntervalFromPriceId', () => {
    it('should return correct interval for monthly prices', () => {
      const monthlyMapping = STRIPE_PRICE_MAPPINGS.find(m => m.interval === 'month');
      const interval = getBillingIntervalFromPriceId(monthlyMapping!.priceId);
      expect(interval).toBe('month');
    });

    it('should return correct interval for annual prices', () => {
      const annualMapping = STRIPE_PRICE_MAPPINGS.find(m => m.interval === 'year');
      const interval = getBillingIntervalFromPriceId(annualMapping!.priceId);
      expect(interval).toBe('year');
    });

    it('should return null for invalid price ID', () => {
      const interval = getBillingIntervalFromPriceId('invalid_price_id');
      expect(interval).toBeNull();
    });
  });

  describe('isValidStripePriceId', () => {
    it('should return true for all configured price IDs', () => {
      STRIPE_PRICE_MAPPINGS.forEach(mapping => {
        expect(isValidStripePriceId(mapping.priceId)).toBe(true);
      });
    });

    it('should return false for invalid price ID', () => {
      expect(isValidStripePriceId('invalid_price_id')).toBe(false);
      expect(isValidStripePriceId('')).toBe(false);
      expect(isValidStripePriceId('price_xyz123')).toBe(false);
    });
  });

  describe('getStripePriceId', () => {
    it('should return price ID for Starter monthly', () => {
      const priceId = getStripePriceId('Starter', 'month');
      expect(priceId).toBeDefined();
      
      const mapping = getStripePriceMapping(priceId!);
      expect(mapping?.tier).toBe('Starter');
      expect(mapping?.interval).toBe('month');
    });

    it('should return price ID for Professional annual', () => {
      const priceId = getStripePriceId('Professional', 'year');
      expect(priceId).toBeDefined();
      
      const mapping = getStripePriceMapping(priceId!);
      expect(mapping?.tier).toBe('Professional');
      expect(mapping?.interval).toBe('year');
    });

    it('should return price ID for Business monthly', () => {
      const priceId = getStripePriceId('Business', 'month');
      expect(priceId).toBeDefined();
      
      const mapping = getStripePriceMapping(priceId!);
      expect(mapping?.tier).toBe('Business');
      expect(mapping?.interval).toBe('month');
    });
  });

  describe('getStripePriceIdsForTier', () => {
    it('should return two price IDs for Starter (monthly and annual)', () => {
      const priceIds = getStripePriceIdsForTier('Starter');
      expect(priceIds).toHaveLength(2);
      
      const intervals = priceIds.map(id => getBillingIntervalFromPriceId(id));
      expect(intervals).toContain('month');
      expect(intervals).toContain('year');
    });

    it('should return two price IDs for Professional', () => {
      const priceIds = getStripePriceIdsForTier('Professional');
      expect(priceIds).toHaveLength(2);
    });

    it('should return two price IDs for Business', () => {
      const priceIds = getStripePriceIdsForTier('Business');
      expect(priceIds).toHaveLength(2);
    });
  });

  describe('Price Consistency', () => {
    it('should have annual prices approximately equal to 10 months of monthly price', () => {
      const tiers = ['Starter', 'Professional', 'Business'] as const;
      
      tiers.forEach(tier => {
        const monthly = STRIPE_PRICE_MAPPINGS.find(
          m => m.tier === tier && m.interval === 'month'
        );
        const annual = STRIPE_PRICE_MAPPINGS.find(
          m => m.tier === tier && m.interval === 'year'
        );
        
        expect(monthly).toBeDefined();
        expect(annual).toBeDefined();
        
        const expectedAnnual = monthly!.amount * 10;
        const variance = Math.abs(annual!.amount - expectedAnnual);
        const maxVariance = monthly!.amount * 2; // Allow 2 months variance
        
        expect(variance).toBeLessThanOrEqual(maxVariance);
      });
    });

    it('should have increasing prices from Starter to Business', () => {
      const starterMonthly = STRIPE_PRICE_MAPPINGS.find(
        m => m.tier === 'Starter' && m.interval === 'month'
      );
      const professionalMonthly = STRIPE_PRICE_MAPPINGS.find(
        m => m.tier === 'Professional' && m.interval === 'month'
      );
      const businessMonthly = STRIPE_PRICE_MAPPINGS.find(
        m => m.tier === 'Business' && m.interval === 'month'
      );
      
      expect(professionalMonthly!.amount).toBeGreaterThan(starterMonthly!.amount);
      expect(businessMonthly!.amount).toBeGreaterThan(professionalMonthly!.amount);
    });
  });
});
