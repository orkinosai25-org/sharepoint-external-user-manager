# Pricing & Plans System

## Overview

The SaaS backend now includes a comprehensive pricing and plans system with four distinct tiers designed to meet the needs of organizations of all sizes.

## Plan Tiers

### Starter
**Best for:** Small teams getting started with external user management

**Pricing:** $29/month or $290/year

**Limits:**
- Client Spaces: 5
- External Users: 50
- Libraries: 25
- Audit Retention: 30 days
- Support Level: Community
- API Calls: 10,000/month
- Admins: 2

**Features:**
- Basic external user management
- Library access control
- Basic audit logs
- Email notifications

### Professional
**Best for:** Growing businesses with advanced needs

**Pricing:** $99/month or $990/year

**Limits:**
- Client Spaces: 20
- External Users: 250
- Libraries: 100
- Audit Retention: 90 days
- Support Level: Email
- API Calls: 50,000/month
- Admins: 5

**Features:**
- All Starter features, plus:
- Audit export
- Bulk operations
- Custom policies
- API access
- Advanced permissions

### Business
**Best for:** Established organizations requiring comprehensive features

**Pricing:** $299/month or $2,990/year

**Limits:**
- Client Spaces: 100
- External Users: 1,000
- Libraries: 500
- Audit Retention: 365 days
- Support Level: Priority
- API Calls: 250,000/month
- Admins: 15

**Features:**
- All Professional features, plus:
- Advanced reporting
- Scheduled reviews
- SSO integration
- Priority support
- Enhanced audit capabilities

### Enterprise
**Best for:** Large organizations requiring unlimited resources

**Pricing:** $999/month or $9,990/year

**Limits:**
- Client Spaces: **Unlimited**
- External Users: Unlimited
- Libraries: Unlimited
- Audit Retention: **Unlimited**
- Support Level: Dedicated
- API Calls: Unlimited
- Admins: 999

**Features:**
- All Business features, plus:
- Custom branding
- Dedicated support
- Unlimited resources
- Advanced security features
- SLA guarantees

## Architecture

### Plan Definitions (`plan.ts`)

The core plan definitions include:
- `PlanTier`: Type for plan tiers (Starter, Professional, Business, Enterprise)
- `PlanLimits`: Interface defining resource limits per plan
- `PlanFeatures`: Interface defining feature availability per plan
- `PLAN_DEFINITIONS`: Complete plan configuration

Key functions:
- `getPlanDefinition(tier)`: Get full plan details
- `getPlanLimits(tier)`: Get resource limits for a tier
- `getPlanFeatures(tier)`: Get feature flags for a tier
- `isUnlimited(tier, limitName)`: Check if a limit is unlimited
- `hasFeature(tier, featureName)`: Check if a feature is available

### Plan Enforcement (`plan-enforcement.ts`)

The enforcement service provides:
- Tenant plan detection and mapping
- Feature access validation
- Resource limit checking
- Quota enforcement

Key functions:
- `getTenantPlan(context)`: Determine tenant's active plan
- `checkFeatureAccess(context, feature)`: Verify feature availability
- `checkClientSpaceLimit(context, count)`: Validate client space quota
- `checkExternalUserLimit(context, count)`: Validate user quota
- `enforceFeatureAccess(context, feature)`: Throw error if feature unavailable
- `enforceClientSpaceLimit(context, count)`: Throw error if quota exceeded

## Implementation Guide

### Checking Feature Access

```typescript
import { enforceFeatureAccess } from '../services/plan-enforcement';

// In your API handler
async function exportAuditLogs(request: HttpRequest, context: TenantContext) {
  // This will throw FeatureNotAvailableError if tenant doesn't have access
  enforceFeatureAccess(context, 'auditExport');
  
  // Proceed with export...
}
```

### Checking Resource Limits

```typescript
import { enforceClientSpaceLimit } from '../services/plan-enforcement';

async function createClientSpace(request: HttpRequest, context: TenantContext) {
  const currentSpaces = await getClientSpaceCount(context.tenantId);
  
  // This will throw ForbiddenError if limit exceeded
  enforceClientSpaceLimit(context, currentSpaces);
  
  // Proceed with creation...
}
```

### Checking Without Throwing

```typescript
import { checkFeatureAccess, checkClientSpaceLimit } from '../services/plan-enforcement';

async function getAccountStatus(context: TenantContext) {
  const currentSpaces = await getClientSpaceCount(context.tenantId);
  const spaceCheck = checkClientSpaceLimit(context, currentSpaces);
  
  return {
    plan: getTenantPlan(context),
    clientSpaces: {
      current: spaceCheck.currentUsage,
      limit: spaceCheck.limit,
      canAddMore: spaceCheck.allowed
    },
    features: {
      auditExport: checkFeatureAccess(context, 'auditExport').allowed
    }
  };
}
```

## Backward Compatibility

The system maintains backward compatibility with legacy tier names:
- `Free` → `Starter`
- `Pro` → `Professional`
- `Enterprise` → `Enterprise`

Helper functions in `subscription.ts`:
- `mapSubscriptionToPlanTier(tier)`: Convert legacy to new tier
- `mapPlanToSubscriptionTier(tier)`: Convert new to legacy tier

## Testing

Comprehensive test suites are provided:
- `plan.spec.ts`: Tests for plan definitions and structure
- `plan-enforcement.spec.ts`: Tests for enforcement logic

Run tests:
```bash
npm test -- plan.spec.ts
npm test -- plan-enforcement.spec.ts
```

## Key Features

### Unlimited Flags

Enterprise plan includes special "unlimited" flags for:
- `unlimitedClientSpaces`: No limit on client spaces
- `unlimitedAuditRetention`: Audit logs retained indefinitely

Use `isUnlimited()` to check these flags:
```typescript
if (isUnlimited(tier, 'maxClientSpaces')) {
  // Skip limit check for Enterprise
}
```

### Support Levels

Each plan has a defined support level:
- **Community**: Self-service documentation and community forums
- **Email**: Email support with 48-hour response time
- **Priority**: Priority email/phone support with 24-hour response
- **Dedicated**: Dedicated account manager and 4-hour response SLA

### Audit Retention

Audit retention automatically enforced:
```typescript
import { shouldRetainAudit } from '../services/plan-enforcement';

async function cleanupAuditLogs(context: TenantContext) {
  const audits = await getAllAuditLogs(context.tenantId);
  
  for (const audit of audits) {
    if (!shouldRetainAudit(context, audit.timestamp)) {
      await deleteAuditLog(audit.id);
    }
  }
}
```

## Future Enhancements

Potential areas for expansion:
1. Usage-based pricing for certain features
2. Add-on modules (e.g., advanced analytics, integrations)
3. Regional pricing variations
4. Custom enterprise plans with negotiated terms
5. Pay-as-you-go options for API calls
6. Partner/reseller pricing tiers
