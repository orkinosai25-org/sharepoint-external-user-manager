# CORS Security Configuration Guide

## Overview

Cross-Origin Resource Sharing (CORS) is a security feature implemented in web browsers that restricts web pages from making requests to a different domain than the one that served the web page. This guide explains how CORS is configured in the SharePoint External User Manager API to securely allow cross-origin requests from authorized sources only.

## Security Principles

### ❌ What We DON'T Do (Insecure)

```csharp
// NEVER DO THIS IN PRODUCTION
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()  // ❌ SECURITY RISK
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
```

**Why this is dangerous:**
- Allows requests from ANY website on the internet
- Opens your API to Cross-Site Request Forgery (CSRF) attacks
- Violates security best practices
- Makes it impossible to track legitimate vs. malicious usage

### ✅ What We DO (Secure)

```csharp
// Secure CORS configuration with specific allowed origins
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedOrigins", policy =>
    {
        policy.WithOrigins(corsAllowedOrigins)  // ✅ Specific origins only
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});
```

**Why this is secure:**
- Only allows requests from explicitly configured origins
- Supports authentication cookies and credentials
- Easy to audit which origins have access
- Follows OWASP security recommendations

## Configuration

### Configuration Structure

CORS allowed origins are configured in `appsettings.json`:

```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://your-portal.azurewebsites.net",
      "https://*.sharepoint.com"
    ]
  }
}
```

### Configuration by Environment

#### 1. Development Environment

**File:** `appsettings.Development.json`

```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://localhost:5001",
      "http://localhost:5001",
      "https://localhost:7001",
      "http://localhost:7001"
    ]
  }
}
```

**Purpose:**
- Allow local development of Blazor portal
- Support both HTTP and HTTPS for local testing
- Common localhost ports for Portal (5001) and API (7071)

**Security Note:** These origins are safe because `localhost` is only accessible from the local machine.

#### 2. Production Environment

**File:** `appsettings.Production.json`

```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://your-portal.azurewebsites.net",
      "https://*.sharepoint.com"
    ]
  }
}
```

**Purpose:**
- Allow only your production Blazor portal URL
- Allow SharePoint Online sites (for future SPFx web parts)
- Block all other origins

**Important:** Replace `your-portal.azurewebsites.net` with your actual portal URL.

#### 3. Staging/Testing Environment

**File:** `appsettings.Staging.json`

```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://your-staging-portal.azurewebsites.net",
      "https://*.sharepoint.com"
    ]
  }
}
```

**Purpose:**
- Separate staging environment testing
- Prevents mixing staging and production traffic

## How It Works

### Request Flow

1. **Browser makes a request** from `https://portal.example.com` to `https://api.example.com`
2. **Browser sends preflight request** (OPTIONS method)
   - Contains `Origin: https://portal.example.com` header
3. **API checks CORS policy**
   - Compares origin against `AllowedOrigins` configuration
   - If match found: Returns appropriate CORS headers
   - If no match: Blocks the request
4. **Browser processes response**
   - If CORS headers present: Allows the actual request
   - If CORS headers missing: Blocks the request and shows error in console

### CORS Headers Returned

When origin is allowed:

```http
Access-Control-Allow-Origin: https://portal.example.com
Access-Control-Allow-Methods: GET, POST, PUT, DELETE, PATCH, OPTIONS
Access-Control-Allow-Headers: *
Access-Control-Allow-Credentials: true
```

### Behavior When No Origins Configured

If `AllowedOrigins` is empty or not configured:

- **Development:** Falls back to default localhost origins
- **Production:** CORS is effectively disabled (empty origins list)
- **Result:** Prevents accidental `AllowAnyOrigin` in production

```csharp
// Fallback behavior in code
if (corsAllowedOrigins.Length == 0 && builder.Environment.IsDevelopment())
{
    // Use localhost defaults for development
}
else if (corsAllowedOrigins.Length == 0)
{
    // Production with no origins = secure by default
    policy.WithOrigins(); // Empty list
}
```

## Environment Variables Override

You can configure CORS origins using environment variables (useful for Azure App Service):

### Single Origin

```bash
Cors__AllowedOrigins__0=https://portal.example.com
```

### Multiple Origins

```bash
Cors__AllowedOrigins__0=https://portal.example.com
Cors__AllowedOrigins__1=https://staging-portal.example.com
Cors__AllowedOrigins__2=https://*.sharepoint.com
```

### Azure App Service Configuration

In Azure Portal → App Service → Configuration → Application Settings:

| Name | Value |
|------|-------|
| `Cors__AllowedOrigins__0` | `https://your-portal.azurewebsites.net` |
| `Cors__AllowedOrigins__1` | `https://*.sharepoint.com` |

## Wildcard Domains

### SharePoint Online Sites

To allow all SharePoint Online sites (for SPFx web parts):

```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://*.sharepoint.com"
    ]
  }
}
```

**Note:** ASP.NET Core CORS middleware doesn't natively support wildcard domains in `WithOrigins()`. For production, you may need to implement custom CORS policy or list specific SharePoint tenant URLs.

### Custom Wildcard Implementation (If Needed)

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedOrigins", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
        {
            // Check exact matches
            if (corsAllowedOrigins.Contains(origin)) return true;
            
            // Check wildcard patterns
            foreach (var pattern in corsAllowedOrigins.Where(o => o.Contains("*")))
            {
                var regex = new Regex("^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$");
                if (regex.IsMatch(origin)) return true;
            }
            
            return false;
        })
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});
```

## Common Scenarios

### Scenario 1: Blazor Portal + API

**Architecture:**
- Blazor Portal: `https://portal.example.com`
- API: `https://api.example.com`

