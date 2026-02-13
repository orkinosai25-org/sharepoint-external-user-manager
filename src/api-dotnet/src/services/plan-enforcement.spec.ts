/**
 * Tests for plan enforcement service
 */

import {
  getTenantPlan,
  getTenantPlanLimits,
  getTenantPlanFeatures,
  checkFeatureAccess,
  checkClientSpaceLimit,
  checkExternalUserLimit,
  checkLibraryLimit,
  checkApiCallLimit,
  getAuditRetentionDays,
  shouldRetainAudit,
  getSupportLevel,
  enforceFeatureAccess,
  enforceClientSpaceLimit,
  enforceExternalUserLimit,
  hasMinimumTier,
  checkGlobalSearchAccess,
  checkFullTextSearchAccess,
  checkAdvancedSearchFiltersAccess,
  enforceGlobalSearchAccess,
  enforceFullTextSearchAccess,
  enforceAdvancedSearchFiltersAccess
} from '../services/plan-enforcement';
import { TenantContext } from '../models/common';
import { PlanTier } from '../models/plan';
import { FeatureNotAvailableError, ForbiddenError } from '../models/common';

describe('Plan Enforcement Service', () => {
  describe('Tenant Plan Detection', () => {
    it('should map Free tier to Starter plan', () => {
      const context: TenantContext = {
        tenantId: 1,
        entraIdTenantId: 'test-tenant-id',
        userId: 'user-123',
        userEmail: 'test@example.com',
        roles: ['Owner'],
        subscriptionTier: 'Free'
      };

      expect(getTenantPlan(context)).toBe('Starter');
    });

    it('should map Pro tier to Professional plan', () => {
      const context: TenantContext = {
        tenantId: 1,
        entraIdTenantId: 'test-tenant-id',
        userId: 'user-123',
        userEmail: 'test@example.com',
        roles: ['Owner'],
        subscriptionTier: 'Pro'
      };

      expect(getTenantPlan(context)).toBe('Professional');
    });

    it('should map Enterprise tier to Enterprise plan', () => {
      const context: TenantContext = {
        tenantId: 1,
        entraIdTenantId: 'test-tenant-id',
        userId: 'user-123',
        userEmail: 'test@example.com',
        roles: ['Owner'],
        subscriptionTier: 'Enterprise'
      };

      expect(getTenantPlan(context)).toBe('Enterprise');
    });

    it('should handle new plan tier names directly', () => {
      const starterContext: TenantContext = {
        tenantId: 1,
        entraIdTenantId: 'test-tenant-id',
        userId: 'user-123',
        userEmail: 'test@example.com',
        roles: ['Owner'],
        subscriptionTier: 'Starter' as any
      };

      expect(getTenantPlan(starterContext)).toBe('Starter');
    });
  });

  describe('Plan Limits and Features', () => {
    it('should get correct limits for tenant plan', () => {
      const context: TenantContext = {
        tenantId: 1,
        entraIdTenantId: 'test-tenant-id',
        userId: 'user-123',
        userEmail: 'test@example.com',
        roles: ['Owner'],
        subscriptionTier: 'Pro'
      };

      const limits = getTenantPlanLimits(context);
      
      expect(limits.maxClientSpaces).toBe(20);
      expect(limits.auditRetentionDays).toBe(90);
      expect(limits.supportLevel).toBe('email');
    });

    it('should get correct features for tenant plan', () => {
      const context: TenantContext = {
        tenantId: 1,
        entraIdTenantId: 'test-tenant-id',
        userId: 'user-123',
        userEmail: 'test@example.com',
        roles: ['Owner'],
        subscriptionTier: 'Pro'
      };

      const features = getTenantPlanFeatures(context);
      
      expect(features.auditExport).toBe(true);
      expect(features.bulkOperations).toBe(true);
      expect(features.advancedReporting).toBe(false);
    });
  });

  describe('Feature Access Checking', () => {
    it('should allow access to features available in tenant plan', () => {
      const context: TenantContext = {
        tenantId: 1,
        entraIdTenantId: 'test-tenant-id',
        userId: 'user-123',
        userEmail: 'test@example.com',
        roles: ['Owner'],
        subscriptionTier: 'Pro'
      };

      const result = checkFeatureAccess(context, 'auditExport');
      
      expect(result.allowed).toBe(true);
    });

    it('should deny access to features not available in tenant plan', () => {
      const context: TenantContext = {
        tenantId: 1,
        entraIdTenantId: 'test-tenant-id',
        userId: 'user-123',
        userEmail: 'test@example.com',
        roles: ['Owner'],
        subscriptionTier: 'Free'
      };

      const result = checkFeatureAccess(context, 'auditExport');
      
      expect(result.allowed).toBe(false);
      expect(result.reason).toContain('auditExport');
      expect(result.requiredTier).toBe('Professional');
    });

    it('should enforce feature access and throw error when not allowed', () => {
      const context: TenantContext = {
        tenantId: 1,
        entraIdTenantId: 'test-tenant-id',
        userId: 'user-123',
        userEmail: 'test@example.com',
        roles: ['Owner'],
        subscriptionTier: 'Free'
      };

      expect(() => {
        enforceFeatureAccess(context, 'customBranding');
      }).toThrow(FeatureNotAvailableError);
    });
  });

  describe('Client Space Limit Checking', () => {
    it('should allow creating client space when under limit', () => {
      const context: TenantContext = {
        tenantId: 1,
        entraIdTenantId: 'test-tenant-id',
        userId: 'user-123',
        userEmail: 'test@example.com',
        roles: ['Owner'],
        subscriptionTier: 'Free'
      };

      const result = checkClientSpaceLimit(context, 3);
      
      expect(result.allowed).toBe(true);
      expect(result.currentUsage).toBe(3);
      expect(result.limit).toBe(5);
    });

    it('should deny creating client space when at limit', () => {
      const context: TenantContext = {
        tenantId: 1,
        entraIdTenantId: 'test-tenant-id',
        userId: 'user-123',
        userEmail: 'test@example.com',
        roles: ['Owner'],
        subscriptionTier: 'Free'
      };

      const result = checkClientSpaceLimit(context, 5);
      
      expect(result.allowed).toBe(false);
      expect(result.reason).toContain('Starter');
      expect(result.currentUsage).toBe(5);
      expect(result.limit).toBe(5);
      expect(result.requiredTier).toBe('Professional');
    });

    it('should allow unlimited client spaces for Enterprise plan', () => {
      const context: TenantContext = {
        tenantId: 1,
        entraIdTenantId: 'test-tenant-id',
        userId: 'user-123',
        userEmail: 'test@example.com',
        roles: ['Owner'],
        subscriptionTier: 'Enterprise'
      };

      const result = checkClientSpaceLimit(context, 1000000);
      
      expect(result.allowed).toBe(true);
    });

    it('should enforce client space limit and throw error when exceeded', () => {
      const context: TenantContext = {
        tenantId: 1,
        entraIdTenantId: 'test-tenant-id',
        userId: 'user-123',
        userEmail: 'test@example.com',
        roles: ['Owner'],
        subscriptionTier: 'Free'
      };

      expect(() => {
        enforceClientSpaceLimit(context, 10);
      }).toThrow(ForbiddenError);
    });
  });

  describe('External User Limit Checking', () => {
    it('should allow adding user when under limit', () => {
      const context: TenantContext = {
        tenantId: 1,
        entraIdTenantId: 'test-tenant-id',
        userId: 'user-123',
        userEmail: 'test@example.com',
        roles: ['Owner'],
        subscriptionTier: 'Free'
      };

      const result = checkExternalUserLimit(context, 30);
      
      expect(result.allowed).toBe(true);
      expect(result.currentUsage).toBe(30);
      expect(result.limit).toBe(50);
    });

    it('should deny adding user when at limit', () => {
      const context: TenantContext = {
        tenantId: 1,
        entraIdTenantId: 'test-tenant-id',
        userId: 'user-123',
        userEmail: 'test@example.com',
        roles: ['Owner'],
        subscriptionTier: 'Free'
      };

      const result = checkExternalUserLimit(context, 50);
      
      expect(result.allowed).toBe(false);
      expect(result.reason).toContain('Starter');
      expect(result.limit).toBe(50);
    });

    it('should enforce external user limit and throw error when exceeded', () => {
      const context: TenantContext = {
        tenantId: 1,
        entraIdTenantId: 'test-tenant-id',
        userId: 'user-123',
        userEmail: 'test@example.com',
        roles: ['Owner'],
        subscriptionTier: 'Free'
      };

      expect(() => {
        enforceExternalUserLimit(context, 100);
      }).toThrow(ForbiddenError);
    });
  });

  describe('Library Limit Checking', () => {
    it('should allow adding library when under limit', () => {
      const context: TenantContext = {
        tenantId: 1,
        entraIdTenantId: 'test-tenant-id',
        userId: 'user-123',
        userEmail: 'test@example.com',
        roles: ['Owner'],
        subscriptionTier: 'Free'
      };

      const result = checkLibraryLimit(context, 15);
      
      expect(result.allowed).toBe(true);
      expect(result.currentUsage).toBe(15);
      expect(result.limit).toBe(25);
    });

    it('should deny adding library when at limit', () => {
      const context: TenantContext = {
        tenantId: 1,
        entraIdTenantId: 'test-tenant-id',
        userId: 'user-123',
        userEmail: 'test@example.com',
        roles: ['Owner'],
        subscriptionTier: 'Free'
      };

      const result = checkLibraryLimit(context, 25);
      
      expect(result.allowed).toBe(false);
      expect(result.reason).toContain('Starter');
      expect(result.limit).toBe(25);
    });
  });

  describe('API Call Limit Checking', () => {
    it('should allow API call when under limit', () => {
      const context: TenantContext = {
        tenantId: 1,
        entraIdTenantId: 'test-tenant-id',
        userId: 'user-123',
        userEmail: 'test@example.com',
        roles: ['Owner'],
        subscriptionTier: 'Free'
      };

      const result = checkApiCallLimit(context, 5000);
      
      expect(result.allowed).toBe(true);
      expect(result.currentUsage).toBe(5000);
      expect(result.limit).toBe(10000);
    });

    it('should deny API call when at limit', () => {
      const context: TenantContext = {
        tenantId: 1,
        entraIdTenantId: 'test-tenant-id',
        userId: 'user-123',
        userEmail: 'test@example.com',
        roles: ['Owner'],
        subscriptionTier: 'Free'
      };

      const result = checkApiCallLimit(context, 10000);
      
      expect(result.allowed).toBe(false);
      expect(result.reason).toContain('Starter');
      expect(result.limit).toBe(10000);
    });
  });

  describe('Audit Retention', () => {
    it('should get correct audit retention days for plan', () => {
      const starterContext: TenantContext = {
        tenantId: 1,
        entraIdTenantId: 'test-tenant-id',
        userId: 'user-123',
        userEmail: 'test@example.com',
        roles: ['Owner'],
        subscriptionTier: 'Free'
      };

      expect(getAuditRetentionDays(starterContext)).toBe(30);

      const proContext: TenantContext = {
        ...starterContext,
        subscriptionTier: 'Pro'
      };

      expect(getAuditRetentionDays(proContext)).toBe(90);
    });

    it('should retain audit logs within retention period', () => {
      const context: TenantContext = {
        tenantId: 1,
        entraIdTenantId: 'test-tenant-id',
        userId: 'user-123',
        userEmail: 'test@example.com',
        roles: ['Owner'],
        subscriptionTier: 'Free'
      };

      const recentDate = new Date();
      recentDate.setDate(recentDate.getDate() - 15);

      expect(shouldRetainAudit(context, recentDate)).toBe(true);
    });

    it('should not retain audit logs outside retention period', () => {
      const context: TenantContext = {
        tenantId: 1,
        entraIdTenantId: 'test-tenant-id',
        userId: 'user-123',
        userEmail: 'test@example.com',
        roles: ['Owner'],
        subscriptionTier: 'Free'
      };

      const oldDate = new Date();
      oldDate.setDate(oldDate.getDate() - 60);

      expect(shouldRetainAudit(context, oldDate)).toBe(false);
    });

    it('should retain all audit logs for Enterprise with unlimited retention', () => {
      const context: TenantContext = {
        tenantId: 1,
        entraIdTenantId: 'test-tenant-id',
        userId: 'user-123',
        userEmail: 'test@example.com',
        roles: ['Owner'],
        subscriptionTier: 'Enterprise'
      };

      const veryOldDate = new Date();
      veryOldDate.setFullYear(veryOldDate.getFullYear() - 10);

      expect(shouldRetainAudit(context, veryOldDate)).toBe(true);
    });
  });

  describe('Support Level', () => {
    it('should return correct support level for each plan', () => {
      const starterContext: TenantContext = {
        tenantId: 1,
        entraIdTenantId: 'test-tenant-id',
        userId: 'user-123',
        userEmail: 'test@example.com',
        roles: ['Owner'],
        subscriptionTier: 'Free'
      };

      expect(getSupportLevel(starterContext)).toBe('community');

      const proContext: TenantContext = {
        ...starterContext,
        subscriptionTier: 'Pro'
      };

      expect(getSupportLevel(proContext)).toBe('email');

      const enterpriseContext: TenantContext = {
        ...starterContext,
        subscriptionTier: 'Enterprise'
      };

      expect(getSupportLevel(enterpriseContext)).toBe('dedicated');
    });
  });

  describe('Minimum Tier Checking', () => {
    it('should correctly identify if tenant meets minimum tier', () => {
      const proContext: TenantContext = {
        tenantId: 1,
        entraIdTenantId: 'test-tenant-id',
        userId: 'user-123',
        userEmail: 'test@example.com',
        roles: ['Owner'],
        subscriptionTier: 'Pro'
      };

      expect(hasMinimumTier(proContext, 'Starter')).toBe(true);
      expect(hasMinimumTier(proContext, 'Professional')).toBe(true);
      expect(hasMinimumTier(proContext, 'Business')).toBe(false);
      expect(hasMinimumTier(proContext, 'Enterprise')).toBe(false);
    });

    it('should correctly identify Enterprise as meeting all tiers', () => {
      const enterpriseContext: TenantContext = {
        tenantId: 1,
        entraIdTenantId: 'test-tenant-id',
        userId: 'user-123',
        userEmail: 'test@example.com',
        roles: ['Owner'],
        subscriptionTier: 'Enterprise'
      };

      expect(hasMinimumTier(enterpriseContext, 'Starter')).toBe(true);
      expect(hasMinimumTier(enterpriseContext, 'Professional')).toBe(true);
      expect(hasMinimumTier(enterpriseContext, 'Business')).toBe(true);
      expect(hasMinimumTier(enterpriseContext, 'Enterprise')).toBe(true);
    });
  });

  describe('Search Feature Access', () => {
    describe('checkGlobalSearchAccess', () => {
      it('should deny global search for Starter tier', () => {
        const context: TenantContext = {
          tenantId: 1,
          entraIdTenantId: 'test-tenant-id',
          userId: 'user-123',
          userEmail: 'test@example.com',
          roles: ['User'],
          subscriptionTier: 'Free'
        };

        const result = checkGlobalSearchAccess(context);
        expect(result.allowed).toBe(false);
        expect(result.reason).toContain('Professional');
        expect(result.requiredTier).toBe('Professional');
      });

      it('should allow global search for Professional tier', () => {
        const context: TenantContext = {
          tenantId: 1,
          entraIdTenantId: 'test-tenant-id',
          userId: 'user-123',
          userEmail: 'test@example.com',
          roles: ['User'],
          subscriptionTier: 'Pro'
        };

        const result = checkGlobalSearchAccess(context);
        expect(result.allowed).toBe(true);
      });

      it('should allow global search for Enterprise tier', () => {
        const context: TenantContext = {
          tenantId: 1,
          entraIdTenantId: 'test-tenant-id',
          userId: 'user-123',
          userEmail: 'test@example.com',
          roles: ['User'],
          subscriptionTier: 'Enterprise'
        };

        const result = checkGlobalSearchAccess(context);
        expect(result.allowed).toBe(true);
      });
    });

    describe('checkFullTextSearchAccess', () => {
      it('should deny full-text search for Starter tier', () => {
        const context: TenantContext = {
          tenantId: 1,
          entraIdTenantId: 'test-tenant-id',
          userId: 'user-123',
          userEmail: 'test@example.com',
          roles: ['User'],
          subscriptionTier: 'Free'
        };

        const result = checkFullTextSearchAccess(context);
        expect(result.allowed).toBe(false);
        expect(result.requiredTier).toBe('Professional');
      });

      it('should allow full-text search for Professional tier', () => {
        const context: TenantContext = {
          tenantId: 1,
          entraIdTenantId: 'test-tenant-id',
          userId: 'user-123',
          userEmail: 'test@example.com',
          roles: ['User'],
          subscriptionTier: 'Pro'
        };

        const result = checkFullTextSearchAccess(context);
        expect(result.allowed).toBe(true);
      });
    });

    describe('checkAdvancedSearchFiltersAccess', () => {
      it('should deny advanced filters for Starter tier', () => {
        const context: TenantContext = {
          tenantId: 1,
          entraIdTenantId: 'test-tenant-id',
          userId: 'user-123',
          userEmail: 'test@example.com',
          roles: ['User'],
          subscriptionTier: 'Free'
        };

        const result = checkAdvancedSearchFiltersAccess(context);
        expect(result.allowed).toBe(false);
      });

      it('should allow advanced filters for Professional tier', () => {
        const context: TenantContext = {
          tenantId: 1,
          entraIdTenantId: 'test-tenant-id',
          userId: 'user-123',
          userEmail: 'test@example.com',
          roles: ['User'],
          subscriptionTier: 'Pro'
        };

        const result = checkAdvancedSearchFiltersAccess(context);
        expect(result.allowed).toBe(true);
      });
    });

    describe('enforceGlobalSearchAccess', () => {
      it('should throw error for Starter tier', () => {
        const context: TenantContext = {
          tenantId: 1,
          entraIdTenantId: 'test-tenant-id',
          userId: 'user-123',
          userEmail: 'test@example.com',
          roles: ['User'],
          subscriptionTier: 'Free'
        };

        expect(() => enforceGlobalSearchAccess(context)).toThrow(FeatureNotAvailableError);
      });

      it('should not throw error for Professional tier', () => {
        const context: TenantContext = {
          tenantId: 1,
          entraIdTenantId: 'test-tenant-id',
          userId: 'user-123',
          userEmail: 'test@example.com',
          roles: ['User'],
          subscriptionTier: 'Pro'
        };

        expect(() => enforceGlobalSearchAccess(context)).not.toThrow();
      });
    });

    describe('enforceFullTextSearchAccess', () => {
      it('should throw error for Starter tier', () => {
        const context: TenantContext = {
          tenantId: 1,
          entraIdTenantId: 'test-tenant-id',
          userId: 'user-123',
          userEmail: 'test@example.com',
          roles: ['User'],
          subscriptionTier: 'Free'
        };

        expect(() => enforceFullTextSearchAccess(context)).toThrow(FeatureNotAvailableError);
      });

      it('should not throw error for Professional tier', () => {
        const context: TenantContext = {
          tenantId: 1,
          entraIdTenantId: 'test-tenant-id',
          userId: 'user-123',
          userEmail: 'test@example.com',
          roles: ['User'],
          subscriptionTier: 'Pro'
        };

        expect(() => enforceFullTextSearchAccess(context)).not.toThrow();
      });
    });

    describe('enforceAdvancedSearchFiltersAccess', () => {
      it('should throw error for Starter tier', () => {
        const context: TenantContext = {
          tenantId: 1,
          entraIdTenantId: 'test-tenant-id',
          userId: 'user-123',
          userEmail: 'test@example.com',
          roles: ['User'],
          subscriptionTier: 'Free'
        };

        expect(() => enforceAdvancedSearchFiltersAccess(context)).toThrow(FeatureNotAvailableError);
      });

      it('should not throw error for Professional tier', () => {
        const context: TenantContext = {
          tenantId: 1,
          entraIdTenantId: 'test-tenant-id',
          userId: 'user-123',
          userEmail: 'test@example.com',
          roles: ['User'],
          subscriptionTier: 'Pro'
        };

        expect(() => enforceAdvancedSearchFiltersAccess(context)).not.toThrow();
      });
    });
  });
});
