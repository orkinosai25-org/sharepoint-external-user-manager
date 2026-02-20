# Rate Limiting Configuration

## Overview

The SharePoint External User Manager API implements per-tenant rate limiting to prevent abuse and ensure fair usage across all tenants. Rate limiting is enforced at the API gateway level using ASP.NET Core 8's built-in rate limiting middleware.

## Configuration

### Rate Limit Policy

- **Limit**: 100 requests per minute per tenant
- **Window**: Fixed window of 1 minute
- **Partitioning**: Based on tenant ID (`tid` claim from JWT token)
- **Queue**: No queueing - requests exceeding the limit are immediately rejected

### Implementation Details

The rate limiter is configured in `Program.cs` using the following settings:

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        // Extract tenant ID from JWT claims
        var tenantId = context.User?.FindFirst("tid")?.Value ?? "anonymous";
        
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: tenantId,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });
});
```

### Tenant Isolation

Each tenant's rate limit is independent and isolated from other tenants:

- **Authenticated users**: Rate limited by their tenant ID (`tid` claim)
- **Anonymous users**: All anonymous requests share the same rate limit partition ("anonymous")

This ensures that one tenant's high traffic does not affect other tenants' access to the API.

## Error Response

When a client exceeds the rate limit, the API returns:

**Status Code**: `429 Too Many Requests`

**Response Body**:
```json
{
  "error": "RATE_LIMIT_EXCEEDED",
  "message": "Too many requests. Please try again later.",
  "retryAfter": "60 seconds"
}
```

## Best Practices for Clients

1. **Implement Exponential Backoff**: When receiving a 429 response, wait before retrying
2. **Respect the Retry-After Header**: Wait at least 60 seconds before making new requests
3. **Batch Operations**: Group multiple operations into single API calls when possible
4. **Cache Responses**: Cache API responses client-side to reduce redundant requests
5. **Monitor Rate Limit Usage**: Track your request patterns to stay within limits

## Adjusting Rate Limits

To modify the rate limit configuration:

1. Update the `PermitLimit` value in `Program.cs`
2. Adjust the `Window` timespan if needed (e.g., `TimeSpan.FromHours(1)` for hourly limits)
3. Rebuild and redeploy the application

### Example: Change to 200 requests per minute

```csharp
PermitLimit = 200,
Window = TimeSpan.FromMinutes(1)
```

### Example: Change to 1000 requests per hour

```csharp
PermitLimit = 1000,
Window = TimeSpan.FromHours(1)
```

## Monitoring

Rate limit violations are logged with the following information:

```
Rate limit exceeded for tenant {TenantId} on path {Path}
```

Monitor these logs to identify:
- Tenants approaching or exceeding limits
- Endpoints receiving excessive traffic
- Potential abuse patterns

## Testing

The rate limiting functionality is tested in `RateLimitingTests.cs`:

- **Integration Tests**: Verify rate limit enforcement in real scenarios
- **Logic Tests**: Ensure tenant ID extraction and partition isolation works correctly

Run tests with:
```bash
dotnet test --filter "FullyQualifiedName~RateLimiting"
```

## Related Documentation

- [ASP.NET Core Rate Limiting](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit)
- [Global Exception Handling](./GLOBAL_EXCEPTION_MIDDLEWARE_GUIDE.md)
- [Authentication Configuration](./AZURE_AD_APP_SETUP.md)

## Troubleshooting

### Issue: Rate limit too restrictive for legitimate use

**Solution**: Analyze actual usage patterns and increase the `PermitLimit` if needed. Consider implementing different limits for different plan tiers.

### Issue: Rate limit not working

**Verification**:
1. Ensure `UseRateLimiter()` is called in the middleware pipeline
2. Verify JWT token contains `tid` claim
3. Check logs for rate limit violations

### Issue: All requests being rate limited together

**Cause**: Tenant ID not being extracted from claims

**Solution**: Verify JWT token structure and ensure `tid` claim is present and accessible.
