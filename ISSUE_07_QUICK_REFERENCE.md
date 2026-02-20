# ISSUE 7 — Rate Limiting Quick Reference

## What Was Implemented

Per-tenant API rate limiting using ASP.NET Core 8's built-in rate limiter.

## Configuration

- **Limit**: 100 requests per minute
- **Scope**: Per tenant (based on JWT `tid` claim)
- **Algorithm**: Fixed window
- **Rejection**: Immediate (no queuing)

## Error Response

When rate limit exceeded (HTTP 429):
```json
{
  "error": "RATE_LIMIT_EXCEEDED",
  "message": "Too many requests. Please try again later.",
  "retryAfter": 60
}
```

## Key Files

### Modified
- `src/api-dotnet/WebApi/SharePointExternalUserManager.Api/Program.cs` - Rate limiting configuration
- `src/api-dotnet/WebApi/SharePointExternalUserManager.Api.Tests/SharePointExternalUserManager.Api.Tests.csproj` - Test dependencies

### Created
- `src/api-dotnet/WebApi/SharePointExternalUserManager.Api.Tests/Middleware/RateLimitingTests.cs` - 5 tests
- `RATE_LIMITING_CONFIGURATION.md` - Configuration guide
- `ISSUE_07_IMPLEMENTATION_COMPLETE.md` - Implementation summary
- `ISSUE_07_SECURITY_SUMMARY.md` - Security analysis
- `ISSUE_07_QUICK_REFERENCE.md` - This file

## How It Works

1. Request arrives at API
2. Rate limiter extracts tenant ID from JWT `tid` claim
3. Checks request count for this tenant in current 1-minute window
4. If < 100 requests: Allow request
5. If ≥ 100 requests: Return HTTP 429 with error JSON

## Testing

Run rate limiting tests:
```bash
cd src/api-dotnet/WebApi/SharePointExternalUserManager.Api.Tests
dotnet test --filter "FullyQualifiedName~RateLimiting"
```

Run all tests:
```bash
dotnet test
```

**Result**: 77/77 tests pass ✅

## Adjusting the Limit

In `Program.cs`, change:
```csharp
PermitLimit = 100,  // Change this number
Window = TimeSpan.FromMinutes(1)
```

## Monitoring

Watch logs for rate limit violations:
```
Rate limit exceeded for tenant {TenantId} on path {Path}
```

## Client Guidance

When you get HTTP 429:
1. Wait at least 60 seconds
2. Implement exponential backoff
3. Consider caching responses
4. Batch operations when possible

## Security

- ✅ CodeQL scan: 0 vulnerabilities
- ✅ Protects against DoS attacks
- ✅ Tenant isolation enforced
- ✅ Production ready

## Documentation

- **Configuration**: [RATE_LIMITING_CONFIGURATION.md](./RATE_LIMITING_CONFIGURATION.md)
- **Implementation**: [ISSUE_07_IMPLEMENTATION_COMPLETE.md](./ISSUE_07_IMPLEMENTATION_COMPLETE.md)
- **Security**: [ISSUE_07_SECURITY_SUMMARY.md](./ISSUE_07_SECURITY_SUMMARY.md)

## Summary

✅ 100 requests/minute per tenant  
✅ Tenant-isolated  
✅ Production-ready  
✅ Fully tested  
✅ Documented  
✅ Security verified
