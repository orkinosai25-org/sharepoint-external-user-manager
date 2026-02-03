# Security Architecture

## Overview

This document outlines the comprehensive security architecture for the SharePoint External User Manager SaaS backend, covering authentication, authorization, data protection, network security, and compliance.

## Authentication

### Microsoft Entra ID (Azure AD) Integration

**Multi-Tenant Application Registration**:

```json
{
  "appRegistration": {
    "displayName": "SharePoint External User Manager",
    "signInAudience": "AzureADMultipleOrgs",
    "web": {
      "redirectUris": [
        "https://api.spexternal.com/auth/callback",
        "https://app.spexternal.com/auth/callback"
      ],
      "implicitGrantSettings": {
        "enableIdTokenIssuance": false,
        "enableAccessTokenIssuance": false
      }
    },
    "requiredResourceAccess": [
      {
        "resourceAppId": "00000003-0000-0000-c000-000000000000",
        "resourceAccess": [
          {
            "id": "df021288-bdef-4463-88db-98f22de89214",
            "type": "Scope",
            "comment": "User.Read.All"
          },
          {
            "id": "5f8c59db-677d-491f-a6b8-5f174b11ec1d",
            "type": "Scope",
            "comment": "Sites.Manage.All"
          }
        ]
      }
    ],
    "appRoles": [
      {
        "id": "uuid-1",
        "displayName": "Tenant Admin",
        "value": "Tenant.Admin",
        "allowedMemberTypes": ["User"]
      },
      {
        "id": "uuid-2",
        "displayName": "User Manager",
        "value": "User.Manage",
        "allowedMemberTypes": ["User"]
      }
    ]
  }
}
```

### JWT Token Validation

**Middleware Implementation**:

```typescript
import jwt from 'jsonwebtoken';
import jwksClient from 'jwks-rsa';

interface TokenPayload {
  sub: string;          // Subject (user ID)
  tid: string;          // Tenant ID
  oid: string;          // Object ID
  email: string;
  roles: string[];
  iss: string;          // Issuer
  aud: string;          // Audience
  exp: number;          // Expiration
  nbf: number;          // Not before
  iat: number;          // Issued at
}

const client = jwksClient({
  jwksUri: 'https://login.microsoftonline.com/common/discovery/v2.0/keys',
  cache: true,
  cacheMaxAge: 86400000 // 24 hours
});

function getKey(header: any, callback: any) {
  client.getSigningKey(header.kid, (err, key) => {
    if (err) {
      callback(err);
    } else {
      const signingKey = key?.getPublicKey();
      callback(null, signingKey);
    }
  });
}

export async function validateToken(token: string): Promise<TokenPayload> {
  return new Promise((resolve, reject) => {
    jwt.verify(
      token,
      getKey,
      {
        audience: process.env.AZURE_AD_CLIENT_ID,
        issuer: `https://login.microsoftonline.com/${process.env.AZURE_AD_TENANT_ID}/v2.0`,
        algorithms: ['RS256']
      },
      (err, decoded) => {
        if (err) {
          reject(new UnauthorizedError('Invalid token'));
        } else {
          resolve(decoded as TokenPayload);
        }
      }
    );
  });
}
```

### Authentication Middleware

```typescript
import { Request, Response, NextFunction } from 'express';

export async function authMiddleware(
  req: Request,
  res: Response,
  next: NextFunction
) {
  try {
    // Extract bearer token
    const authHeader = req.headers.authorization;
    if (!authHeader || !authHeader.startsWith('Bearer ')) {
      return res.status(401).json({
        success: false,
        error: {
          code: 'UNAUTHORIZED',
          message: 'Missing or invalid authorization header'
        }
      });
    }

    const token = authHeader.substring(7);
    
    // Validate JWT token
    const payload = await validateToken(token);
    
    // Attach user info to request
    req.user = {
      id: payload.oid,
      email: payload.email,
      tenantId: payload.tid,
      roles: payload.roles || []
    };
    
    // Log authentication event
    await auditLogger.logAuth(req.user.id, req.ip);
    
    next();
  } catch (error) {
    return res.status(401).json({
      success: false,
      error: {
        code: 'UNAUTHORIZED',
        message: 'Authentication failed',
        details: error.message
      }
    });
  }
}
```

## Authorization

### Role-Based Access Control (RBAC)

**Role Definitions**:

```typescript
enum AppRole {
  TenantAdmin = 'Tenant.Admin',
  UserManager = 'User.Manage',
  PolicyManager = 'Policy.Manage',
  AuditReader = 'Audit.Read',
  BillingManager = 'Billing.Manage'
}

