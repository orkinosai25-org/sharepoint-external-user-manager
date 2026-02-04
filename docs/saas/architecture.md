# SaaS Architecture

## Overview

The SharePoint External User Manager SaaS backend is a multi-tenant solution built on Azure serverless infrastructure. It provides secure API endpoints for the SPFx web part to manage external users and collaboration policies with tenant isolation, subscription enforcement, and audit logging.

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         Client Layer (SPFx)                              │
│  ┌────────────────┐  ┌────────────────┐  ┌────────────────────────┐    │
│  │ External User  │  │ Connect Tenant │  │  Subscription Status   │    │
│  │   Manager UI   │  │   Admin Page   │  │     Admin Page         │    │
│  └────────┬───────┘  └────────┬───────┘  └──────────┬─────────────┘    │
└───────────┼──────────────────┼──────────────────────┼──────────────────┘
            │                  │                      │
            │ HTTPS + Bearer Token Authentication    │
            ▼                  ▼                      ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                      API Gateway (Azure APIM - Future)                   │
│  • Rate Limiting                                                        │
│  • Request/Response Transformation                                      │
│  • API Versioning                                                       │
└─────────────────────────────────────────────────────────────────────────┘
            │
            ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                     Azure Functions (Serverless API)                     │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │  Authentication Middleware                                        │   │
│  │  • JWT Token Validation (Entra ID)                               │   │
│  │  • Tenant Context Resolution                                     │   │
│  │  • Role-based Authorization (Owner/Admin/ReadOnly)               │   │
│  └──────────────────────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │  Subscription Middleware                                          │   │
│  │  • License Tier Validation (Free/Pro/Enterprise)                 │   │
│  │  • Feature Gate Enforcement                                      │   │
│  │  • Trial Expiration Logic                                        │   │
│  └──────────────────────────────────────────────────────────────────┘   │
│  ┌──────────────────┬────────────────────┬─────────────────────────┐   │
│  │ Tenant Mgmt      │ User Management    │  Policy & Audit         │   │
│  │ - POST /onboard  │ - GET /ext-users   │  - GET/PUT /policies    │   │
│  │ - GET /me        │ - POST /invite     │  - GET /audit           │   │
│  │ - GET /sub       │ - POST /remove     │                         │   │
│  └──────────────────┴────────────────────┴─────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────┘
            │                           │
            ▼                           ▼
┌──────────────────────────┐  ┌────────────────────────────────────────┐
│   Azure SQL Database     │  │   Microsoft Graph API                  │
│   (Multi-tenant)         │  │   • SharePoint Online APIs             │
│  ┌────────────────────┐  │  │   • User Management                    │
│  │ Tenant             │  │  │   • Permission Management              │
│  │ Subscription       │  │  │   • Admin Consent Flows                │
│  │ AuditLog           │  │  └────────────────────────────────────────┘
│  │ Policy             │  │
│  │ UserAction         │  │
│  └────────────────────┘  │
└──────────────────────────┘
            │
            ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                   Monitoring & Observability                             │
