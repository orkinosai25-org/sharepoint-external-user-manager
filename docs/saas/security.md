# Security Architecture

## Overview

This document outlines the security controls, threat model, and compliance measures for the SharePoint External User Manager SaaS platform.

## Security Layers

```
┌────────────────────────────────────────────────────────────────┐
│ Layer 1: Network Security                                      │
│  • HTTPS/TLS 1.2+ only                                        │
│  • Azure Front Door WAF (future)                              │
│  • DDoS protection                                            │
└────────────────────────────────────────────────────────────────┘
┌────────────────────────────────────────────────────────────────┐
│ Layer 2: Authentication & Authorization                        │
│  • Azure AD JWT token validation                              │
│  • Multi-tenant app registration                              │
│  • Role-based access control (RBAC)                           │
└────────────────────────────────────────────────────────────────┘
┌────────────────────────────────────────────────────────────────┐
│ Layer 3: Application Security                                  │
│  • Input validation & sanitization                            │
│  • Output encoding                                            │
│  • CORS policy enforcement                                    │
│  • Rate limiting                                              │
└────────────────────────────────────────────────────────────────┘
┌────────────────────────────────────────────────────────────────┐
│ Layer 4: Data Security                                         │
│  • Tenant isolation (row-level)                               │
│  • Encryption at rest (AES-256)                               │
│  • Encryption in transit (TLS 1.2+)                           │
│  • Secure credential storage (Key Vault)                      │
└────────────────────────────────────────────────────────────────┘
┌────────────────────────────────────────────────────────────────┐
│ Layer 5: Monitoring & Incident Response                        │
│  • Audit logging (immutable)                                  │
│  • Security alerts                                            │
│  • Anomaly detection                                          │
└────────────────────────────────────────────────────────────────┘
```

## Authentication

### Azure AD Multi-Tenant App Registration

**App Registration Settings**:
```json
{
  "appId": "{app-id}",
  "displayName": "SharePoint External User Manager",
  "signInAudience": "AzureADMultipleOrgs",
  "identifierUris": ["api://spexternal.com"],
  "replyUrls": [
    "https://api.spexternal.com/.auth/callback",
    "https://*.sharepoint.com/*"
  ]
}
```

**Required API Permissions**:
- **Microsoft Graph**:
  - `Sites.Read.All` (Delegated) - Read SharePoint sites
  - `User.Read` (Delegated) - Read user profile
  - `User.ReadWrite.All` (Application) - Manage users [Admin consent required]
  
- **SharePoint**:
  - `Sites.Manage.All` (Delegated) - Manage SharePoint libraries
  - `AllSites.FullControl` (Application) - Full control [Admin consent required]

### JWT Token Validation

```typescript
import { verify } from 'jsonwebtoken';
import jwksClient from 'jwks-rsa';

export async function validateToken(token: string): Promise<TokenClaims> {
  const client = jwksClient({
    jwksUri: 'https://login.microsoftonline.com/common/discovery/v2.0/keys'
  });
  
  const getKey = (header, callback) => {
    client.getSigningKey(header.kid, (err, key) => {
      const signingKey = key.getPublicKey();
      callback(null, signingKey);
    });
  };
  
  return new Promise((resolve, reject) => {
    verify(token, getKey, {
      audience: process.env.AZURE_AD_CLIENT_ID,
      issuer: [
        'https://login.microsoftonline.com/{tenantId}/v2.0',
        'https://sts.windows.net/{tenantId}/'
      ],
      algorithms: ['RS256']
    }, (err, decoded) => {
      if (err) reject(err);
      else resolve(decoded as TokenClaims);
    });
  });
}
```

**Token Claims Required**:
- `tid`: Tenant ID (Azure AD)
- `oid`: User object ID
- `upn` or `email`: User email
- `roles` or `groups`: User roles (optional)

### Admin Consent Flow

1. User clicks "Connect Tenant" in SPFx admin page
2. SPFx redirects to Azure AD consent endpoint:
   ```
   https://login.microsoftonline.com/{tenant}/v2.0/adminconsent
     ?client_id={clientId}
     &redirect_uri={redirectUri}
     &scope={scopes}
   ```
3. Tenant admin reviews and grants permissions
4. Azure AD redirects back with `admin_consent=True`
5. Backend creates tenant record and subscription

## Authorization

### Role-Based Access Control (RBAC)

**Roles**:

| Role | Description | Permissions |
|------|-------------|-------------|
| **Owner** | Tenant creator/owner | Full access including billing |
| **Admin** | Tenant administrator | Manage users, policies, view audit |
| **User** | Regular user | View data, limited actions |
| **ReadOnly** | Auditor/viewer | Read-only access to all data |

