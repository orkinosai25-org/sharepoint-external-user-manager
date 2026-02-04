# SharePoint Site Provisioning Implementation

## Overview

This document describes the automatic SharePoint site provisioning feature implemented for client spaces. When a solicitor creates a new client record, a SharePoint site is automatically provisioned in the background.

## Feature Details

### Endpoint

**POST** `/api/clients`

### Request Body

```json
{
  "clientName": "Acme Corporation",
  "siteTemplate": "Team"  // Optional: "Team" or "Communication", defaults to "Team"
}
```

### Response

```json
{
  "success": true,
  "data": {
    "id": 123,
    "tenantId": 1,
    "clientName": "Acme Corporation",
    "siteUrl": "",  // Empty initially, populated after provisioning
    "siteId": "",   // Empty initially, populated after provisioning
    "createdBy": "admin@contoso.com",
    "createdAt": "2024-01-15T10:30:00Z",
    "status": "Provisioning"  // Will change to "Active" or "Error"
  }
}
```

## Provisioning Flow

1. **Client Record Creation**
   - API receives POST request with client name and optional site template
   - Client record is immediately created in database with status="Provisioning"
   - Empty siteUrl and siteId (populated after provisioning completes)
   - API returns immediately with the client record

2. **Async Site Provisioning**
   - Background process initiates SharePoint site creation via Microsoft Graph API
   - For "Team" sites: Creates Microsoft 365 Group (includes SharePoint site)
   - For "Communication" sites: Creates standalone SharePoint site
   - Process updates client record upon completion

3. **Status Updates**
   - **Provisioning**: Site creation is in progress
   - **Active**: Site successfully created, siteUrl and siteId populated
   - **Error**: Site creation failed, errorMessage field contains details

## Status Tracking

### Polling for Status

After creating a client, poll the GET endpoint to check provisioning status:

**GET** `/api/clients/{clientId}`

```json
{
  "success": true,
  "data": {
    "id": 123,
    "status": "Active",  // or "Provisioning" or "Error"
    "siteUrl": "https://contoso.sharepoint.com/sites/acme-corporation",
    "siteId": "mock-site-1234567890",
    "errorMessage": null  // Contains error details if status="Error"
  }
}
```

## Site Templates

### Team Site
- Creates a Microsoft 365 Group
- Includes SharePoint site, Teams, Outlook group
- Private by default
- Best for internal collaboration with external guests

### Communication Site
- Creates standalone SharePoint site
- No associated Microsoft 365 Group
- Better for publishing and broadcasting content
- Lighter weight than Team sites

## Configuration

### Environment Variables

- `ENABLE_GRAPH_INTEGRATION`: Set to "true" to enable actual Graph API calls
  - If not set or "false": Uses mock mode (returns mock site IDs/URLs)
- `AZURE_TENANT_ID`: Azure AD tenant ID
- `AZURE_CLIENT_ID`: App registration client ID
- `AZURE_CLIENT_SECRET`: App registration client secret

### Required Permissions

The Azure AD app registration requires the following Microsoft Graph API permissions:

- `Sites.FullControl.All` (Application): Create and manage SharePoint sites
- `Group.ReadWrite.All` (Application): Create Microsoft 365 Groups for Team sites
- `Directory.ReadWrite.All` (Application): Access directory information

## Error Handling

### Common Errors

1. **Site Name Already Exists**
   - Status: "Error"
   - ErrorMessage: "A site with this alias already exists"
   - Resolution: Client name must be unique

2. **Insufficient Permissions**
   - Status: "Error"
   - ErrorMessage: "Access denied: Insufficient permissions"
   - Resolution: Verify Graph API permissions

3. **Graph API Unavailable**
   - Status: "Error"
   - ErrorMessage: "Microsoft Graph API unavailable"
   - Resolution: Retry or check service health

### Error Message Storage

- All error messages stored in `ErrorMessage` column (NVARCHAR(MAX))
- Includes full error details from Graph API for troubleshooting
- Visible in UI for admin users

## Audit Logging

All provisioning activities are logged to audit logs:

- **ClientCreated**: When client record is created
- **SiteProvisioned**: When site is successfully created
- **SiteProvisioningFailed**: When site creation fails

## Production Deployment Notes

### Current MVP Implementation

The current implementation uses `setTimeout` for async processing, which is **not production-ready**. Azure Functions may terminate the execution context before provisioning completes.

### Production Recommendations

