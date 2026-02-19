# Global Exception Middleware - Implementation Guide

## Overview

The Global Exception Middleware provides consistent error handling across the entire SharePoint External User Manager API. It ensures that all unhandled exceptions are caught, logged with correlation IDs and tenant context, and returned in a standardized JSON format.

## Features

### ✅ Consistent Error Response Format

All errors follow this standard format:

```json
{
  "error": "ERROR_CODE",
  "message": "Human-readable error message",
  "correlationId": "abc-123-def-456"
}
```

In Development mode, an additional `details` field includes the full stack trace:

```json
{
  "error": "ERROR_CODE",
  "message": "Human-readable error message",
  "correlationId": "abc-123-def-456",
  "details": "Full stack trace..."
}
```

### ✅ Comprehensive Logging

All exceptions are logged with:
- **CorrelationId**: Unique identifier for tracking requests
- **TenantId**: Multi-tenant isolation (from JWT `tid` claim)
- **UserId**: User identification (from JWT `oid` claim)
- **Request Path**: The endpoint that threw the exception
- **Exception Details**: Full exception information

Example log output:
```
Unhandled exception occurred. CorrelationId: 7d4f8a2b-1c3e-4567-89ab-cdef01234567, TenantId: contoso-tenant-id, UserId: user-object-id, Path: /api/clients/123
```

### ✅ Exception Type Mapping

The middleware maps specific exception types to appropriate HTTP status codes and error codes:

| Exception Type | HTTP Status | Error Code | Use Case |
|---------------|-------------|------------|----------|
| `UnauthorizedAccessException` | 403 Forbidden | `ACCESS_DENIED` | Insufficient permissions |
| `InvalidOperationException` (with "limit") | 403 Forbidden | `PLAN_LIMIT_EXCEEDED` | Plan resource limits exceeded |
| `KeyNotFoundException` | 404 Not Found | `NOT_FOUND` | Resource doesn't exist |
| `ArgumentNullException` | 400 Bad Request | `INVALID_INPUT` | Missing required parameter |
| `ArgumentException` | 400 Bad Request | `INVALID_INPUT` | Invalid parameter value |
| `NotImplementedException` | 501 Not Implemented | `NOT_IMPLEMENTED` | Feature not available |
| `TimeoutException` | 408 Request Timeout | `TIMEOUT` | Request took too long |
| All other exceptions | 500 Internal Server Error | `INTERNAL_ERROR` | Unexpected errors |

## Implementation

### Middleware Location

**File**: `/src/api-dotnet/WebApi/SharePointExternalUserManager.Api/Middleware/GlobalExceptionMiddleware.cs`

### Registration in Pipeline

The middleware is registered early in the request pipeline in `Program.cs`:

```csharp
var app = builder.Build();

// Global exception handling middleware (must be early in pipeline)
app.UseGlobalExceptionHandler();

// Other middleware...
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

**Important**: The middleware must be registered **before** authentication and authorization to catch all exceptions.

## Usage Examples

### Throwing Custom Exceptions in Controllers

Controllers can throw specific exceptions that will be automatically handled:

```csharp
// Plan limit exceeded
if (clientCount >= maxClients)
{
    throw new InvalidOperationException($"Client limit exceeded for your plan. Upgrade to create more spaces.");
}

// Resource not found
var client = await _context.Clients.FindAsync(id);
if (client == null)
{
    throw new KeyNotFoundException($"Client with ID {id} not found");
}

// Unauthorized access
if (client.TenantId != tenantId)
{
    throw new UnauthorizedAccessException("You do not have permission to access this client");
}

