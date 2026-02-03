# Security Documentation

## Overview

This document outlines the comprehensive security controls, threat models, and compliance measures for the SharePoint External User Manager SaaS platform.

## Security Principles

1. **Defense in Depth**: Multiple layers of security controls
2. **Least Privilege**: Minimum permissions for all operations
3. **Zero Trust**: Verify every request, trust nothing
4. **Data Protection**: Encrypt data at rest and in transit
5. **Audit Everything**: Comprehensive logging and monitoring

## Threat Model

### Assets to Protect
- Customer tenant data (external user lists, permissions)
- Authentication credentials (tokens, secrets)
- Application secrets (connection strings, API keys)
- Audit logs and compliance data
- Source code and intellectual property

### Threat Actors
1. **External Attackers**: Attempting unauthorized access
2. **Malicious Insiders**: Employees or contractors with access
3. **Compromised Accounts**: Legitimate users with stolen credentials
4. **Automated Bots**: Scanning for vulnerabilities

### Attack Vectors
- API endpoint exploitation
- SQL injection attacks
- Cross-site scripting (XSS)
- Authentication bypass
- Token theft/replay attacks
- DDoS attacks
- Data exfiltration

## Authentication & Authorization

### Azure AD Integration

**Multi-Tenant Application Registration:**
```json
{
  "appId": "00000000-0000-0000-0000-000000000000",
  "signInAudience": "AzureADMultipleOrgs",
  "oauth2Permissions": [
    {
      "adminConsentDescription": "Access SharePoint External User Manager API",
      "adminConsentDisplayName": "Access API",
      "id": "user_impersonation_guid",
      "isEnabled": true,
      "type": "User",
      "userConsentDescription": "Allow the application to access API on your behalf",
      "userConsentDisplayName": "Access API",
      "value": "user_impersonation"
    }
  ]
}
```

**Required API Permissions:**
```
Microsoft Graph:
- Sites.Read.All (Delegated)
- Sites.Manage.All (Delegated)  
- User.Read.All (Delegated)
- Directory.Read.All (Delegated)

SharePoint:
- AllSites.Manage (Delegated)
- User.ReadWrite.All (Delegated)
```

### JWT Token Validation

**Token Validation Steps:**
1. Verify token signature using Azure AD public keys
2. Validate issuer (`iss` claim)
3. Validate audience (`aud` claim)
4. Check token expiration (`exp` claim)
5. Validate tenant ID (`tid` claim)
6. Extract user identity (`upn` or `email` claim)

**Implementation:**
```typescript
import { verify, decode } from 'jsonwebtoken';
import * as jwksClient from 'jwks-rsa';

const client = jwksClient({
  jwksUri: 'https://login.microsoftonline.com/common/discovery/v2.0/keys',
  cache: true,
  cacheMaxAge: 86400000 // 24 hours
});

async function validateToken(token: string): Promise<TokenPayload> {
  const decoded = decode(token, { complete: true });
  const kid = decoded.header.kid;
  
  const key = await client.getSigningKey(kid);
  const signingKey = key.getPublicKey();
  
  const verified = verify(token, signingKey, {
    audience: process.env.AZURE_AD_CLIENT_ID,
    issuer: `https://sts.windows.net/${process.env.AZURE_AD_TENANT_ID}/`,
    algorithms: ['RS256']
  });
  
  return verified as TokenPayload;
}
```

### Role-Based Access Control (RBAC)

**Roles Hierarchy:**
```
TenantOwner (highest privileges)
  └─ TenantAdmin
      └─ LibraryOwner
          └─ LibraryContributor
              └─ LibraryReader (lowest privileges)
```

**Permission Matrix:**

| Operation | TenantOwner | TenantAdmin | LibraryOwner | LibraryContributor | LibraryReader |
|-----------|-------------|-------------|--------------|-------------------|---------------|
| Manage Tenant Settings | ✓ | ✓ | ✗ | ✗ | ✗ |
| View Subscription | ✓ | ✓ | ✗ | ✗ | ✗ |
| Manage Admins | ✓ | ✗ | ✗ | ✗ | ✗ |
| Create Library | ✓ | ✓ | ✓ | ✗ | ✗ |
| Delete Library | ✓ | ✓ | ✓ | ✗ | ✗ |
| Invite Users | ✓ | ✓ | ✓ | ✓ | ✗ |
| Remove Users | ✓ | ✓ | ✓ | ✓ | ✗ |
| View Users | ✓ | ✓ | ✓ | ✓ | ✓ |
| View Audit Logs | ✓ | ✓ | ✗ | ✗ | ✗ |
| Export Data | ✓ | ✓ | ✗ | ✗ | ✗ |

## Network Security

### HTTPS/TLS Configuration
- **Minimum Version**: TLS 1.2
- **Cipher Suites**: AES-GCM preferred
- **Certificate Management**: Azure Key Vault
- **Certificate Rotation**: Automated every 90 days

### API Gateway Protection
```yaml
Azure API Management Policies:
  - Rate Limiting: 100 req/min per tenant
  - IP Whitelisting: Optional for enterprise customers
  - CORS: Strict origin validation
  - Request Size Limit: 5 MB max payload
  - Timeout: 30 seconds per request