│  ┌──────────────────┬────────────────────┬─────────────────────────┐   │
│  │ Application      │  Azure Key Vault   │   Azure Monitor         │   │
│  │  Insights        │  • Secrets         │   • Alerts              │   │
│  │  • Telemetry     │  • Connection Strs │   • Dashboards          │   │
│  │  • Traces        │  • Certificates    │   • Metrics             │   │
│  └──────────────────┴────────────────────┴─────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────┘
```

## Components

### 1. Client Layer (SPFx Web Part)
- **React-based UI**: Fluent UI components for user management
- **Authentication**: Obtains Azure AD tokens for API calls
- **API Client**: HTTP client with bearer token authentication
- **Admin Pages**: Tenant connection and subscription management

### 2. API Layer (Azure Functions)

#### Function App Structure
```
backend/
├── src/
│   ├── functions/
│   │   ├── tenant/
│   │   │   ├── onboard.ts          # POST /tenants/onboard
│   │   │   ├── getTenant.ts        # GET /tenants/me
│   │   │   └── getSubscription.ts  # GET /tenants/subscription
│   │   ├── users/
│   │   │   ├── listUsers.ts        # GET /external-users
│   │   │   ├── inviteUser.ts       # POST /external-users/invite
│   │   │   └── removeUser.ts       # POST /external-users/remove
│   │   ├── policies/
│   │   │   ├── getPolicies.ts      # GET /policies
│   │   │   └── updatePolicies.ts   # PUT /policies
│   │   └── audit/
│   │       └── getAuditLogs.ts     # GET /audit
│   ├── middleware/
│   │   ├── auth.ts                 # JWT validation
│   │   ├── subscription.ts         # License enforcement
│   │   └── tenantContext.ts        # Tenant resolution
│   ├── services/
│   │   ├── database.ts             # Data access layer
│   │   ├── graphClient.ts          # Microsoft Graph integration
│   │   └── auditLogger.ts          # Audit logging service
│   └── models/
│       ├── tenant.ts
│       ├── subscription.ts
│       ├── user.ts
│       └── policy.ts
```

### 3. Data Layer

#### Multi-Tenant Database Design
- **Tenant Isolation**: TenantId column on all tables with row-level security
- **Connection Pool**: Shared connection pool with tenant filtering
- **Schema**: Single database, multi-tenant tables

#### Core Tables
```sql
Tenant              -- Tenant registration and config
Subscription        -- Subscription tier and billing status
AuditLog            -- Audit trail for all operations
Policy              -- Tenant-level collaboration policies
UserAction          -- External user management actions
```

### 4. External Integrations

#### Microsoft Graph API
- **Authentication**: Application permissions + delegated user permissions
- **Scopes Required**:
  - `Sites.Read.All` - Read SharePoint sites
  - `User.ReadWrite.All` - Manage external users
  - `Directory.Read.All` - Read directory information

#### Entra ID (Azure AD)
- **Multi-tenant App Registration**
- **Admin Consent Flow**: First-time tenant onboarding
- **Token Validation**: JWT signature verification

## Multi-Tenant Architecture

### Tenant Isolation Strategy

**Row-Level Isolation**: All database queries are scoped by `tenantId`

```typescript
// Example: Tenant-scoped query
const users = await db.query(
  'SELECT * FROM UserAction WHERE tenantId = @tenantId',
  { tenantId: context.tenantId }
);
```

### Tenant Context Resolution

```typescript
// Middleware extracts tenant from token claims
export async function resolveTenant(req: HttpRequest): Promise<TenantContext> {
  const token = validateJWT(req);
  const tenantId = token.claims.tid; // Azure AD tenant ID
  const tenant = await db.getTenant(tenantId);
  
  if (!tenant) {
    throw new Error('Tenant not onboarded');
  }
  
  return {
    tenantId: tenant.id,
    subscriptionTier: tenant.subscriptionTier,
    features: tenant.enabledFeatures
  };
}
```

## Environments

### Development
- **Function App**: `func-spexternal-dev`
- **Database**: `sql-spexternal-dev`
- **Storage**: `stspexternaldev`
- **App Insights**: `ai-spexternal-dev`

### Staging
- **Function App**: `func-spexternal-stage`
- **Database**: `sql-spexternal-stage`
- **Storage**: `stspexternalstage`
- **App Insights**: `ai-spexternal-stage`

### Production
- **Function App**: `func-spexternal-prod`
- **Database**: `sql-spexternal-prod`
- **Storage**: `stspexternalprod`
- **App Insights**: `ai-spexternal-prod`

## Security Architecture

### Authentication Flow
1. User accesses SPFx web part in SharePoint
2. SPFx obtains Azure AD token using MSAL
3. SPFx includes token in API request header
4. Backend validates JWT signature and claims
5. Backend resolves tenant context from token
6. Backend authorizes user based on roles

### Authorization Model

**Roles**:
- **Tenant Owner**: Full administrative access
- **Tenant Admin**: Manage users and policies
- **Read Only**: View-only access

**Permission Checks**:
```typescript
if (context.role === 'ReadOnly' && req.method !== 'GET') {
  throw new ForbiddenError('Insufficient permissions');
}
```

## Scalability Design

### Horizontal Scaling
- **Azure Functions**: Auto-scale 0-200 instances based on load
- **Database**: Azure SQL with elastic pool for tenant isolation
- **Storage**: Geo-redundant storage for high availability

### Performance Optimization
- **Caching**: In-memory cache for tenant metadata
- **Connection Pooling**: Reuse database connections
- **Async Processing**: Queue-based processing for bulk operations
- **CDN**: Static assets served via Azure CDN

## Monitoring Strategy

### Application Insights
- **Request/Response Tracking**: All API calls logged
- **Performance Metrics**: Response times, dependency calls
- **Custom Events**: Subscription changes, tenant onboarding
- **Exception Tracking**: Errors and stack traces

### Alerts
- **High Error Rate**: > 5% error rate triggers alert
- **Slow Response**: > 2s response time triggers alert
- **Subscription Expiry**: Notify 7 days before expiration

### Dashboards
- **Operations Dashboard**: Real-time API health
- **Business Metrics**: Active tenants, API usage by tier
- **Security Dashboard**: Failed auth attempts, anomalies

## Disaster Recovery

### Backup Strategy
- **Database**: Automated daily backups, 7-day retention
- **Point-in-Time Recovery**: Up to 35 days
- **Geo-Replication**: Failover to secondary region

### Recovery Procedures
- **RTO (Recovery Time Objective)**: 4 hours
- **RPO (Recovery Point Objective)**: 1 hour
- **Automated Failover**: Traffic Manager for regional failover

## Compliance & Governance

### Data Residency
- Tenant data stored in customer's preferred Azure region
- Compliant with GDPR, CCPA requirements

### Audit Logging
- All user actions logged with timestamp, user, and tenant
- Immutable audit trail for compliance
- Retention: 7 years for audit logs

### Data Retention
- Active tenant data: Indefinite
- Cancelled subscriptions: 90-day grace period
- Deleted tenants: 30-day soft delete, then purged

## Cost Optimization

### Consumption-Based Pricing
- **Azure Functions**: Pay per execution (~$0.20 per million executions)
- **Database**: Serverless tier with auto-pause
- **Storage**: Cool tier for archived audit logs

### Estimated Monthly Cost (per 100 tenants)
- Functions: $50
- Database: $100
- Storage: $20
- Application Insights: $30
- **Total**: ~$200/month

## Future Enhancements

### Phase 2 (Post-MVP)
- API Management for advanced rate limiting
- Redis Cache for improved performance
- Cosmos DB for global distribution
- Azure Front Door for CDN + WAF

### Phase 3 (Enterprise)
- Multi-region active-active deployment
- Advanced analytics and reporting
- Custom domain support
- Workflow automation engine
The SharePoint External User Manager SaaS backend is a multi-tenant Azure-based solution that provides secure, scalable API services for managing external users and collaboration policies across SharePoint tenants.

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           Client Layer (SPFx)                               │
│  ┌────────────────┐   ┌────────────────┐   ┌────────────────┐            │
│  │  Web Part UI   │   │  Auth Service  │   │  API Client    │            │
│  │  (React)       │──▶│  (Azure AD)    │──▶│  (HTTP)        │            │
│  └────────────────┘   └────────────────┘   └────────────────┘            │
└──────────────────────────────────┬──────────────────────────────────────────┘
                                   │ HTTPS + Bearer Token
                                   ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                          API Gateway Layer                                  │
│  ┌────────────────────────────────────────────────────────────────┐        │
│  │  Azure Functions (HTTP Triggers)                               │        │
│  │  - JWT Validation Middleware                                   │        │
│  │  - Tenant Context Resolution                                   │        │
│  │  - Rate Limiting & Throttling                                  │        │
│  │  - License/Subscription Enforcement                            │        │
│  └────────────────────────────────────────────────────────────────┘        │
└──────────────────────────────────┬──────────────────────────────────────────┘
                                   │
                    ┌──────────────┼──────────────┐
                    ▼              ▼              ▼
┌──────────────────────┐  ┌──────────────┐  ┌──────────────────┐
│   Business Logic     │  │  Microsoft   │  │  Data Layer      │
│   - Tenant Mgmt      │  │  Graph API   │  │  - Azure SQL     │
│   - User Mgmt        │  │  - Users     │  │  - Cosmos DB     │
│   - Policy Mgmt      │  │  - Sites     │  │  - Blob Storage  │
│   - Audit Logging    │  │  - Groups    │  │                  │
└──────────────────────┘  └──────────────┘  └──────────────────┘
```

