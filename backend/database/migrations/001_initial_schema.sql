-- V1: Create Tenants Table
CREATE TABLE Tenants (
    tenant_id VARCHAR(50) PRIMARY KEY,
    tenant_name NVARCHAR(255) NOT NULL,
    azure_tenant_id VARCHAR(50) UNIQUE NULL, -- NULL allowed for pending onboarding
    domain NVARCHAR(255),
    onboarding_date DATETIME2 DEFAULT GETUTCDATE(),
    status VARCHAR(20) DEFAULT 'Active' CHECK (status IN ('Active', 'Suspended', 'Trial', 'Churned', 'Pending')),
    primary_admin_email NVARCHAR(255) NOT NULL,
    subscription_tier VARCHAR(20) DEFAULT 'Free' CHECK (subscription_tier IN ('Free', 'Trial', 'Pro', 'Enterprise')),
    trial_end_date DATETIME2,
    is_verified BIT DEFAULT 0,
    settings NVARCHAR(MAX),
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2 DEFAULT GETUTCDATE()
);

CREATE INDEX IX_Tenants_AzureTenantId ON Tenants(azure_tenant_id);
CREATE INDEX IX_Tenants_Status ON Tenants(status);
CREATE INDEX IX_Tenants_SubscriptionTier ON Tenants(subscription_tier);

-- V2: Create Subscriptions Table
CREATE TABLE Subscriptions (
    subscription_id VARCHAR(50) PRIMARY KEY,
    tenant_id VARCHAR(50) NOT NULL,
    tier VARCHAR(20) NOT NULL CHECK (tier IN ('Free', 'Trial', 'Pro', 'Enterprise')),
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
    marketplace_subscription_id VARCHAR(255),
    limits NVARCHAR(MAX),
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY (tenant_id) REFERENCES Tenants(tenant_id) ON DELETE CASCADE
);

CREATE INDEX IX_Subscriptions_TenantId ON Subscriptions(tenant_id);
CREATE INDEX IX_Subscriptions_Status ON Subscriptions(status);

-- V3: Create Users Table
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
    metadata NVARCHAR(MAX),
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY (tenant_id) REFERENCES Tenants(tenant_id) ON DELETE CASCADE
);

CREATE INDEX IX_Users_TenantId ON Users(tenant_id);
CREATE INDEX IX_Users_Email ON Users(email);
CREATE INDEX IX_Users_Status ON Users(status);

-- V4: Create Policies Table
CREATE TABLE Policies (
    policy_id VARCHAR(50) PRIMARY KEY,
    tenant_id VARCHAR(50) NOT NULL,
    policy_name NVARCHAR(255) NOT NULL,
    policy_type VARCHAR(50) NOT NULL,
    is_enabled BIT DEFAULT 1,
    configuration NVARCHAR(MAX) NOT NULL,
    applies_to VARCHAR(50) DEFAULT 'All',
    created_by VARCHAR(50),
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY (tenant_id) REFERENCES Tenants(tenant_id) ON DELETE CASCADE
);

CREATE INDEX IX_Policies_TenantId ON Policies(tenant_id);
CREATE INDEX IX_Policies_IsEnabled ON Policies(is_enabled);

-- V5: Create Row-Level Security
CREATE FUNCTION dbo.fn_tenantAccessPredicate(@tenant_id VARCHAR(50))
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN SELECT 1 AS fn_tenantAccessPredicate_result
WHERE 
    @tenant_id = CAST(SESSION_CONTEXT(N'tenant_id') AS VARCHAR(50))
    OR IS_MEMBER('db_owner') = 1;
GO

CREATE SECURITY POLICY TenantIsolationPolicy
ADD FILTER PREDICATE dbo.fn_tenantAccessPredicate(tenant_id) ON dbo.Users,
ADD BLOCK PREDICATE dbo.fn_tenantAccessPredicate(tenant_id) ON dbo.Users AFTER INSERT,
ADD FILTER PREDICATE dbo.fn_tenantAccessPredicate(tenant_id) ON dbo.Policies,
ADD BLOCK PREDICATE dbo.fn_tenantAccessPredicate(tenant_id) ON dbo.Policies AFTER INSERT
WITH (STATE = ON);
GO