```

### DDoS Protection
- Azure DDoS Protection Standard enabled
- Automatic traffic filtering and mitigation
- Real-time attack metrics and alerts

## Data Security

### Encryption at Rest

**Azure SQL Database:**
- Transparent Data Encryption (TDE) enabled
- AES-256 encryption algorithm
- Microsoft-managed keys (default)
- Customer-managed keys (enterprise option via Key Vault)

**Cosmos DB:**
- Automatic encryption for all data
- AES-256 encryption
- Microsoft-managed keys

**Blob Storage:**
- Azure Storage Service Encryption (SSE)
- AES-256 encryption
- Encryption scopes for data isolation

### Encryption in Transit
- All API calls require HTTPS
- TLS 1.2 or higher
- Perfect Forward Secrecy (PFS) enabled
- Backend-to-backend communication via private endpoints

### Key Management

**Azure Key Vault Configuration:**
```json
{
  "secrets": [
    "CosmosDB-ConnectionString",
    "SQL-ConnectionString-Master",
    "AzureAD-ClientSecret",
    "ApplicationInsights-InstrumentationKey"
  ],
  "keys": [
    "DataEncryption-Key",
    "TokenSigning-Key"
  ],
  "certificates": [
    "API-TLS-Certificate"
  ],
  "accessPolicies": [
    {
      "principalId": "function-app-managed-identity",
      "permissions": {
        "secrets": ["get", "list"],
        "keys": ["get", "decrypt", "encrypt"]
      }
    }
  ]
}
```

**Key Rotation Policy:**
- Secrets: Every 90 days
- Keys: Every 180 days
- Certificates: Every 365 days
- Automated rotation via Azure DevOps pipelines

## Input Validation & Output Encoding

### Input Validation Rules
```typescript
// Email validation
const emailRegex = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;

// URL validation (SharePoint URLs)
const urlRegex = /^https:\/\/[a-z0-9-]+\.sharepoint\.com\/.+$/i;

// Permission level validation
const validPermissions = ['read', 'contribute', 'fullcontrol'];

// Input sanitization
import * as validator from 'validator';

function sanitizeInput(input: string): string {
  return validator.escape(validator.trim(input));
}
```

### SQL Injection Prevention
- **Parameterized Queries**: All SQL queries use parameters
- **ORM Usage**: TypeORM or Prisma for type-safe queries
- **Input Validation**: Whitelist validation for all inputs
- **Stored Procedures**: For complex operations

```typescript
// Good: Parameterized query
const users = await db.query(
  'SELECT * FROM ExternalUsers WHERE Email = @email',
  { email: userEmail }
);

// Bad: String concatenation (NEVER DO THIS)
// const users = await db.query(`SELECT * FROM ExternalUsers WHERE Email = '${userEmail}'`);
```

### XSS Prevention
- Output encoding for all user-generated content
- Content Security Policy (CSP) headers
- HttpOnly and Secure flags on cookies
- No inline JavaScript in responses

## Audit Logging

### Event Types to Log
1. **Authentication Events**
   - Login attempts (success/failure)
   - Token issuance
   - Token validation failures
   - Role changes

2. **Authorization Events**
   - Permission checks (allow/deny)
   - Role assignments
   - Policy changes

3. **Data Access Events**
   - External user list queries
   - User detail views
   - Export operations
   - Bulk operations

4. **Data Modification Events**
   - User invitations
   - User removals
   - Permission changes
   - Library additions/deletions
   - Policy updates

5. **System Events**
   - Tenant onboarding
   - Subscription changes
   - Configuration updates
   - API errors

### Audit Log Format
```json
{
  "timestamp": "2024-01-20T14:35:22.123Z",
  "correlationId": "550e8400-e29b-41d4-a716-446655440000",
  "tenantId": "contoso.onmicrosoft.com",
  "eventType": "user.invite",
  "actor": {
    "userId": "user-guid",
    "email": "admin@contoso.com",
    "ipAddress": "203.0.113.45",
    "userAgent": "Mozilla/5.0..."
  },
  "action": "POST /external-users/invite",
  "status": "success",
  "target": {
    "resourceType": "externalUser",
    "resourceId": "extuser-guid",
    "email": "partner@external.com"
  },
  "metadata": {
    "libraryId": "lib-guid",
    "permissions": "read",
    "requestDurationMs": 245
  },
  "changes": {
    "before": null,
    "after": { "status": "invited" }
  }
}
```

### Log Retention
- **Operational Logs**: 90 days (hot storage)
- **Compliance Logs**: 7 years (cold storage)
- **Security Logs**: 2 years (warm storage)

## Security Monitoring & Incident Response

### Real-Time Monitoring

**Application Insights Alerts:**
```yaml
Alerts:
  - HighErrorRate:
      condition: errorRate > 5%
      window: 5 minutes
      action: Email security team
  
  - FailedAuthAttempts:
      condition: failedLogins > 10
      window: 1 minute
      action: Block IP + Email security team
  
  - UnusualDataAccess:
      condition: dataExportVolume > 1GB
      window: 10 minutes
      action: Email compliance team
  
  - SQLInjectionAttempt:
      condition: sqlErrorPattern detected
      window: immediate
      action: Block IP + Email security team