## Component Architecture

### 1. API Gateway (Azure Functions)

**Technology Stack:**
- Runtime: Node.js 18 LTS
- Language: TypeScript
- Framework: Azure Functions v4
- Hosting: Consumption Plan (auto-scale)

**Key Functions:**
```
/backend
├── tenants/
│   ├── onboard.ts          # POST /tenants/onboard
│   ├── get-tenant.ts       # GET /tenants/me
│   └── update-tenant.ts    # PUT /tenants/me
├── external-users/
│   ├── list.ts             # GET /external-users
│   ├── invite.ts           # POST /external-users/invite
│   ├── remove.ts           # POST /external-users/remove
│   └── get-details.ts      # GET /external-users/{id}
├── policies/
│   ├── get.ts              # GET /policies
│   └── update.ts           # PUT /policies
├── audit/
│   └── list.ts             # GET /audit
└── shared/
    ├── auth/
    │   ├── jwt-validator.ts
    │   ├── tenant-resolver.ts
    │   └── rbac.ts
    ├── graph/
    │   └── graph-client.ts
    ├── storage/
    │   ├── tenant-repository.ts
    │   ├── audit-repository.ts
    │   └── subscription-repository.ts
    └── middleware/
        ├── license-check.ts
        ├── rate-limit.ts
        └── error-handler.ts
```