interface RolePermissions {
  resources: string[];
  actions: string[];
}

const rolePermissions: Record<AppRole, RolePermissions> = {
  [AppRole.TenantAdmin]: {
    resources: ['*'],
    actions: ['create', 'read', 'update', 'delete']
  },
  [AppRole.UserManager]: {
    resources: ['users', 'permissions', 'invitations'],
    actions: ['create', 'read', 'update', 'delete']
  },
  [AppRole.PolicyManager]: {
    resources: ['policies', 'settings'],
    actions: ['create', 'read', 'update', 'delete']
  },
  [AppRole.AuditReader]: {
    resources: ['audit-logs', 'usage-metrics'],
    actions: ['read']
  },
  [AppRole.BillingManager]: {
    resources: ['subscriptions', 'invoices'],
    actions: ['read', 'update']
  }
};
```

### Authorization Middleware

```typescript
export function requireRole(...allowedRoles: AppRole[]) {
  return (req: Request, res: Response, next: NextFunction) => {
    const userRoles = req.user?.roles || [];
    
    // Check if user has any of the required roles
    const hasRole = allowedRoles.some(role => userRoles.includes(role));
    
    if (!hasRole) {
      return res.status(403).json({
        success: false,
        error: {
          code: 'FORBIDDEN',
          message: 'Insufficient permissions',
          details: `Required roles: ${allowedRoles.join(', ')}`
        }
      });
    }
    
    next();
  };
}

// Usage in routes
router.post('/policies', 
  authMiddleware,
  requireRole(AppRole.TenantAdmin, AppRole.PolicyManager),
  createPolicy
);
```

### Resource-Level Authorization

```typescript
export async function canAccessResource(
  userId: string,
  resourceType: string,
  resourceId: string,
  action: string
): Promise<boolean> {
  // Check if user has direct permission
  const directPermission = await db.query(`
    SELECT 1 FROM Permissions
    WHERE user_id = @userId
    AND resource_type = @resourceType
    AND resource_id = @resourceId
    AND permission_level IN (SELECT level FROM PermissionLevels WHERE action = @action)
  `, { userId, resourceType, resourceId, action });
  
  if (directPermission.length > 0) {
    return true;
  }
  
  // Check if user has role-based permission
  const rolePermission = await db.query(`
    SELECT 1 FROM AdminRoles ar
    JOIN RolePermissions rp ON ar.role_name = rp.role_name
    WHERE ar.user_id = @userId
    AND rp.resource_type = @resourceType
    AND rp.action = @action
    AND ar.is_active = 1
  `, { userId, resourceType, action });
  
  return rolePermission.length > 0;
}
```

## Tenant Isolation

### Middleware for Tenant Context

```typescript
export async function tenantMiddleware(
  req: Request,
  res: Response,
  next: NextFunction
) {
  try {
    // Extract tenant ID from header or token
    const tenantId = req.headers['x-tenant-id'] as string || req.user?.tenantId;
    
    if (!tenantId) {
      return res.status(400).json({
        success: false,
        error: {
          code: 'MISSING_TENANT_ID',
          message: 'Tenant ID is required'
        }
      });
    }
    
    // Verify tenant exists and is active
    const tenant = await tenantService.getTenant(tenantId);
    if (!tenant || tenant.status !== 'Active') {
      return res.status(403).json({
        success: false,
        error: {
          code: 'TENANT_NOT_ACTIVE',
          message: 'Tenant is not active'
        }
      });
    }
    
    // Verify user belongs to this tenant
    if (req.user?.tenantId !== tenantId) {
      await auditLogger.logSecurityEvent('TENANT_MISMATCH', {
        userId: req.user?.id,
        requestedTenant: tenantId,
        userTenant: req.user?.tenantId,
        ip: req.ip
      });
      
      return res.status(403).json({
        success: false,
        error: {
          code: 'FORBIDDEN',
          message: 'Access denied to this tenant'
        }
      });
    }
    
    // Set session context for row-level security
    await db.setSessionContext('tenant_id', tenantId);
    
    // Attach tenant to request
    req.tenant = tenant;
    
    next();
  } catch (error) {
    next(error);
  }
}
```

## Data Protection

### Encryption at Rest

**Azure SQL Database**:
- Transparent Data Encryption (TDE) enabled by default
- AES-256 encryption for all data files
- Automatic key rotation

**Azure Cosmos DB**:
- Encryption at rest enabled by default
- Service-managed keys
- Support for customer-managed keys (CMK)

**Configuration**:
```typescript
// Database connection with encryption
const sqlConfig = {
  server: process.env.SQL_SERVER,
  database: process.env.SQL_DATABASE,
  authentication: {
    type: 'azure-active-directory-msi-app-service'
  },
  options: {
    encrypt: true,
    trustServerCertificate: false,
    enableArithAbort: true
  }
};
```

### Encryption in Transit

**TLS Configuration**:
```typescript
import https from 'https';
import fs from 'fs';

