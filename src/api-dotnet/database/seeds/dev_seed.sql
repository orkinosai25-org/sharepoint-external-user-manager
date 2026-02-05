-- ============================================================================
-- Development Seed Data
-- Description: Sample data for local development and testing
-- WARNING: DO NOT run this in production!
-- ============================================================================

-- Check if seed data already exists
IF EXISTS (SELECT 1 FROM dbo.Tenant WHERE EntraIdTenantId = 'dev-tenant-001')
BEGIN
    PRINT 'Seed data already exists. Skipping...';
    RETURN;
END
GO

-- ============================================================================
-- Seed: Demo Tenant
-- ============================================================================
DECLARE @TenantId INT;

INSERT INTO dbo.Tenant (
    EntraIdTenantId,
    OrganizationName,
    PrimaryAdminEmail,
    OnboardedDate,
    Status,
    Settings
) VALUES (
    'dev-tenant-001',
    'Contoso Corporation',
    'admin@contoso.com',
    DATEADD(DAY, -30, GETUTCDATE()),
    'Active',
    '{"timezone":"UTC","locale":"en-US","notifications":true}'
);

SET @TenantId = SCOPE_IDENTITY();
PRINT 'Created tenant: ' + CAST(@TenantId AS NVARCHAR(10));

-- ============================================================================
-- Seed: Trial Subscription
-- ============================================================================
INSERT INTO dbo.Subscription (
    TenantId,
    Tier,
    StartDate,
    EndDate,
    TrialExpiry,
    Status,
    MaxUsers,
    Features
) VALUES (
    @TenantId,
    'Pro',
    DATEADD(DAY, -30, GETUTCDATE()),
    NULL,
    DATEADD(DAY, 30, GETUTCDATE()),
    'Trial',
    100,
    '{"auditHistoryDays":90,"exportEnabled":true,"scheduledReviews":false,"advancedPolicies":true}'
);

PRINT 'Created trial subscription for tenant ' + CAST(@TenantId AS NVARCHAR(10));

-- ============================================================================
-- Seed: Default Policies
-- ============================================================================
INSERT INTO dbo.Policy (TenantId, PolicyType, Enabled, Configuration) VALUES
(@TenantId, 'GuestExpiration', 0, '{"expirationDays":90,"notifyBeforeDays":7}'),
(@TenantId, 'RequireApproval', 0, '{"approvers":["admin@contoso.com"]}'),
(@TenantId, 'AllowedDomains', 0, '{"whitelist":[],"blacklist":["competitor.com"]}'),
(@TenantId, 'AutoRemoval', 0, '{"daysInactive":180,"notifyBeforeDays":14}');

PRINT 'Created 4 default policies';

-- ============================================================================
-- Seed: Sample Audit Logs
-- ============================================================================
INSERT INTO dbo.AuditLog (
    TenantId,
    Timestamp,
    UserId,
    UserEmail,
    Action,
    ResourceType,
    ResourceId,
    Details,
    IpAddress,
    CorrelationId,
    Status
) VALUES
(@TenantId, DATEADD(DAY, -30, GETUTCDATE()), 'user-001', 'admin@contoso.com', 'TenantOnboarded', 'Tenant', CAST(@TenantId AS NVARCHAR(10)), '{"method":"AdminConsent"}', '203.0.113.1', NEWID(), 'Success'),
(@TenantId, DATEADD(DAY, -29, GETUTCDATE()), 'user-001', 'admin@contoso.com', 'SubscriptionCreated', 'Subscription', '1', '{"tier":"Pro","status":"Trial"}', '203.0.113.1', NEWID(), 'Success'),
(@TenantId, DATEADD(DAY, -25, GETUTCDATE()), 'user-001', 'admin@contoso.com', 'UserInvited', 'ExternalUser', 'partner@external.com', '{"library":"Marketing Docs","permissions":"Read"}', '203.0.113.1', NEWID(), 'Success'),
(@TenantId, DATEADD(DAY, -20, GETUTCDATE()), 'user-001', 'admin@contoso.com', 'PolicyUpdated', 'Policy', '1', '{"policyType":"GuestExpiration","enabled":true}', '203.0.113.1', NEWID(), 'Success'),
(@TenantId, DATEADD(DAY, -15, GETUTCDATE()), 'user-001', 'admin@contoso.com', 'UserRemoved', 'ExternalUser', 'oldpartner@external.com', '{"library":"Marketing Docs","reason":"AccessRevoked"}', '203.0.113.1', NEWID(), 'Success');

PRINT 'Created 5 sample audit log entries';