### 2. Multi-Tenant Data Model

**Tenant Isolation Strategy:** Database per tenant with shared metadata

#### Azure SQL Database (Tenant-Specific Data)
```sql
-- Database naming: sp_external_user_mgr_{tenantId}
-- Each tenant gets isolated database

Tables per tenant:
- ExternalUsers
- UserInvitations
- AccessPermissions
- SharePointLibraries
- UserMetadata (company, project)
- AuditLogs (operational)
- TenantConfiguration
```

#### Cosmos DB (Shared System Data)
```json
{
  "containers": {
    "Tenants": {
      "partitionKey": "/tenantId",
      "documents": "Tenant registration, settings"
    },
    "Subscriptions": {
      "partitionKey": "/tenantId",
      "documents": "License info, billing status"
    },
    "GlobalAuditLogs": {
      "partitionKey": "/tenantId",
      "documents": "Cross-tenant audit trail"
    },
    "UsageMetrics": {
      "partitionKey": "/tenantId",
      "documents": "API usage, feature usage"
    }
  }
}
```

### 3. Authentication Flow

```
1. User opens SPFx Web Part
   ↓
2. SPFx acquires Azure AD token (on behalf of user)
   - Scope: api://spexternal.com/user_impersonation
   ↓
3. SPFx calls Backend API with Bearer token
   - Header: Authorization: Bearer {token}
   - Header: X-Tenant-ID: {tenantId}
   ↓
4. Azure Function validates JWT
   - Issuer check (Azure AD)
   - Audience check (our API)
   - Signature verification
   ↓
5. Tenant Context Resolution
   - Extract tenantId from token claims
   - Validate against X-Tenant-ID header
   - Load tenant configuration from Cosmos DB
   ↓
6. License/Subscription Check
   - Validate subscription status
   - Check feature entitlements
   - Enforce rate limits
   ↓
7. Execute business logic
   - Call Microsoft Graph API
   - Access tenant-specific database
   - Log audit trail
   ↓
8. Return response to SPFx
```

### 4. Environments

| Environment | Purpose | Azure Subscription | Deployment |
|------------|---------|-------------------|------------|
| **Development** | Developer testing | Dev Sub | Automatic on push to `dev` branch |
| **Staging** | QA and pre-prod | Staging Sub | Automatic on push to `staging` branch |
| **Production** | Live customer tenants | Prod Sub | Manual approval required |

**Resource Naming Convention:**
```
{environment}-{region}-{service}-{suffix}

Examples:
- dev-eastus-func-spexternal
- prod-eastus-sql-spexternal-001
- stg-eastus-cosmos-spexternal
```

### 5. Security Architecture

