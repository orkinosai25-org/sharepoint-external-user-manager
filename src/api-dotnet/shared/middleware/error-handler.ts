import { InvocationContext } from '@azure/functions';
import { ApiResponse, ErrorCode } from '../models/types';
import { v4 as uuidv4 } from 'uuid';

/**
 * Error handler middleware
 */
export function handleError(
  error: unknown,
  context: InvocationContext,
  correlationId?: string
): ApiResponse<never> {
  const id = correlationId || uuidv4();
  
  context.error(`Error ${id}:`, error);

  // Handle known error types
  if (error instanceof ValidationError) {
    return {
      success: false,
      error: {
        code: ErrorCode.VALIDATION_ERROR,
        message: error.message,
        details: error.details,
        correlationId: id,
        timestamp: new Date().toISOString()
      }
    };
  }

  if (error instanceof NotFoundError) {
    return {
      success: false,
      error: {
        code: ErrorCode.NOT_FOUND,
        message: error.message,
        details: error.details,
        correlationId: id,
        timestamp: new Date().toISOString()
      }
    };
  }

  if (error instanceof ConflictError) {
    return {
      success: false,
      error: {
        code: ErrorCode.CONFLICT,
        message: error.message,
        details: error.details,
        correlationId: id,
        timestamp: new Date().toISOString()
      }
    };
  }

  // Generic error
  return {
    success: false,
    error: {
      code: ErrorCode.INTERNAL_ERROR,
      message: 'An unexpected error occurred',
      details: 'Please try again later. If the problem persists, contact support.',
      correlationId: id,
      timestamp: new Date().toISOString()
    }
  };
}

/**
 * Custom error classes
 */
export class ValidationError extends Error {
  constructor(message: string, public details?: string) {
    super(message);
    this.name = 'ValidationError';
  }
}

export class NotFoundError extends Error {
  constructor(message: string, public details?: string) {
    super(message);
    this.name = 'NotFoundError';
  }
}

export class ConflictError extends Error {
  constructor(message: string, public details?: string) {
    super(message);
    this.name = 'ConflictError';
  }
}

export class UnauthorizedError extends Error {
  constructor(message: string, public details?: string) {
    super(message);
    this.name = 'UnauthorizedError';
  }
}

export class ForbiddenError extends Error {
  constructor(message: string, public details?: string) {
    super(message);
    this.name = 'ForbiddenError';
  }
}