```

### Security Information and Event Management (SIEM)
- Integration with Azure Sentinel
- Automated threat detection
- Correlation with Microsoft threat intelligence
- Custom detection rules for application-specific threats

### Incident Response Plan

**1. Detection Phase:**
- Automated alerts trigger investigation
- Security team notified via PagerDuty/Teams
- Initial triage within 15 minutes

**2. Containment Phase:**
- Isolate affected systems
- Block malicious IP addresses
- Revoke compromised tokens
- Disable affected user accounts

**3. Investigation Phase:**
- Analyze audit logs
- Identify scope of breach
- Determine root cause
- Preserve evidence

**4. Remediation Phase:**
- Patch vulnerabilities
- Rotate compromised secrets
- Restore from backups if needed
- Update security controls

**5. Recovery Phase:**
- Restore normal operations
- Monitor for recurring issues
- Notify affected customers (if required)

**6. Post-Incident Phase:**
- Document incident details
- Update runbooks
- Conduct lessons learned
- Implement preventive measures

## Vulnerability Management

### Dependency Scanning
```yaml
GitHub Advanced Security:
  - Dependabot: Automated dependency updates
  - Code Scanning: Static analysis via CodeQL
  - Secret Scanning: Detect committed secrets
```

### Penetration Testing
- **Frequency**: Annually + after major releases
- **Scope**: Full application stack
- **Provider**: Third-party security firm
- **Remediation**: All high/critical findings within 30 days

### Security Patching
- **Critical**: Within 24 hours
- **High**: Within 7 days
- **Medium**: Within 30 days
- **Low**: Next release cycle

## Compliance & Certifications

### Current Compliance
- **GDPR**: Data protection and privacy
- **SOC 2 Type II**: Security, availability, confidentiality (in progress)
- **ISO 27001**: Information security management (planned)

### Data Privacy
- Data Processing Agreement (DPA) available
- Sub-processor transparency
- Data residency options (US, EU, Asia)
- Customer data isolation guaranteed

### Compliance Controls
```yaml
GDPR Controls:
  - Right to Access: API endpoint for data export
  - Right to Erasure: Hard delete functionality
  - Right to Portability: JSON/CSV export
  - Data Breach Notification: 72-hour SLA
  - Privacy by Design: Default settings secure
  - Data Minimization: Only collect necessary data

SOC 2 Controls:
  - Access Controls: RBAC implemented
  - Change Management: All changes via CI/CD
  - Logical Security: MFA enforced
  - System Operations: Monitoring and alerting
  - Risk Mitigation: Vulnerability management
```

## Secure Development Lifecycle

### Code Review Requirements
- All code changes require peer review
- Security-focused review for auth/data access
- Automated SAST scanning on every PR
- No secrets in source code (enforced by pre-commit hooks)

### Security Testing
```yaml
Testing Phases:
  1. Unit Tests: Security functions tested
  2. Integration Tests: Auth flows validated
  3. Security Tests: OWASP Top 10 coverage
  4. Penetration Tests: Annual third-party assessment
```

### Deployment Security
- Secrets managed via Key Vault
- Managed identities for Azure resources
- No hardcoded credentials
- Least privilege for service accounts
- Automated security scanning in CI/CD

## Security Best Practices for Developers

1. **Never log sensitive data** (tokens, passwords, PII)
2. **Always use parameterized queries**
3. **Validate all inputs** (whitelist approach)
4. **Encode all outputs** (prevent XSS)
5. **Use secure random generators** (crypto.randomBytes)
6. **Implement rate limiting** (prevent abuse)
7. **Set secure HTTP headers** (CSP, X-Frame-Options)
8. **Keep dependencies updated** (Dependabot)
9. **Use HTTPS everywhere** (no HTTP)
10. **Follow least privilege principle** (minimum permissions)

---

**Last Updated**: 2024-02-03
**Version**: 1.0
**Owner**: Security Team
