# Security Notes

This document outlines critical security considerations for the SharePoint External User Manager SaaS platform. All developers and operators must understand and follow these guidelines.

## Overview

This application is a **multi-tenant SaaS platform** that manages sensitive SharePoint data and external user access. Security is paramount and must be considered at every level of the stack.

---

## 🔐 Core Security Principles

### 1. Zero Trust Architecture
- Never trust input from any source (clients, APIs, databases)
- Always validate authentication and authorisation
- Assume breach and limit blast radius
- Log everything for audit trail

### 2. Defence in Depth
- Multiple layers of security controls
- Network, application, and data-level protection
- Security at every tier of the architecture

### 3. Principle of Least Privilege
- Grant minimum required permissions
- Use role-based access control (RBAC)
- Regularly review and revoke unused permissions

---

## 🚨 Critical Security Rules

### **RULE 1: Never Commit Secrets**

**❌ NEVER commit these to the repository:**
- API keys and secrets
- Connection strings with credentials
- Authentication tokens
- Passwords or passphrases
- Private keys or certificates
- OAuth client secrets
- Stripe API keys (secret keys)
- Azure AD client secrets
- Database passwords

**✅ Instead, use:**
- Azure Key Vault for production secrets
- Local settings files (added to `.gitignore`)
- Environment variables
- GitHub Secrets for CI/CD
- Configuration files with `.example` suffix (with dummy values)

**Example `.gitignore` entries:**
```gitignore
# Local settings (may contain secrets)
local.settings.json
appsettings.Development.json
.env
.env.local

# Secret configuration files
**/appsettings.*.json
!**/appsettings.*.example.json

# Stripe configuration
stripe.config.json
local.settings.stripe.json
```

### **RULE 2: Enforce Tenant Isolation**

**Every data access MUST include tenant filtering:**

```typescript
// ❌ WRONG: No tenant filter
const clients = await db.clients.findAll();

// ✅ CORRECT: Always filter by tenant
const clients = await db.clients.findAll({
  where: { tenantId: context.tenantId }
});
```

**Database schema requirements:**
- Every table MUST have a `TenantId` column
- Every query MUST filter by `TenantId`
- Indexes MUST include `TenantId` as first column
- Foreign keys MUST preserve tenant boundaries

**Middleware validation:**
```typescript
// Extract and validate tenant from JWT token
const tenantId = extractTenantFromToken(req.headers.authorization);
if (!tenantId) {
  throw new UnauthorizedError('Missing tenant context');
}

// Attach to request context
req.context = { tenantId, userId, roles };
```

### **RULE 3: Validate All Inputs**

**Never trust client input:**

```typescript
// ✅ CORRECT: Validate and sanitise
import { body, validationResult } from 'express-validator';

app.post('/clients',
  body('name').isString().trim().isLength({ min: 1, max: 100 }),
  body('description').optional().isString().trim(),
  async (req, res) => {
    const errors = validationResult(req);
    if (!errors.isEmpty()) {
      return res.status(400).json({ errors: errors.array() });
    }
    // Process validated input
  }
);
```

**Input validation rules:**
- Validate type, length, format, and range
- Sanitise HTML input to prevent XSS
- Use parameterised queries to prevent SQL injection
- Validate file uploads (type, size, content)
- Rate limit to prevent abuse

### **RULE 4: Implement Proper Authentication**

**Azure AD authentication requirements:**

```typescript
// ✅ CORRECT: Validate JWT token
const jwtValidator = {
  issuer: `https://login.microsoftonline.com/${tenantId}/v2.0`,
  audience: process.env.AZURE_CLIENT_ID,
  algorithms: ['RS256']
};

// Validate token signature and claims
const decoded = await validateJwtToken(token, jwtValidator);

// Extract user identity
const userId = decoded.oid || decoded.sub;
const tenantId = decoded.tid;
const roles = decoded.roles || [];
```

**Authentication checklist:**
- ✅ Validate token signature with Azure AD public keys
- ✅ Verify issuer matches expected Azure AD endpoint
- ✅ Verify audience matches application client ID
- ✅ Check token expiration (`exp` claim)
- ✅ Validate token not used before valid time (`nbf` claim)
- ✅ Extract and validate tenant ID from token
- ✅ Extract user roles for authorisation

### **RULE 5: Enforce Authorisation**

**Role-based access control (RBAC):**

```typescript
// ✅ CORRECT: Check permissions before action
import { requirePermission, PERMISSIONS } from './middleware/permissions';

