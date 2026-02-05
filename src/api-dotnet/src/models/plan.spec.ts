/**
 * Tests for plan definitions
 */

import {
  PlanTier,
  PLAN_DEFINITIONS,
  getPlanDefinition,
  getPlanLimits,
  getPlanFeatures,
  isUnlimited,
  hasFeature,
  getMinimumTierForFeature,
  getAllPlanTiers
} from '../models/plan';

describe('Plan Definitions', () => {
  describe('Plan Structure', () => {
    it('should have all four plan tiers defined', () => {
      const tiers: PlanTier[] = ['Starter', 'Professional', 'Business', 'Enterprise'];
      
      tiers.forEach(tier => {
        expect(PLAN_DEFINITIONS[tier]).toBeDefined();
        expect(PLAN_DEFINITIONS[tier].tier).toBe(tier);
      });
    });

    it('should have complete plan definition for each tier', () => {
      const tiers = getAllPlanTiers();
      
      tiers.forEach(tier => {
        const plan = PLAN_DEFINITIONS[tier];
        
        // Check basic fields
        expect(plan.tier).toBe(tier);
        expect(plan.displayName).toBeDefined();
        expect(plan.description).toBeDefined();
        
        // Check pricing
        expect(plan.pricing).toBeDefined();
        expect(plan.pricing.monthly).toBeGreaterThan(0);
        expect(plan.pricing.annual).toBeGreaterThan(0);
        expect(plan.pricing.currency).toBe('USD');
        
        // Check limits
        expect(plan.limits).toBeDefined();
        expect(plan.limits.maxClientSpaces).toBeGreaterThan(0);
        expect(plan.limits.auditRetentionDays).toBeGreaterThan(0);
        expect(plan.limits.supportLevel).toBeDefined();
        expect(plan.limits.maxExternalUsers).toBeGreaterThan(0);
        expect(plan.limits.maxLibraries).toBeGreaterThan(0);
        expect(plan.limits.apiCallsPerMonth).toBeGreaterThan(0);
        expect(plan.limits.maxAdmins).toBeGreaterThan(0);
        
        // Check features
        expect(plan.features).toBeDefined();
      });
    });
  });

  describe('Plan Limits', () => {
    it('should have Starter plan with correct limits', () => {
      const limits = getPlanLimits('Starter');
      
      expect(limits.maxClientSpaces).toBe(5);
      expect(limits.auditRetentionDays).toBe(30);
      expect(limits.supportLevel).toBe('community');
      expect(limits.maxExternalUsers).toBe(50);
      expect(limits.maxLibraries).toBe(25);
      expect(limits.apiCallsPerMonth).toBe(10000);
      expect(limits.maxAdmins).toBe(2);
    });

    it('should have Professional plan with correct limits', () => {
      const limits = getPlanLimits('Professional');
      
      expect(limits.maxClientSpaces).toBe(20);
      expect(limits.auditRetentionDays).toBe(90);
      expect(limits.supportLevel).toBe('email');
      expect(limits.maxExternalUsers).toBe(250);
      expect(limits.maxLibraries).toBe(100);
      expect(limits.apiCallsPerMonth).toBe(50000);
      expect(limits.maxAdmins).toBe(5);
    });

    it('should have Business plan with correct limits', () => {
      const limits = getPlanLimits('Business');
      
      expect(limits.maxClientSpaces).toBe(100);
      expect(limits.auditRetentionDays).toBe(365);
      expect(limits.supportLevel).toBe('priority');
      expect(limits.maxExternalUsers).toBe(1000);
      expect(limits.maxLibraries).toBe(500);
      expect(limits.apiCallsPerMonth).toBe(250000);
      expect(limits.maxAdmins).toBe(15);
    });

    it('should have Enterprise plan with correct limits', () => {
      const limits = getPlanLimits('Enterprise');
      
      expect(limits.maxClientSpaces).toBe(999999);
      expect(limits.auditRetentionDays).toBe(2555);
      expect(limits.supportLevel).toBe('dedicated');
      expect(limits.maxExternalUsers).toBe(999999);
      expect(limits.maxLibraries).toBe(999999);
      expect(limits.apiCallsPerMonth).toBe(999999);
      expect(limits.maxAdmins).toBe(999);
      expect(limits.unlimitedClientSpaces).toBe(true);
      expect(limits.unlimitedAuditRetention).toBe(true);
    });

    it('should have increasing limits from Starter to Enterprise', () => {
      const starter = getPlanLimits('Starter');
      const professional = getPlanLimits('Professional');
      const business = getPlanLimits('Business');
      const enterprise = getPlanLimits('Enterprise');
      
      expect(professional.maxClientSpaces).toBeGreaterThan(starter.maxClientSpaces);
      expect(business.maxClientSpaces).toBeGreaterThan(professional.maxClientSpaces);
      expect(enterprise.maxClientSpaces).toBeGreaterThan(business.maxClientSpaces);
      
      expect(professional.auditRetentionDays).toBeGreaterThan(starter.auditRetentionDays);
      expect(business.auditRetentionDays).toBeGreaterThan(professional.auditRetentionDays);
      expect(enterprise.auditRetentionDays).toBeGreaterThan(business.auditRetentionDays);
      
      expect(professional.maxExternalUsers).toBeGreaterThan(starter.maxExternalUsers);
      expect(business.maxExternalUsers).toBeGreaterThan(professional.maxExternalUsers);
      expect(enterprise.maxExternalUsers).toBeGreaterThan(business.maxExternalUsers);
    });
  });

  describe('Plan Features', () => {
    it('should have Starter plan with basic features only', () => {
      const features = getPlanFeatures('Starter');
      
      expect(features.auditExport).toBe(false);
      expect(features.bulkOperations).toBe(false);
      expect(features.advancedReporting).toBe(false);
      expect(features.customPolicies).toBe(false);
      expect(features.apiAccess).toBe(false);
      expect(features.scheduledReviews).toBe(false);
      expect(features.ssoIntegration).toBe(false);
      expect(features.customBranding).toBe(false);
    });

    it('should have Professional plan with intermediate features', () => {
      const features = getPlanFeatures('Professional');
      
      expect(features.auditExport).toBe(true);
      expect(features.bulkOperations).toBe(true);
      expect(features.advancedReporting).toBe(false);
      expect(features.customPolicies).toBe(true);
      expect(features.apiAccess).toBe(true);
      expect(features.scheduledReviews).toBe(false);
      expect(features.ssoIntegration).toBe(false);
      expect(features.customBranding).toBe(false);
    });

    it('should have Business plan with advanced features', () => {
      const features = getPlanFeatures('Business');
      
      expect(features.auditExport).toBe(true);
      expect(features.bulkOperations).toBe(true);
      expect(features.advancedReporting).toBe(true);
      expect(features.customPolicies).toBe(true);
      expect(features.apiAccess).toBe(true);
      expect(features.scheduledReviews).toBe(true);
      expect(features.ssoIntegration).toBe(true);
      expect(features.customBranding).toBe(false);
    });

    it('should have Enterprise plan with all features', () => {
      const features = getPlanFeatures('Enterprise');
      
      expect(features.auditExport).toBe(true);
      expect(features.bulkOperations).toBe(true);
      expect(features.advancedReporting).toBe(true);
      expect(features.customPolicies).toBe(true);
      expect(features.apiAccess).toBe(true);
      expect(features.scheduledReviews).toBe(true);
      expect(features.ssoIntegration).toBe(true);
      expect(features.customBranding).toBe(true);
    });
  });

  describe('Unlimited Flags', () => {
    it('should identify Enterprise as having unlimited client spaces', () => {
      expect(isUnlimited('Enterprise', 'maxClientSpaces')).toBe(true);
    });

    it('should identify Enterprise as having unlimited audit retention', () => {
      expect(isUnlimited('Enterprise', 'auditRetentionDays')).toBe(true);
    });

    it('should identify non-Enterprise plans as not having unlimited limits', () => {
      expect(isUnlimited('Starter', 'maxClientSpaces')).toBe(false);
      expect(isUnlimited('Professional', 'maxClientSpaces')).toBe(false);
      expect(isUnlimited('Business', 'maxClientSpaces')).toBe(false);
      
      expect(isUnlimited('Starter', 'auditRetentionDays')).toBe(false);
      expect(isUnlimited('Professional', 'auditRetentionDays')).toBe(false);
      expect(isUnlimited('Business', 'auditRetentionDays')).toBe(false);
    });
  });

  describe('Feature Availability', () => {
    it('should correctly identify feature availability', () => {
      expect(hasFeature('Starter', 'auditExport')).toBe(false);
      expect(hasFeature('Professional', 'auditExport')).toBe(true);
      expect(hasFeature('Business', 'auditExport')).toBe(true);
      expect(hasFeature('Enterprise', 'auditExport')).toBe(true);
      
      expect(hasFeature('Starter', 'advancedReporting')).toBe(false);
      expect(hasFeature('Professional', 'advancedReporting')).toBe(false);
      expect(hasFeature('Business', 'advancedReporting')).toBe(true);
      expect(hasFeature('Enterprise', 'advancedReporting')).toBe(true);
    });

    it('should find minimum tier for features', () => {
      expect(getMinimumTierForFeature('auditExport')).toBe('Professional');
      expect(getMinimumTierForFeature('bulkOperations')).toBe('Professional');
      expect(getMinimumTierForFeature('advancedReporting')).toBe('Business');
      expect(getMinimumTierForFeature('customBranding')).toBe('Enterprise');
    });
  });

  describe('Support Levels', () => {
    it('should have correct support levels for each tier', () => {
      expect(getPlanLimits('Starter').supportLevel).toBe('community');
      expect(getPlanLimits('Professional').supportLevel).toBe('email');
      expect(getPlanLimits('Business').supportLevel).toBe('priority');
      expect(getPlanLimits('Enterprise').supportLevel).toBe('dedicated');
    });
  });

  describe('Pricing', () => {
    it('should have increasing pricing from Starter to Enterprise', () => {
      const starter = getPlanDefinition('Starter');
      const professional = getPlanDefinition('Professional');
      const business = getPlanDefinition('Business');
      const enterprise = getPlanDefinition('Enterprise');
      
      expect(professional.pricing.monthly).toBeGreaterThan(starter.pricing.monthly);
      expect(business.pricing.monthly).toBeGreaterThan(professional.pricing.monthly);
      expect(enterprise.pricing.monthly).toBeGreaterThan(business.pricing.monthly);
      
      expect(professional.pricing.annual).toBeGreaterThan(starter.pricing.annual);
      expect(business.pricing.annual).toBeGreaterThan(professional.pricing.annual);
      expect(enterprise.pricing.annual).toBeGreaterThan(business.pricing.annual);
    });

    it('should have annual pricing at approximately 10 months of monthly price', () => {
      const tiers = getAllPlanTiers();
      
      tiers.forEach(tier => {
        const plan = getPlanDefinition(tier);
        const expectedAnnual = plan.pricing.monthly * 10;
        
        // Allow for some variance in pricing strategy
        expect(plan.pricing.annual).toBeGreaterThanOrEqual(expectedAnnual * 0.9);
        expect(plan.pricing.annual).toBeLessThanOrEqual(expectedAnnual * 1.1);
      });
    });
  });
});
