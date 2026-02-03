# SaaS Backend Architecture

## Overview

The SharePoint External User Manager SaaS backend is designed as a scalable, multi-tenant solution powered by Azure services, leveraging Microsoft Startup Support for resources and growth. The architecture follows modern cloud-native principles with strong security, monitoring, and licensing enforcement.

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                          Client Layer (Multi-Tenant)                                │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                     │
│  ┌──────────────────┐    ┌──────────────────┐    ┌──────────────────┐            │
│  │  SPFx Web Part   │    │  Mobile Apps     │    │  Power Platform  │            │
│  │  (React/TS)      │    │  (Future)        │    │  (Power Apps)    │            │
│  │  - Fluent UI     │    │  - iOS/Android   │    │  - Power Automate│            │
│  │  - MSAL Auth     │    │  - MAUI/.NET     │    │  - Connectors    │            │
│  └──────────────────┘    └──────────────────┘    └──────────────────┘            │
│           │                      │                        │                        │
└───────────┼──────────────────────┼────────────────────────┼────────────────────────┘
            │                      │                        │
            └──────────────────────┼────────────────────────┘
                                   │
                          HTTPS (TLS 1.2+)
                                   │
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                        Azure Front Door / CDN                                       │
│  - Global Load Balancing                                                           │
│  - WAF Protection (OWASP Top 10)                                                   │
│  - DDoS Mitigation                                                                 │
│  - SSL/TLS Termination                                                             │
└─────────────────────────────────────────────────────────────────────────────────────┘
                                   │
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                      Azure API Management (Premium)                                 │
│  - API Gateway & Routing                                                           │
│  - Rate Limiting (per tenant)                                                      │
│  - JWT Token Validation                                                            │
│  - Request/Response Transformation                                                 │
│  - API Versioning (/v1, /v2)                                                       │
└─────────────────────────────────────────────────────────────────────────────────────┘
                                   │
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                   Azure Functions - Serverless API Layer                           │
│                       (.NET 8 / C# - Isolated Worker)                              │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                     │
│  ┌─────────────────────────────────────────────────────────────────────────┐      │
│  │  Authentication & Authorization Middleware                               │      │
│  │  - JWT Validation (Entra ID)                                            │      │
│  │  - Tenant Context Resolution                                            │      │
│  │  - Role-Based Access Control (RBAC)                                     │      │
│  │  - License Tier Enforcement                                             │      │
│  └─────────────────────────────────────────────────────────────────────────┘      │
│                                                                                     │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐                │
│  │ Tenant Onboarding│  │ User Management  │  │ Policy Mgmt      │                │
│  │ Functions        │  │ Functions        │  │ Functions        │                │
│  │                  │  │                  │  │                  │                │
│  │ - Register       │  │ - List Users     │  │ - Get Policies   │                │
│  │ - Verify Admin   │  │ - Invite User    │  │ - Update Policies│                │
│  │ - Grant Perms    │  │ - Revoke Access  │  │ - Enforce Rules  │                │
│  │ - Provision DB   │  │ - Update Perms   │  │                  │                │
│  └──────────────────┘  └──────────────────┘  └──────────────────┘                │
│                                                                                     │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐                │
│  │ Audit Log        │  │ Licensing        │  │ Marketplace      │                │
│  │ Functions        │  │ Functions        │  │ Functions        │                │
│  │                  │  │                  │  │                  │                │
│  │ - Log Events     │  │ - Check Status   │  │ - Fulfillment    │                │
│  │ - Query Logs     │  │ - Update Sub     │  │ - Webhook Handler│                │
│  │ - Compliance     │  │ - Trial Logic    │  │ - Provisioning   │                │
│  │ - Retention      │  │ - Tier Gates     │  │                  │                │
│  └──────────────────┘  └──────────────────┘  └──────────────────┘                │
│                                                                                     │
└─────────────────────────────────────────────────────────────────────────────────────┘
                    │                      │                      │
       ┌────────────┴──────────┬───────────┴──────────┬──────────┴────────────┐
       │                       │                      │                       │
┌──────▼────────┐    ┌────────▼────────┐   ┌────────▼────────┐   ┌─────────▼──────┐
│ Azure Key     │    │ Application     │   │ Azure Storage   │   │ Microsoft Graph│
│ Vault         │    │ Insights        │   │ Account         │   │ API            │
│               │    │                 │   │                 │   │                │
│ - Secrets     │    │ - Telemetry     │   │ - Blob Storage  │   │ - User Mgmt    │
│ - Certs       │    │ - Logging       │   │ - Queue Storage │   │ - SharePoint   │
│ - Connection  │    │ - Metrics       │   │ - Table Storage │   │ - Admin APIs   │
│   Strings     │    │ - Alerts        │   │                 │   │                │
└───────────────┘    └─────────────────┘   └─────────────────┘   └────────────────┘

┌─────────────────────────────────────────────────────────────────────────────────────┐
│                        Data Layer - Multi-Tenant Isolation                          │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                     │
│  ┌──────────────────────────────────────────────────────────────────────────┐     │
│  │                      Azure SQL Database (Elastic Pool)                    │     │
│  │                     Tenant Isolation: Database-per-Tenant                 │     │
│  ├──────────────────────────────────────────────────────────────────────────┤     │
│  │                                                                           │     │
│  │  Master DB                   Tenant DBs (Isolated)                       │     │
│  │  ┌─────────────┐            ┌──────────────┐  ┌──────────────┐          │     │
│  │  │ Tenants     │            │ tenant_abc   │  │ tenant_xyz   │  ...     │     │
│  │  │ Subscript.  │            │              │  │              │          │     │
│  │  │ Licensing   │            │ - Libraries  │  │ - Libraries  │          │     │
│  │  │ Routing     │            │ - Users      │  │ - Users      │          │     │
│  │  └─────────────┘            │ - Permissions│  │ - Permissions│          │     │
│  │                             │ - Audit Logs │  │ - Audit Logs │          │     │
│  │                             └──────────────┘  └──────────────┘          │     │
│  └──────────────────────────────────────────────────────────────────────────┘     │
│                                                                                     │
│  ┌──────────────────────────────────────────────────────────────────────────┐     │
│  │                      Azure Cosmos DB (NoSQL)                              │     │
│  │                   Global Distribution - Multi-Region                      │     │
│  ├──────────────────────────────────────────────────────────────────────────┤     │
│  │                                                                           │     │
│  │  - Shared Metadata & Configuration (Partition by TenantId)               │     │
│  │  - Real-time Audit Event Streaming                                       │     │
│  │  - Usage Analytics & Metrics                                             │     │
│  │  - Session State & Caching                                               │     │
│  │  - Feature Flags & Configuration                                         │     │
│  └──────────────────────────────────────────────────────────────────────────┘     │
└─────────────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────────────┐
│                    Identity & Access Management                                     │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                     │
│  ┌──────────────────────────────────────────────────────────────────────────┐     │
│  │                   Microsoft Entra ID (Azure AD)                           │     │
│  │                    Multi-Tenant App Registration                          │     │
│  ├──────────────────────────────────────────────────────────────────────────┤     │
│  │                                                                           │     │
│  │  Authentication:                                                          │     │
│  │  - OAuth 2.0 / OpenID Connect                                            │     │
│  │  - JWT Token Issuance                                                    │     │
│  │  - MFA Enforcement                                                       │     │
│  │  - Conditional Access Policies                                           │     │
│  │                                                                           │     │
│  │  Authorization:                                                           │     │
│  │  - Role Assignments (SaaS Admin, Tenant Admin, Library Owner)           │     │
│  │  - Permission Scopes (Sites.ReadWrite.All, User.Read.All)               │     │
│  │  - Admin Consent Flow                                                    │     │
│  │                                                                           │     │
│  │  Tenant Onboarding:                                                       │     │
│  │  - Verify Admin Identity                                                 │     │
│  │  - Validate Tenant Context                                               │     │
│  │  - Grant Application Permissions                                         │     │
│  │  - Provision Tenant Resources                                            │     │
│  └──────────────────────────────────────────────────────────────────────────┘     │
└─────────────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────────────┐
│                         Licensing & Subscription Management                         │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                     │
│  Tier Structure:                                                                   │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐                            │
│  │ Free Tier    │  │ Pro Tier     │  │ Enterprise   │                            │
│  │              │  │              │  │ Tier         │                            │
│  │ - 5 Users    │  │ - 50 Users   │  │ - Unlimited  │                            │
│  │ - 3 Libs     │  │ - 25 Libs    │  │ - Unlimited  │                            │
│  │ - Basic      │  │ - Advanced   │  │ - Premium    │                            │
│  │   Features   │  │   Features   │  │   Features   │                            │
│  │ - 30d Trial  │  │ - Priority   │  │ - Dedicated  │                            │
│  │              │  │   Support    │  │   Support    │                            │
│  └──────────────┘  └──────────────┘  └──────────────┘                            │
│                                                                                     │
│  Enforcement:                                                                       │
│  - Middleware checks subscription status before processing requests                │
│  - Returns 402 (Payment Required) for expired subscriptions                        │
│  - Returns 403 (Forbidden) for feature not available in current tier              │
│  - Grace period: 7 days after subscription expiration                             │
│  - Trial period: 30 days for new tenants                                          │
│                                                                                     │
│  Integration:                                                                       │
│  - Azure Marketplace fulfillment API (Phase 2)                                    │
│  - Custom billing portal (Phase 1 - MVP)                                          │
│  - Subscription status reflected in SPFx UI                                       │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

## Key Components

### 1. API Layer - Azure Functions (.NET 8)

**Technology Stack:**
- Runtime: .NET 8 (LTS) with Isolated Worker Process
- Language: C# 12
- Hosting: Azure Functions Consumption Plan (auto-scaling)
- Triggers: HTTP with Azure AD authentication

**Function Organization:**
```
/backend/
├── src/
│   ├── Functions/
│   │   ├── TenantOnboarding/
│   │   │   ├── RegisterTenantFunction.cs
│   │   │   ├── VerifyAdminFunction.cs
│   │   │   └── ProvisionResourcesFunction.cs
│   │   ├── UserManagement/
│   │   │   ├── ListUsersFunction.cs
│   │   │   ├── InviteUserFunction.cs
│   │   │   ├── RevokeAccessFunction.cs
│   │   │   └── UpdatePermissionsFunction.cs
│   │   ├── PolicyManagement/
│   │   │   ├── GetPoliciesFunction.cs
│   │   │   └── UpdatePoliciesFunction.cs
│   │   ├── AuditLog/
│   │   │   ├── LogEventFunction.cs
│   │   │   └── QueryLogsFunction.cs
│   │   └── Licensing/
│   │       ├── CheckSubscriptionFunction.cs
│   │       └── UpdateSubscriptionFunction.cs
│   ├── Middleware/
│   │   ├── AuthenticationMiddleware.cs
│   │   ├── TenantIsolationMiddleware.cs
│   │   ├── LicenseEnforcementMiddleware.cs
│   │   └── AuditLoggingMiddleware.cs
│   ├── Services/
│   │   ├── GraphApiService.cs
│   │   ├── SharePointService.cs
│   │   ├── TenantService.cs
│   │   └── LicensingService.cs
│   └── Models/
│       ├── Tenant.cs
│       ├── ExternalUser.cs
│       ├── Policy.cs
│       └── AuditEvent.cs
└── tests/
    └── Functions.Tests/
```

### 2. Data Layer - Multi-Tenant Architecture

**Isolation Strategy: Database-per-Tenant**

**Azure SQL Database (Elastic Pool):**
- **Master Database:** Tenant registry, routing, licensing metadata
- **Tenant Databases:** Isolated database per tenant for strong isolation
- **Schema:** Identical schema across all tenant databases
- **Migrations:** Automated migration deployment to all tenant databases
- **Backup:** Geo-redundant backup with 35-day retention

**Azure Cosmos DB (NoSQL):**
- **Purpose:** Real-time data, caching, global distribution
- **Containers:**
  - `SharedMetadata`: Global configuration (partitioned by tenantId)
  - `AuditEvents`: Real-time audit streaming
  - `UsageMetrics`: Analytics and telemetry
  - `SessionCache`: User session state

### 3. Authentication & Authorization

**Entra ID Multi-Tenant Registration:**
```json
{
  "appId": "generate-guid",
  "displayName": "SharePoint External User Manager",
  "signInAudience": "AzureADMultipleOrgs",
  "web": {
    "redirectUris": [
      "https://api.spexternal.com/auth/callback",
      "https://localhost:7071/auth/callback"
    ]
  },
  "requiredResourceAccess": [
    {
      "resourceAppId": "00000003-0000-0000-c000-000000000000",
      "resourceAccess": [
        {
          "id": "Sites.ReadWrite.All",
          "type": "Scope"
        },
        {
          "id": "User.Read.All",
          "type": "Scope"
        }
      ]
    }
  ]
}
```

**Role Model:**
- **SaaS Admin:** Full system access, manage all tenants
- **Tenant Admin:** Full access within tenant scope
- **Library Owner:** Manage specific libraries and users
- **Library Contributor:** Limited user management
- **Read-Only User:** View-only access

### 4. Security Architecture

**Network Security:**
- Azure Front Door with WAF (OWASP rules)
- DDoS protection (Azure DDoS Standard)
- Private endpoints for backend services
- Network Security Groups (NSG) for traffic control

**Data Security:**
- Encryption at rest (Azure Storage Service Encryption)
- Encryption in transit (TLS 1.2+)
- Azure Key Vault for all secrets/certificates
- Customer-managed encryption keys (optional)

**Application Security:**
- Input validation and sanitization
- SQL injection prevention (parameterized queries)
- XSS protection (content security policy)
- CSRF protection (anti-forgery tokens)

### 5. Monitoring & Observability

**Application Insights:**
- Request/response telemetry
- Dependency tracking (SQL, Cosmos, Graph API)
- Exception tracking with stack traces
- Custom events and metrics
- Distributed tracing (correlation IDs)

**Alerts:**
- High error rate (>5% in 5 minutes)
- Slow response time (>2s p95)
- Database connection failures
- License enforcement failures
- Subscription expiration warnings

**Dashboards:**
- Real-time API health and performance
- Per-tenant usage metrics
- License tier distribution
- Revenue and subscription analytics

### 6. Scalability & Performance

**Auto-Scaling:**
- Azure Functions: 0-200 instances (consumption plan)
- SQL Elastic Pool: Scale up/down based on DTU usage
- Cosmos DB: Auto-scale RU/s (400-4000)
- API Management: Premium tier with multi-region

**Caching Strategy:**
- Cosmos DB for hot data (session state, config)
- Application-level caching (memory cache)
- CDN for static assets (Azure Front Door)

**Performance Targets:**
- API Response Time: <500ms (p95), <200ms (p50)
- Availability: 99.9% uptime SLA
- Database Query: <100ms (p95)
- Graph API Calls: Batched and optimized

## Deployment Architecture

**Environments:**
- **Development:** Auto-deploy on PR merge to develop branch
- **Staging:** Manual approval, smoke tests before production
- **Production:** Blue-green deployment with traffic shifting

**Infrastructure as Code:**
- Bicep templates for all Azure resources
- GitHub Actions for CI/CD automation
- Environment-specific configuration via Key Vault

**Disaster Recovery:**
- RTO (Recovery Time Objective): 4 hours
- RPO (Recovery Point Objective): 1 hour
- Geo-redundant backup for all data
- Automated failover to secondary region

## Cost Optimization

**Azure Services (Estimated Monthly - MVP):**
- Azure Functions (Consumption): $50-200
- Azure SQL (Elastic Pool): $200-500
- Cosmos DB (Auto-scale): $100-300
- API Management (Consumption): $100-200
- Application Insights: $50-100
- Key Vault: $5-10
- Storage Account: $20-50

**Total Estimated: $525-1,360/month** (scales with tenant growth)

**Optimization Strategies:**
- Use consumption plans for variable workloads
- Auto-scale based on demand
- Archive old data to cool/archive storage tiers
- Reserved instances for predictable workloads

## Next Steps

1. **Phase 1 (Weeks 1-2):** Core infrastructure setup and authentication
2. **Phase 2 (Weeks 3-4):** API implementation and data layer
3. **Phase 3 (Weeks 5-6):** Licensing system and marketplace integration
4. **Phase 4 (Weeks 7-8):** Testing, monitoring, and production deployment

## References

- [Azure Functions Best Practices](https://learn.microsoft.com/azure/azure-functions/functions-best-practices)
- [Multi-Tenant SaaS Patterns](https://learn.microsoft.com/azure/architecture/guide/multitenant/overview)
- [Entra ID Authentication](https://learn.microsoft.com/entra/identity-platform/)
- [Microsoft Graph API](https://learn.microsoft.com/graph/overview)