const tlsOptions = {
  minVersion: 'TLSv1.3',
  ciphers: [
    'TLS_AES_256_GCM_SHA384',
    'TLS_CHACHA20_POLY1305_SHA256',
    'TLS_AES_128_GCM_SHA256'
  ].join(':'),
  honorCipherOrder: true,
  cert: fs.readFileSync(process.env.SSL_CERT_PATH),
  key: fs.readFileSync(process.env.SSL_KEY_PATH)
};

const server = https.createServer(tlsOptions, app);
```

### Secrets Management

**Azure Key Vault Integration**:

```typescript
import { SecretClient } from '@azure/keyvault-secrets';
import { DefaultAzureCredential } from '@azure/identity';

class SecretsManager {
  private client: SecretClient;
  private cache: Map<string, { value: string; expires: number }>;

  constructor() {
    const credential = new DefaultAzureCredential();
    const vaultUrl = process.env.KEY_VAULT_URL;
    this.client = new SecretClient(vaultUrl, credential);
    this.cache = new Map();
  }

  async getSecret(secretName: string): Promise<string> {
    // Check cache first
    const cached = this.cache.get(secretName);
    if (cached && cached.expires > Date.now()) {
      return cached.value;
    }

    // Fetch from Key Vault
    const secret = await this.client.getSecret(secretName);
    
    // Cache for 5 minutes
    this.cache.set(secretName, {
      value: secret.value,
      expires: Date.now() + 300000
    });

    return secret.value;
  }

  async setSecret(secretName: string, secretValue: string): Promise<void> {
    await this.client.setSecret(secretName, secretValue);
    this.cache.delete(secretName); // Invalidate cache
  }
}

export const secretsManager = new SecretsManager();
```

### PII Protection

**Field-Level Encryption**:

```typescript
import crypto from 'crypto';

class PIIEncryption {
  private algorithm = 'aes-256-gcm';
  private keyLength = 32;

  async encrypt(plaintext: string): Promise<string> {
    const key = await this.getEncryptionKey();
    const iv = crypto.randomBytes(16);
    const cipher = crypto.createCipheriv(this.algorithm, key, iv);
    
    let encrypted = cipher.update(plaintext, 'utf8', 'base64');
    encrypted += cipher.final('base64');
    
    const authTag = cipher.getAuthTag();
    
    // Return: iv:authTag:encrypted
    return `${iv.toString('base64')}:${authTag.toString('base64')}:${encrypted}`;
  }

  async decrypt(ciphertext: string): Promise<string> {
    const [ivBase64, authTagBase64, encrypted] = ciphertext.split(':');
    const key = await this.getEncryptionKey();
    const iv = Buffer.from(ivBase64, 'base64');
    const authTag = Buffer.from(authTagBase64, 'base64');
    
    const decipher = crypto.createDecipheriv(this.algorithm, key, iv);
    decipher.setAuthTag(authTag);
    
    let decrypted = decipher.update(encrypted, 'base64', 'utf8');
    decrypted += decipher.final('utf8');
    
    return decrypted;
  }

  private async getEncryptionKey(): Promise<Buffer> {
    const keyBase64 = await secretsManager.getSecret('pii-encryption-key');
    return Buffer.from(keyBase64, 'base64');
  }
}

export const piiEncryption = new PIIEncryption();
```

## Network Security

### Virtual Network Integration

**Architecture**:
```
Internet
   │
   ▼
Azure Front Door (WAF)
   │
   ▼
API Management (Gateway)
   │
   ▼
VNet Integration
   │
   ├─► App Service (Private Endpoint)
   │
   ├─► Azure SQL (Private Endpoint)
   │
   └─► Cosmos DB (Private Endpoint)