app.post('/clients',
  authenticate,
  requirePermission(PERMISSIONS.CLIENTS_WRITE),
  async (req, res) => {
    // User has permission to create clients
  }
);
```

**Authorisation rules:**
- Always check user has required permission
- Never rely on client-side permission checks
- Log authorisation failures for audit
- Use granular permissions (read, write, delete)
- Implement resource-level permissions where needed

**Permission matrix:**

| Role | Clients | External Users | Libraries/Lists | Subscription |
|------|---------|----------------|-----------------|--------------|
| Owner | Read/Write/Delete | Read/Write/Delete | Read/Write/Delete | Manage |
| Admin | Read/Write/Delete | Read/Write/Delete | Read/Write/Delete | View |
| FirmAdmin | Read/Write | Read/Write | Read/Write | View |
| FirmUser | Read | Read | Read | View |
| ReadOnly | Read | Read | Read | View |

---

## 🔒 Secrets Management

### Azure Key Vault (Production)

**Store all production secrets in Azure Key Vault:**

```bash
# Set secrets in Key Vault
az keyvault secret set \
  --vault-name kv-spexternal-prod \
  --name DatabaseConnectionString \
  --value "Server=..."

az keyvault secret set \
  --vault-name kv-spexternal-prod \
  --name StripeSecretKey \
  --value "sk_live_..."
```

**Access secrets in application:**

```csharp
// .NET API
var keyVaultUrl = configuration["KeyVault:Url"];
var client = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());
var secret = await client.GetSecretAsync("DatabaseConnectionString");
var connectionString = secret.Value.Value;
```

### Local Development Settings

**Use local settings files (not committed):**

**`local.settings.json` (Azure Functions):**
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "node",
    "DatabaseConnectionString": "Server=localhost;Database=spexternal_dev;",
    "AZURE_CLIENT_ID": "00000000-0000-0000-0000-000000000000",
    "AZURE_TENANT_ID": "00000000-0000-0000-0000-000000000000",
    "STRIPE_SECRET_KEY": "sk_test_..."
  }
}
```

**`appsettings.Development.json` (.NET):**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=spexternal_dev;"
  },
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "ClientId": "00000000-0000-0000-0000-000000000000",
    "TenantId": "common"
  },
  "Stripe": {
    "SecretKey": "sk_test_...",
    "WebhookSecret": "whsec_..."
  }
}
```

**Provide example files (committed):**
- `local.settings.example.json`
- `appsettings.Development.example.json`

### GitHub Secrets (CI/CD)

**Configure in GitHub repository settings:**

**Required secrets:**
- `AZURE_CLIENT_ID` - Azure AD application ID
- `AZURE_CLIENT_SECRET` - Azure AD application secret
- `AZURE_TENANT_ID` - Azure AD tenant ID
- `AZURE_SUBSCRIPTION_ID` - Azure subscription ID
- `STRIPE_SECRET_KEY` - Stripe API secret key (test mode for dev)
- `DATABASE_CONNECTION_STRING` - Database connection string
- `SPO_URL` - SharePoint tenant URL
- `SPO_USERNAME` - SharePoint admin username
- `SPO_PASSWORD` - SharePoint admin password

---

## 🛡️ Tenant Isolation

### Database Design

**All tables include TenantId:**

```sql
CREATE TABLE Clients (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    TenantId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    -- other columns
    INDEX IX_Clients_TenantId (TenantId, Id)
);

CREATE TABLE ExternalUsers (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    TenantId UNIQUEIDENTIFIER NOT NULL,
    ClientId UNIQUEIDENTIFIER NOT NULL,
    Email NVARCHAR(255) NOT NULL,
    -- other columns
    INDEX IX_ExternalUsers_TenantId_ClientId (TenantId, ClientId),
    FOREIGN KEY (ClientId) REFERENCES Clients(Id)
);
```

**Index strategy:**
- First column in composite indexes: `TenantId`
- Ensures efficient filtering by tenant
- Prevents cross-tenant queries

### Query Patterns

**Always filter by TenantId:**

```csharp
// ✅ CORRECT: Entity Framework Core
var clients = await _context.Clients
    .Where(c => c.TenantId == tenantId)
    .ToListAsync();

