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
# Data Model Documentation

## Overview

This document defines the complete data model for the SharePoint External User Manager SaaS platform, including database schemas, relationships, and data flow patterns.

## Multi-Tenant Data Architecture

### Strategy
- **Primary**: Azure SQL Database (one per tenant)
- **Shared Metadata**: Azure Cosmos DB (partitioned by tenantId)
- **Blob Storage**: Azure Storage Account for files/exports

### Tenant Isolation
Every tenant gets an isolated SQL database with the naming pattern:
```
sp_external_user_mgr_{sanitized_tenant_id}
```

## Cosmos DB Schema (Shared System Data)

### Container: Tenants

**Partition Key**: `/tenantId`

```json
{
  "id": "tenant-guid-123",
  "tenantId": "contoso.onmicrosoft.com",
  "displayName": "Contoso Ltd",
  "status": "active",
  "subscriptionTier": "pro",
  "subscriptionStatus": "active",
  "trialEndDate": "2024-06-30T23:59:59Z",
  "billingEmail": "billing@contoso.com",
  "adminEmail": "admin@contoso.com",
  "createdDate": "2024-01-15T10:00:00Z",
  "lastModifiedDate": "2024-01-20T14:30:00Z",
  "settings": {
    "apiBaseUrl": "https://api.spexternal.com",
    "webhookUrl": null,
    "features": {
      "auditExport": true,
      "bulkOperations": true,
      "advancedReporting": true,
      "customPolicies": false
    }
  },
  "azureAdAppId": "app-guid-456",
  "onboardingCompleted": true,
  "dataLocation": "eastus"
}
```

### Container: Subscriptions

**Partition Key**: `/tenantId`

```json
{
  "id": "sub-guid-789",
  "tenantId": "contoso.onmicrosoft.com",
  "tier": "pro",
  "status": "active",
  "startDate": "2024-01-15T00:00:00Z",
  "endDate": "2025-01-15T00:00:00Z",
  "autoRenew": true,
  "billingCycle": "annual",
  "pricing": {
    "amount": 999.00,
    "currency": "USD",
    "perSeat": false
  },
  "limits": {
    "maxExternalUsers": 500,
    "maxLibraries": 100,
    "apiCallsPerMonth": 100000,
    "auditRetentionDays": 365,
    "maxAdmins": 10
  },
  "usage": {
    "externalUsersCount": 127,
    "librariesCount": 23,
    "apiCallsThisMonth": 12450,
    "storageUsedMB": 250
  },
  "paymentMethod": "invoice",
  "marketplaceIntegration": {
    "enabled": false,
    "marketplaceSubscriptionId": null,
    "offerName": null
  },
  "createdDate": "2024-01-15T10:05:00Z",
  "lastModifiedDate": "2024-01-20T14:30:00Z"
}
```

### Container: GlobalAuditLogs

**Partition Key**: `/tenantId`

```json
{
  "id": "audit-guid-101",
  "tenantId": "contoso.onmicrosoft.com",
  "timestamp": "2024-01-20T14:35:22Z",
  "eventType": "user.invite",
  "actor": {
    "userId": "user-guid-202",
    "email": "admin@contoso.com",
    "displayName": "John Admin"
  },
  "target": {
    "resourceType": "externalUser",
    "resourceId": "extuser-guid-303",
    "email": "partner@external.com"
  },
  "action": "invite",
  "status": "success",
  "metadata": {
    "libraryId": "lib-guid-404",
    "libraryName": "Partner Documents",
    "permissions": "read",
    "ipAddress": "203.0.113.45",
    "userAgent": "Mozilla/5.0...",
    "correlationId": "corr-guid-505"
  },
  "changes": {
    "before": null,
    "after": {
      "status": "invited",
      "permissions": "read"
    }
  }
}
```

### Container: UsageMetrics

**Partition Key**: `/tenantId`