```

### Web Application Firewall (WAF)

**Azure Front Door Rules**:

```json
{
  "wafPolicy": {
    "managedRules": {
      "managedRuleSets": [
        {
          "ruleSetType": "OWASP",
          "ruleSetVersion": "3.2"
        },
        {
          "ruleSetType": "Microsoft_BotManagerRuleSet",
          "ruleSetVersion": "1.0"
        }
      ]
    },
    "customRules": [
      {
        "name": "RateLimitRule",
        "priority": 1,
        "ruleType": "RateLimitRule",
        "rateLimitThreshold": 100,
        "rateLimitDurationInMinutes": 1,
        "action": "Block"
      },
      {
        "name": "GeoBlockRule",
        "priority": 2,
        "ruleType": "MatchRule",
        "matchConditions": [
          {
            "matchVariable": "RemoteAddr",
            "operator": "GeoMatch",
            "negateCondition": false,
            "matchValue": ["CN", "RU"]
          }
        ],
        "action": "Block"
      }
    ]
  }
}
```

### DDoS Protection

```typescript
// Rate limiting middleware
import rateLimit from 'express-rate-limit';

const limiter = rateLimit({
  windowMs: 60 * 1000, // 1 minute
  max: async (req) => {
    // Different limits based on subscription tier
    const tier = req.tenant?.subscription_tier;
    switch (tier) {
      case 'Free': return 50;
      case 'Pro': return 200;
      case 'Enterprise': return 1000;
      default: return 50;
    }
  },
  message: {
    success: false,
    error: {
      code: 'RATE_LIMIT_EXCEEDED',
      message: 'Too many requests from this tenant'
    }
  },
  standardHeaders: true,
  legacyHeaders: false,
  keyGenerator: (req) => req.tenant?.tenant_id || req.ip
});

app.use('/api/', limiter);
```

## Audit Logging

### Comprehensive Audit Trail

```typescript
interface AuditLog {
  id: string;
  tenant_id: string;
  timestamp: Date;
  event_type: string;
  event_category: 'Authentication' | 'Authorization' | 'UserManagement' | 'PolicyChange' | 'DataAccess';
  severity: 'Info' | 'Warning' | 'Error' | 'Critical';
  actor: {
    user_id: string;
    email: string;
    ip_address: string;
    user_agent: string;
  };
  target: {
    resource_type: string;
    resource_id: string;
    resource_name: string;
  };
  action: {
    name: string;
    result: 'Success' | 'Failure';
    details: any;
  };
  context: {
    session_id: string;
    request_id: string;
    correlation_id: string;
  };
}

class AuditLogger {
  async log(auditLog: Partial<AuditLog>): Promise<void> {
    // Write to Cosmos DB
    await cosmosClient.container('audit-logs').items.create({
      ...auditLog,
      id: uuidv4(),
      timestamp: new Date(),
      _ts: Math.floor(Date.now() / 1000)
    });
    
    // Also write to Application Insights
    appInsights.trackEvent({
      name: auditLog.event_type,
      properties: auditLog
    });
  }

  async logSecurityEvent(eventType: string, details: any): Promise<void> {
    await this.log({
      event_type: eventType,
      event_category: 'Authorization',
      severity: 'Warning',
      action: {
        name: eventType,
        result: 'Failure',
        details
      }
    });
  }
}

export const auditLogger = new AuditLogger();
```

## Compliance

### GDPR Compliance

```typescript
class GDPRService {
  // Right to Access
  async exportUserData(userId: string, tenantId: string): Promise<any> {
    const userData = await db.query(`
      SELECT * FROM Users WHERE user_id = @userId AND tenant_id = @tenantId
    `);
    
    const permissions = await db.query(`
      SELECT * FROM Permissions WHERE user_id = @userId AND tenant_id = @tenantId
    `);
    
    const auditLogs = await cosmosClient.query(
      `SELECT * FROM c WHERE c.actor.user_id = '${userId}' AND c.tenant_id = '${tenantId}'`
    );
    
    return {
      user_data: userData[0],
      permissions,
      audit_logs: auditLogs
    };
  }

