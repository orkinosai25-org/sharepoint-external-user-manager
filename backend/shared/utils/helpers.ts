import { v4 as uuidv4 } from 'uuid';

/**
 * Generate correlation ID
 */
export function generateCorrelationId(): string {
  return uuidv4();
}

/**
 * Sanitize tenant ID for use in database names
 */
export function sanitizeTenantId(tenantId: string): string {
  return tenantId.replace(/[^a-z0-9]/gi, '_').toLowerCase();
}

/**
 * Calculate pagination metadata
 */
export function calculatePagination(
  page: number,
  pageSize: number,
  total: number
): {
  page: number;
  pageSize: number;
  total: number;
  totalPages: number;
  hasNext: boolean;
  hasPrevious: boolean;
} {
  const totalPages = Math.ceil(total / pageSize);
  
  return {
    page,
    pageSize,
    total,
    totalPages,
    hasNext: page < totalPages,
    hasPrevious: page > 1
  };
}

/**
 * Parse pagination parameters from query string
 */
export function parsePaginationParams(
  params: URLSearchParams
): { page: number; pageSize: number } {
  const page = Math.max(1, parseInt(params.get('page') || '1', 10));
  const pageSize = Math.min(100, Math.max(1, parseInt(params.get('pageSize') || '50', 10)));
  
  return { page, pageSize };
}

/**
 * Get client IP address from request
 */
export function getClientIp(headers: Record<string, string>): string {
  return headers['x-forwarded-for'] || 
         headers['x-real-ip'] || 
         'unknown';
}