// ✅ CORRECT: Dapper
var clients = await connection.QueryAsync<Client>(
    "SELECT * FROM Clients WHERE TenantId = @TenantId",
    new { TenantId = tenantId }
);
```

### Middleware Enforcement

**Automatically inject TenantId filter:**

```typescript
// Azure Functions middleware
export async function tenantContextMiddleware(
  context: Context,
  req: HttpRequest
): Promise<void> {
  // Extract tenant from JWT token
  const token = extractBearerToken(req.headers.authorization);
  const decoded = await validateToken(token);
  
  // Attach tenant context
  context.req.tenantContext = {
    tenantId: decoded.tid,
    userId: decoded.oid,
    roles: decoded.roles || []
  };
}
```

```csharp
// .NET API middleware
public class TenantContextMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var user = context.User;
        var tenantId = user.FindFirst("tid")?.Value;
        
        if (string.IsNullOrEmpty(tenantId))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Missing tenant context");
            return;
        }
        
        context.Items["TenantId"] = Guid.Parse(tenantId);
        await next(context);
    }
}
```

### Testing Tenant Isolation

**Create tests to verify isolation:**

```typescript
describe('Tenant Isolation', () => {
  it('should not return clients from other tenants', async () => {
    const tenant1Token = generateToken({ tid: 'tenant-1' });
    const tenant2Token = generateToken({ tid: 'tenant-2' });
    
    // Create client for tenant 1
    await request(app)
      .post('/clients')
      .set('Authorization', `Bearer ${tenant1Token}`)
      .send({ name: 'Client A' });
    
    // Try to fetch with tenant 2 token
    const response = await request(app)
      .get('/clients')
      .set('Authorization', `Bearer ${tenant2Token}`);
    
    // Should not see tenant 1's client
    expect(response.body).toHaveLength(0);
  });
});
```

---

## 🔍 Security Scanning

### Pre-Commit Checks

**Use git hooks to prevent secret commits:**

```bash
# .git/hooks/pre-commit
#!/bin/bash

# Check for common secret patterns
if git diff --cached | grep -E "(api_key|secret_key|password|private_key)" > /dev/null; then
    echo "❌ Possible secret detected in commit"
    echo "Please remove secrets before committing"
    exit 1
fi
```

### CI/CD Security Checks

**Automated scanning in GitHub Actions:**

1. **Secret scanning**: TruffleHog
2. **Dependency vulnerabilities**: npm audit, dotnet list package --vulnerable
3. **Code scanning**: CodeQL (optional)

**Review scan results before merging:**
- Check for newly introduced secrets
- Investigate flagged dependencies
- Update or remove vulnerable packages

### Manual Security Review

**Before every release:**
- ✅ Review all new code for security issues
- ✅ Check for hardcoded credentials
- ✅ Verify tenant isolation in new queries
- ✅ Confirm proper input validation
- ✅ Test authentication and authorisation

---

## 🚦 Rate Limiting

### API Rate Limits

**Prevent abuse with rate limiting:**

```typescript
import rateLimit from 'express-rate-limit';

// Per-tenant rate limiting
const tenantRateLimiter = rateLimit({
  windowMs: 15 * 60 * 1000, // 15 minutes
  max: 100, // 100 requests per window per tenant
  keyGenerator: (req) => req.context.tenantId,
  message: 'Too many requests, please try again later'
});