```json
{
  "id": "metrics-2024-01-20",
  "tenantId": "contoso.onmicrosoft.com",
  "date": "2024-01-20",
  "metrics": {
    "apiCalls": {
      "total": 450,
      "byEndpoint": {
        "/external-users": 200,
        "/policies": 50,
        "/audit": 150,
        "/tenants/me": 50
      }
    },
    "activeUsers": {
      "internalAdmins": 3,
      "externalUsers": 127
    },
    "operations": {
      "userInvites": 12,
      "userRemovals": 3,
      "policyUpdates": 2
    },
    "performance": {
      "avgResponseTimeMs": 145,
      "p95ResponseTimeMs": 320,
      "errorRate": 0.02
    }
  },
  "timestamp": "2024-01-20T23:59:59Z"
}
```

## Azure SQL Schema (Per-Tenant Data)

### Table: ExternalUsers

```sql
CREATE TABLE ExternalUsers (
    UserId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Email NVARCHAR(255) NOT NULL,
    DisplayName NVARCHAR(255),
    FirstName NVARCHAR(100),
    LastName NVARCHAR(100),
    Company NVARCHAR(255),
    Project NVARCHAR(255),
    Department NVARCHAR(100),
    InvitedBy NVARCHAR(255) NOT NULL,
    InvitedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastAccessDate DATETIME2,
    Status NVARCHAR(50) NOT NULL, -- 'invited', 'active', 'expired', 'revoked'
    ExpirationDate DATETIME2,
    AzureAdGuestId UNIQUEIDENTIFIER,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    CONSTRAINT CK_Status CHECK (Status IN ('invited', 'active', 'expired', 'revoked')),
    INDEX IX_Email (Email),
    INDEX IX_Company (Company),
    INDEX IX_Project (Project),
    INDEX IX_Status (Status),
    INDEX IX_InvitedDate (InvitedDate)
);
```

### Table: UserLibraryAccess

```sql
CREATE TABLE UserLibraryAccess (
    AccessId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    LibraryId UNIQUEIDENTIFIER NOT NULL,
    PermissionLevel NVARCHAR(50) NOT NULL, -- 'read', 'contribute', 'fullcontrol'
    GrantedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    GrantedBy NVARCHAR(255) NOT NULL,
    ExpirationDate DATETIME2,
    Status NVARCHAR(50) NOT NULL, -- 'active', 'expired', 'revoked'
    SharePointPermissionId NVARCHAR(255),
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    FOREIGN KEY (UserId) REFERENCES ExternalUsers(UserId),
    FOREIGN KEY (LibraryId) REFERENCES SharePointLibraries(LibraryId),
    CONSTRAINT CK_PermissionLevel CHECK (PermissionLevel IN ('read', 'contribute', 'fullcontrol')),
    CONSTRAINT CK_AccessStatus CHECK (Status IN ('active', 'expired', 'revoked')),
    INDEX IX_UserId (UserId),
    INDEX IX_LibraryId (LibraryId),
    INDEX IX_Status (Status)
);
```

### Table: SharePointLibraries

```sql
CREATE TABLE SharePointLibraries (
    LibraryId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    SiteUrl NVARCHAR(500) NOT NULL,
    LibraryName NVARCHAR(255) NOT NULL,
    LibraryUrl NVARCHAR(500) NOT NULL,
    Description NVARCHAR(1000),
    Owner NVARCHAR(255) NOT NULL,
    SharePointSiteId UNIQUEIDENTIFIER,
    SharePointLibraryId UNIQUEIDENTIFIER,
    ExternalSharingEnabled BIT NOT NULL DEFAULT 1,
    RequireApproval BIT NOT NULL DEFAULT 0,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastSyncDate DATETIME2,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    INDEX IX_SiteUrl (SiteUrl),
    INDEX IX_Owner (Owner),
    UNIQUE INDEX UX_LibraryUrl (LibraryUrl)
);
```

### Table: UserInvitations

