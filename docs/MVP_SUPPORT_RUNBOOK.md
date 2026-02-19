# ClientSpace MVP Support Runbook

**Comprehensive troubleshooting and support guide for ClientSpace MVP**

This runbook provides support staff with procedures for diagnosing and resolving common issues, analyzing logs, and escalating complex problems.

## Table of Contents

1. [Overview](#overview)
2. [Common Issues & Solutions](#common-issues--solutions)
3. [Debug Procedures](#debug-procedures)
4. [Log Analysis](#log-analysis)
5. [Performance Troubleshooting](#performance-troubleshooting)
6. [Security Incident Response](#security-incident-response)
7. [Data Recovery](#data-recovery)
8. [Escalation Procedures](#escalation-procedures)

---

## Overview

### Support Tiers

| Tier | Scope | Response Time | Handled By |
|------|-------|---------------|------------|
| **L1** | Basic inquiries, documentation | 24 hours | Support Team |
| **L2** | Technical issues, configuration | 4 hours | Technical Support |
| **L3** | System issues, bugs | 1 hour | Engineering Team |
| **L4** | Critical outages, security | 15 minutes | Senior Engineering |

### Tools Required

- **Azure Portal**: Access to tenant resources
- **Application Insights**: Log analysis and monitoring
- **Log Analytics**: Advanced query capabilities
- **Azure CLI**: Command-line management
- **Support Portal**: Ticket management system

---

## Common Issues & Solutions

### Issue 1: User Can't Sign In

#### Symptoms
- Error: "Application with identifier 'XXX' was not found"
- Redirect loop during sign-in
- "Access denied" after signing in

#### Diagnosis

**Check 1: Azure AD App Registration**
```bash
# Verify app exists
az ad app show --id <client-id>

# Check redirect URIs
az ad app show --id <client-id> --query web.redirectUris
```

**Check 2: Tenant Configuration**
```bash
# Check if tenant is configured
curl -H "Authorization: Bearer <admin-token>" \
  https://api.clientspace.app/api/v1/tenant
```

**Check 3: User Permissions**
```bash
# Check user's role
curl -H "Authorization: Bearer <admin-token>" \
  https://api.clientspace.app/api/v1/users/<user-id>
```

#### Resolution

**Solution 1: Fix Redirect URI**
```bash
# Update redirect URI in Azure AD
az ad app update \
  --id <client-id> \
  --web-redirect-uris \
    "https://portal.clientspace.app/signin-oidc" \
    "https://portal-dev.clientspace.app/signin-oidc"
```

**Solution 2: Grant Admin Consent**
1. Navigate to Azure Portal → Azure AD → App Registrations
2. Select the ClientSpace Portal app
3. Go to API Permissions
4. Click "Grant admin consent for [Tenant]"

**Solution 3: Restart Portal App Service**
```bash
az webapp restart \
  --name <portal-app-name> \
  --resource-group <resource-group>
```

---

### Issue 2: External User Invitation Not Received

#### Symptoms
- User reports no invitation email
- Invitation status stuck on "Pending"
- Email bounces

#### Diagnosis

**Check 1: Email Address**
```bash
# Verify user record
curl -H "Authorization: Bearer <admin-token>" \
  https://api.clientspace.app/api/v1/clients/<client-id>/users/<user-id>
```

**Check 2: SharePoint Invitation Status**
```bash
# Check Graph API for invitation
curl -H "Authorization: Bearer <graph-token>" \
  https://graph.microsoft.com/v1.0/sites/<site-id>/permissions
```

**Check 3: Email Logs**
```kusto
// Application Insights query
traces
| where timestamp > ago(24h)
| where message contains "SendInvitation"
| where customDimensions.userId == "<user-id>"
| project timestamp, message, customDimensions
```

#### Resolution

**Solution 1: Resend Invitation**
```bash
# Via API
curl -X POST \
  -H "Authorization: Bearer <admin-token>" \
  https://api.clientspace.app/api/v1/clients/<client-id>/users/<user-id>/resend-invitation
```

**Solution 2: Check Spam Folder**
- Ask user to check spam/junk folder
- Add noreply@sharepoint.com to safe senders

**Solution 3: Verify External Sharing Settings**
```bash
# Check SharePoint tenant settings
Get-SPOTenant | Select-Object SharingCapability
# Should be "ExternalUserAndGuestSharing" or "ExternalUserSharingOnly"
```

**Solution 4: Manual Invitation via SharePoint**
1. Navigate to SharePoint site
2. Click "Share" on library
3. Enter user's email manually
4. Send invitation

---

### Issue 3: Client Space Not Provisioning

#### Symptoms
- "Provisioning" status for > 5 minutes
- Error: "Failed to create site"
- Site URL returns 404

#### Diagnosis

**Check 1: Provisioning Status**
```bash
# Check client record
curl -H "Authorization: Bearer <admin-token>" \
  https://api.clientspace.app/api/v1/clients/<client-id>
```

**Check 2: SharePoint Site Status**
```bash
# Check if site exists
curl -H "Authorization: Bearer <graph-token>" \
  https://graph.microsoft.com/v1.0/sites/<tenant>.sharepoint.com:/sites/<site-name>
```

**Check 3: Background Job Status**
```kusto
// Check provisioning logs
traces
| where timestamp > ago(1h)
| where message contains "ProvisionClientSite"
| where customDimensions.clientId == "<client-id>"
| project timestamp, severityLevel, message, customDimensions
```

#### Resolution

**Solution 1: Retry Provisioning**
```bash
# Trigger retry
curl -X POST \
  -H "Authorization: Bearer <admin-token>" \
  https://api.clientspace.app/api/v1/clients/<client-id>/retry-provisioning
```

**Solution 2: Check Subscription Limits**
```bash
# Verify tenant hasn't exceeded client limit
curl -H "Authorization: Bearer <admin-token>" \
  https://api.clientspace.app/api/v1/tenant/usage
```

**Solution 3: Manual Site Creation**
If automatic provisioning fails repeatedly:
1. Create site manually in SharePoint Admin Center
2. Update client record with site URL:
```bash
curl -X PUT \
  -H "Authorization: Bearer <admin-token>" \
  -H "Content-Type: application/json" \
  -d '{"siteUrl": "https://tenant.sharepoint.com/sites/ABC-Corp"}' \
  https://api.clientspace.app/api/v1/clients/<client-id>
```

---

### Issue 4: Search Not Working

#### Symptoms
- No search results returned
- Search returns 500 error
- Results missing or incomplete

#### Diagnosis

**Check 1: Search Index Status**
```bash
# Check if search indexing is working
curl -H "Authorization: Bearer <admin-token>" \
  https://api.clientspace.app/api/v1/admin/search/status
```

**Check 2: User Permissions**
```bash
# Verify user has search permissions
curl -H "Authorization: Bearer <admin-token>" \
  https://api.clientspace.app/api/v1/users/<user-id>/permissions
```

**Check 3: Search Service Logs**
```kusto
// Check search API calls
requests
| where timestamp > ago(1h)
| where url contains "/search"
| where resultCode != 200
| project timestamp, url, resultCode, duration, customDimensions
```

#### Resolution

**Solution 1: Verify Subscription Tier**
- Global search requires Professional or Enterprise tier
- Client-scoped search available on all tiers

**Solution 2: Trigger Reindex**
```bash
# Trigger search reindex
curl -X POST \
  -H "Authorization: Bearer <admin-token>" \
  https://api.clientspace.app/api/v1/admin/search/reindex
```

**Solution 3: Check SharePoint Search**
1. Navigate to SharePoint site
2. Use built-in search
3. If SharePoint search works, issue is with API integration
4. If SharePoint search fails, issue is with SharePoint indexing

---

### Issue 5: Payment/Billing Issues

#### Symptoms
- Payment fails during checkout
- Subscription shows "Past Due"
- Invoice not generated

#### Diagnosis

**Check 1: Stripe Status**
```bash
# Check subscription in Stripe
curl -u <stripe-secret-key>: \
  https://api.stripe.com/v1/subscriptions/<subscription-id>
```

**Check 2: Customer Payment Method**
```bash
# Check payment method
curl -u <stripe-secret-key>: \
  https://api.stripe.com/v1/customers/<customer-id>/payment_methods
```

**Check 3: Webhook Status**
```bash
# Check webhook deliveries
curl -u <stripe-secret-key>: \
  https://api.stripe.com/v1/webhook_endpoints/<webhook-id>/deliveries
```

#### Resolution

**Solution 1: Update Payment Method**
1. Send customer link to update payment method:
```bash
curl -X POST \
  -H "Authorization: Bearer <admin-token>" \
  https://api.clientspace.app/api/v1/subscription/update-payment-method-link
```

**Solution 2: Retry Payment**
```bash
# Retry failed payment in Stripe
curl -X POST \
  -u <stripe-secret-key>: \
  https://api.stripe.com/v1/invoices/<invoice-id>/pay
```

**Solution 3: Contact Stripe Support**
- For payment gateway issues
- For fraud detection false positives

---

### Issue 6: API Rate Limit Exceeded

#### Symptoms
- Error 429 "Too Many Requests"
- Slow application performance
- Some features not loading

#### Diagnosis

**Check 1: Current Rate Limit Usage**
```bash
# Check tenant usage
curl -H "Authorization: Bearer <admin-token>" \
  https://api.clientspace.app/api/v1/tenant/usage
```

**Check 2: Recent API Calls**
```kusto
// Query API request volume
requests
| where timestamp > ago(1h)
| where customDimensions.tenantId == "<tenant-id>"
| summarize count() by bin(timestamp, 1m)
| render timechart
```

#### Resolution

**Solution 1: Identify Source of High Traffic**
```kusto
// Find top API consumers
requests
| where timestamp > ago(1h)
| where customDimensions.tenantId == "<tenant-id>"
| summarize count() by url, userId = customDimensions.userId
| top 10 by count_ desc
```

**Solution 2: Temporary Rate Limit Increase**
```bash
# Temporarily increase limit (requires admin)
curl -X POST \
  -H "Authorization: Bearer <admin-token>" \
  -H "Content-Type: application/json" \
  -d '{"limitMultiplier": 2, "duration": 3600}' \
  https://api.clientspace.app/api/v1/admin/rate-limits/<tenant-id>/temporary-increase
```

**Solution 3: Recommend Plan Upgrade**
- If usage is consistently high
- Suggest upgrading to higher tier with increased limits

---

## Debug Procedures

### Procedure 1: Troubleshoot Authentication Issues

#### Steps

1. **Verify Azure AD Configuration**
```bash
# Check tenant ID
az account show --query tenantId

# Check app registration
az ad app show --id <client-id>
```

2. **Check Token Claims**
- Use https://jwt.ms to decode JWT token
- Verify:
  - `aud` (audience) matches API client ID
  - `iss` (issuer) is correct tenant
  - `roles` includes required roles
  - Token not expired (`exp` claim)

3. **Test Authentication Flow**
```bash
# Test token acquisition
curl -X POST \
  https://login.microsoftonline.com/<tenant-id>/oauth2/v2.0/token \
  -d "client_id=<client-id>" \
  -d "scope=api://clientspace-api/user_impersonation" \
  -d "code=<auth-code>" \
  -d "redirect_uri=<redirect-uri>" \
  -d "grant_type=authorization_code" \
  -d "client_secret=<client-secret>"
```

4. **Check API Authorization**
```bash
# Test API call with token
curl -H "Authorization: Bearer <token>" \
  https://api.clientspace.app/api/v1/tenant
```

---

### Procedure 2: Troubleshoot SharePoint Integration

#### Steps

1. **Verify Graph API Permissions**
```bash
# Check app permissions
az ad app permission list --id <client-id>

# Required permissions:
# - Sites.ReadWrite.All
# - User.Invite.All
# - Directory.ReadWrite.All
```

2. **Test Graph API Access**
```bash
# Get access token for Graph
curl -X POST \
  https://login.microsoftonline.com/<tenant-id>/oauth2/v2.0/token \
  -d "client_id=<client-id>" \
  -d "client_secret=<client-secret>" \
  -d "scope=https://graph.microsoft.com/.default" \
  -d "grant_type=client_credentials"

# Test Graph call
curl -H "Authorization: Bearer <graph-token>" \
  https://graph.microsoft.com/v1.0/sites
```

3. **Check SharePoint Site Permissions**
```bash
# List site permissions
curl -H "Authorization: Bearer <graph-token>" \
  https://graph.microsoft.com/v1.0/sites/<site-id>/permissions
```

4. **Verify External Sharing Enabled**
```powershell
# Connect to SharePoint Online
Connect-SPOService -Url https://tenant-admin.sharepoint.com

# Check tenant settings
Get-SPOTenant | Select-Object SharingCapability

# Check site settings
Get-SPOSite -Identity https://tenant.sharepoint.com/sites/ClientSite | Select-Object SharingCapability
```

---

### Procedure 3: Troubleshoot Performance Issues

#### Steps

1. **Check Application Insights Performance**
```kusto
// Slow API calls
requests
| where timestamp > ago(1h)
| where duration > 5000 // > 5 seconds
| project timestamp, name, url, duration, resultCode
| order by duration desc
| take 20
```

2. **Check Database Performance**
```bash
# Query database DTU usage
az sql db show-usage \
  --resource-group <resource-group> \
  --server <sql-server> \
  --name <database-name>
```

3. **Check App Service Metrics**
```bash
# CPU usage
az monitor metrics list \
  --resource "/subscriptions/<sub-id>/resourceGroups/<rg>/providers/Microsoft.Web/sites/<app-name>" \
  --metric "CpuPercentage" \
  --interval PT1M

# Memory usage
az monitor metrics list \
  --resource "/subscriptions/<sub-id>/resourceGroups/<rg>/providers/Microsoft.Web/sites/<app-name>" \
  --metric "MemoryPercentage" \
  --interval PT1M
```

4. **Identify Bottlenecks**
```kusto
// Dependency calls (DB, external APIs)
dependencies
| where timestamp > ago(1h)
| where duration > 1000
| summarize avg(duration), count() by name
| order by avg_duration desc
```

---

## Log Analysis

### Application Insights Queries

#### Query 1: Find Errors in Last Hour

```kusto
exceptions
| where timestamp > ago(1h)
| project timestamp, type, outerMessage, innermostMessage, customDimensions
| order by timestamp desc
```

#### Query 2: User Activity

```kusto
requests
| where timestamp > ago(24h)
| where customDimensions.userId == "<user-id>"
| project timestamp, url, resultCode, duration
| order by timestamp desc
```

#### Query 3: API Performance by Endpoint

```kusto
requests
| where timestamp > ago(24h)
| summarize 
    count=count(), 
    avgDuration=avg(duration), 
    p95Duration=percentile(duration, 95)
  by url
| order by count desc
```

#### Query 4: Failed Requests

```kusto
requests
| where timestamp > ago(1h)
| where success == false
| project timestamp, url, resultCode, customDimensions
| order by timestamp desc
```

#### Query 5: Trace Specific Transaction

```kusto
union traces, requests, dependencies, exceptions
| where operation_Id == "<operation-id>"
| order by timestamp asc
| project timestamp, itemType, message, name, duration
```

### Log Locations

| Component | Log Location | Access Method |
|-----------|--------------|---------------|
| **API** | Application Insights | Azure Portal or Azure CLI |
| **Portal** | Application Insights | Azure Portal or Azure CLI |
| **App Service** | Log Stream | Azure Portal → App Service → Log Stream |
| **Database** | Query Performance Insight | Azure Portal → SQL Database → Performance |
| **Azure AD** | Sign-in Logs | Azure Portal → Azure AD → Sign-in logs |

---

## Performance Troubleshooting

### Symptom: Slow API Response Times

#### Investigation

1. **Check API Performance Metrics**
```kusto
requests
| where timestamp > ago(1h)
| where url contains "/api/"
| summarize 
    avg(duration), 
    percentile(duration, 95), 
    percentile(duration, 99) 
  by bin(timestamp, 5m)
| render timechart
```

2. **Identify Slow Endpoints**
```kusto
requests
| where timestamp > ago(1h)
| where duration > 2000
| summarize count() by url
| order by count_ desc
```

3. **Check Dependencies**
```kusto
dependencies
| where timestamp > ago(1h)
| summarize avg(duration), count() by name, type
| order by avg_duration desc
```

#### Solutions

**Solution 1: Database Query Optimization**
- Identify slow queries
- Add database indexes
- Optimize EF Core queries
- Enable query caching

**Solution 2: Scale Up App Service**
```bash
az appservice plan update \
  --name <plan-name> \
  --resource-group <resource-group> \
  --sku S2
```

**Solution 3: Enable Caching**
- Implement Redis cache for frequently accessed data
- Configure HTTP caching headers
- Use CDN for static assets

---

### Symptom: High Database DTU Usage

#### Investigation

1. **Query DTU Metrics**
```bash
az monitor metrics list \
  --resource "<database-resource-id>" \
  --metric "dtu_consumption_percent" \
  --interval PT1M \
  --start-time 2024-02-19T00:00:00Z \
  --end-time 2024-02-19T23:59:59Z
```

2. **Identify Expensive Queries**
- Azure Portal → SQL Database → Query Performance Insight
- Look for queries with high CPU, duration, or execution count

3. **Check Missing Indexes**
- Azure Portal → SQL Database → Performance recommendations
- Review index recommendations

#### Solutions

**Solution 1: Add Indexes**
```sql
-- Example: Add index on frequently queried column
CREATE NONCLUSTERED INDEX IX_ExternalUsers_ClientId 
ON ExternalUsers (ClientId) 
INCLUDE (Email, Status, CreatedAt);
```

**Solution 2: Scale Up Database**
```bash
az sql db update \
  --resource-group <resource-group> \
  --server <sql-server> \
  --name <database-name> \
  --service-objective S3
```

**Solution 3: Optimize Queries**
- Add `AsNoTracking()` for read-only queries
- Use pagination for large result sets
- Implement connection pooling

---

## Security Incident Response

### Incident 1: Unauthorized Access Attempt

#### Detection
- Failed login attempts spike
- Login from unusual location
- Azure AD Identity Protection alert

#### Response Steps

1. **Verify the Alert**
```bash
# Check sign-in logs
az ad signin list \
  --filter "userPrincipalName eq '<user-email>'" \
  --query "[].{timestamp:createdDateTime, status:status, location:location}" \
  --output table
```

2. **Lock the Account** (if compromised)
```bash
# Revoke user sessions
az ad user update \
  --id <user-id> \
  --account-enabled false
```

3. **Notify User**
- Email user about suspicious activity
- Request password reset
- Enable MFA if not already enabled

4. **Audit Activity**
```kusto
// Check user's recent API activity
requests
| where timestamp > ago(7d)
| where customDimensions.userId == "<user-id>"
| project timestamp, url, clientIp, resultCode
| order by timestamp desc
```

5. **Document Incident**
- Log details in incident management system
- Record timeline of events
- Note remediation actions

---

### Incident 2: Data Breach Suspected

#### Detection
- Large data export
- Unusual API activity
- External alert from security tool

#### Response Steps

1. **Immediate Actions**
```bash
# Revoke API access
curl -X POST \
  -H "Authorization: Bearer <admin-token>" \
  https://api.clientspace.app/api/v1/admin/tenants/<tenant-id>/suspend

# Rotate all secrets
az keyvault secret set \
  --vault-name <vault-name> \
  --name AzureAd--ClientSecret \
  --value "<new-secret>"
```

2. **Investigate Scope**
```kusto
// Large data exports
requests
| where timestamp > ago(7d)
| where url contains "export"
| project timestamp, userId = customDimensions.userId, url, customDimensions
| order by timestamp desc
```

3. **Notify Stakeholders**
- Inform affected customers
- Notify legal team
- File regulatory reports if required (GDPR, etc.)

4. **Forensic Analysis**
- Preserve logs for analysis
- Engage security consultant if needed
- Document all findings

5. **Remediation**
- Patch vulnerability
- Enhance monitoring
- Update security procedures
- Conduct training

---

## Data Recovery

### Scenario 1: Accidental Client Deletion

#### Recovery Steps

1. **Check Soft Delete**
```bash
# List deleted clients (if soft delete enabled)
curl -H "Authorization: Bearer <admin-token>" \
  https://api.clientspace.app/api/v1/admin/clients/deleted
```

2. **Restore from Backup**
```bash
# List database backups
az sql db restore --help

# Restore to point in time
az sql db restore \
  --resource-group <resource-group> \
  --server <sql-server> \
  --name <database-name> \
  --dest-name <database-name>-restored \
  --time "2024-02-19T10:00:00Z"
```

3. **Manual Data Entry** (last resort)
- If backup unavailable
- Recreate client space
- Reinvite external users
- Provide credit to customer

---

### Scenario 2: Database Corruption

#### Recovery Steps

1. **Assess Damage**
```sql
-- Check for corruption
DBCC CHECKDB('<database-name>') WITH NO_INFOMSGS;
```

2. **Restore from Backup**
```bash
# Restore latest backup
az sql db restore \
  --resource-group <resource-group> \
  --server <sql-server> \
  --name <database-name> \
  --dest-name <database-name>-recovered \
  --time "<latest-backup-time>"
```

3. **Data Reconciliation**
- Compare restored data with live data
- Identify missing records
- Replay transactions if possible

---

## Escalation Procedures

### When to Escalate

| Issue Type | Escalate When | To Whom |
|------------|---------------|---------|
| **Authentication** | Can't resolve in 30 mins | L3 Engineering |
| **Performance** | Service degraded > 1 hour | L3 Engineering |
| **Data Loss** | Any data loss | L4 Senior Engineering |
| **Security** | Suspected breach | L4 + Security Team |
| **Outage** | Service unavailable > 15 mins | L4 + Management |

### Escalation Template

```
Subject: [ESCALATION] <Issue Summary>

Priority: P1 / P2 / P3 / P4
Affected Tenant: <tenant-name>
Issue Started: <timestamp>

Description:
<Brief description of the issue>

Steps Taken:
1. <action taken>
2. <action taken>

Current Status:
<current state of the issue>

Impact:
- Users affected: <number>
- Features impacted: <list>
- Business impact: <description>

Requestor: <your name>
Contact: <your email/phone>
```

---

## Appendix

### Useful Commands

#### Check Service Health
```bash
# API health
curl https://api.clientspace.app/health

# Portal health
curl https://portal.clientspace.app/

# Database connection
az sql db show --resource-group <rg> --server <server> --name <db>
```

#### Restart Services
```bash
# Restart API
az webapp restart --name <api-app> --resource-group <rg>

# Restart Portal
az webapp restart --name <portal-app> --resource-group <rg>
```

#### View Logs
```bash
# Tail API logs
az webapp log tail --name <api-app> --resource-group <rg>

# Download logs
az webapp log download --name <api-app> --resource-group <rg>
```

### Contact Information

- **L1 Support**: support@clientspace.com
- **L2 Technical Support**: techsupport@clientspace.com
- **L3 Engineering**: engineering@clientspace.com
- **L4 Emergency**: emergency@clientspace.com (PagerDuty)
- **Security Team**: security@clientspace.com

### Related Documentation

- **[Deployment Runbook](MVP_DEPLOYMENT_RUNBOOK.md)**: Deployment procedures
- **[API Reference](MVP_API_REFERENCE.md)**: API documentation
- **[User Guide](USER_GUIDE.md)**: Feature documentation
- **[Quick Start](MVP_QUICK_START.md)**: Getting started guide

---

*Last Updated: February 2026*  
*Version: MVP 1.0*  
*Support Version: 1.0*
