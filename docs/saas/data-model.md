# SaaS Data Model

## Overview

This document defines the data model for the SharePoint External User Manager SaaS backend, including database schemas, relationships, and data governance policies.

## Database Strategy

### Multi-Tenant Approach

**Selected Strategy**: Shared database with tenant isolation via `tenant_id` column

**Rationale**:
- Cost-effective for MVP
- Simplified maintenance
- Good performance for expected scale
- Easy cross-tenant analytics
- Row-level security enforces isolation

### Database Technologies

1. **Azure SQL Database** - Primary relational data store
   - Tenant configurations
   - Subscription information
   - User records and permissions
   - Collaboration policies

2. **Azure Cosmos DB** - High-volume, time-series data
   - Audit logs
   - Usage metrics
   - Telemetry data
   - Real-time analytics

## Azure SQL Database Schema

### Tenants Table

```sql
CREATE TABLE Tenants (
    tenant_id VARCHAR(50) PRIMARY KEY,
    tenant_name NVARCHAR(255) NOT NULL,
    azure_tenant_id VARCHAR(50) UNIQUE NOT NULL,
    domain NVARCHAR(255),
    onboarding_date DATETIME2 DEFAULT GETUTCDATE(),
    status VARCHAR(20) DEFAULT 'Active' CHECK (status IN ('Active', 'Suspended', 'Trial', 'Churned')),
    primary_admin_email NVARCHAR(255) NOT NULL,
    subscription_tier VARCHAR(20) DEFAULT 'Free' CHECK (subscription_tier IN ('Free', 'Pro', 'Enterprise')),
    trial_end_date DATETIME2,
    is_verified BIT DEFAULT 0,
    settings NVARCHAR(MAX), -- JSON column
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2 DEFAULT GETUTCDATE()
);

CREATE INDEX IX_Tenants_AzureTenantId ON Tenants(azure_tenant_id);
CREATE INDEX IX_Tenants_Status ON Tenants(status);
CREATE INDEX IX_Tenants_SubscriptionTier ON Tenants(subscription_tier);
```

### Subscriptions Table

```sql
CREATE TABLE Subscriptions (
    subscription_id VARCHAR(50) PRIMARY KEY,
    tenant_id VARCHAR(50) NOT NULL,
    tier VARCHAR(20) NOT NULL CHECK (tier IN ('Free', 'Pro', 'Enterprise')),
    status VARCHAR(20) DEFAULT 'Active' CHECK (status IN ('Active', 'Cancelled', 'PastDue', 'Suspended', 'Trial')),
    billing_cycle VARCHAR(10) DEFAULT 'Monthly' CHECK (billing_cycle IN ('Monthly', 'Annual')),
    price_per_month DECIMAL(10, 2),
    currency VARCHAR(3) DEFAULT 'USD',
    start_date DATETIME2 NOT NULL,
    end_date DATETIME2,
    renewal_date DATETIME2,
    trial_start_date DATETIME2,
    trial_end_date DATETIME2,
    auto_renew BIT DEFAULT 1,
    payment_method VARCHAR(50),
    marketplace_subscription_id VARCHAR(255), -- For Azure Marketplace integration
    marketplace_offer_id VARCHAR(255),
    limits NVARCHAR(MAX), -- JSON: { maxExternalUsers, auditRetentionDays, etc. }
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY (tenant_id) REFERENCES Tenants(tenant_id) ON DELETE CASCADE
);

CREATE INDEX IX_Subscriptions_TenantId ON Subscriptions(tenant_id);
CREATE INDEX IX_Subscriptions_Status ON Subscriptions(status);
CREATE INDEX IX_Subscriptions_RenewalDate ON Subscriptions(renewal_date);
```

### Users Table

