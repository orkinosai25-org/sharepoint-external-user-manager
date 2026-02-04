/**
 * Global error handler
 */

import { HttpResponseInit } from '@azure/functions';
import { AppError, ApiResponse } from '../models/common';

export function handleError(error: unknown, correlationId: string): HttpResponseInit {
  // Error logging would go here in production
  if (process.env.NODE_ENV !== 'test') {
    console.error('Error:', error);
  }

  if (error instanceof AppError) {
    const errorResponse: ApiResponse = {
      success: false,
      error: {
        code: error.code,
        message: error.message,
        details: error.details,
        correlationId
      }
    };

    return {
      status: error.statusCode,
      jsonBody: errorResponse,
      headers: {
        'Content-Type': 'application/json',
        'X-Correlation-ID': correlationId
      }
    };
  }

  // Unknown error
  const errorResponse: ApiResponse = {
    success: false,
    error: {
      code: 'INTERNAL_ERROR',
      message: 'An internal error occurred',
      details: error instanceof Error ? error.message : 'Unknown error',
      correlationId
    }
  };

  return {
    status: 500,
    jsonBody: errorResponse,
    headers: {
      'Content-Type': 'application/json',
      'X-Correlation-ID': correlationId
    }
  };
}

export function createSuccessResponse<T>(
  data: T,
  correlationId: string,
  status: number = 200
): HttpResponseInit {
  const response: ApiResponse<T> = {
    success: true,
    data,
    meta: {
      correlationId,
      timestamp: new Date().toISOString()
    }
  };

  return {
    status,
    jsonBody: response,
    headers: {
      'Content-Type': 'application/json',
      'X-Correlation-ID': correlationId
    }
  };
}
