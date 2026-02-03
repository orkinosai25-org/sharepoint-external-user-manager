# SaaS Backend Architecture

## Overview

The SharePoint External User Manager SaaS backend is designed as a secure, scalable, multi-tenant system that enables organizations to manage external collaboration and user access across SharePoint sites and libraries.

## System Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           Client Applications Layer                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                               │
│  ┌────────────────────┐    ┌──────────────────┐    ┌──────────────────┐    │
│  │   SPFx Web Part    │    │   Admin Portal   │    │   Mobile Apps    │    │
│  │   (React/TS)       │    │   (React SPA)    │    │   (Future)       │    │
│  └──────────┬─────────┘    └────────┬─────────┘    └────────┬─────────┘    │
│             │                       │                       │              │
│             └───────────────────────┼───────────────────────┘              │
│                                     │                                        │
└─────────────────────────────────────┼────────────────────────────────────────┘
                                      │
                                      ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                          API Gateway & Security Layer                        │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                               │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │              Azure API Management (APIM)                                │ │
│  │  • SSL/TLS Termination      • Rate Limiting                            │ │
│  │  • API Versioning           • Request/Response Transformation          │ │
│  │  • CORS Configuration       • Analytics & Monitoring                   │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                                                               │
└─────────────────────────────────────────────────────────────────────────────┘
                                      │
                                      ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                       Authentication & Authorization Layer                   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                               │
│  ┌──────────────────┐    ┌──────────────────┐    ┌──────────────────┐     │
│  │   Entra ID       │◄──►│   JWT           │◄──►│   Key Vault      │     │
│  │   Multi-Tenant   │    │   Validation     │    │   • Secrets      │     │
│  │   App Reg        │    │   Middleware     │    │   • Certs        │     │
│  └──────────────────┘    └──────────────────┘    └──────────────────┘     │
│                                                                               │
└─────────────────────────────────────────────────────────────────────────────┘
                                      │
                                      ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                          Application Services Layer                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                               │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                    Node.js/Express API Server                          │ │
│  │                     (Azure App Service / Functions)                    │ │
│  ├────────────────────────────────────────────────────────────────────────┤ │
│  │                                                                          │ │
│  │  Middleware Stack:                                                      │ │
│  │  • JWT Authentication        • Tenant Isolation                        │ │
│  │  • Licensing Enforcement     • Error Handling                          │ │
│  │  • Audit Logging             • Request Validation                      │ │
│  │                                                                          │ │
│  ├────────────────────────────────────────────────────────────────────────┤ │
│  │                                                                          │ │
│  │  API Routes:                                                            │ │
│  │  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐       │ │
│  │  │  Tenant         │  │  External User  │  │  Collaboration  │       │ │
│  │  │  Onboarding     │  │  Management     │  │  Policies       │       │ │
│  │  └─────────────────┘  └─────────────────┘  └─────────────────┘       │ │
│  │  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐       │ │
│  │  │  Licensing /    │  │  Audit Logs     │  │  Subscription   │       │ │
│  │  │  Subscription   │  │  & Reporting    │  │  Management     │       │ │
│  │  └─────────────────┘  └─────────────────┘  └─────────────────┘       │ │
│  │                                                                          │ │
│  ├────────────────────────────────────────────────────────────────────────┤ │
│  │                                                                          │ │
│  │  Business Services:                                                     │ │
│  │  • TenantService           • SubscriptionService                       │ │
│  │  • UserManagementService   • AuditService                              │ │
│  │  • PolicyService           • NotificationService                       │ │
│  │  • GraphAPIClient          • SharePointClient                          │ │
│  │                                                                          │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                                                               │
└─────────────────────────────────────────────────────────────────────────────┘
                                      │
                                      ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                            Data Persistence Layer                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                               │
│  ┌──────────────────┐    ┌──────────────────┐    ┌──────────────────┐     │
│  │   Azure SQL DB   │    │   Azure Cosmos   │    │   Azure Storage  │     │
│  │                  │    │   DB (NoSQL)     │    │   Account        │     │
│  │  • Tenant Config │    │  • Audit Logs    │    │  • Files/Blobs   │     │
│  │  • Subscriptions │    │  • Usage Metrics │    │  • Reports       │     │
│  │  • User Records  │    │  • Telemetry     │    │  • Backups       │     │
│  │  • Policies      │    │  • Cache         │    │                  │     │
│  └──────────────────┘    └──────────────────┘    └──────────────────┘     │
│                                                                               │
└─────────────────────────────────────────────────────────────────────────────┘
                                      │
                                      ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                         External Integration Layer                           │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                               │