**Role Assignment**:
```typescript
export interface UserContext {
  userId: string;
  email: string;
  tenantId: string;
  role: 'Owner' | 'Admin' | 'User' | 'ReadOnly';
}

export function checkPermission(
  context: UserContext, 
  action: string
): boolean {
  const permissions = {
    Owner: ['*'],
    Admin: [
      'users:read', 'users:write', 'users:delete',
      'policies:read', 'policies:write',
      'audit:read'
    ],
    User: ['users:read', 'policies:read'],
    ReadOnly: ['users:read', 'policies:read', 'audit:read']
  };
  
  const userPerms = permissions[context.role] || [];
  return userPerms.includes('*') || userPerms.includes(action);
}
```

## Input Validation

### Request Validation

```typescript
import Joi from 'joi';

// Example: Validate tenant onboarding request
const onboardSchema = Joi.object({
  organizationName: Joi.string().min(1).max(255).required(),
  primaryAdminEmail: Joi.string().email().required(),
  settings: Joi.object().optional()
});

export function validateRequest<T>(
  data: any, 
  schema: Joi.Schema
): T {
  const { error, value } = schema.validate(data);
  if (error) {
    throw new ValidationError(error.message);
  }
  return value as T;
}
```

### SQL Injection Prevention

**Parameterized Queries Only**:
```typescript
// ✅ GOOD: Parameterized query
const result = await db.query(
  'SELECT * FROM AuditLog WHERE TenantId = @tenantId AND UserId = @userId',
  { tenantId, userId }
);

// ❌ BAD: String concatenation (NEVER DO THIS)
const result = await db.query(
  `SELECT * FROM AuditLog WHERE TenantId = ${tenantId}`
);
```

### XSS Prevention

- All user inputs sanitized before storage
- Output encoding when returning data
- Content Security Policy headers

```typescript
import sanitizeHtml from 'sanitize-html';

export function sanitizeInput(input: string): string {
  return sanitizeHtml(input, {
    allowedTags: [], // No HTML tags allowed
    allowedAttributes: {}
  });
}
```

## Tenant Isolation

### Data Isolation Strategy

**Row-Level Security**: Every table has `TenantId` column

```typescript
export class TenantScopedRepository {
  constructor(private tenantId: number) {}
  
  async find(filters: any): Promise<any[]> {
    // ALWAYS include tenantId in WHERE clause
    return await db.query(
      'SELECT * FROM UserAction WHERE TenantId = @tenantId AND ...',
      { tenantId: this.tenantId, ...filters }
    );
  }
}
```

### Tenant Context Middleware

```typescript
export async function tenantContextMiddleware(
  req: HttpRequest, 
  res: HttpResponse, 
  next: Function
) {
  try {
    // 1. Validate JWT token
    const token = req.headers.authorization?.replace('Bearer ', '');
    const claims = await validateToken(token);
    
    // 2. Resolve tenant from token
    const tenant = await db.getTenantByEntraId(claims.tid);
    if (!tenant) {
      return res.status(404).json({ error: 'Tenant not found' });
    }
    
    // 3. Attach tenant context to request
    req.tenantContext = {
      tenantId: tenant.id,
      entraIdTenantId: tenant.entraIdTenantId,
      userId: claims.oid,
      userEmail: claims.email
    };
    
    next();
  } catch (error) {
    return res.status(401).json({ error: 'Unauthorized' });
  }
}
```

## Secrets Management

### Azure Key Vault Integration

**Secrets Stored**:
- Database connection strings
- Azure AD app client secret
- API keys for external services
- Encryption keys

**Access Pattern**:
```typescript
import { SecretClient } from '@azure/keyvault-secrets';
import { DefaultAzureCredential } from '@azure/identity';

const credential = new DefaultAzureCredential();
const client = new SecretClient(
  process.env.KEY_VAULT_URL, 
  credential
);

export async function getSecret(name: string): Promise<string> {
  const secret = await client.getSecret(name);
  return secret.value;
}
```

**Managed Identity**:
- Function App uses Azure Managed Identity
- No credentials stored in code or environment variables
- Least privilege access to Key Vault

## Encryption

### Data at Rest

- **Azure SQL Database**: Transparent Data Encryption (TDE) enabled by default
- **Azure Storage**: Service-side encryption with Microsoft-managed keys
- **Backups**: Encrypted automatically

### Data in Transit

- **HTTPS Only**: HTTP requests redirected to HTTPS
- **TLS 1.2+**: Minimum TLS version enforced
- **Certificate Management**: Azure-managed certificates

```typescript
// Function app configuration
export const httpsSettings = {
  minTlsVersion: '1.2',
  requireHttps: true,
  allowInsecureHttp: false
};
```

## Rate Limiting

### API Rate Limits

| Tier | Requests/Minute | Burst Allowance |
|------|-----------------|-----------------|
| Free | 10 | 20 |
| Pro | 100 | 150 |
| Enterprise | 500 | 1000 |

