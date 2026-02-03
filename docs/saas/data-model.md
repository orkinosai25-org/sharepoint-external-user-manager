# Data Model

## Overview

This document defines the database schema for the SharePoint External User Manager SaaS backend. The design follows multi-tenant best practices with tenant isolation at the row level.

## Database Technology

**Azure SQL Database** (Serverless tier for MVP)
- Multi-tenant single database approach
- Row-level security with TenantId partitioning
- Auto-pause for cost optimization
- Point-in-time recovery (35 days)

## Schema Diagram

```
┌──────────────────────────────────────────────────────────────┐
│                      Tenant Management                        │
├──────────────────────────────────────────────────────────────┤
│  Tenant                                                       │
│  ├── Id (PK)                                                 │
│  ├── EntraIdTenantId (Unique)                               │
│  ├── OrganizationName                                        │
│  ├── PrimaryAdminEmail                                       │
│  ├── OnboardedDate                                           │
│  ├── Status (Active/Suspended/Cancelled)                     │
│  └── Settings (JSON)                                         │
│                                                              │
│  Subscription                                                │
│  ├── Id (PK)                                                 │
│  ├── TenantId (FK → Tenant.Id)                              │
│  ├── Tier (Free/Pro/Enterprise)                             │
│  ├── StartDate                                               │
│  ├── EndDate                                                 │
│  ├── TrialExpiry                                             │
│  ├── GracePeriodEnd                                          │
│  ├── Status (Trial/Active/Expired/Cancelled)                │
│  ├── MaxUsers (quota)                                        │
│  └── Features (JSON)                                         │
└──────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────┐
│                  Collaboration Policies                       │
├──────────────────────────────────────────────────────────────┤
│  Policy                                                       │
│  ├── Id (PK)                                                 │
│  ├── TenantId (FK → Tenant.Id)                              │
│  ├── PolicyType (ExternalSharing/GuestExpiration/etc)       │
│  ├── Enabled                                                 │
│  ├── Configuration (JSON)                                    │
│  ├── CreatedDate                                             │
│  └── ModifiedDate                                            │
└──────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────┐
│                      Audit & Compliance                       │
├──────────────────────────────────────────────────────────────┤
│  AuditLog                                                     │
│  ├── Id (PK)                                                 │
│  ├── TenantId (FK → Tenant.Id)                              │
│  ├── Timestamp                                               │
│  ├── UserId (Entra ID user)                                  │
│  ├── UserEmail                                               │
│  ├── Action (UserInvited/UserRemoved/PolicyUpdated/etc)     │
│  ├── ResourceType (User/Library/Policy)                      │
│  ├── ResourceId                                              │
│  ├── Details (JSON)                                          │
│  ├── IpAddress                                               │
│  ├── CorrelationId                                           │
│  └── Status (Success/Failed)                                 │
│                                                              │
│  UserAction                                                  │
│  ├── Id (PK)                                                 │
│  ├── TenantId (FK → Tenant.Id)                              │
│  ├── ExternalUserEmail                                       │
│  ├── ActionType (Invited/Removed/PermissionChanged)         │
│  ├── TargetLibrary (SharePoint library URL)                 │
│  ├── PerformedBy                                             │
│  ├── PerformedDate                                           │
│  └── Metadata (JSON - company, project, etc)                │
└──────────────────────────────────────────────────────────────┘
```

## Table Definitions

### Tenant

Stores tenant registration and configuration.

```sql
CREATE TABLE [dbo].[Tenant] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [EntraIdTenantId] NVARCHAR(100) NOT NULL UNIQUE,
    [OrganizationName] NVARCHAR(255) NOT NULL,
    [PrimaryAdminEmail] NVARCHAR(255) NOT NULL,
    [OnboardedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [Status] NVARCHAR(50) NOT NULL DEFAULT 'Active',
    [Settings] NVARCHAR(MAX), -- JSON configuration
    [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [ModifiedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    INDEX IX_Tenant_EntraIdTenantId (EntraIdTenantId)
);
```

**Fields**:
- `Id`: Internal primary key
- `EntraIdTenantId`: Azure AD tenant ID (GUID)
- `OrganizationName`: Company/organization name
- `PrimaryAdminEmail`: Primary contact email
- `OnboardedDate`: When tenant registered
- `Status`: Active, Suspended, Cancelled
- `Settings`: JSON blob for tenant-specific configuration

**Sample Data**:
```json
{
  "Id": 1,
  "EntraIdTenantId": "12345678-1234-1234-1234-123456789abc",
  "OrganizationName": "Contoso Ltd",
  "PrimaryAdminEmail": "admin@contoso.com",
  "OnboardedDate": "2024-01-15T10:00:00Z",
  "Status": "Active",
  "Settings": "{\"timezone\":\"UTC\",\"locale\":\"en-US\"}"
}
```