```sql
CREATE TABLE Users (
    user_id VARCHAR(50) PRIMARY KEY,
    tenant_id VARCHAR(50) NOT NULL,
    email NVARCHAR(255) NOT NULL,
    display_name NVARCHAR(255),
    user_type VARCHAR(20) DEFAULT 'External' CHECK (user_type IN ('Internal', 'External', 'Guest')),
    status VARCHAR(20) DEFAULT 'Active' CHECK (status IN ('Active', 'Invited', 'Suspended', 'Revoked')),
    invited_by VARCHAR(50),
    invited_date DATETIME2 DEFAULT GETUTCDATE(),
    last_access_date DATETIME2,
    access_expiration_date DATETIME2,
    company_name NVARCHAR(255),
    job_title NVARCHAR(100),
    metadata NVARCHAR(MAX), -- JSON column for custom fields
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY (tenant_id) REFERENCES Tenants(tenant_id) ON DELETE CASCADE
);

CREATE INDEX IX_Users_TenantId ON Users(tenant_id);
CREATE INDEX IX_Users_Email ON Users(email);
CREATE INDEX IX_Users_Status ON Users(status);
CREATE INDEX IX_Users_UserType ON Users(user_type);
```

### Permissions Table

```sql
CREATE TABLE Permissions (
    permission_id VARCHAR(50) PRIMARY KEY,
    tenant_id VARCHAR(50) NOT NULL,
    user_id VARCHAR(50) NOT NULL,
    resource_type VARCHAR(50) NOT NULL, -- 'Site', 'Library', 'List'
    resource_id VARCHAR(255) NOT NULL,
    resource_url NVARCHAR(500),
    permission_level VARCHAR(50) NOT NULL, -- 'Read', 'Contribute', 'Edit', 'FullControl'
    granted_by VARCHAR(50),
    granted_date DATETIME2 DEFAULT GETUTCDATE(),
    expiration_date DATETIME2,
    is_active BIT DEFAULT 1,
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY (tenant_id) REFERENCES Tenants(tenant_id) ON DELETE CASCADE,
    FOREIGN KEY (user_id) REFERENCES Users(user_id) ON DELETE CASCADE
);

CREATE INDEX IX_Permissions_TenantId ON Permissions(tenant_id);
CREATE INDEX IX_Permissions_UserId ON Permissions(user_id);
CREATE INDEX IX_Permissions_ResourceId ON Permissions(resource_id);
```

### Policies Table

```sql
CREATE TABLE Policies (
    policy_id VARCHAR(50) PRIMARY KEY,
    tenant_id VARCHAR(50) NOT NULL,
    policy_name NVARCHAR(255) NOT NULL,
    policy_type VARCHAR(50) NOT NULL, -- 'ExternalSharing', 'AccessExpiration', 'GuestInvitation', etc.
    is_enabled BIT DEFAULT 1,
    configuration NVARCHAR(MAX) NOT NULL, -- JSON configuration
    applies_to VARCHAR(50) DEFAULT 'All', -- 'All', 'Specific Sites', 'Specific Users'
    scope NVARCHAR(MAX), -- JSON: list of site URLs or user IDs
    created_by VARCHAR(50),
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY (tenant_id) REFERENCES Tenants(tenant_id) ON DELETE CASCADE
);

CREATE INDEX IX_Policies_TenantId ON Policies(tenant_id);
CREATE INDEX IX_Policies_PolicyType ON Policies(policy_type);
CREATE INDEX IX_Policies_IsEnabled ON Policies(is_enabled);
```

### AdminRoles Table

```sql
CREATE TABLE AdminRoles (
    role_id VARCHAR(50) PRIMARY KEY,
    tenant_id VARCHAR(50) NOT NULL,
    user_id VARCHAR(50) NOT NULL,
    role_name VARCHAR(50) NOT NULL CHECK (role_name IN ('TenantAdmin', 'UserManager', 'PolicyManager', 'AuditReader')),
    granted_by VARCHAR(50),
    granted_date DATETIME2 DEFAULT GETUTCDATE(),
    is_active BIT DEFAULT 1,
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY (tenant_id) REFERENCES Tenants(tenant_id) ON DELETE CASCADE
);

CREATE INDEX IX_AdminRoles_TenantId ON AdminRoles(tenant_id);
CREATE INDEX IX_AdminRoles_UserId ON AdminRoles(user_id);
CREATE INDEX IX_AdminRoles_RoleName ON AdminRoles(role_name);
```