```sql
CREATE TABLE UserInvitations (
    InvitationId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER,
    Email NVARCHAR(255) NOT NULL,
    DisplayName NVARCHAR(255),
    LibraryId UNIQUEIDENTIFIER NOT NULL,
    PermissionLevel NVARCHAR(50) NOT NULL,
    InvitedBy NVARCHAR(255) NOT NULL,
    InvitedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    Status NVARCHAR(50) NOT NULL, -- 'pending', 'accepted', 'expired', 'cancelled'
    InvitationMessage NVARCHAR(2000),
    ExpirationDate DATETIME2,
    AcceptedDate DATETIME2,
    SharePointInvitationId NVARCHAR(255),
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    FOREIGN KEY (LibraryId) REFERENCES SharePointLibraries(LibraryId),
    CONSTRAINT CK_InvitationStatus CHECK (Status IN ('pending', 'accepted', 'expired', 'cancelled')),
    INDEX IX_Email (Email),
    INDEX IX_Status (Status),
    INDEX IX_InvitedDate (InvitedDate)
);
```

### Table: AuditLogs

```sql
CREATE TABLE AuditLogs (
    AuditId BIGINT IDENTITY(1,1) PRIMARY KEY,
    Timestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    EventType NVARCHAR(100) NOT NULL,
    Actor NVARCHAR(255) NOT NULL,
    Action NVARCHAR(100) NOT NULL,
    ResourceType NVARCHAR(100),
    ResourceId UNIQUEIDENTIFIER,
    Status NVARCHAR(50) NOT NULL,
    Details NVARCHAR(MAX), -- JSON
    IpAddress NVARCHAR(50),
    UserAgent NVARCHAR(500),
    CorrelationId UNIQUEIDENTIFIER,
    
    INDEX IX_Timestamp (Timestamp),
    INDEX IX_EventType (EventType),
    INDEX IX_Actor (Actor),
    INDEX IX_CorrelationId (CorrelationId)
);
```

### Table: TenantPolicies

```sql
CREATE TABLE TenantPolicies (
    PolicyId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PolicyName NVARCHAR(100) NOT NULL,
    PolicyType NVARCHAR(50) NOT NULL, -- 'expiration', 'approval', 'restriction'
    IsEnabled BIT NOT NULL DEFAULT 1,
    Configuration NVARCHAR(MAX), -- JSON
    CreatedBy NVARCHAR(255) NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy NVARCHAR(255),
    ModifiedDate DATETIME2,
    
    INDEX IX_PolicyType (PolicyType),
    UNIQUE INDEX UX_PolicyName (PolicyName)
);
```

### Table: TenantConfiguration

```sql
CREATE TABLE TenantConfiguration (
    ConfigId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ConfigKey NVARCHAR(100) NOT NULL,
    ConfigValue NVARCHAR(MAX),
    DataType NVARCHAR(50) NOT NULL, -- 'string', 'number', 'boolean', 'json'
    Description NVARCHAR(500),
    IsEditable BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    UNIQUE INDEX UX_ConfigKey (ConfigKey)
);
```

## Entity Relationships

```
┌─────────────────────┐
│  ExternalUsers      │
│  - UserId (PK)      │
│  - Email            │
│  - Company          │
│  - Project          │
└──────────┬──────────┘
           │
           │ 1:N
           │
           ▼
┌─────────────────────┐      N:1      ┌─────────────────────┐
│ UserLibraryAccess   │◄──────────────│ SharePointLibraries │
│ - AccessId (PK)     │               │ - LibraryId (PK)    │
│ - UserId (FK)       │               │ - LibraryName       │
│ - LibraryId (FK)    │               │ - SiteUrl           │
│ - PermissionLevel   │               └─────────────────────┘
└─────────────────────┘                         │
                                                │ 1:N
                                                │
                                                ▼
                                       ┌─────────────────────┐
                                       │ UserInvitations     │
                                       │ - InvitationId (PK) │
                                       │ - LibraryId (FK)    │
                                       │ - Email             │
                                       │ - Status            │
                                       └─────────────────────┘
```

## Data Flow Patterns