**Configuration:**

```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://portal.example.com"
    ]
  }
}
```

**Why:** Portal makes server-side API calls with authentication tokens.

### Scenario 2: SPFx Web Parts + API

**Architecture:**
- SPFx Web Parts hosted in: `https://contoso.sharepoint.com`
- API: `https://api.example.com`

**Configuration:**

```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://contoso.sharepoint.com",
      "https://contoso-my.sharepoint.com"
    ]
  }
}
```

**Why:** SPFx web parts run in browser context and make direct API calls.

### Scenario 3: Multiple Environments

**Configuration:**

```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://portal.example.com",
      "https://staging-portal.example.com",
      "https://dev-portal.example.com"
    ]
  }
}
```

**Why:** Support multiple deployment environments from single API.

## Troubleshooting

### Error: "CORS policy: No 'Access-Control-Allow-Origin' header"

**Symptom:** Browser console shows CORS error

**Causes:**
1. Origin not in `AllowedOrigins` configuration
2. CORS middleware not registered
3. CORS middleware in wrong position in pipeline

**Solutions:**
1. Add your origin to configuration
2. Verify `app.UseCors("AllowedOrigins")` is called in Program.cs
3. Ensure CORS is registered before authentication/authorization

### Error: "CORS policy: Credentials flag is true but 'Access-Control-Allow-Origin' is '*'"

**Symptom:** Can't use credentials with wildcard origins

**Cause:** Browser security restriction - can't use `AllowCredentials()` with `AllowAnyOrigin()`

**Solution:** This error should not occur with our implementation because we never use `AllowAnyOrigin()`. If you see this, check for custom CORS policies.

### CORS Works in Development but Not Production

**Symptom:** API calls work locally but fail in deployed environment

**Causes:**
1. Production origins not configured
2. Environment-specific configuration file missing
3. Azure App Service configuration not set

**Solutions:**
1. Add production URL to `appsettings.Production.json`
2. Set environment variables in Azure App Service
3. Verify configuration is loaded (check application logs)

## Testing CORS Configuration

### Using Browser DevTools

1. Open your application in browser
2. Open Developer Tools (F12)
3. Go to Network tab
4. Look for OPTIONS requests (preflight)
5. Check Response Headers:
   - `Access-Control-Allow-Origin` should match your origin
   - `Access-Control-Allow-Credentials` should be `true`

### Using curl

```bash
# Test preflight request
curl -X OPTIONS https://api.example.com/api/health \
  -H "Origin: https://portal.example.com" \
  -H "Access-Control-Request-Method: GET" \
  -i

# Expected response includes:
# Access-Control-Allow-Origin: https://portal.example.com
# Access-Control-Allow-Methods: GET, POST, PUT, DELETE, ...
```

### Using Postman

1. Postman doesn't enforce CORS (it's a browser security feature)
2. For CORS testing, use an actual browser or curl
3. Postman is good for testing API functionality, not CORS

## Security Best Practices

### ✅ DO

1. **Use specific origins only**
   - List exact URLs that need access
   - Update configuration when adding new clients

2. **Use HTTPS in production**
   - Never allow HTTP origins in production
   - Only `https://` URLs should be configured

3. **Review origins regularly**
   - Audit `AllowedOrigins` quarterly
   - Remove deprecated/unused origins

4. **Use environment-specific configuration**
   - Different origins for dev/staging/prod
   - Never mix environments

5. **Monitor CORS requests**
   - Log failed CORS attempts
   - Alert on unusual patterns

### ❌ DON'T

1. **Never use `AllowAnyOrigin()` in production**
   - Defeats the purpose of CORS
   - Opens security vulnerabilities

2. **Never combine `AllowAnyOrigin()` with `AllowCredentials()`**
   - Browser will reject the configuration
   - Security anti-pattern

3. **Don't allow HTTP origins in production**
   - Credentials sent over HTTP can be intercepted
   - Always use HTTPS

4. **Don't use overly broad wildcards**
   - `*` allows everything
   - `*.com` is too broad

5. **Don't commit secrets in CORS configuration**
   - Origins are public information
   - But ensure origin URLs are legitimate

## Compliance Considerations

### OWASP Top 10

CORS configuration addresses:
- **A05:2021 – Security Misconfiguration**
  - Proper CORS settings prevent misconfigurations
- **A07:2021 – Identification and Authentication Failures**
  - Combined with authentication, prevents unauthorized access

### GDPR/Privacy

CORS doesn't directly impact GDPR compliance, but:
- Properly configured CORS helps ensure data is only sent to authorized applications
- Prevents accidental data leakage to unauthorized origins

## Related Documentation

- [Swagger Security Guide](./SWAGGER_SECURITY_GUIDE.md)
- [Security Summary](./SECURITY_SUMMARY.md)
- [Rate Limiting Configuration](./RATE_LIMITING_CONFIGURATION.md)
- [Azure AD App Setup](./AZURE_AD_APP_SETUP.md)

## Summary

Our CORS implementation follows security best practices:

✅ **No `AllowAnyOrigin()`** - Specific origins only  
✅ **Environment-aware** - Different configs for dev/prod  
✅ **Secure by default** - Empty origins list if not configured in production  
✅ **Configurable** - Easy to add/remove origins via appsettings  
✅ **Credentials support** - Allows authenticated requests  
✅ **Wildcard support** - Can support SharePoint domains with custom logic  

This configuration satisfies the security requirement to **"lock down CORS (no AllowAnyOrigin in prod)"** while providing flexibility for legitimate use cases.
