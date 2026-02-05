/**
 * GET /audit - Get audit logs with filtering
 */

import { app, HttpRequest, HttpResponseInit, InvocationContext } from '@azure/functions';
import { databaseService } from '../../services/database';
import { authenticateRequest } from '../../middleware/auth';
import { enforceSubscription } from '../../middleware/subscription';
import { validateQuery, auditLogQuerySchema } from '../../utils/validation';
import { attachCorrelationId } from '../../utils/correlation';
import { handleError } from '../../middleware/errorHandler';
import { handleCorsPreFlight, applyCorsHeaders } from '../../middleware/cors';
import { AuditLogRequest, AuditLogResponse } from '../../models/audit';
import { ApiResponse, PaginationInfo } from '../../models/common';
import { TIER_LIMITS } from '../../models/subscription';

async function getAuditLogs(req: HttpRequest, context: InvocationContext): Promise<HttpResponseInit> {
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

    // Enforce subscription
    await enforceSubscription(req, tenantContext);

    // Parse and validate query parameters
    const queryParams: Record<string, string> = {};
    req.query.forEach((value: string, key: string) => {
      queryParams[key] = value;
    });

    const validatedQuery = validateQuery<AuditLogRequest>(queryParams, auditLogQuerySchema);

    // Check audit history retention based on tier
    const tierLimits = TIER_LIMITS[tenantContext.subscriptionTier];
    const auditHistoryDays = tierLimits.auditHistoryDays;

    // Enforce audit history retention
    const now = new Date();
    const minDate = new Date(now.getTime() - auditHistoryDays * 24 * 60 * 60 * 1000);

    let startDate = validatedQuery.startDate ? new Date(validatedQuery.startDate) : minDate;
    if (startDate < minDate) {
      startDate = minDate;
    }

    const endDate = validatedQuery.endDate ? new Date(validatedQuery.endDate) : now;

    // Get audit logs from database
    const { logs, total } = await databaseService.getAuditLogs(tenantContext.tenantId, {
      action: validatedQuery.action,
      userId: validatedQuery.userId,
      startDate,
      endDate,
      page: validatedQuery.page || 1,
      pageSize: validatedQuery.pageSize || 50
    });

    // Format response
    const auditLogResponses: AuditLogResponse[] = logs.map(log => ({
      id: log.id,
      timestamp: log.timestamp.toISOString(),
      userId: log.userId,
      userEmail: log.userEmail,
      action: log.action,
      resourceType: log.resourceType,
      resourceId: log.resourceId,
      details: log.details,
      ipAddress: log.ipAddress,
      status: log.status
    }));

    const page = validatedQuery.page || 1;
    const pageSize = validatedQuery.pageSize || 50;
    const totalPages = Math.ceil(total / pageSize);

    const pagination: PaginationInfo = {
      page,
      pageSize,
      total,
      totalPages,
      hasNext: page < totalPages,
      hasPrev: page > 1
    };

    const response: ApiResponse<AuditLogResponse[]> = {
      success: true,
      data: auditLogResponses,
      pagination,
      meta: {
        correlationId,
        timestamp: new Date().toISOString()
      }
    };

    const successResponse = {
      status: 200,
      jsonBody: response,
      headers: {
        'Content-Type': 'application/json',
        'X-Correlation-ID': correlationId
      }
    };

    return applyCorsHeaders(successResponse, req);
  } catch (error) {
    const errorResponse = handleError(error, correlationId);
    return applyCorsHeaders(errorResponse, req);
  }
}

app.http('getAuditLogs', {
  methods: ['GET', 'OPTIONS'],
  authLevel: 'anonymous',
  route: 'audit',
  handler: getAuditLogs
});
