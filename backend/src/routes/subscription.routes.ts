import express, { Router, Response } from 'express';
import { authMiddleware } from '../middleware/auth.middleware';
import { tenantMiddleware, TenantRequest } from '../middleware/tenant.middleware';
import { getSubscriptionTier } from '../config/subscription-tiers.config';

const router: Router = express.Router();

// GET /api/v1/subscription - Get current subscription details
router.get('/', authMiddleware, tenantMiddleware, async (req: TenantRequest, res: Response) => {
  try {
    const tier = req.tenant?.subscription_tier || 'Free';
    const tierConfig = getSubscriptionTier(tier);
    
    return res.json({
      success: true,
      data: {
        subscription_id: 'sub-abc123',
        tenant_id: req.tenant?.tenant_id,
        tier,
        status: 'Active',
        billing_cycle: 'Monthly',
        price_per_month: tier === 'Pro' ? 49.00 : tier === 'Enterprise' ? 199.00 : 0,
        currency: 'USD',
        start_date: new Date('2024-01-01').toISOString(),
        renewal_date: new Date('2024-02-01').toISOString(),
        auto_renew: true,
        limits: {
          max_external_users: tierConfig.limits.maxExternalUsers,
          audit_log_retention_days: tierConfig.limits.auditLogRetentionDays,
          api_rate_limit: tierConfig.limits.apiRateLimit,
          advanced_policies: tierConfig.limits.advancedPolicies,
          support_level: tierConfig.limits.supportLevel
        },
        usage: {
          external_users_count: 5,
          external_users_percentage: tierConfig.limits.maxExternalUsers === -1 ? 0 : (5 / tierConfig.limits.maxExternalUsers) * 100,
          api_calls_this_month: 1250,
          storage_used_gb: 2.5
        }
      }
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      error: {
        code: 'INTERNAL_ERROR',
        message: 'Failed to retrieve subscription'
      }
    });
  }
});

// POST /api/v1/subscription/upgrade - Upgrade subscription tier
router.post('/upgrade', authMiddleware, tenantMiddleware, async (req: TenantRequest, res: Response) => {
  try {
    const { new_tier, billing_cycle } = req.body;
    
    if (!new_tier || !['Pro', 'Enterprise'].includes(new_tier)) {
      return res.status(400).json({
        success: false,
        error: {
          code: 'VALIDATION_ERROR',
          message: 'Invalid tier. Must be Pro or Enterprise'
        }
      });
    }
    
    // In production: Process upgrade, update database, integrate with payment provider
    
    return res.json({
      success: true,
      data: {
        subscription_id: 'sub-abc123',
        tier: new_tier,
        status: 'Active',
        billing_cycle: billing_cycle || 'Monthly',
        price_per_year: new_tier === 'Enterprise' ? 1990.00 : 490.00,
        currency: 'USD',
        upgrade_date: new Date().toISOString(),
        next_billing_date: new Date(Date.now() + 365 * 24 * 60 * 60 * 1000).toISOString()
      }
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      error: {
        code: 'INTERNAL_ERROR',
        message: 'Failed to upgrade subscription'
      }
    });
  }
});

// POST /api/v1/subscription/cancel - Cancel subscription
router.post('/cancel', authMiddleware, tenantMiddleware, async (req: TenantRequest, res: Response) => {
  try {
    const { reason, feedback } = req.body;
    
    // In production: Process cancellation, update database
    
    return res.json({
      success: true,
      data: {
        subscription_id: 'sub-abc123',
        status: 'Cancelled',
        cancellation_date: new Date().toISOString(),
        service_end_date: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString(),
        message: 'Your subscription will remain active until service end date'
      }
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      error: {
        code: 'INTERNAL_ERROR',
        message: 'Failed to cancel subscription'
      }
    });
  }
});

export default router;