#### Network Security
- All endpoints require HTTPS (TLS 1.2+)
- Azure Functions behind Application Gateway (optional for premium)
- IP whitelisting for admin operations
- DDoS protection via Azure Front Door

#### Identity & Access
- Azure AD multi-tenant application
- OAuth 2.0 / OpenID Connect
- Role-Based Access Control (RBAC)
- Conditional Access policies support

#### Data Security
- Encryption at rest (Azure Storage Service Encryption)
- Encryption in transit (TLS 1.2+)
- Secrets in Azure Key Vault
- Database connection strings from Key Vault
- Managed Identity for Azure service authentication

#### Compliance
- Audit logging for all data modifications
- Data retention policies (configurable per tenant)
- GDPR compliance (data export, deletion)
- SOC 2 ready infrastructure

### 6. Scalability & Performance

#### Auto-Scaling
- Azure Functions: 0-200 instances (Consumption plan)
- SQL Database: Elastic pool for tenant databases
- Cosmos DB: Auto-scale throughput (400-10000 RU/s)

#### Caching Strategy
- In-memory cache for tenant configuration (5 min TTL)
- Redis cache for frequently accessed data (optional)
- CDN for static assets

#### Performance Targets
- API response time: < 200ms (P95)
- Tenant onboarding: < 5 seconds
- User invitation: < 1 second
- Concurrent requests: 1000+ per region

### 7. Monitoring & Observability

#### Application Insights
- Request/response tracking with correlation IDs
- Exception and error tracking
- Custom events for business metrics
- Dependency tracking (Graph API, SQL, Cosmos)

#### Alerts
- HTTP 5xx errors > 1% of requests
- API response time > 2 seconds (P95)
- Failed authentication attempts > 10 per minute
- Database connection failures
- Subscription expiration warnings

#### Dashboards
- Real-time API health status
- Tenant usage metrics
- Subscription status overview
- Error rate and latency trends

### 8. Disaster Recovery

**Backup Strategy:**
- Azure SQL: Automated backups (7-35 days retention)
- Cosmos DB: Continuous backup (30 days)
- Configuration: Infrastructure as Code in Git

**Recovery Targets:**
- RTO (Recovery Time Objective): 4 hours
- RPO (Recovery Point Objective): 1 hour

### 9. Cost Optimization

**Consumption-Based Services:**
- Azure Functions: Pay per execution (first 1M free)
- Cosmos DB: Pay per RU/s consumed
- Application Insights: Pay per GB ingested

**Cost Controls:**
- Auto-shutdown for dev environments (nights/weekends)
- Archive old audit logs to cold storage (> 90 days)
- Right-size SQL databases based on usage
- Reserved instances for predictable workloads

**Estimated Monthly Cost (Production):**
- Azure Functions: $50-200 (based on usage)
- Cosmos DB: $100-500 (based on throughput)
- SQL Database: $200-1000 (based on tenant count)
- Application Insights: $50-200
- **Total: ~$400-2000/month** (scales with tenant count)

## API Flow Examples

### Example 1: Tenant Onboarding
```
POST /tenants/onboard
{
  "tenantId": "contoso.onmicrosoft.com",
  "adminEmail": "admin@contoso.com",
  "companyName": "Contoso Ltd",
  "subscriptionTier": "trial"
}

Backend Process:
1. Validate Azure AD consent
2. Create tenant record in Cosmos DB
3. Provision tenant SQL database
4. Initialize default policies
5. Create audit log entry
6. Return tenant API key
```

### Example 2: List External Users
```
GET /external-users?filter=active&company=Acme

Backend Process:
1. Validate JWT token
2. Resolve tenant context
3. Check license (feature: user listing)
4. Query tenant SQL database
5. Call Graph API for live user status
6. Merge and return results
7. Log access in audit trail
```

## Next Steps

1. **Phase 1 (MVP)**: Implement core endpoints (tenants, users, policies, audit)
2. **Phase 2**: Add subscription enforcement and marketplace integration
3. **Phase 3**: Advanced features (approvals, risk flags, review campaigns)
4. **Phase 4**: Microsoft Commercial Marketplace listing

## References

- [Data Model Documentation](./data-model.md)
- [Security Controls](./security.md)
- [API Specification](./api-spec.md)
- [Onboarding Flow](./onboarding.md)