// Invalid input
if (string.IsNullOrWhiteSpace(request.Name))
{
    throw new ArgumentNullException(nameof(request.Name), "Client name is required");
}
```

### Error Response Examples

**404 Not Found**:
```json
{
  "error": "NOT_FOUND",
  "message": "The requested resource was not found.",
  "correlationId": "7d4f8a2b-1c3e-4567-89ab-cdef01234567"
}
```

**403 Plan Limit Exceeded**:
```json
{
  "error": "PLAN_LIMIT_EXCEEDED",
  "message": "Client limit exceeded for your plan. Upgrade to create more spaces.",
  "correlationId": "8e5g9b3c-2d4f-5678-90bc-defg12345678"
}
```

**400 Invalid Input**:
```json
{
  "error": "INVALID_INPUT",
  "message": "Required parameter missing: Name",
  "correlationId": "9f6h0c4d-3e5g-6789-01cd-efgh23456789"
}
```

**500 Internal Error**:
```json
{
  "error": "INTERNAL_ERROR",
  "message": "An unexpected error occurred. Please try again later.",
  "correlationId": "0g7i1d5e-4f6h-7890-12de-fghi34567890"
}
```

## Testing

### Unit Tests

Comprehensive unit tests are located at:
**File**: `/src/api-dotnet/WebApi/SharePointExternalUserManager.Api.Tests/Middleware/GlobalExceptionMiddlewareTests.cs`

The test suite includes 16 tests covering:
- ✅ All exception type mappings
- ✅ Correlation ID generation
- ✅ Tenant/User logging (authenticated & anonymous)
- ✅ Development/Production mode differences
- ✅ Response format validation (camelCase)
- ✅ Request path logging

Run tests with:
```bash
cd src/api-dotnet/WebApi/SharePointExternalUserManager.Api.Tests
dotnet test --filter "FullyQualifiedName~GlobalExceptionMiddlewareTests"
```

### Manual Testing

To manually test the middleware:

1. Start the API in Development mode
2. Trigger an error (e.g., request a non-existent resource)
3. Verify the response format
4. Check Application Insights or logs for correlation ID

## Security Considerations

### ✅ Information Disclosure Prevention

- **Production Mode**: Stack traces are **never** included in error responses
- **Development Mode**: Stack traces are included for debugging
- Generic error messages prevent revealing system internals
- Sensitive information is never logged in error messages

### ✅ Tenant Isolation

- Tenant context (from JWT claims) is always logged
- Helps audit trail and security investigations
- Supports multi-tenant isolation requirements

### ✅ Correlation IDs

- Every error gets a unique correlation ID
- Allows tracking requests across services
- Facilitates troubleshooting without exposing sensitive data
- Logged on both client and server side

## Best Practices

### Do's ✅

1. **Throw specific exceptions** for different error scenarios
2. **Include descriptive messages** in exceptions
3. **Use correlation IDs** when reporting errors to users
4. **Log before throwing** if additional context is needed
5. **Test error paths** to ensure proper handling

### Don'ts ❌

1. **Don't catch and swallow exceptions** without re-throwing
2. **Don't include sensitive data** in exception messages
3. **Don't use generic Exception** when specific types are appropriate
4. **Don't bypass the middleware** with custom error handling in controllers
5. **Don't log PII** (Personally Identifiable Information) in error messages

## Troubleshooting

### Finding Errors by Correlation ID

When a user reports an error:

1. Get the correlation ID from their error response
2. Search Application Insights or logs for the correlation ID
3. Find the full exception details with stack trace
4. Identify the root cause and fix

Example query:
```kusto
traces
| where message contains "CorrelationId: 7d4f8a2b-1c3e-4567-89ab-cdef01234567"
| project timestamp, message, exception
```

### Common Issues

**Issue**: Stack traces visible in production
- **Solution**: Verify `ASPNETCORE_ENVIRONMENT` is set to "Production"

**Issue**: Correlation IDs not appearing in logs
- **Solution**: Ensure middleware is registered before other middleware

**Issue**: Custom error messages not working
- **Solution**: Check that exceptions are thrown from within the middleware pipeline

## Integration with Other Features

### Audit Logging

The middleware works alongside the `AuditLogService` to provide comprehensive tracking:
- **Middleware**: Catches unhandled exceptions
- **AuditLogService**: Logs successful operations

### Plan Enforcement

The middleware recognizes plan limit exceptions:
```csharp
throw new InvalidOperationException("limit exceeded...");
// Automatically mapped to PLAN_LIMIT_EXCEEDED
```

### Multi-Tenant Isolation

Tenant context from JWT tokens is automatically extracted and logged with every error, supporting the multi-tenant architecture.

## Performance Impact

- **Minimal overhead**: Only active when exceptions occur
- **No impact on successful requests**: Pass-through on normal flow
- **Efficient logging**: Structured logging with minimal serialization
- **JSON serialization**: Uses System.Text.Json (fastest option)

## Acceptance Criteria ✅

All requirements from ISSUE 6 are met:

- ✅ **Consistent error responses**: Standardized JSON format
- ✅ **Return format**: `{ "error", "message", "correlationId" }`
- ✅ **Log correlationId**: Every error logged with unique ID
- ✅ **Include tenantId in logs**: Extracted from JWT claims
- ✅ **Early pipeline registration**: Catches all exceptions
- ✅ **Comprehensive test coverage**: 16 unit tests

## Future Enhancements

Potential improvements (not in current scope):

1. **Rate limiting for errors**: Prevent error spam attacks
2. **Error aggregation**: Group similar errors in monitoring
3. **Retry policies**: Automatic retry for transient failures
4. **Circuit breaker**: Fail fast when downstream services are down
5. **Custom error codes**: Domain-specific error codes for API consumers

## Related Documentation

- [ISSUE_06_IMPLEMENTATION_COMPLETE.md](../ISSUE_06_IMPLEMENTATION_COMPLETE.md) - Previous Issue 6 (Libraries/Lists)
- [Program.cs](../src/api-dotnet/WebApi/SharePointExternalUserManager.Api/Program.cs) - Middleware registration
- [GlobalExceptionMiddleware.cs](../src/api-dotnet/WebApi/SharePointExternalUserManager.Api/Middleware/GlobalExceptionMiddleware.cs) - Implementation

---

**Status**: ✅ Complete and Production-Ready  
**Version**: 1.0  
**Last Updated**: 2026-02-19