### 1. User Invitation Flow
```
1. API Request → POST /external-users/invite
2. Create UserInvitations record (status: pending)
3. Call Microsoft Graph API (send invitation)
4. Update UserInvitations (SharePointInvitationId)
5. Create AuditLog entry
6. Return invitation details
```

### 2. User Access Query Flow
```
1. API Request → GET /external-users
2. Query ExternalUsers table (tenant DB)
3. Join with UserLibraryAccess
4. Optionally query Graph API for live status
5. Cache results (5 min TTL)
6. Create AuditLog entry (if configured)
7. Return user list
```

### 3. Subscription Check Flow
```
1. Middleware intercepts API request
2. Extract tenantId from JWT
3. Query Cosmos DB Subscriptions container
4. Check subscription.status === 'active'
5. Check subscription.limits vs actual usage
6. Allow/Deny request
7. Log usage metrics
```

## Data Retention Policies

| Data Type | Retention Period | Action After Expiry |
|-----------|-----------------|---------------------|
| Active External Users | While access is active | Mark as deleted (soft delete) |
| Revoked External Users | 90 days | Archive to blob storage |
| Audit Logs (Operational) | Subscription-dependent | Move to cold storage |
| Audit Logs (Compliance) | 7 years | Move to cold storage |
| Invitation Records | 1 year | Archive to blob storage |
| Usage Metrics | 2 years | Aggregate and archive |
| Tenant Configuration | While tenant is active | N/A |

## Indexing Strategy

### High-Priority Indexes
```sql
-- Most frequent queries
CREATE INDEX IX_ExternalUsers_Status_InvitedDate 
    ON ExternalUsers(Status, InvitedDate DESC);

CREATE INDEX IX_AuditLogs_Timestamp_EventType 
    ON AuditLogs(Timestamp DESC, EventType);

-- Covering indexes for common queries
CREATE INDEX IX_UserLibraryAccess_UserId_Status 
    ON UserLibraryAccess(UserId, Status) 
    INCLUDE (LibraryId, PermissionLevel, GrantedDate);
```

## Data Migration Scripts

### Initial Schema Setup
```sql
-- See /backend/migrations/001_initial_schema.sql
-- Creates all tables with constraints
-- Inserts default configuration
```

### Tenant Provisioning
```sql
-- See /backend/migrations/provision_tenant_database.sql
-- Creates new database for tenant
-- Runs all migration scripts
-- Initializes default data
```

## GDPR Compliance

### Data Subject Rights
1. **Right to Access**: Export all user data via API
2. **Right to Erasure**: Hard delete user records and cascade
3. **Right to Portability**: JSON/CSV export of user data
4. **Right to Rectification**: Update user information via API

### Implementation
```typescript
// Example: Delete user and all related data
async function deleteUserData(userId: string): Promise<void> {
    await deleteUserLibraryAccess(userId);
    await deleteUserInvitations(userId);
    await deleteAuditLogsForUser(userId);
    await deleteExternalUser(userId);
    await logDataDeletionEvent(userId);
}
```

## Performance Considerations

### Query Optimization
- Use pagination for all list queries (max 100 items per page)
- Implement read replicas for high-traffic tenants
- Use connection pooling (max 100 connections per tenant DB)

### Caching Strategy
- Tenant configuration: 5 min TTL
- User lists: 2 min TTL (for dashboard views)
- Subscription status: 10 min TTL
- Policy settings: 5 min TTL

### Sharding (Future)
For tenants with > 10,000 external users:
- Partition ExternalUsers table by date range
- Use temporal tables for historical data
- Implement read replicas per region

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
- **SQL Database**: Point-in-time restore (7-35 days)
- **Cosmos DB**: Continuous backup (30 days)
- **Blob Storage**: Geo-redundant storage (GRS)

### Recovery Procedures
1. Identify affected tenant(s)
2. Restore tenant database to point-in-time
3. Validate data integrity
4. Notify tenant admin
5. Document incident in audit log

---

**Last Updated**: 2024-02-03
**Version**: 1.0