**Implementation**:
```typescript
import rateLimit from 'express-rate-limit';

export const createRateLimiter = (tier: SubscriptionTier) => {
  const limits = {
    Free: { windowMs: 60000, max: 10 },
    Pro: { windowMs: 60000, max: 100 },
    Enterprise: { windowMs: 60000, max: 500 }
  };
  
  return rateLimit({
    ...limits[tier],
    message: 'Too many requests, please try again later'
  });
};
```

## CORS Policy

```typescript
export const corsOptions = {
  origin: (origin, callback) => {
    // Allow SharePoint domains only
    const allowedDomains = [
      /^https:\/\/.*\.sharepoint\.com$/,
      /^https:\/\/.*\.sharepoint-df\.com$/,  // GCC
      process.env.ADMIN_PORTAL_URL
    ];
    
    if (!origin || allowedDomains.some(d => d.test(origin))) {
      callback(null, true);
    } else {
      callback(new Error('CORS not allowed'));
    }
  },
  credentials: true,
  methods: ['GET', 'POST', 'PUT', 'DELETE'],
  allowedHeaders: ['Authorization', 'Content-Type', 'X-Correlation-ID']
};
```

## Audit Logging

### Audit Requirements

**Log All Security Events**:
- Authentication attempts (success/failure)
- Authorization failures
- Data access (read/write/delete)
- Configuration changes
- Subscription changes

**Audit Log Schema**:
```typescript
interface AuditLogEntry {
  id: number;
  tenantId: number;
  timestamp: Date;
  userId: string;
  userEmail: string;
  action: string;
  resourceType: string;
  resourceId: string;
  details: object;
  ipAddress: string;
  correlationId: string;
  status: 'Success' | 'Failed';
}
```

**Immutable Logs**:
- Append-only table (no updates/deletes)
- Tamper detection via checksums
- Long-term retention (7 years)

## Threat Model

### Identified Threats

| Threat | Mitigation |
|--------|------------|
| **SQL Injection** | Parameterized queries, ORM usage |
| **XSS** | Input sanitization, output encoding |
| **CSRF** | SameSite cookies, CORS policy |
| **Token Theft** | Short token lifetime, HTTPS only |
| **Data Leakage** | Tenant isolation, row-level security |
| **DDoS** | Rate limiting, Azure DDoS protection |
| **Privilege Escalation** | RBAC enforcement, least privilege |
| **Insider Threat** | Audit logging, access reviews |

### Security Testing

**Regular Activities**:
- Dependency vulnerability scanning (npm audit, Snyk)
- Penetration testing (annual)
- Security code reviews
- Threat modeling updates

## Incident Response

### Security Incident Procedure

1. **Detection**: Alert triggers via Azure Monitor
2. **Assessment**: Severity classification (Low/Medium/High/Critical)
3. **Containment**: Isolate affected resources
4. **Investigation**: Root cause analysis from audit logs
5. **Remediation**: Apply fixes, rotate credentials
6. **Recovery**: Restore service, validate security
7. **Post-Incident**: Report, lessons learned, improve controls

### Breach Notification

- **Timeline**: Notify affected tenants within 72 hours
- **Communication**: Email to primary admin + in-app notification
- **Information**: Incident details, impact, remediation steps

## Compliance

### Standards Alignment

- **GDPR**: Data subject rights, right to erasure
- **SOC 2 Type II**: Security, availability, confidentiality (future)
- **ISO 27001**: Information security management (future)
- **HIPAA**: Not currently in scope

### Data Residency

- Tenants choose data region during onboarding
- Data never leaves chosen region
- Compliant with EU data protection requirements

### Right to Erasure (GDPR)

```typescript
export async function deleteTenantData(tenantId: number): Promise<void> {
  // 1. Mark tenant as deleted (soft delete)
  await db.updateTenant(tenantId, { status: 'Deleted' });
  
  // 2. Schedule hard delete after 30 days
  await db.scheduleDataPurge(tenantId, addDays(new Date(), 30));
  
  // 3. Audit the deletion request
  await auditLog.log({
    action: 'TenantDataDeletionRequested',
    tenantId,
    timestamp: new Date()
  });
}
```

## Security Checklist

### Pre-Production

- [ ] All secrets in Azure Key Vault
- [ ] HTTPS enforced (no HTTP)
- [ ] JWT validation implemented
- [ ] Tenant isolation verified
- [ ] Rate limiting configured
- [ ] CORS policy restrictive
- [ ] Input validation on all endpoints
- [ ] SQL injection prevention verified
- [ ] Audit logging enabled
- [ ] Security alerts configured

### Production

- [ ] Penetration test completed
- [ ] Vulnerability scan passed
- [ ] Backup and recovery tested
- [ ] Incident response plan documented
- [ ] Security training completed
- [ ] Compliance certifications obtained (if applicable)

## Responsible Disclosure

**Security Bug Reporting**:
- Email: security@spexternal.com
- Response time: 48 hours
- Coordinated disclosure: 90 days
- Bug bounty program (future)