### InvitationHistory Table

```sql
CREATE TABLE InvitationHistory (
    invitation_id VARCHAR(50) PRIMARY KEY,
    tenant_id VARCHAR(50) NOT NULL,
    user_id VARCHAR(50),
    email NVARCHAR(255) NOT NULL,
    invited_by VARCHAR(50) NOT NULL,
    invitation_date DATETIME2 DEFAULT GETUTCDATE(),
    status VARCHAR(20) DEFAULT 'Pending' CHECK (status IN ('Pending', 'Accepted', 'Declined', 'Expired')),
    resource_type VARCHAR(50),
    resource_id VARCHAR(255),
    permission_level VARCHAR(50),
    invitation_message NVARCHAR(1000),
    accepted_date DATETIME2,
    expiration_date DATETIME2,
    reminder_sent_count INT DEFAULT 0,
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY (tenant_id) REFERENCES Tenants(tenant_id) ON DELETE CASCADE
);

CREATE INDEX IX_InvitationHistory_TenantId ON InvitationHistory(tenant_id);
CREATE INDEX IX_InvitationHistory_Email ON InvitationHistory(email);
CREATE INDEX IX_InvitationHistory_Status ON InvitationHistory(status);
```

### UsageMetrics Table

```sql
CREATE TABLE UsageMetrics (
    metric_id VARCHAR(50) PRIMARY KEY,
    tenant_id VARCHAR(50) NOT NULL,
    metric_date DATE NOT NULL,
    metric_type VARCHAR(50) NOT NULL, -- 'ExternalUsersCount', 'APICallsCount', 'StorageUsed', etc.
    metric_value DECIMAL(18, 2),
    metadata NVARCHAR(MAX), -- JSON for additional details
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY (tenant_id) REFERENCES Tenants(tenant_id) ON DELETE CASCADE
);

CREATE INDEX IX_UsageMetrics_TenantId ON UsageMetrics(tenant_id);
CREATE INDEX IX_UsageMetrics_MetricDate ON UsageMetrics(metric_date);
CREATE INDEX IX_UsageMetrics_MetricType ON UsageMetrics(metric_type);
```

## Azure Cosmos DB Collections

### AuditLogs Collection

**Container**: `audit-logs`
**Partition Key**: `/tenant_id`
**TTL**: Varies by subscription tier (30 days to unlimited)

```json
{
  "id": "audit-log-uuid",
  "tenant_id": "tenant-123",
  "timestamp": "2024-01-15T10:30:00Z",
  "event_type": "UserInvited",
  "event_category": "UserManagement",
  "severity": "Info",
  "actor": {
    "user_id": "user-456",
    "email": "admin@contoso.com",
    "ip_address": "192.168.1.1",
    "user_agent": "Mozilla/5.0..."
  },
  "target": {
    "resource_type": "User",
    "resource_id": "user-789",
    "resource_name": "partner@external.com"
  },
  "action": {
    "name": "InviteUser",
    "result": "Success",
    "details": {
      "permission_level": "Contribute",
      "site_url": "https://contoso.sharepoint.com/sites/partners"
    }
  },
  "context": {
    "session_id": "session-abc",
    "request_id": "req-xyz",
    "correlation_id": "corr-123"
  },
  "_ts": 1705318200
}
```

### TelemetryData Collection

**Container**: `telemetry`
**Partition Key**: `/tenant_id`
**TTL**: 90 days

