/**
 * CORS configuration middleware
 */

import { HttpRequest, HttpResponseInit } from '@azure/functions';
import { config } from '../utils/config';

export function getCorsHeaders(req: HttpRequest): Record<string, string> {
  const origin = req.headers.get('origin') || '';
  const allowedOrigins = config.cors.allowedOrigins;

  // Check if origin is allowed
  const isAllowed = allowedOrigins.some(allowed => {
    // Support wildcards
    if (allowed.includes('*')) {
      const pattern = new RegExp('^' + allowed.replace(/\*/g, '.*') + '$');
      return pattern.test(origin);
    }
    return allowed === origin;
  });

  // Default allowed origin patterns for SharePoint
  const sharePointPatterns = [
    /^https:\/\/.*\.sharepoint\.com$/,
    /^https:\/\/.*\.sharepoint-df\.com$/ // GCC
  ];

  const isSharePointOrigin = sharePointPatterns.some(pattern => pattern.test(origin));

  if (!isAllowed && !isSharePointOrigin && origin !== '') {
    console.warn(`CORS: Origin ${origin} not in allowed list`);
  }

  const headers: Record<string, string> = {
    'Access-Control-Allow-Origin': (isAllowed || isSharePointOrigin) ? origin : '',
    'Access-Control-Allow-Methods': 'GET, POST, PUT, DELETE, OPTIONS',
    'Access-Control-Allow-Headers': 'Content-Type, Authorization, X-Correlation-ID',
    'Access-Control-Allow-Credentials': 'true',
    'Access-Control-Max-Age': '86400'
  };

  return headers;
}

export function handleCorsPreFlight(req: HttpRequest): HttpResponseInit | null {
  if (req.method === 'OPTIONS') {
    return {
      status: 204,
      headers: getCorsHeaders(req)
    };
  }
  return null;
}

export function applyCorsHeaders(response: HttpResponseInit, req: HttpRequest): HttpResponseInit {
  const corsHeaders = getCorsHeaders(req);
  
  return {
    ...response,
    headers: {
      ...response.headers,
      ...corsHeaders
    }
  };
}