app.use('/api', tenantRateLimiter);
```

**Rate limit tiers by subscription:**

| Plan | Requests/15min | Requests/hour | Requests/day |
|------|----------------|---------------|--------------|
| Starter | 100 | 400 | 5,000 |
| Professional | 500 | 2,000 | 25,000 |
| Business | 2,000 | 8,000 | 100,000 |
| Enterprise | Custom | Custom | Custom |

---

## 📊 Audit Logging

### What to Log

**Log all security-relevant events:**
- ✅ Authentication attempts (success and failure)
- ✅ Authorisation failures
- ✅ Admin actions (create, update, delete)
- ✅ External user invitations and removals
- ✅ Configuration changes
- ✅ Subscription changes
- ✅ Data exports

### Log Format

**Structured logging with correlation IDs:**

```typescript
logger.info('Client created', {
  correlationId: req.id,
  tenantId: context.tenantId,
  userId: context.userId,
  action: 'CLIENT_CREATE',
  resourceId: client.id,
  resourceType: 'Client',
  timestamp: new Date().toISOString(),
  ipAddress: req.ip
});
```

### Log Storage

**Use Application Insights for centralised logging:**
- Automatic correlation tracking
- Query logs with Kusto Query Language (KQL)
- Set up alerts for security events
- Retain logs for compliance (90+ days)

---

## 🔐 Stripe Security

### Webhook Signature Verification

**Always verify webhook signatures:**

```typescript
import Stripe from 'stripe';

// ✅ CORRECT: Verify signature
app.post('/billing/webhook',
  express.raw({ type: 'application/json' }),
  async (req, res) => {
    const signature = req.headers['stripe-signature'];
    const webhookSecret = process.env.STRIPE_WEBHOOK_SECRET;
    
    try {
      const event = stripe.webhooks.constructEvent(
        req.body,
        signature,
        webhookSecret
      );
      
      // Process verified event
      await handleStripeEvent(event);
      res.json({ received: true });
    } catch (err) {
      logger.error('Webhook signature verification failed', err);
      return res.status(400).send(`Webhook Error: ${err.message}`);
    }
  }
);
```

### Key Management

**Separate keys for development and production:**
- Use `sk_test_...` keys for development
- Use `sk_live_...` keys for production only
- Store live keys in Azure Key Vault
- Rotate keys if compromised

---

## 📱 SPFx Security

### No Privileged Operations in Client

**SPFx must call backend API, never Graph directly:**

```typescript
// ❌ WRONG: Direct Graph call from SPFx (requires elevated permissions)
await graphClient.api('/sites/{siteId}/permissions').post(permission);

// ✅ CORRECT: Call backend API
await apiClient.post('/clients/{clientId}/external-users/invite', {
  email: 'user@example.com',
  permission: 'Read'
});
```

**Backend API uses app-only Graph permissions:**
- SPFx uses user-delegated permissions only
- Backend holds privileged permissions
- Enforces tenant isolation
- Validates all requests

---

## ✅ Security Checklist

Before every release, verify:

### Code
- [ ] No secrets in repository
- [ ] All inputs validated and sanitised
- [ ] All queries filter by TenantId
- [ ] Authentication required on all endpoints
- [ ] Authorisation checked before actions
- [ ] Error messages don't leak sensitive info

### Configuration
- [ ] Production secrets in Azure Key Vault
- [ ] HTTPS enforced on all endpoints
- [ ] CORS configured correctly
- [ ] Rate limiting enabled
- [ ] Logging configured

### Testing
- [ ] Security scans passed
- [ ] Tenant isolation tests passed
- [ ] Authentication tests passed
- [ ] Authorisation tests passed

### Deployment
- [ ] Secrets rotated if needed
- [ ] Audit logging enabled
- [ ] Monitoring alerts configured
- [ ] Incident response plan ready

---

## 📚 Related Documentation

- [Branch Protection Rules](./BRANCH_PROTECTION.md)
- [Release Checklist](./RELEASE_CHECKLIST.md)
- [Azure Security Best Practices](./saas/security.md)
- [Developer Guide](../DEVELOPER_GUIDE.md)

---

## 🆘 Security Incident Response

If a security incident is discovered:

1. **Immediate**: Revoke compromised credentials
2. **Assess**: Determine scope and impact
3. **Contain**: Prevent further damage
4. **Notify**: Inform affected parties
5. **Remediate**: Fix vulnerability
6. **Review**: Conduct post-incident review

---

**Last Updated**: 2026-02-08  
**Maintained By**: Security Team & Repository Administrators
