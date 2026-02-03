-- Insert sample tenant for development/testing
INSERT INTO Tenants (
    tenant_id,
    tenant_name,
    azure_tenant_id,
    domain,
    status,
    primary_admin_email,
    subscription_tier,
    is_verified,
    settings
) VALUES (
    'tenant-dev-001',
    'Development Tenant',
    '12345678-1234-1234-1234-123456789012',
    'dev.contoso.com',
    'Active',
    'admin@dev.contoso.com',
    'Pro',
    1,
    '{"external_sharing_enabled":true,"allow_anonymous_links":false,"default_link_permission":"View","external_user_expiration_days":90}'
);

-- Insert sample subscription
INSERT INTO Subscriptions (
    subscription_id,
    tenant_id,
    tier,
    status,
    billing_cycle,
    price_per_month,
    start_date,
    renewal_date,
    limits
) VALUES (
    'sub-dev-001',
    'tenant-dev-001',
    'Pro',
    'Active',
    'Monthly',
    49.00,
    GETUTCDATE(),
    DATEADD(month, 1, GETUTCDATE()),
    '{"maxExternalUsers":100,"auditLogRetentionDays":365,"apiRateLimit":200,"advancedPolicies":true}'
);

-- Insert sample policy
INSERT INTO Policies (
    policy_id,
    tenant_id,
    policy_name,
    policy_type,
    is_enabled,
    configuration,
    applies_to,
    created_by
) VALUES (
    'policy-dev-001',
    'tenant-dev-001',
    'External Access Expiration',
    'AccessExpiration',
    1,
    '{"default_expiration_days":90,"send_expiration_reminder":true,"reminder_days_before":[7,1]}',
    'All',
    'system'
);
