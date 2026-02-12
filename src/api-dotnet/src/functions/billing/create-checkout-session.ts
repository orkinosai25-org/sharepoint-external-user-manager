/**
 * POST /billing/create-checkout-session - Create a Stripe checkout session
 */

import { app, HttpRequest, HttpResponseInit, InvocationContext } from '@azure/functions';
import { databaseService } from '../../services/database';
import { getStripeService } from '../../services/stripe-service';
import { authenticateRequest } from '../../middleware/auth';
import { attachCorrelationId } from '../../utils/correlation';
import { handleError, createSuccessResponse } from '../../middleware/errorHandler';
import { handleCorsPreFlight, applyCorsHeaders } from '../../middleware/cors';
import { ValidationError } from '../../models/common';
// eslint-disable-next-line @typescript-eslint/no-unused-vars
import { getPlanDefinition, PlanTier } from '../../models/plan';
// eslint-disable-next-line @typescript-eslint/no-unused-vars
import { getStripePriceId, BillingInterval } from '../../config/stripe-config';
import * as Joi from 'joi';

interface CreateCheckoutSessionRequest {
  priceId?: string;
  planTier?: 'Starter' | 'Professional' | 'Business';
  billingInterval?: 'month' | 'year';
  successUrl: string;
  cancelUrl: string;
}

const requestSchema = Joi.object({
  priceId: Joi.string().optional(),
  planTier: Joi.string().valid('Starter', 'Professional', 'Business').optional(),
  billingInterval: Joi.string().valid('month', 'year').optional(),
  successUrl: Joi.string().uri().required(),
  cancelUrl: Joi.string().uri().required()
}).xor('priceId', 'planTier')
  .with('planTier', 'billingInterval');

async function createCheckoutSession(req: HttpRequest, context: InvocationContext): Promise<HttpResponseInit> {
  const correlationId = attachCorrelationId(req);

  try {
    // Handle CORS preflight
    const corsResponse = handleCorsPreFlight(req);
    if (corsResponse) {
      return corsResponse;
    }

    // Ensure database is connected
    await databaseService.connect();

    // Authenticate request
    const tenantContext = await authenticateRequest(req, context);

    // Parse and validate request body
    const body = await req.json() as any;
    const { error, value } = requestSchema.validate(body);
    
    if (error) {
      throw new ValidationError('Invalid request body', error.details[0].message);
    }

    const validatedRequest = value as CreateCheckoutSessionRequest;

    // Determine the price ID to use
    let priceId: string | undefined;
    
    if (validatedRequest.priceId) {
      priceId = validatedRequest.priceId;
    } else if (validatedRequest.planTier && validatedRequest.billingInterval) {
      // Map planTier and billingInterval to Stripe price ID
      priceId = getStripePriceId(validatedRequest.planTier, validatedRequest.billingInterval);
      
      if (!priceId) {
        throw new ValidationError(
          'Invalid plan configuration',
          `No price ID found for ${validatedRequest.planTier} plan with ${validatedRequest.billingInterval} billing`
        );
      }
    }

    if (!priceId) {
      throw new ValidationError('Invalid request', 'Either priceId or planTier with billingInterval must be provided');
    }

    // Get tenant details
    const tenant = await databaseService.getTenantById(tenantContext.tenantId);
    if (!tenant) {
      throw new Error('Tenant not found');
    }

    // Initialize Stripe service
    const stripeService = getStripeService();

    // Validate price ID
    if (!stripeService.validatePriceId(priceId)) {
      throw new ValidationError('Invalid price ID', `The price ID ${priceId} is not valid`);
    }

    // Create checkout session
    const session = await stripeService.createCheckoutSession({
      priceId,
      customerEmail: tenant.primaryAdminEmail,
      successUrl: validatedRequest.successUrl,
      cancelUrl: validatedRequest.cancelUrl,
      metadata: {
        tenantId: tenantContext.tenantId.toString(),
        entraIdTenantId: tenant.entraIdTenantId,
        correlationId
      }
    });

    // Return session details
    const response = {
      sessionId: session.id,
      url: session.url,
      expiresAt: new Date(session.expires_at * 1000).toISOString()
    };

    const successResponse = createSuccessResponse(response, correlationId, 201);
    return applyCorsHeaders(successResponse, req);
  } catch (error) {
    const errorResponse = handleError(error, correlationId);
    return applyCorsHeaders(errorResponse, req);
  }
}

app.http('createCheckoutSession', {
  methods: ['POST', 'OPTIONS'],
  authLevel: 'anonymous',
  route: 'billing/create-checkout-session',
  handler: createCheckoutSession
});