### Subscription

Tracks subscription tier and licensing status per tenant.

```sql
CREATE TABLE [dbo].[Subscription] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [TenantId] INT NOT NULL,
    [Tier] NVARCHAR(50) NOT NULL, -- Free, Pro, Enterprise
    [StartDate] DATETIME2 NOT NULL,
    [EndDate] DATETIME2,
    [TrialExpiry] DATETIME2,
    [GracePeriodEnd] DATETIME2,
    [Status] NVARCHAR(50) NOT NULL DEFAULT 'Trial',
    [MaxUsers] INT,
    [Features] NVARCHAR(MAX), -- JSON feature flags
    [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [ModifiedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (TenantId) REFERENCES [dbo].[Tenant](Id),
    INDEX IX_Subscription_TenantId (TenantId)
);
```

**Fields**:
- `TenantId`: Foreign key to Tenant table
- `Tier`: Free, Pro, Enterprise
- `StartDate`: Subscription start
- `EndDate`: Subscription end (null = ongoing)
- `TrialExpiry`: Trial period end date
- `GracePeriodEnd`: Grace period after expiry
- `Status`: Trial, Active, Expired, Cancelled
- `MaxUsers`: User quota for tier
- `Features`: JSON feature flags

**Subscription Tiers**:

| Tier | Max Users | Audit History | Export | Advanced Features |
|------|-----------|---------------|--------|-------------------|
| Free | 10 | 30 days | No | No |
| Pro | 100 | 90 days | Yes | Limited |
| Enterprise | Unlimited | 365 days | Yes | Full |

**Sample Data**:
```json
{
  "Id": 1,
  "TenantId": 1,
  "Tier": "Pro",
  "StartDate": "2024-01-15T10:00:00Z",
  "EndDate": null,
  "TrialExpiry": "2024-02-14T10:00:00Z",
  "GracePeriodEnd": null,
  "Status": "Trial",
  "MaxUsers": 100,
  "Features": "{\"auditHistoryDays\":90,\"exportEnabled\":true,\"scheduledReviews\":false}"
}
```

### Policy

Stores collaboration policies per tenant.

```sql
CREATE TABLE [dbo].[Policy] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [TenantId] INT NOT NULL,
    [PolicyType] NVARCHAR(100) NOT NULL,
    [Enabled] BIT NOT NULL DEFAULT 1,
    [Configuration] NVARCHAR(MAX), -- JSON policy settings
    [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [ModifiedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (TenantId) REFERENCES [dbo].[Tenant](Id),
    INDEX IX_Policy_TenantId (TenantId)
);
```

**Policy Types**:
- `ExternalSharingDefault`: Default external sharing settings
- `GuestExpiration`: Auto-expiration for guest users
- `RequireApproval`: Require approval for external invites
- `AllowedDomains`: Domain whitelist/blacklist
- `ReviewCampaigns`: Periodic access review settings

**Sample Data**:
```json
{
  "Id": 1,
  "TenantId": 1,
  "PolicyType": "GuestExpiration",
  "Enabled": true,
  "Configuration": "{\"expirationDays\":90,\"notifyBeforeDays\":7}"
}
```

### AuditLog

Immutable audit trail for all system operations.

```sql
CREATE TABLE [dbo].[AuditLog] (
    [Id] BIGINT IDENTITY(1,1) PRIMARY KEY,
    [TenantId] INT NOT NULL,
    [Timestamp] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UserId] NVARCHAR(100), -- Entra ID user object ID
    [UserEmail] NVARCHAR(255),
    [Action] NVARCHAR(100) NOT NULL,
    [ResourceType] NVARCHAR(50),
    [ResourceId] NVARCHAR(255),
    [Details] NVARCHAR(MAX), -- JSON additional details
    [IpAddress] NVARCHAR(50),
    [CorrelationId] NVARCHAR(100),
    [Status] NVARCHAR(50) NOT NULL,
    FOREIGN KEY (TenantId) REFERENCES [dbo].[Tenant](Id),
    INDEX IX_AuditLog_TenantId_Timestamp (TenantId, Timestamp DESC),
    INDEX IX_AuditLog_CorrelationId (CorrelationId)
);
```

**Action Types**:
- `TenantOnboarded`
- `SubscriptionUpdated`
- `UserInvited`
- `UserRemoved`
- `PermissionChanged`
- `PolicyUpdated`
- `AuditExported`