│  ┌──────────────────┐    ┌──────────────────┐    ┌──────────────────┐     │
│  │  Microsoft Graph │    │   SharePoint     │    │   Azure          │     │
│  │  API             │    │   Online REST    │    │   Marketplace    │     │
│  │  • User Mgmt     │    │   API            │    │   • Fulfillment  │     │
│  │  • Tenant Info   │    │  • Sites/Lists   │    │   • Webhook      │     │
│  │  • Permissions   │    │  • Permissions   │    │   • Billing      │     │
│  └──────────────────┘    └──────────────────┘    └──────────────────┘     │
│                                                                               │
└─────────────────────────────────────────────────────────────────────────────┘
                                      │
                                      ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                     Monitoring & Observability Layer                         │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                               │
│  ┌──────────────────┐    ┌──────────────────┐    ┌──────────────────┐     │
│  │  Application     │    │   Log Analytics  │    │   Azure Monitor  │     │
│  │  Insights        │    │   Workspace      │    │   • Alerts       │     │
│  │  • Performance   │    │  • Centralized   │    │   • Dashboards   │     │
│  │  • Exceptions    │    │  • Queries       │    │   • Health       │     │
│  └──────────────────┘    └──────────────────┘    └──────────────────┘     │
│                                                                               │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Core Components

### 1. API Gateway (Azure API Management)

**Purpose**: Entry point for all client requests with security, throttling, and routing.

**Responsibilities**:
- SSL/TLS termination
- JWT token validation (pre-auth)
- Rate limiting per subscription tier
- CORS policy enforcement
- Request/response transformation
- API versioning and routing
- Analytics and monitoring

**Configuration**:
```xml
<policies>
    <inbound>
        <cors>
            <allowed-origins>
                <origin>https://*.sharepoint.com</origin>
                <origin>https://*.microsoft.com</origin>
            </allowed-origins>
        </cors>
        <validate-jwt>
            <openid-config url="https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration" />
        </validate-jwt>
        <rate-limit-by-key calls="100" renewal-period="60" counter-key="@(context.Request.Headers.GetValueOrDefault("X-Tenant-ID"))" />
    </inbound>
</policies>
```

### 2. Backend API Service

**Technology Stack**:
- **Runtime**: Node.js 18.x LTS
- **Framework**: Express.js 4.x
- **Language**: TypeScript 5.x
- **Hosting**: Azure App Service (Linux) or Azure Functions

**Project Structure**:
```
backend/
├── src/
│   ├── middleware/
│   │   ├── auth.middleware.ts          # JWT validation
│   │   ├── tenant.middleware.ts        # Tenant isolation
│   │   ├── licensing.middleware.ts     # Subscription enforcement
│   │   ├── audit.middleware.ts         # Audit logging
│   │   └── error.middleware.ts         # Error handling
│   ├── routes/
│   │   ├── tenant.routes.ts            # Onboarding endpoints
│   │   ├── user.routes.ts              # External user management
│   │   ├── policy.routes.ts            # Collaboration policies
│   │   ├── subscription.routes.ts      # Licensing/billing
│   │   └── audit.routes.ts             # Audit log queries
│   ├── services/
│   │   ├── TenantService.ts
│   │   ├── UserManagementService.ts
│   │   ├── PolicyService.ts
│   │   ├── SubscriptionService.ts
│   │   ├── AuditService.ts
│   │   ├── GraphAPIClient.ts
│   │   └── SharePointClient.ts
│   ├── models/
│   │   ├── Tenant.ts
│   │   ├── User.ts
│   │   ├── Policy.ts
│   │   ├── Subscription.ts
│   │   └── AuditLog.ts
│   ├── config/
│   │   ├── database.config.ts
│   │   ├── auth.config.ts
│   │   └── subscription-tiers.config.ts
│   └── app.ts
├── infrastructure/
│   ├── bicep/
│   │   ├── main.bicep
│   │   ├── app-service.bicep
│   │   ├── database.bicep
│   │   └── key-vault.bicep
│   └── terraform/
├── tests/
└── package.json
```

### 3. Multi-Tenant Data Architecture

**Strategy**: Shared database with tenant isolation via tenant_id column

**Primary Database**: Azure SQL Database (for relational data)
- Tenant metadata
- Subscription information
- User records
- Policies and configurations

**Secondary Storage**: Azure Cosmos DB (for high-volume data)
- Audit logs
- Usage metrics
- Telemetry data
- Session cache