1. **Use Durable Functions**
   ```typescript
   // Replace setTimeout with Durable Functions orchestration
   const client = df.getClient(context);
   const instanceId = await client.startNew('ProvisionSiteOrchestrator', undefined, {
     clientId: client.id,
     clientName: validatedRequest.clientName,
     siteTemplate: validatedRequest.siteTemplate
   });
   ```

2. **Use Azure Queue Storage**
   ```typescript
   // Send message to queue for background processing
   await queueClient.sendMessage(JSON.stringify({
     clientId: client.id,
     clientName: validatedRequest.clientName,
     siteTemplate: validatedRequest.siteTemplate
   }));
   ```

3. **Use Azure Service Bus**
   - More reliable than Queue Storage
   - Supports dead-letter queues for failed provisioning
   - Better retry policies

4. **Implement Proper Polling**
   - SharePoint provisioning can take 30+ seconds
   - Use exponential backoff for polling
   - Consider webhooks for completion notifications

### Graph API Improvements

The current Graph API implementation is simplified. Production should:

1. Use proper site creation endpoints with full configuration
2. Apply site templates after creation
3. Set proper permissions and settings
4. Handle partial failures gracefully

## Database Schema

### Client Table

```sql
CREATE TABLE [dbo].[Client] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [TenantId] INT NOT NULL,
    [ClientName] NVARCHAR(255) NOT NULL,
    [SiteUrl] NVARCHAR(500) NULL,        -- Populated after provisioning
    [SiteId] NVARCHAR(100) NULL,         -- Populated after provisioning
    [CreatedBy] NVARCHAR(255) NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [Status] NVARCHAR(50) NOT NULL DEFAULT 'Provisioning',
    [ErrorMessage] NVARCHAR(MAX) NULL,   -- Added in migration 003
    CONSTRAINT [PK_Client] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Client_Tenant] FOREIGN KEY ([TenantId]) 
        REFERENCES [dbo].[Tenant] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [CHK_Client_Status] CHECK ([Status] IN ('Provisioning', 'Active', 'Error'))
);
```

### Migration

Run migration `003_client_table_error_tracking.sql` to add ErrorMessage column to existing tables.

## Testing

### Mock Mode Testing

With `ENABLE_GRAPH_INTEGRATION` not set or set to "false":

```bash
curl -X POST http://localhost:7071/api/clients \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "clientName": "Test Client",
    "siteTemplate": "Team"
  }'
```

Response includes mock siteId and siteUrl after provisioning completes.

### Integration Testing

With `ENABLE_GRAPH_INTEGRATION=true`:

1. Ensure app registration has required Graph API permissions
2. Configure authentication environment variables
3. Create a test client
4. Poll for status until "Active" or "Error"
5. Verify site exists in SharePoint admin center
6. Clean up test sites after testing

## Monitoring

### Application Insights Queries

Monitor provisioning success rate:

```kusto
traces
| where customDimensions.Action == "SiteProvisioned" or customDimensions.Action == "SiteProvisioningFailed"
| summarize Successes = countif(customDimensions.Action == "SiteProvisioned"),
            Failures = countif(customDimensions.Action == "SiteProvisioningFailed")
            by bin(timestamp, 1h)
```

### Alerts

Set up alerts for:
- High provisioning failure rate (>5% in 1 hour)
- Provisioning taking longer than expected (>5 minutes)
- Graph API errors

## Security Considerations

1. **Client Secret Protection**
   - Store in Azure Key Vault
   - Rotate regularly (every 6 months)
   - Never log or expose in responses

2. **Permission Scoping**
   - Use least-privilege permissions
   - Consider delegated permissions for user-initiated actions

3. **Audit Trail**
   - All provisioning attempts logged
   - Includes user, timestamp, and outcome
   - Immutable audit log in database

4. **Rate Limiting**
   - Implement per-tenant rate limits
   - Prevent provisioning abuse
   - Queue excess requests

## Future Enhancements

1. **Site Templates**
   - Support custom site templates
   - Pre-configure libraries and lists
   - Apply branding and themes

2. **Webhooks**
   - Subscribe to SharePoint events
   - Real-time status updates
   - Eliminate polling

3. **Batch Provisioning**
   - Bulk client creation with CSV import
   - Queue-based processing
   - Progress tracking

4. **Site Configuration**
   - Custom permission levels
   - Pre-configured document libraries
   - Default site settings

5. **Rollback Support**
   - Delete site if provisioning fails
   - Clean up partial configurations
   - Retry with error recovery