**Sample Data**:
```json
{
  "Id": 1001,
  "TenantId": 1,
  "Timestamp": "2024-01-15T14:30:00Z",
  "UserId": "user-obj-id-123",
  "UserEmail": "admin@contoso.com",
  "Action": "UserInvited",
  "ResourceType": "ExternalUser",
  "ResourceId": "partner@external.com",
  "Details": "{\"library\":\"Marketing Docs\",\"permissions\":\"Read\"}",
  "IpAddress": "203.0.113.42",
  "CorrelationId": "cor-12345",
  "Status": "Success"
}
```

### UserAction

Tracks external user management actions (subset of AuditLog for quick queries).

```sql
CREATE TABLE [dbo].[UserAction] (
    [Id] BIGINT IDENTITY(1,1) PRIMARY KEY,
    [TenantId] INT NOT NULL,
    [ExternalUserEmail] NVARCHAR(255) NOT NULL,
    [ActionType] NVARCHAR(50) NOT NULL,
    [TargetLibrary] NVARCHAR(500),
    [PerformedBy] NVARCHAR(255) NOT NULL,
    [PerformedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [Metadata] NVARCHAR(MAX), -- JSON: company, project, custom fields
    FOREIGN KEY (TenantId) REFERENCES [dbo].[Tenant](Id),
    INDEX IX_UserAction_TenantId_Email (TenantId, ExternalUserEmail),
    INDEX IX_UserAction_TenantId_Date (TenantId, PerformedDate DESC)
);
```

**Sample Data**:
```json
{
  "Id": 501,
  "TenantId": 1,
  "ExternalUserEmail": "partner@external.com",
  "ActionType": "Invited",
  "TargetLibrary": "https://contoso.sharepoint.com/sites/project1/docs",
  "PerformedBy": "admin@contoso.com",
  "PerformedDate": "2024-01-15T14:30:00Z",
  "Metadata": "{\"company\":\"Partner Corp\",\"project\":\"Q1 Campaign\"}"
}
```

## Multi-Tenant Security

### Row-Level Security (RLS)

All queries MUST include tenant context to prevent data leakage.

```sql
-- Example: Secure query pattern
SELECT * FROM AuditLog 
WHERE TenantId = @TenantId 
  AND Timestamp >= @StartDate
ORDER BY Timestamp DESC;
```

### Tenant Isolation Enforcement

Application-level enforcement in data access layer:

```typescript
export class DatabaseService {
  async query<T>(
    sql: string, 
    params: any, 
    tenantId: number
  ): Promise<T[]> {
    // ALWAYS inject tenantId filter
    const secureSql = this.injectTenantFilter(sql, tenantId);
    return await this.pool.query(secureSql, params);
  }
}
```

## Indexes

### Performance Indexes

```sql
-- Tenant lookups by Entra ID
CREATE INDEX IX_Tenant_EntraIdTenantId ON Tenant(EntraIdTenantId);

-- Subscription by tenant
CREATE INDEX IX_Subscription_TenantId ON Subscription(TenantId);

-- Audit logs by tenant and date (most common query)
CREATE INDEX IX_AuditLog_TenantId_Timestamp 
  ON AuditLog(TenantId, Timestamp DESC);

-- User actions by email for quick user history
CREATE INDEX IX_UserAction_TenantId_Email 
  ON UserAction(TenantId, ExternalUserEmail);
```

## Data Retention Policies

| Table | Retention | Cleanup Strategy |
|-------|-----------|------------------|
| Tenant | Indefinite | Soft delete (Status='Cancelled') |
| Subscription | Indefinite | Historical record |
| Policy | Indefinite | Version history in AuditLog |
| AuditLog | 7 years (Free: 30 days, Pro: 90 days, Enterprise: 365 days active query) | Archive to blob storage |
| UserAction | 1 year | Summarized in AuditLog |

## Migration Scripts

### Initial Schema Creation

See `backend/database/migrations/001_initial_schema.sql`

### Seed Data for Development

See `backend/database/seeds/dev_seed.sql`

## Backup & Recovery

### Automated Backups
- **Frequency**: Daily at 2:00 AM UTC
- **Retention**: 7 days (short-term), 35 days (point-in-time)
- **Recovery**: Azure SQL automated backups

### Disaster Recovery
- **Geo-Replication**: Read replica in secondary region
- **Failover Time**: < 30 minutes
- **Data Loss**: < 5 minutes (RPO)

## Future Enhancements

### Phase 2
- **Cosmos DB Integration**: For global distribution and high throughput
- **Partitioning**: Physical database per tenant for large customers
- **Archive Storage**: Cold storage for old audit logs (Azure Blob)

### Phase 3
- **Graph Database**: For relationship mapping (user-library-permissions)
- **Time-Series DB**: For analytics and reporting (Azure Data Explorer)