  // Right to Erasure
  async deleteUserData(userId: string, tenantId: string): Promise<void> {
    // Anonymize instead of hard delete for audit trail
    await db.query(`
      UPDATE Users
      SET email = 'deleted-user@anonymous.local',
          display_name = 'Deleted User',
          company_name = NULL,
          metadata = NULL,
          status = 'Deleted'
      WHERE user_id = @userId AND tenant_id = @tenantId
    `);
    
    await auditLogger.log({
      tenant_id: tenantId,
      event_type: 'UserDataDeleted',
      event_category: 'UserManagement',
      severity: 'Info',
      target: {
        resource_type: 'User',
        resource_id: userId,
        resource_name: 'User Data'
      },
      action: {
        name: 'DeleteUserData',
        result: 'Success',
        details: { reason: 'GDPR request' }
      }
    });
  }
}
```

### SOC 2 Controls

**Key Control Implementations**:

1. **Access Control (CC6.1)**:
   - Role-based access control
   - Multi-factor authentication
   - Regular access reviews

2. **Encryption (CC6.1)**:
   - TLS 1.3 for data in transit
   - AES-256 for data at rest
   - Key rotation policies

3. **Monitoring (CC7.2)**:
   - Real-time security monitoring
   - Automated alerting
   - Incident response procedures

4. **Audit Logging (CC5.2)**:
   - Comprehensive audit trails
   - Tamper-proof logging
   - Log retention policies

## Security Monitoring

### Threat Detection

```typescript
class ThreatDetection {
  async detectAnomalies(req: Request): Promise<void> {
    // Check for suspicious patterns
    const checks = [
      this.checkBruteForce(req),
      this.checkSQLInjection(req),
      this.checkXSS(req),
      this.checkUnusualAccess(req)
    ];
    
    const threats = await Promise.all(checks);
    const detected = threats.filter(t => t.isActive);
    
    if (detected.length > 0) {
      await this.handleThreat(detected, req);
    }
  }

  private async checkBruteForce(req: Request): Promise<Threat> {
    // Check failed login attempts
    const failedAttempts = await redis.get(`failed-login:${req.ip}`);
    if (failedAttempts && parseInt(failedAttempts) > 5) {
      return {
        type: 'BruteForce',
        severity: 'High',
        isActive: true
      };
    }
    return { type: 'BruteForce', isActive: false };
  }

  private async handleThreat(threats: Threat[], req: Request): Promise<void> {
    // Log to security team
    await auditLogger.logSecurityEvent('THREAT_DETECTED', {
      threats,
      ip: req.ip,
      user: req.user?.email
    });
    
    // Block IP temporarily
    await redis.setex(`blocked-ip:${req.ip}`, 3600, '1');
    
    // Send alert
    await alertService.sendSecurityAlert({
      type: 'ThreatDetected',
      threats,
      ip: req.ip
    });
  }
}
```

## Incident Response

### Security Incident Procedures

1. **Detection**: Automated monitoring and alerting
2. **Containment**: Immediate threat isolation
3. **Eradication**: Remove threat from system
4. **Recovery**: Restore normal operations
5. **Post-Incident**: Review and improve

### Example: Data Breach Response

```typescript
class IncidentResponse {
  async handleDataBreach(incident: DataBreachIncident): Promise<void> {
    // 1. Immediate containment
    await this.isolateAffectedResources(incident.affectedTenants);
    
    // 2. Assess scope
    const scope = await this.assessBreachScope(incident);
    
    // 3. Notify stakeholders
    if (scope.affectedUsers > 0) {
      await this.notifyAffectedUsers(scope.affectedUsers);
      await this.notifyRegulators(scope);
    }
    
    // 4. Document incident
    await this.documentIncident(incident, scope);
    
    // 5. Implement remediation
    await this.implementRemediation(incident.vulnerabilities);
  }
}
```

## Security Best Practices

### Secure Coding Guidelines

1. **Input Validation**:
   ```typescript
   import { validate } from 'class-validator';
   
   async function validateInput<T>(dto: T): Promise<void> {
     const errors = await validate(dto);
     if (errors.length > 0) {
       throw new ValidationError('Invalid input', errors);
     }
   }
   ```

2. **SQL Injection Prevention**:
   ```typescript
   // Always use parameterized queries
   await db.query(
     'SELECT * FROM Users WHERE email = @email',
     { email: userEmail }
   );
   ```

3. **XSS Prevention**:
   ```typescript
   import DOMPurify from 'isomorphic-dompurify';
   
   function sanitizeInput(input: string): string {
     return DOMPurify.sanitize(input);
   }
   ```

## Security Checklist

- [x] JWT validation on all endpoints
- [x] Role-based access control implemented
- [x] Tenant isolation enforced
- [x] Data encryption at rest and in transit
- [x] Secrets stored in Key Vault
- [x] Audit logging for all operations
- [x] Rate limiting enabled
- [x] WAF protection configured
- [x] Security monitoring active
- [x] Incident response plan documented
- [x] GDPR compliance measures
- [x] Regular security assessments

## Next Steps

1. Complete security testing (penetration testing)
2. Conduct security code review
3. Implement automated security scanning
4. Set up security operations center (SOC)
5. Train development team on secure coding
6. Establish regular security audits