-- ============================================================================
-- Seed: Sample User Actions
-- ============================================================================
INSERT INTO dbo.UserAction (
    TenantId,
    ExternalUserEmail,
    ActionType,
    TargetLibrary,
    PerformedBy,
    PerformedDate,
    Metadata
) VALUES
(@TenantId, 'partner@external.com', 'Invited', 'https://contoso.sharepoint.com/sites/marketing/docs', 'admin@contoso.com', DATEADD(DAY, -25, GETUTCDATE()), '{"company":"Partner Corp","project":"Q1 Campaign"}'),
(@TenantId, 'vendor@supplier.com', 'Invited', 'https://contoso.sharepoint.com/sites/procurement/docs', 'admin@contoso.com', DATEADD(DAY, -20, GETUTCDATE()), '{"company":"Supplier Inc","project":"Logistics"}'),
(@TenantId, 'consultant@advisory.com', 'Invited', 'https://contoso.sharepoint.com/sites/projects/docs', 'manager@contoso.com', DATEADD(DAY, -18, GETUTCDATE()), '{"company":"Advisory LLC","project":"Digital Transformation"}'),
(@TenantId, 'oldpartner@external.com', 'Removed', 'https://contoso.sharepoint.com/sites/marketing/docs', 'admin@contoso.com', DATEADD(DAY, -15, GETUTCDATE()), '{"company":"Partner Corp","project":"Q1 Campaign","reason":"Project completed"}'),
(@TenantId, 'partner@external.com', 'PermissionChanged', 'https://contoso.sharepoint.com/sites/marketing/docs', 'admin@contoso.com', DATEADD(DAY, -10, GETUTCDATE()), '{"oldPermissions":"Read","newPermissions":"Contribute","company":"Partner Corp"}');

PRINT 'Created 5 sample user action records';

-- ============================================================================
-- Seed: Second Demo Tenant (for multi-tenant testing)
-- ============================================================================
DECLARE @TenantId2 INT;

INSERT INTO dbo.Tenant (
    EntraIdTenantId,
    OrganizationName,
    PrimaryAdminEmail,
    OnboardedDate,
    Status,
    Settings
) VALUES (
    'dev-tenant-002',
    'Fabrikam Industries',
    'it@fabrikam.com',
    DATEADD(DAY, -15, GETUTCDATE()),
    'Active',
    '{"timezone":"America/New_York","locale":"en-US","notifications":true}'
);

SET @TenantId2 = SCOPE_IDENTITY();
PRINT 'Created second tenant: ' + CAST(@TenantId2 AS NVARCHAR(10));

INSERT INTO dbo.Subscription (
    TenantId,
    Tier,
    StartDate,
    Status,
    MaxUsers,
    Features
) VALUES (
    @TenantId2,
    'Free',
    DATEADD(DAY, -15, GETUTCDATE()),
    'Active',
    10,
    '{"auditHistoryDays":30,"exportEnabled":false,"scheduledReviews":false,"advancedPolicies":false}'
);

PRINT 'Created free subscription for tenant ' + CAST(@TenantId2 AS NVARCHAR(10));

INSERT INTO dbo.Policy (TenantId, PolicyType, Enabled, Configuration) VALUES
(@TenantId2, 'GuestExpiration', 1, '{"expirationDays":60,"notifyBeforeDays":7}'),
(@TenantId2, 'RequireApproval', 1, '{"approvers":["it@fabrikam.com"]}'),
(@TenantId2, 'AllowedDomains', 0, '{"whitelist":[],"blacklist":[]}'),
(@TenantId2, 'AutoRemoval', 0, '{"daysInactive":90,"notifyBeforeDays":7}');

PRINT 'Created 4 default policies for second tenant';

-- ============================================================================
-- Summary
-- ============================================================================
PRINT '';
PRINT '========================================';
PRINT 'Development seed data created successfully!';
PRINT '========================================';
PRINT '';
PRINT 'Summary:';
PRINT '  - 2 demo tenants';
PRINT '  - 2 subscriptions (1 Trial, 1 Free)';
PRINT '  - 8 policies (4 per tenant)';
PRINT '  - 5 audit log entries';
PRINT '  - 5 user action records';
PRINT '';
PRINT 'Demo Tenants:';
PRINT '  1. Contoso Corporation (Pro Trial)';
PRINT '     Entra ID: dev-tenant-001';
PRINT '     Admin: admin@contoso.com';
PRINT '';
PRINT '  2. Fabrikam Industries (Free)';
PRINT '     Entra ID: dev-tenant-002';
PRINT '     Admin: it@fabrikam.com';
PRINT '';
PRINT 'Use these tenants for API testing and development.';
GO