**Tenant Isolation**:
```typescript
// Row-level security in SQL
CREATE SECURITY POLICY TenantIsolationPolicy
ADD FILTER PREDICATE dbo.fn_tenantAccessPredicate(tenant_id) ON dbo.Users,
ADD BLOCK PREDICATE dbo.fn_tenantAccessPredicate(tenant_id) ON dbo.Users
WITH (STATE = ON);
```

### 4. Authentication & Authorization

**Identity Provider**: Microsoft Entra ID (Azure AD)

**App Registration Configuration**:
- **Type**: Multi-tenant web application
- **Redirect URIs**: API endpoints for token validation
- **Required Permissions**:
  - Microsoft Graph: `User.Read.All`, `Directory.Read.All`
  - SharePoint: `Sites.Manage.All`, `AllSites.Manage`

**Authorization Model**:
```typescript
interface UserClaims {
  sub: string;              // User ID
  tid: string;              // Tenant ID
  oid: string;              // Object ID
  roles: string[];          // App roles
  scp: string;              // Scopes
  email: string;
}

enum AppRole {
  TenantAdmin = 'Tenant.Admin',
  UserManager = 'User.Manage',
  PolicyManager = 'Policy.Manage',
  AuditReader = 'Audit.Read'
}
```

### 5. Licensing & Subscription Tiers

**Tiers**:

| Tier | Price | Features |
|------|-------|----------|
| **Free** | $0/month | • Up to 10 external users<br>• Basic audit logs (30 days)<br>• Email support |
| **Pro** | $49/month | • Up to 100 external users<br>• Advanced policies<br>• Audit logs (1 year)<br>• Priority support |
| **Enterprise** | $199/month | • Unlimited external users<br>• Custom policies<br>• Unlimited audit logs<br>• Dedicated support<br>• SLA 99.9% |

**Enforcement**:
```typescript
interface SubscriptionLimits {
  maxExternalUsers: number;
  auditLogRetentionDays: number;
  advancedPolicies: boolean;
  apiRateLimit: number;
  supportLevel: 'email' | 'priority' | 'dedicated';
}
```

### 6. Monitoring & Observability

**Application Insights**:
- Request/response telemetry
- Exception tracking
- Dependency monitoring
- Custom events and metrics

**Log Analytics**:
- Centralized logging
- KQL queries for analysis
- Dashboards and reports

**Alerts**:
- High error rate (>5%)
- Slow response time (>2s)
- Failed authentications
- License violations

## Scalability Design

### Horizontal Scaling
- **App Service**: Scale out to 10+ instances
- **Database**: Read replicas for load distribution
- **Cosmos DB**: Auto-scale throughput

### Caching Strategy
- **In-Memory**: Node.js cache for frequently accessed data
- **Distributed**: Redis for session state
- **CDN**: Static assets via Azure Front Door

### Performance Targets
- **API Response Time**: <500ms (p95)
- **Concurrent Tenants**: 1000+
- **Concurrent Users per Tenant**: 500+
- **API Throughput**: 10,000 req/min

## Security Considerations

1. **Authentication**: OAuth 2.0 / OpenID Connect via Entra ID
2. **Authorization**: Role-based access control (RBAC)
3. **Data Encryption**: At rest (AES-256) and in transit (TLS 1.3)
4. **Secrets Management**: Azure Key Vault
5. **Network Security**: Virtual Network integration, Private Endpoints
6. **Compliance**: GDPR, SOC 2, ISO 27001 ready

## Disaster Recovery

- **RPO (Recovery Point Objective)**: 1 hour
- **RTO (Recovery Time Objective)**: 4 hours
- **Backup Strategy**: Automated daily backups with 35-day retention
- **Geographic Redundancy**: Multi-region deployment (primary + secondary)

## Cost Estimation (Monthly)

| Service | Tier | Cost |
|---------|------|------|
| App Service | Basic B1 | $13 |
| Azure SQL | Basic 5 DTU | $5 |
| Cosmos DB | Serverless | $25 (usage-based) |
| Key Vault | Standard | $0.03 per 10K ops |
| Application Insights | 1GB/day | $2.30 |
| **Total** | | **~$45-60/month** |

*Note: Costs scale with usage. Production environment with APIM Premium and higher tiers will be higher.*

## Deployment Strategy

1. **Development**: Continuous deployment from feature branches
2. **Staging**: Manual approval from main branch
3. **Production**: Manual approval with change management process

**Blue-Green Deployment**: Zero-downtime deployments using deployment slots

## Next Steps

1. Complete detailed API specification
2. Implement authentication middleware
3. Set up database schemas and migrations
4. Implement core business services
5. Create infrastructure-as-code templates
6. Set up CI/CD pipelines
7. Implement monitoring and alerting
