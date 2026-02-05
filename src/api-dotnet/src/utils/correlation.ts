/**
 * Correlation ID generation and management
 */

import { v4 as uuidv4 } from 'uuid';
import { HttpRequest } from '@azure/functions';

export function generateCorrelationId(): string {
  return uuidv4();
}

export function extractCorrelationId(req: HttpRequest): string {
  const headerValue = req.headers.get('x-correlation-id') || 
                      req.headers.get('X-Correlation-ID');
  
  return headerValue || generateCorrelationId();
}

export function attachCorrelationId(req: HttpRequest): string {
  const correlationId = extractCorrelationId(req);
  (req as any).correlationId = correlationId;
  return correlationId;
}