```json
{
  "id": "telemetry-uuid",
  "tenant_id": "tenant-123",
  "timestamp": "2024-01-15T10:30:00Z",
  "event_name": "APICall",
  "properties": {
    "endpoint": "/api/v1/users",
    "method": "GET",
    "status_code": 200,
    "duration_ms": 245,
    "user_id": "user-456",
    "user_agent": "SPFx/1.18.2"
  },
  "measurements": {
    "response_size_bytes": 4096,
    "db_query_time_ms": 120
  }
}
```

### SessionCache Collection

**Container**: `session-cache`
**Partition Key**: `/tenant_id`
**TTL**: 3600 seconds (1 hour)

```json
{
  "id": "session-uuid",
  "tenant_id": "tenant-123",
  "user_id": "user-456",
  "session_token": "encrypted-token",
  "created_at": "2024-01-15T10:00:00Z",
  "expires_at": "2024-01-15T11:00:00Z",
  "data": {
    "preferences": {},
    "cached_permissions": []
  }
}
```

## Row-Level Security Implementation

### Tenant Isolation Function

```sql
CREATE FUNCTION dbo.fn_tenantAccessPredicate(@tenant_id VARCHAR(50))
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN SELECT 1 AS fn_tenantAccessPredicate_result
WHERE 
    @tenant_id = CAST(SESSION_CONTEXT(N'tenant_id') AS VARCHAR(50))
    OR IS_MEMBER('db_owner') = 1;
GO

-- Apply to all tenant-scoped tables
CREATE SECURITY POLICY TenantIsolationPolicy
ADD FILTER PREDICATE dbo.fn_tenantAccessPredicate(tenant_id) ON dbo.Users,
ADD BLOCK PREDICATE dbo.fn_tenantAccessPredicate(tenant_id) ON dbo.Users AFTER INSERT,
ADD FILTER PREDICATE dbo.fn_tenantAccessPredicate(tenant_id) ON dbo.Permissions,
ADD BLOCK PREDICATE dbo.fn_tenantAccessPredicate(tenant_id) ON dbo.Permissions AFTER INSERT,
ADD FILTER PREDICATE dbo.fn_tenantAccessPredicate(tenant_id) ON dbo.Policies,
ADD BLOCK PREDICATE dbo.fn_tenantAccessPredicate(tenant_id) ON dbo.Policies AFTER INSERT
WITH (STATE = ON);
```

### Usage in Application

```typescript
// Before any database operation, set session context
await db.query(
  "EXEC sp_set_session_context @key = N'tenant_id', @value = @tenantId",
  { tenantId: req.tenant.id }
);

// Now all queries automatically filtered by tenant_id
const users = await db.query("SELECT * FROM Users");
```

## Data Retention Policies

### By Subscription Tier

| Data Type | Free | Pro | Enterprise |
|-----------|------|-----|------------|
| Audit Logs | 30 days | 1 year | Unlimited |
| Usage Metrics | 90 days | 1 year | Unlimited |
| User Records | Active only | 2 years after deletion | Unlimited |
| Invitation History | 6 months | 2 years | Unlimited |
| Telemetry Data | 30 days | 90 days | 1 year |

### Cleanup Jobs

```typescript
// Scheduled job to clean up expired data
interface CleanupJob {
  runFrequency: string; // Cron expression
  actions: {
    deleteExpiredInvitations: boolean;
    archiveOldAuditLogs: boolean;
    purgeRevokedUsers: boolean;
    cleanupSessionCache: boolean;
  };
}

const cleanupJobs: CleanupJob = {
  runFrequency: '0 2 * * *', // Daily at 2 AM
  actions: {
    deleteExpiredInvitations: true,
    archiveOldAuditLogs: true,
    purgeRevokedUsers: false, // Keep for compliance
    cleanupSessionCache: true
  }
};
```

## Migrations

### Migration Strategy

Using **Flyway** or **Liquibase** for database migrations:

