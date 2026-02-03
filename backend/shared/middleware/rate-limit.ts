import { HttpRequest, InvocationContext } from '@azure/functions';
import { ApiResponse, ErrorCode } from '../models/types';

interface RateLimitEntry {
  count: number;
  resetTime: number;
}

// In-memory rate limit storage (use Redis in production)
const rateLimitStore = new Map<string, RateLimitEntry>();

/**
 * Rate limiting middleware
 */
export async function checkRateLimit(
  request: HttpRequest,
  tenantId: string,
  context: InvocationContext,
  limit: number = 100,
  windowSeconds: number = 60
): Promise<{ allowed: boolean; response?: ApiResponse<never>; headers?: Record<string, string> }> {
  try {
    const key = `ratelimit:${tenantId}`;
    const now = Date.now();
    const windowMs = windowSeconds * 1000;

    // Get or create rate limit entry
    let entry = rateLimitStore.get(key);
    
    if (!entry || entry.resetTime < now) {
      // Create new window
      entry = {
        count: 0,
        resetTime: now + windowMs
      };
      rateLimitStore.set(key, entry);
    }

    // Increment count
    entry.count++;

    // Calculate headers
    const remaining = Math.max(0, limit - entry.count);
    const resetTime = Math.floor(entry.resetTime / 1000);
    const retryAfter = Math.ceil((entry.resetTime - now) / 1000);

    const headers = {
      'X-RateLimit-Limit': limit.toString(),
      'X-RateLimit-Remaining': remaining.toString(),
      'X-RateLimit-Reset': resetTime.toString()
    };

    // Check if limit exceeded
    if (entry.count > limit) {
      return {
        allowed: false,
        headers: {
          ...headers,
          'X-RateLimit-Retry-After': retryAfter.toString()
        },
        response: {
          success: false,
          error: {
            code: ErrorCode.RATE_LIMIT_EXCEEDED,
            message: 'Rate limit exceeded',
            details: `Too many requests. Please try again in ${retryAfter} seconds.`,
            timestamp: new Date().toISOString()
          }
        }
      };
    }

    return {
      allowed: true,
      headers
    };
  } catch (error) {
    context.error('Error checking rate limit', error);
    // On error, allow request but log
    return { allowed: true };
  }
}

/**
 * Clean up old rate limit entries (call periodically)
 */
export function cleanupRateLimitStore(): void {
  const now = Date.now();
  for (const [key, entry] of rateLimitStore.entries()) {
    if (entry.resetTime < now) {
      rateLimitStore.delete(key);
    }
  }
}

// Clean up every 5 minutes
setInterval(cleanupRateLimitStore, 5 * 60 * 1000);
