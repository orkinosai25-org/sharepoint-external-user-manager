import { app, HttpRequest, HttpResponseInit, InvocationContext } from '@azure/functions';
import { v4 as uuidv4 } from 'uuid';
import Joi from 'joi';
import { createTenant, tenantExists } from '../shared/storage/tenant-repository';
import { createTenantSubscription } from '../shared/storage/subscription-repository';
import { createAuditLog } from '../shared/storage/audit-repository';
import { validateToken, getTenantId } from '../shared/auth/jwt-validator';
import { handleError, ValidationError, ConflictError } from '../shared/middleware/error-handler';
import { generateCorrelationId } from '../shared/utils/helpers';
import { Tenant, Subscription, ApiResponse } from '../shared/models/types';

/**
 * POST /tenants/onboard
 * Onboard a new tenant to the platform
 */
async function onboardTenant(request: HttpRequest, context: InvocationContext): Promise<HttpResponseInit> {
  const correlationId = generateCorrelationId();
  
  try {
    // Validate authentication
    const tokenPayload = await validateToken(request, context);
    if (!tokenPayload) {
      return {
        status: 401,
        jsonBody: {
          success: false,
          error: {
            code: 'UNAUTHORIZED',
            message: 'Authentication required',
            correlationId
          }
        }
      };
    }

    // Parse and validate request body
    const body = await request.json() as any;
    const schema = Joi.object({
      tenantId: Joi.string().required(),
      adminEmail: Joi.string().email().required(),
      companyName: Joi.string().required(),
      subscriptionTier: Joi.string().valid('trial', 'pro', 'enterprise').default('trial'),
      dataLocation: Joi.string().valid('eastus', 'westeurope', 'southeastasia').default('eastus')
    });

    const { error, value } = schema.validate(body);
    if (error) {
      throw new ValidationError('Invalid request body', error.details[0].message);
    }

    // Check if tenant already exists
    const exists = await tenantExists(value.tenantId);
    if (exists) {
      throw new ConflictError('Tenant already exists', `Tenant ${value.tenantId} is already onboarded`);
    }

    // Create tenant record
    const tenant: Tenant = {
      id: uuidv4(),
      tenantId: value.tenantId,
      displayName: value.companyName,
      status: 'active',
      subscriptionTier: value.subscriptionTier,
      subscriptionStatus: 'active',
      trialEndDate: value.subscriptionTier === 'trial' 
        ? new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString() // 30 days from now
        : undefined,
      billingEmail: value.adminEmail,
      adminEmail: value.adminEmail,
      createdDate: new Date().toISOString(),
      lastModifiedDate: new Date().toISOString(),
      settings: {
        apiBaseUrl: 'https://api.spexternal.com/v1',
        features: {
          auditExport: value.subscriptionTier === 'enterprise',
          bulkOperations: value.subscriptionTier !== 'trial',
          advancedReporting: value.subscriptionTier === 'enterprise',
          customPolicies: value.subscriptionTier === 'enterprise'
        }
      },
      azureAdAppId: tokenPayload.aud,
      onboardingCompleted: true,
      dataLocation: value.dataLocation
    };

    const createdTenant = await createTenant(tenant);

    // Create subscription record
    const subscription: Subscription = {
      id: uuidv4(),
      tenantId: value.tenantId,
      tier: value.subscriptionTier,
      status: 'active',
      startDate: new Date().toISOString(),
      endDate: tenant.trialEndDate,
      autoRenew: false,
      billingCycle: 'monthly',
      pricing: {
        amount: value.subscriptionTier === 'pro' ? 49 : value.subscriptionTier === 'enterprise' ? 199 : 0,
        currency: 'USD',
        perSeat: false
      },
      limits: getLimitsForTier(value.subscriptionTier),
      usage: {
        externalUsersCount: 0,
        librariesCount: 0,
        apiCallsThisMonth: 0,
        storageUsedMB: 0
      },
      createdDate: new Date().toISOString(),
      lastModifiedDate: new Date().toISOString()
    };

    await createTenantSubscription(subscription);

    // Create audit log
    await createAuditLog({
      correlationId,
      tenantId: value.tenantId,
      eventType: 'tenant.onboarded',
      actor: {
        email: value.adminEmail,
        displayName: tokenPayload.name
      },
      action: 'POST /tenants/onboard',
      status: 'success',
      target: {
        resourceType: 'tenant',
        resourceId: createdTenant.id,
        email: value.adminEmail
      },
      metadata: {
        subscriptionTier: value.subscriptionTier,
        dataLocation: value.dataLocation
      }
    });

    // Return response
    const response: ApiResponse<Partial<Tenant>> = {
      success: true,
      data: {
        tenantId: createdTenant.tenantId,
        displayName: createdTenant.displayName,
        status: createdTenant.status,
        subscriptionTier: createdTenant.subscriptionTier,
        trialEndDate: createdTenant.trialEndDate,
        onboardingCompleted: createdTenant.onboardingCompleted,
        createdDate: createdTenant.createdDate
      }
    };

    return {
      status: 201,
      jsonBody: response
    };
  } catch (error) {
    const errorResponse = handleError(error, context, correlationId);
    const status = errorResponse.error?.code === 'VALIDATION_ERROR' ? 400 :
                   errorResponse.error?.code === 'CONFLICT' ? 409 : 500;
    
    return {
      status,
      jsonBody: errorResponse
    };
  }
}

/**
 * Get subscription limits based on tier
 */
function getLimitsForTier(tier: string): any {
  const limits = {
    trial: {
      maxExternalUsers: 25,
      maxLibraries: 10,
      apiCallsPerMonth: 10000,
      auditRetentionDays: 30,
      maxAdmins: 3
    },
    pro: {
      maxExternalUsers: 500,
      maxLibraries: 100,
      apiCallsPerMonth: 100000,
      auditRetentionDays: 365,
      maxAdmins: 10
    },
    enterprise: {
      maxExternalUsers: 999999,
      maxLibraries: 999999,
      apiCallsPerMonth: 999999,
      auditRetentionDays: 2555,
      maxAdmins: 999
    }
  };

  return limits[tier as keyof typeof limits] || limits.trial;
}

// Register HTTP trigger
app.http('tenantOnboard', {
  methods: ['POST'],
  route: 'tenants/onboard',
  authLevel: 'anonymous',
  handler: onboardTenant
});