```
backend/migrations/
├── V1__create_tenants_table.sql
├── V2__create_subscriptions_table.sql
├── V3__create_users_table.sql
├── V4__create_permissions_table.sql
├── V5__create_policies_table.sql
├── V6__create_admin_roles_table.sql
├── V7__create_invitation_history_table.sql
├── V8__create_usage_metrics_table.sql
├── V9__implement_row_level_security.sql
└── V10__create_indexes.sql
```

### Sample Migration

```sql
-- V1__create_tenants_table.sql
CREATE TABLE Tenants (
    tenant_id VARCHAR(50) PRIMARY KEY,
    tenant_name NVARCHAR(255) NOT NULL,
    azure_tenant_id VARCHAR(50) UNIQUE NOT NULL,
    -- ... rest of schema
);

-- Add comment for tracking
EXEC sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Stores multi-tenant organization information',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE', @level1name = N'Tenants';
```

## Data Backup Strategy

### Automated Backups

```yaml
backup_policy:
  azure_sql:
    type: "Automated"
    frequency: "Daily"
    retention: "35 days"
    geo_redundant: true
    
  cosmos_db:
    type: "Continuous"
    retention: "7 days"
    geo_redundant: true
    
  blob_storage:
    type: "Snapshots"
    frequency: "Hourly"
    retention: "30 days"
```

### Point-in-Time Recovery

Azure SQL: Up to 35 days PITR
Cosmos DB: Up to 7 days continuous backup

## Performance Optimization

### Indexing Strategy

```sql
-- Composite indexes for common queries
CREATE INDEX IX_Users_TenantId_Status 
ON Users(tenant_id, status) 
INCLUDE (email, display_name);

CREATE INDEX IX_Permissions_TenantId_UserId_IsActive
ON Permissions(tenant_id, user_id, is_active)
INCLUDE (resource_id, permission_level);

-- Covering index for policy lookup
CREATE INDEX IX_Policies_TenantId_IsEnabled_PolicyType
ON Policies(tenant_id, is_enabled, policy_type)
INCLUDE (configuration);
```

### Query Optimization

```typescript
// Use projection to limit data transfer
const users = await db.query(`
  SELECT user_id, email, display_name, status
  FROM Users
  WHERE tenant_id = @tenantId
  AND status = 'Active'
`);

// Avoid N+1 queries
const usersWithPermissions = await db.query(`
  SELECT u.*, 
         (SELECT COUNT(*) FROM Permissions p WHERE p.user_id = u.user_id) as permission_count
  FROM Users u
  WHERE u.tenant_id = @tenantId
`);
```

## Compliance & Data Governance

### GDPR Compliance

- **Right to Access**: Export all user data via API
- **Right to Erasure**: Soft delete with anonymization
- **Right to Portability**: JSON/CSV export
- **Data Minimization**: Only collect necessary fields
- **Consent Management**: Track consent for data processing

### Data Classification

```typescript
interface DataClassification {
  level: 'Public' | 'Internal' | 'Confidential' | 'Restricted';
  encryption: boolean;
  pii: boolean;
  retention_days: number;
}

const classifications: Record<string, DataClassification> = {
  'Users.email': { level: 'Confidential', encryption: true, pii: true, retention_days: 730 },
  'AuditLogs.*': { level: 'Internal', encryption: true, pii: false, retention_days: 365 },
  'Tenants.domain': { level: 'Internal', encryption: false, pii: false, retention_days: -1 }
};
```

## Entity Relationship Diagram

```
Tenants (1) ──────── (*) Subscriptions
   │
   │ (1)
   │
   ├──────────────── (*) Users
   │                     │
   │                     │ (1)
   │                     │
   │                     └──────── (*) Permissions
   │
   ├──────────────── (*) Policies
   │
   ├──────────────── (*) AdminRoles
   │
   ├──────────────── (*) InvitationHistory
   │
   └──────────────── (*) UsageMetrics
```

## Next Steps

1. Implement database migration scripts
2. Create seed data for development
3. Set up database connection pooling
4. Implement caching layer
5. Configure automated backups
6. Set up monitoring and alerts
