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
